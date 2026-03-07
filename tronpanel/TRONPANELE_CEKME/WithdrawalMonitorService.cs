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
        // Track successfully processed IDs to avoid re-processing in the same session
        private readonly HashSet<long> _processedIds = new();
        // Track currently active POST requests to avoid spamming the same ID
        private readonly HashSet<long> _activeProcessingIds = new();
        // Track last error message per ID to avoid duplicate log spam
        private readonly Dictionary<long, string> _lastErrorMessages = new();

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

            // Start 3 parallel workers with staggering
            int workerCount = 3;
            var workers = new List<Task>();
            for (int i = 0; i < workerCount; i++)
            {
                workers.Add(RunMonitorLoopAsync(i, stoppingToken));
                await Task.Delay(300, stoppingToken); // 300ms staggered start
            }

            await Task.WhenAll(workers);
        }

        private decimal ParseAmount(string amountStr)
        {
            if (string.IsNullOrWhiteSpace(amountStr)) return 0;

            // Sadece rakam ve nokta (.) tutulur, virgül (,) kullanılmamaktadır.
            var cleaned = new string(amountStr.Where(c => char.IsDigit(c) || c == '.').ToArray());

            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return 0;
        }

        private async Task RunMonitorLoopAsync(int workerId, CancellationToken stoppingToken)
        {
            _logger.LogInformation("👷 Worker-{WorkerId} başlatıldı.", workerId);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Check limits BEFORE starting a new poll
                if (_totalProcessedCount >= _settings.MaxRecordCount)
                {
                    _logger.LogWarning("⛔ Maksimum kayıt sayısına ({MaxCount}) ulaşıldı. Uygulama sonlandırılıyor.", _settings.MaxRecordCount);
                    _lifetime.StopApplication();
                    break;
                }

                try
                {
                    // 1. Ensure we have a CSRF token
                    if (string.IsNullOrEmpty(_csrfToken))
                    {
                        // Staggered race to get token if missing
                        _logger.LogInformation("🔑 Worker-{WorkerId}: CSRF token yenileniyor...", workerId);
                        var pageHtml = await _httpClient.GetAsync(_settings.PageUrl);
                        _csrfToken = ExtractTokenFromHtml(pageHtml);

                        if (string.IsNullOrEmpty(_csrfToken))
                        {
                            await Task.Delay(5000, stoppingToken);
                            continue;
                        }
                    }

                    // 2. Get withdrawal list via POST AJAX with the token
                    var postData = new Dictionary<string, string> { ["_token"] = _csrfToken };
                    var jsonResponse = await _httpClient.PostAjaxAsync(_settings.AjaxUrl, postData);
                    
                    if (string.IsNullOrWhiteSpace(jsonResponse) || jsonResponse.Trim().StartsWith("<"))
                    {
                        _csrfToken = null; // Force refresh
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    var response = JsonConvert.DeserializeObject<WithdrawalListResponse>(jsonResponse);

                    if (response == null || !response.Status)
                    {
                        if (response?.Message?.Contains("CSRF", StringComparison.OrdinalIgnoreCase) == true) _csrfToken = null;
                        await Task.Delay(_settings.PollingIntervalMs, stoppingToken);
                        continue;
                    }

                    var allItems = response.Datas ?? new List<WithdrawalData>();

                    var candidates = allItems.Where(d => 
                    {
                        if (d.Proc != 0) return false;
                        
                        // Kendi başarıyla işlediğimiz kayıtları tekrar denemeyelim
                        lock (_processedIds)
                        {
                            if (_processedIds.Contains(d.Id)) return false;
                        }

                        try 
                        {
                            var amt = ParseAmount(d.Amount);
                            return amt >= _settings.MinAmount && amt <= _settings.MaxAmount;
                        }
                        catch { return false; }
                    }).ToList();

                    if (candidates.Any())
                    {
                        // Process all candidates in parallel without waiting for them to finish before next poll
                        foreach (var data in candidates)
                        {
                            if (stoppingToken.IsCancellationRequested) break;

                            decimal amount = 0;
                            try { amount = ParseAmount(data.Amount); } catch { continue; }

                            // LOGLAMA MANTIĞI: Sadece yeni görülen veya durumu değişen adayları logla
                            bool shouldLog = false;
                            lock (_lastSeenItems)
                            {
                                if (!_lastSeenItems.ContainsKey(data.Id) || _lastSeenItems[data.Id] != data.Proc)
                                {
                                    _lastSeenItems[data.Id] = data.Proc;
                                    shouldLog = true;
                                }
                            }

                            if (shouldLog)
                            {
                                _logger.LogInformation("✨ Worker-{WorkerId}: Aday ID: {Id}, Tutar: {Amount:N2}", workerId, data.Id, amount);
                            }

                            if (_totalProcessedCount >= _settings.MaxRecordCount) continue;
                            if (_totalProcessedAmount + amount > _settings.MaxTotalAmount) continue;

                            // İŞLEME MANTIĞI: Bu ID için hali hazırda devam eden bir istek (POST) varsa yeni istek başlatma
                            lock (_activeProcessingIds)
                            {
                                if (_activeProcessingIds.Contains(data.Id)) continue;
                                _activeProcessingIds.Add(data.Id);
                            }

                            // Fire and forget (don't await) to continue polling immediately
                            _ = ProcessDataAsync(data, amount);
                        }
                    }

                    // Global limit check
                    if (_totalProcessedAmount + _settings.MinAmount > _settings.MaxTotalAmount)
                    {
                        _logger.LogWarning("⛔ Limit aşıldı. Uygulama sonlandırılıyor.");
                        _lifetime.StopApplication();
                        return;
                    }

                    await Task.Delay(_settings.PollingIntervalMs, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError("⚠️ Worker-{WorkerId} Hatası: {Message}", workerId, ex.Message);
                    // Hata durumunda 1 saniye bekle (Spam koruması ve sunucuya nefes aldırma)
                    await Task.Delay(1000, stoppingToken);
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
            if (_settings.PreviewMode) return;

            try
            {
                // Minimize logging in the critical path
                var postData = new Dictionary<string, string> 
                { 
                    ["data-id"] = data.Id.ToString(),
                    ["_token"] = _csrfToken ?? ""
                };
                
                var responseJson = await _httpClient.PostAjaxAsync(_settings.ProcessUrl, postData);
                var response = JsonConvert.DeserializeObject<ProcessResponse>(responseJson);

                if (response != null && response.Status)
                {
                    _logger.LogInformation("✅ ONAYLANDI! ID: {Id}, Tutar: {Amount:N2}", data.Id, amount);
                    _totalProcessedAmount += amount;
                    _totalProcessedCount++;
                    lock (_processedIds) { _processedIds.Add(data.Id); }
                    lock (_lastSeenItems) { _lastSeenItems[data.Id] = 1; }
                    lock (_lastErrorMessages) { _lastErrorMessages.Remove(data.Id); }
                }
                else
                {
                    string msg = response?.Message ?? "Boş Yanıt";
                    bool shouldLogErr = false;
                    lock (_lastErrorMessages)
                    {
                        if (!_lastErrorMessages.ContainsKey(data.Id) || _lastErrorMessages[data.Id] != msg)
                        {
                            _lastErrorMessages[data.Id] = msg;
                            shouldLogErr = true;
                        }
                    }
                    if (shouldLogErr)
                    {
                        _logger.LogWarning("❌ Alınamadı! ID: {Id}, Yanıt: {Message}", data.Id, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("⚠️ İşlem Hatası: ID: {Id}, Mesaj: {Message}", data.Id, ex.Message);
            }
            finally
            {
                lock (_activeProcessingIds) { _activeProcessingIds.Remove(data.Id); }
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
