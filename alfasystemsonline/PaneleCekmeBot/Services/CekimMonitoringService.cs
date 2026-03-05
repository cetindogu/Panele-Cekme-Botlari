using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaneleCekmeBot.Models;

namespace PaneleCekmeBot.Services
{
    public interface ICekimMonitoringService
    {
        Task<List<CekimTalebi>> GetYeniCekimTalepleriAsync();
        Task StartMonitoringAsync(CancellationToken cancellationToken);
        event EventHandler<List<CekimTalebi>>? YeniCekimTalepleriTespit;
    }

    public class CekimMonitoringService : ICekimMonitoringService
    {
        private readonly IHttpClientService _httpClient;
        private readonly ILoginService _loginService;
        private readonly IIstatistikService _istatistikService;
        private readonly ILogger<CekimMonitoringService> _logger;
        private readonly AppSettings _settings;
        private readonly HashSet<string> _islenenCekimler = new();
        private int _bosKayitSayaci = 0;

        public event EventHandler<List<CekimTalebi>>? YeniCekimTalepleriTespit;

        public CekimMonitoringService(
            IHttpClientService httpClient,
            ILoginService loginService,
            IIstatistikService istatistikService,
            ILogger<CekimMonitoringService> logger,
            IOptions<AppSettings> settings)
        {
            _httpClient = httpClient;
            _loginService = loginService;
            _istatistikService = istatistikService;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<List<CekimTalebi>> GetYeniCekimTalepleriAsync()
        {
            string? response = null;
            try
            {
                // Session kontrolü
                if (!await _loginService.IsLoggedInAsync())
                {
                    _logger.LogWarning("⚠️ Session geçersiz, yeniden giriş yapılıyor...");
                    if (!await _loginService.LoginAsync())
                    {
                        _logger.LogError("❌ Yeniden giriş başarısız");
                        return new List<CekimTalebi>();
                    }
                    _logger.LogInformation("✅ Yeniden giriş başarılı");
                }

                // Çekim listesi endpoint'ini çağır
                response = await _httpClient.GetAsync(_settings.Login.ListeleUrl);

                if (string.IsNullOrEmpty(response))
                {
                    _logger.LogWarning("⚠️ Çekim listesi endpoint'inden boş yanıt alındı");
                    return new List<CekimTalebi>();
                }

                // JSON'ı parse et
                var cekimResponse = JsonConvert.DeserializeObject<CekimListesiResponse>(response);

                if (cekimResponse?.Cekim?.Yeni == null)
                {
                    // Boş kayıt - minimal gösterge
                    BosKayitGosterge();
                    return new List<CekimTalebi>();
                }

                var yeniTalepler = cekimResponse.Cekim.Yeni;

                // Boş kayıt kontrolü
                if (yeniTalepler.Count == 0)
                {
                    // Boş kayıt - minimal gösterge
                    BosKayitGosterge();
                    return new List<CekimTalebi>();
                }

                // Dolu kayıt - detaylı loglama
                _logger.LogDebug("📡 Session test yanıtı alındı. Uzunluk: {Length}", response.Length);
                _logger.LogInformation("📄 DOLU KAYIT RESPONSE: {Response}", response);
                _logger.LogDebug("📊 Toplam {Count} çekim talebi bulundu", yeniTalepler.Count);

                // İstatistikleri güncelle - toplam görülen
                _istatistikService.KayitGoruldu(yeniTalepler);

                // Daha önce işlenmemiş talepleri filtrele
                var yeniTalepleriFiltreli = yeniTalepler
                    .Where(t => !string.IsNullOrEmpty(t.Id) && !_islenenCekimler.Contains(t.Id))
                    .Where(t => TutarFiltresiGecerliMi(t))
                    .ToList();

                // İstatistikleri güncelle - filtre geçen
                if (yeniTalepleriFiltreli.Any())
                {
                    _istatistikService.FiltreGecti(yeniTalepleriFiltreli);
                }

                if (yeniTalepleriFiltreli.Any())
                {
                    _logger.LogInformation("🎯 Yeni {Count} çekim talebi tespit edildi", yeniTalepleriFiltreli.Count);

                    if (_settings.Bot.EnableDetailedLogging)
                    {
                        foreach (var talep in yeniTalepleriFiltreli)
                        {
                            var tutar = talep.GetTutarAsDecimal();
                            _logger.LogInformation("💰 Yeni talep: ID={Id}, İsim={Isim}, Tutar={Tutar} ({DecimalTutar:N0} TL)",
                                talep.Id, talep.Isim, talep.Tutar, tutar);
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("📭 Filtre sonrası yeni talep bulunamadı");
                }

                return yeniTalepleriFiltreli;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parse hatası. Response: {Response}",
                    response?.Substring(0, Math.Min(200, response?.Length ?? 0)));
                return new List<CekimTalebi>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çekim talepleri alınırken hata oluştu");
                return new List<CekimTalebi>();
            }
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Çekim monitoring başlatılıyor. Polling interval: {Interval}ms", 
                _settings.Bot.PollingIntervalMs);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var yeniTalepler = await GetYeniCekimTalepleriAsync();

                    if (yeniTalepler.Any())
                    {
                        // Event'i tetikle
                        YeniCekimTalepleriTespit?.Invoke(this, yeniTalepler);

                        // İşlenen talepleri kaydet (memory'de tutuyoruz, gerçek uygulamada DB kullanılabilir)
                        foreach (var talep in yeniTalepler)
                        {
                            _islenenCekimler.Add(talep.Id);
                        }

                        // Memory'yi temiz tutmak için eski kayıtları sil (son 1000 kaydı tut)
                        if (_islenenCekimler.Count > 1000)
                        {
                            var eskiKayitlar = _islenenCekimler.Take(_islenenCekimler.Count - 500).ToList();
                            foreach (var eskiKayit in eskiKayitlar)
                            {
                                _islenenCekimler.Remove(eskiKayit);
                            }
                            _logger.LogDebug("Eski çekim kayıtları temizlendi. Kalan: {Count}", _islenenCekimler.Count);
                        }
                    }

                    // Polling interval kadar bekle
                    await Task.Delay(_settings.Bot.PollingIntervalMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Monitoring durduruldu");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Monitoring döngüsünde hata oluştu, devam ediliyor...");
                    
                    // Hata durumunda biraz daha uzun bekle
                    try
                    {
                        await Task.Delay(_settings.Bot.PollingIntervalMs * 2, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("Çekim monitoring durduruldu");
        }

        public void ClearProcessedItems()
        {
            _islenenCekimler.Clear();
            _logger.LogInformation("İşlenen çekim kayıtları temizlendi");
        }

        private bool TutarFiltresiGecerliMi(CekimTalebi talep)
        {
            // Tutar filtresi kapalıysa tüm talepleri kabul et
            if (!_settings.Bot.TutarFiltre.Enabled)
                return true;

            try
            {
                var tutar = talep.GetTutarAsDecimal();

                // Geçersiz tutarları nasıl ele alacağımızı kontrol et
                if (tutar <= 0)
                {
                    if (_settings.Bot.TutarFiltre.IgnoreInvalidAmounts)
                    {
                        _logger.LogDebug("Geçersiz tutar göz ardı ediliyor. ID: {Id}, Tutar: {Tutar}",
                            talep.Id, talep.Tutar);
                        return false; // Geçersiz tutarları işleme
                    }
                    else
                    {
                        _logger.LogWarning("Geçersiz tutar tespit edildi ancak işlenecek. ID: {Id}, Tutar: {Tutar}",
                            talep.Id, talep.Tutar);
                        return true; // Geçersiz tutarları da işle
                    }
                }

                // Tutar aralığını kontrol et
                var minTutar = _settings.Bot.TutarFiltre.MinTutar;
                var maxTutar = _settings.Bot.TutarFiltre.MaxTutar;

                if (tutar >= minTutar && tutar <= maxTutar)
                {
                    _logger.LogDebug("Tutar filtresi geçti. ID: {Id}, Tutar: {Tutar} ({DecimalTutar}), Aralık: {Min}-{Max}",
                        talep.Id, talep.Tutar, tutar, minTutar, maxTutar);
                    return true;
                }
                else
                {
                    _logger.LogDebug("Tutar filtresi geçmedi. ID: {Id}, Tutar: {Tutar} ({DecimalTutar}), Aralık: {Min}-{Max}",
                        talep.Id, talep.Tutar, tutar, minTutar, maxTutar);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tutar filtresi kontrolünde hata. ID: {Id}, Tutar: {Tutar}",
                    talep.Id, talep.Tutar);

                // Hata durumunda IgnoreInvalidAmounts ayarına göre karar ver
                return !_settings.Bot.TutarFiltre.IgnoreInvalidAmounts;
            }
        }

        private void BosKayitGosterge()
        {
            _bosKayitSayaci++;

            // Her 20 boş kayıtta bir gösterge
            if (_bosKayitSayaci % 20 == 0)
            {
                Console.Write("🔄");

                // Her 100 boş kayıtta bir satır atla
                if (_bosKayitSayaci % 100 == 0)
                {
                    Console.WriteLine($" [{DateTime.Now:HH:mm:ss}]");
                    _bosKayitSayaci = 0; // Reset
                }
            }
        }
    }
}
