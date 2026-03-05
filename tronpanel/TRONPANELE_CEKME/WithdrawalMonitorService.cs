using System.Globalization;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TRONPANELE_CEKME.Models;

namespace TRONPANELE_CEKME.Services
{
    public interface IWithdrawalMonitorService
    {
        Task StartMonitoringAsync(CancellationToken cancellationToken);
    }

    public class WithdrawalMonitorService : IWithdrawalMonitorService
    {
        private readonly IHttpClientService _httpClient;
        private readonly ILogger<WithdrawalMonitorService> _logger;
        private readonly WithdrawalSettings _settings;
        private decimal _totalProcessedAmount = 0;
        private int _totalProcessedCount = 0;
        
        // Track seen IDs to avoid duplicate logging (cleared when data changes)
        private readonly Dictionary<long, int> _lastSeenItems = new(); 

        public WithdrawalMonitorService(
            IHttpClientService httpClient,
            ILogger<WithdrawalMonitorService> logger,
            IOptions<AppSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value.Withdrawals;
        }

        private string? _csrfToken;

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 Çekim izleme başlatıldı. (PreviewMode: {PreviewMode})", _settings.PreviewMode);
            _logger.LogInformation("📊 Limitler: Min Tutar: {Min}, Max Tutar: {Max}, Toplam Max Tutar: {TotalMax}, Max Kayıt: {MaxCount}",
                _settings.MinAmount, _settings.MaxAmount, _settings.MaxTotalAmount, _settings.MaxRecordCount);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 1. First, ensure we have a CSRF token by fetching the page
                    var pageHtml = await _httpClient.GetAsync(_settings.PageUrl);
                    _csrfToken = ExtractTokenFromHtml(pageHtml);

                    if (string.IsNullOrEmpty(_csrfToken))
                    {
                        _logger.LogWarning("⚠️ CSRF token alınamadı. Oturum kapanmış olabilir.");
                        await Task.Delay(10000, cancellationToken);
                        continue;
                    }

                    // 2. Get withdrawal list via POST AJAX with the token
                    var postData = new Dictionary<string, string> { ["_token"] = _csrfToken };
                    var jsonResponse = await _httpClient.PostAjaxAsync(_settings.AjaxUrl, postData);
                    
                    if (string.IsNullOrWhiteSpace(jsonResponse) || jsonResponse.Trim().StartsWith("<"))
                    {
                        _logger.LogWarning("⚠️ JSON beklenirken HTML veya boş yanıt alındı.");
                        await Task.Delay(15000, cancellationToken);
                        continue;
                    }

                    var response = JsonConvert.DeserializeObject<WithdrawalListResponse>(jsonResponse);

                    if (response == null || !response.Status)
                    {
                        _logger.LogWarning("⚠️ Veri çekilemedi veya geçersiz yanıt: {Message}", response?.Message ?? "Boş yanıt");
                        await Task.Delay(_settings.PollingIntervalMs, cancellationToken);
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
                        if (cancellationToken.IsCancellationRequested) break;

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
                            return;
                        }

                        if (_totalProcessedAmount + amount > _settings.MaxTotalAmount)
                        {
                            _logger.LogWarning("⛔ Bu işlemle ({Id} - {Amount:N2}) birlikte maksimum tutar ({MaxTotal}) aşılacaktır. Uygulama sonlandırılıyor.", data.Id, amount, _settings.MaxTotalAmount);
                            return;
                        }

                        // Requirement 8: "maksimum çekilen kayıt sayısına ulaşıldığında veya minimum tutar kadar daha işleme alınırsa maksimum tutarı geçecekse sonlanmalıdır."
                        if (_totalProcessedAmount + _settings.MinAmount > _settings.MaxTotalAmount)
                        {
                            _logger.LogWarning("⛔ Bir sonraki minimum işlem tutarı ({MinAmount}) ile maksimum tutar ({MaxTotal}) aşılacaktır. Uygulama sonlandırılıyor.", _settings.MinAmount, _settings.MaxTotalAmount);
                            return;
                        }

                        await ProcessDataAsync(data, amount);
                    }

                    // Clean up _lastSeenItems to keep memory usage low (optional, but good practice)
                    if (_lastSeenItems.Count > 1000) _lastSeenItems.Clear();

                    await Task.Delay(_settings.PollingIntervalMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "İzleme döngüsü sırasında hata oluştu");
                    await Task.Delay(5000, cancellationToken);
                }
            }
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
