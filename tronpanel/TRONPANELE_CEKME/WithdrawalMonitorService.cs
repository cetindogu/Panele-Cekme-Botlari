using System.Globalization;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using TRONPANELE_CEKME.Models;

namespace TRONPANELE_CEKME.Services
{
    public interface IWithdrawalMonitorService
    {
        Task StartMonitoringAsync(CancellationToken cancellationToken);
    }

    public class WithdrawalMonitorService : BackgroundService, IWithdrawalMonitorService
    {
        private readonly IHttpClientService _httpClient;
        private readonly ILogger<WithdrawalMonitorService> _logger;
        private readonly ILoginService _loginService;
        private readonly WithdrawalSettings _settings;
        private readonly IHostApplicationLifetime _lifetime;
        private decimal _totalProcessedAmount = 0;
        private int _totalProcessedCount = 0;
        
        // Track seen IDs to avoid duplicate logging (cleared when data changes)
        private readonly Dictionary<long, int> _lastSeenItems = new(); 

        public WithdrawalMonitorService(
            IHttpClientService httpClient,
            ILogger<WithdrawalMonitorService> logger,
            IOptions<AppSettings> settings,
            ILoginService loginService,
            IHostApplicationLifetime lifetime)
        {
            _httpClient = httpClient;
            _logger = logger;
            _loginService = loginService;
            _settings = settings.Value.Withdrawals;
            _lifetime = lifetime;
        }

        private string? _csrfToken;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Çekim izleme başlatıldı. (PreviewMode: {PreviewMode})", _settings.PreviewMode);
            _logger.LogInformation("📊 Limitler: Min Tutar: {Min}, Max Tutar: {Max}, Toplam Max Tutar: {TotalMax}, Max Kayıt: {MaxCount}",
                _settings.MinAmount, _settings.MaxAmount, _settings.MaxTotalAmount, _settings.MaxRecordCount);

            // Perform Login once at start
            if (!await _loginService.LoginAsync())
            {
                _logger.LogCritical("❌ Giriş yapılamadı! Uygulama sonlandırılıyor.");
                _lifetime.StopApplication();
                return;
            }

            _logger.LogInformation("✅ Giriş başarılı. İzleme başlatılıyor...");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Check limits BEFORE starting a new poll
                if (_totalProcessedCount >= _settings.MaxRecordCount)
                {
                    _logger.LogWarning("⛔ Maksimum kayıt sayısına ({MaxCount}) ulaşıldı. Uygulama sonlandırılıyor.", _settings.MaxRecordCount);
                    _lifetime.StopApplication();
                    break;
                }

                if (_totalProcessedAmount + _settings.MinAmount > _settings.MaxTotalAmount)
                {
                    _logger.LogWarning("⛔ Bir sonraki minimum işlem tutarı ({MinAmount}) ile maksimum tutar ({MaxTotal}) aşılacaktır. Uygulama sonlandırılıyor.", _settings.MinAmount, _settings.MaxTotalAmount);
                    _lifetime.StopApplication();
                    break;
                }

