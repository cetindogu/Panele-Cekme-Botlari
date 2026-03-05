using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaneleCekmeBot.Models;

namespace PaneleCekmeBot.Services
{
    public interface IPaneleCekmeService
    {
        Task<bool> PaneleCekAsync(string cekimId);
        Task<List<bool>> PaneleCekBulkAsync(List<string> cekimIds);
        Task ProcessYeniTaleplerAsync(List<CekimTalebi> talepler);
    }

    public class PaneleCekmeService : IPaneleCekmeService
    {
        private readonly IHttpClientService _httpClient;
        private readonly ILoginService _loginService;
        private readonly IIstatistikService _istatistikService;
        private readonly ILogger<PaneleCekmeService> _logger;
        private readonly AppSettings _settings;

        public PaneleCekmeService(
            IHttpClientService httpClient,
            ILoginService loginService,
            IIstatistikService istatistikService,
            ILogger<PaneleCekmeService> logger,
            IOptions<AppSettings> settings)
        {
            _httpClient = httpClient;
            _loginService = loginService;
            _istatistikService = istatistikService;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<bool> PaneleCekAsync(string cekimId)
        {
            return await PaneleCekAsync(cekimId, null);
        }

        private async Task<bool> PaneleCekAsync(string cekimId, CekimTalebi? talep)
        {
            if (string.IsNullOrEmpty(cekimId))
            {
                _logger.LogWarning("Geçersiz çekim ID: {CekimId}", cekimId);
                return false;
            }

            var retryCount = 0;
            var maxRetries = _settings.Bot.MaxRetryCount;

            while (retryCount <= maxRetries)
            {
                try
                {
                    _logger.LogDebug("🚀 Panele çekme işlemi başlatılıyor. ID: {CekimId}, Deneme: {Retry}/{MaxRetries}",
                        cekimId, retryCount + 1, maxRetries + 1);

                    // Session kontrolü
                    if (!await _loginService.IsLoggedInAsync())
                    {
                        _logger.LogWarning("⚠️ Session geçersiz, yeniden giriş yapılıyor...");
                        if (!await _loginService.LoginAsync())
                        {
                            _logger.LogError("❌ Yeniden giriş başarısız");
                            return false;
                        }
                        _logger.LogDebug("✅ Session yenilendi");
                    }

                    // POST verisi hazırla
                    var postData = new Dictionary<string, string>
                    {
                        ["id"] = cekimId
                    };
                    _logger.LogDebug("📋 POST verisi hazırlandı: {Data}", string.Join(", ", postData.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                    // Panele çekme isteği gönder
                    _logger.LogDebug("📤 Panele çekme isteği gönderiliyor: {Url}", _settings.Login.PaneleCekUrl);
                    var response = await _httpClient.PostAsync(_settings.Login.PaneleCekUrl, postData);
                    _logger.LogDebug("📨 Panele çekme yanıtı alındı: {Response}", response?.Trim());

                    // Yanıtı kontrol et
                    if (string.IsNullOrEmpty(response))
                    {
                        _logger.LogWarning("⚠️ Panele çekme isteğinden boş yanıt alındı. ID: {CekimId}", cekimId);
                        retryCount++;
                        continue;
                    }

                    var originalResponse = response;
                    response = response.Trim().ToLower();

                    if (response.Contains("ok") || response.Contains("başarılı") || response.Contains("success"))
                    {
                        _logger.LogInformation("✅ Panele çekme başarılı! ID: {CekimId}, Yanıt: {Response}", cekimId, originalResponse);

                        // İstatistikleri güncelle
                        if (talep != null)
                        {
                            _istatistikService.CekimBasarili(talep);
                        }

                        return true;
                    }
                    else if (response.Contains("alınmış") || response.Contains("başka") || response.Contains("taken"))
                    {
                        _logger.LogWarning("⚠️ Çekim başka biri tarafından alınmış. ID: {CekimId}, Yanıt: {Response}", cekimId, originalResponse);

                        // İstatistikleri güncelle
                        if (talep != null)
                        {
                            _istatistikService.CekimBasarisiz(talep);
                        }

                        return false; // Retry yapmaya gerek yok, başkası almış
                    }
                    else
                    {
                        _logger.LogWarning("❓ Beklenmeyen yanıt alındı. ID: {CekimId}, Yanıt: {Response}",
                            cekimId, originalResponse);
                        retryCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Panele çekme işlemi sırasında hata. ID: {CekimId}, Deneme: {Retry}/{MaxRetries}",
                        cekimId, retryCount + 1, maxRetries + 1);
                    retryCount++;
                }

                // Retry öncesi kısa bir bekleme
                if (retryCount <= maxRetries)
                {
                    _logger.LogDebug("⏳ {Delay}ms bekleme sonrası tekrar denenecek...", 100);
                    await Task.Delay(100); // 100ms bekleme
                }
            }

            _logger.LogError("❌ Panele çekme başarısız (max retry aşıldı). ID: {CekimId}, Toplam Deneme: {TotalRetries}",
                cekimId, maxRetries + 1);
            return false;
        }

        public async Task<List<bool>> PaneleCekBulkAsync(List<string> cekimIds)
        {
            if (cekimIds?.Any() != true)
            {
                return new List<bool>();
            }

            _logger.LogInformation("🔄 Toplu panele çekme işlemi başlatılıyor. Toplam: {Count}", cekimIds.Count);

            // Paralel işlem için task'ları oluştur
            var tasks = cekimIds.Select(async id =>
            {
                try
                {
                    return await PaneleCekAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Toplu işlemde hata. ID: {CekimId}", id);
                    return false;
                }
            });

            // Tüm task'ları paralel olarak çalıştır
            var results = await Task.WhenAll(tasks);

            var basariliSayisi = results.Count(r => r);
            _logger.LogInformation("📊 Toplu panele çekme tamamlandı. Başarılı: {Basarili}/{Toplam} (%{Yuzde:F1})",
                basariliSayisi, cekimIds.Count, (basariliSayisi * 100.0 / cekimIds.Count));

            return results.ToList();
        }

        public async Task ProcessYeniTaleplerAsync(List<CekimTalebi> talepler)
        {
            if (talepler?.Any() != true)
            {
                return;
            }

            _logger.LogInformation("🚀 Yeni talepler işleniyor. Toplam: {Count}", talepler.Count);

            // Hız için paralel işlem yap
            var tasks = talepler.Select(async talep =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    var basarili = await PaneleCekAsync(talep.Id, talep);
                    stopwatch.Stop();

                    var tutarDecimal = talep.GetTutarAsDecimal();
                    if (basarili)
                    {
                        _logger.LogInformation("✅ İşlem başarılı! ID: {Id}, İsim: {Isim}, Tutar: {Tutar} ({DecimalTutar:N0} TL), Süre: {Sure}ms",
                            talep.Id, talep.Isim, talep.Tutar, tutarDecimal, stopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        _logger.LogWarning("❌ İşlem başarısız! ID: {Id}, İsim: {Isim}, Tutar: {Tutar} ({DecimalTutar:N0} TL), Süre: {Sure}ms",
                            talep.Id, talep.Isim, talep.Tutar, tutarDecimal, stopwatch.ElapsedMilliseconds);
                    }

                    return basarili;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "Talep işlenirken hata. ID: {Id}, Süre: {Sure}ms", 
                        talep.Id, stopwatch.ElapsedMilliseconds);
                    return false;
                }
            });

            // Tüm task'ları paralel çalıştır
            var results = await Task.WhenAll(tasks);

            var basariliSayisi = results.Count(r => r);
            _logger.LogInformation("📊 Talep işleme tamamlandı. Başarılı: {Basarili}/{Toplam}", 
                basariliSayisi, talepler.Count);
        }
    }
}