                try
                {
                    // 1. First, ensure we have a CSRF token by fetching the page
                    var pageHtml = await _httpClient.GetAsync(_settings.PageUrl);
                    _csrfToken = ExtractTokenFromHtml(pageHtml);

                    if (string.IsNullOrEmpty(_csrfToken))
                    {
                        _logger.LogWarning("⚠️ CSRF token alınamadı. Oturum kapanmış olabilir.");
                        await Task.Delay(10000, stoppingToken);
                        continue;
                    }

                    // 2. Get withdrawal list via POST AJAX with the token
                    var postData = new Dictionary<string, string> { ["_token"] = _csrfToken };
                    var jsonResponse = await _httpClient.PostAjaxAsync(_settings.AjaxUrl, postData);
                    
                    if (string.IsNullOrWhiteSpace(jsonResponse) || jsonResponse.Trim().StartsWith("<"))
                    {
                        _logger.LogWarning("⚠️ JSON beklenirken HTML veya boş yanıt alındı.");
                        await Task.Delay(15000, stoppingToken);
                        continue;
                    }

                    var response = JsonConvert.DeserializeObject<WithdrawalListResponse>(jsonResponse);

                    if (response == null || !response.Status)
                    {
                        _logger.LogWarning("⚠️ Veri çekilemedi veya geçersiz yanıt: {Message}", response?.Message ?? "Boş yanıt");
                        await Task.Delay(_settings.PollingIntervalMs, stoppingToken);
                        continue;
                    }

                    var allItems = response.Datas ?? new List<WithdrawalData>();
                    
                    // Filter candidates based on criteria
                    var candidates = allItems.Where(d => 
                        d.Proc == 0 && // 0: Beklemede
                        decimal.TryParse(d.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt) &&
                        amt >= _settings.MinAmount && 
                        amt <= _settings.MaxAmount
                    ).ToList();

                    _logger.LogInformation("🔍 Toplam: {Total} kayıt | Kriterlere uyan: {CandidateCount} | Toplam İşlenen: {TotalAmount:N2} ({TotalCount} adet)", 
                        allItems.Count, candidates.Count, _totalProcessedAmount, _totalProcessedCount);

                    foreach (var data in candidates)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        decimal.TryParse(data.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);

                        // Check if we've already logged/processed this ID
                        if (_lastSeenItems.ContainsKey(data.Id))
                        {
                            continue; // Skip logging/processing duplicate
                        }
                        
                        _lastSeenItems[data.Id] = data.Proc;

                        _logger.LogInformation("✨ Yeni aday bulundu: ID: {Id}, Kullanıcı: {User}, Tutar: {Amount:N2}, Site: {Site}", 
                            data.Id, data.Userid, amount, data.Site);

                        // Check global limits
                        if (_totalProcessedCount >= _settings.MaxRecordCount)
                        {
                            _logger.LogWarning("⛔ Maksimum kayıt sayısına ({MaxCount}) ulaşıldı. Uygulama sonlandırılıyor.", _settings.MaxRecordCount);
                            _lifetime.StopApplication();
                            return;
                        }

                        if (_totalProcessedAmount + amount > _settings.MaxTotalAmount)
                        {
                            _logger.LogWarning("⚠️ Bu işlem ({Id} - {Amount:N2}) toplam limiti ({MaxTotal}) aşıyor. Atlanıyor, daha küçük tutarlı kayıtlar aranacak.", data.Id, amount, _settings.MaxTotalAmount);
                            continue; // Skip this one, try others
                        }

                        await ProcessDataAsync(data, amount);
                    }

                    // Requirement: "eğer minimum tutar kadar işlem yapıldığında toplamı geçiyorsa sonlanmalı."
                    if (_totalProcessedAmount + _settings.MinAmount > _settings.MaxTotalAmount)
                    {
                        _logger.LogWarning("⛔ Mevcut toplam ({TotalAmount:N2}) üzerine eklenebilecek minimum tutar ({MinAmount}) bile maksimum limiti ({MaxTotal}) aşıyor. Uygulama sonlandırılıyor.", _totalProcessedAmount, _settings.MinAmount, _settings.MaxTotalAmount);
                        _lifetime.StopApplication();
                        return;
                    }

                    // Clean up _lastSeenItems to keep memory usage low (optional, but good practice)
                    if (_lastSeenItems.Count > 1000) _lastSeenItems.Clear();

                    await Task.Delay(_settings.PollingIntervalMs, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "İzleme döngüsü sırasında hata oluştu");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        public Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            // This is now handled by ExecuteAsync via BackgroundService
            return Task.CompletedTask;
        }

        private async Task ProcessDataAsync(WithdrawalData data, decimal amount)
        {
            if (_settings.PreviewMode)
            {
                // Already logged in loop
                return;
            }

            try
            {
                _logger.LogInformation("⚙️ İşleme alınıyor: ID: {Id}, Tutar: {Amount:N2}...", data.Id, amount);

                var postData = new Dictionary<string, string> 
                { 
                    ["data-id"] = data.Id.ToString(),
                    ["_token"] = _csrfToken ?? ""
                };
                var responseJson = await _httpClient.PostAjaxAsync(_settings.ProcessUrl, postData);
                var response = JsonConvert.DeserializeObject<ProcessResponse>(responseJson);

                if (response != null && response.Status)
                {
                    _logger.LogInformation("✅ Başarıyla işleme alındı! ID: {Id}, Yanıt: {Message}", data.Id, response.Message);
                    _totalProcessedAmount += amount;
                    _totalProcessedCount++;
                    _lastSeenItems[data.Id] = 1; // Mark as processed to avoid re-processing if still in response
                }
                else
                {
                    _logger.LogError("❌ İşleme alınamadı! ID: {Id}, Yanıt: {Message}", data.Id, response?.Message ?? "Boş yanıt");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt işleme sırasında hata oluştu: ID: {Id}", data.Id);
            }
        }

        private string? ExtractTokenFromHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            // Look for name="_token" or name="token" in meta or input
            var tokenInput = doc.DocumentNode.SelectSingleNode("//input[@name='_token']") ?? 
                             doc.DocumentNode.SelectSingleNode("//input[@name='token']") ??
                             doc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']");
            
            if (tokenInput?.Name == "meta")
                return tokenInput.GetAttributeValue("content", string.Empty);
            
            return tokenInput?.GetAttributeValue("value", string.Empty);
        }
    }
}
