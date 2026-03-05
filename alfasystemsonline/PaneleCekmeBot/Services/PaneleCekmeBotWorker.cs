using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaneleCekmeBot.Models;

namespace PaneleCekmeBot.Services
{
    public class PaneleCekmeBotWorker : BackgroundService
    {
        private readonly ILoginService _loginService;
        private readonly ICekimMonitoringService _monitoringService;
        private readonly IPaneleCekmeService _paneleCekmeService;
        private readonly IAyarOnaylamaService _ayarOnaylamaService;
        private readonly IIstatistikService _istatistikService;
        private readonly ILogger<PaneleCekmeBotWorker> _logger;
        private readonly AppSettings _settings;

        public PaneleCekmeBotWorker(
            ILoginService loginService,
            ICekimMonitoringService monitoringService,
            IPaneleCekmeService paneleCekmeService,
            IAyarOnaylamaService ayarOnaylamaService,
            IIstatistikService istatistikService,
            ILogger<PaneleCekmeBotWorker> logger,
            IOptions<AppSettings> settings)
        {
            _loginService = loginService;
            _monitoringService = monitoringService;
            _paneleCekmeService = paneleCekmeService;
            _ayarOnaylamaService = ayarOnaylamaService;
            _istatistikService = istatistikService;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🤖 Panele Çekme Bot başlatılıyor...");

            try
            {
                // Ayarları onaylat
                if (!await _ayarOnaylamaService.AyarlariOnaylaAsync())
                {
                    _logger.LogCritical("❌ Ayarlar onaylanmadı, bot durduruluyor");
                    return;
                }

                // İlk giriş işlemi
                if (!await PerformInitialLoginAsync())
                {
                    _logger.LogCritical("❌ İlk giriş başarısız, bot durduruluyor");
                    return;
                }

                // Monitoring event'ini bağla
                _monitoringService.YeniCekimTalepleriTespit += OnYeniCekimTalepleriTespit;

                _logger.LogInformation("🚀 Bot başarıyla başlatıldı ve monitoring başlıyor...");

                // Ana monitoring döngüsünü başlat
                await _monitoringService.StartMonitoringAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 Bot durdurma isteği alındı");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "💥 Bot çalışırken kritik hata oluştu");
            }
            finally
            {
                // Event bağlantısını kaldır
                _monitoringService.YeniCekimTalepleriTespit -= OnYeniCekimTalepleriTespit;
                
                // Çıkış işlemi
                await _loginService.LogoutAsync();
                _logger.LogInformation("👋 Bot durduruldu");
            }
        }

        private async Task<bool> PerformInitialLoginAsync()
        {
            var maxLoginRetries = 3;
            var loginRetryCount = 0;

            while (loginRetryCount < maxLoginRetries)
            {
                try
                {
                    _logger.LogInformation("🔐 Giriş yapılıyor... (Deneme: {Retry}/{Max})", 
                        loginRetryCount + 1, maxLoginRetries);

                    if (await _loginService.LoginAsync())
                    {
                        _logger.LogInformation("✅ Giriş başarılı!");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("❌ Giriş başarısız");
                        loginRetryCount++;
                        
                        if (loginRetryCount < maxLoginRetries)
                        {
                            _logger.LogInformation("⏳ 5 saniye bekleyip tekrar denenecek...");
                            await Task.Delay(5000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Giriş işlemi sırasında hata (Deneme: {Retry})", loginRetryCount + 1);
                    loginRetryCount++;
                    
                    if (loginRetryCount < maxLoginRetries)
                    {
                        await Task.Delay(5000);
                    }
                }
            }

            return false;
        }

        private async void OnYeniCekimTalepleriTespit(object? sender, List<CekimTalebi> talepler)
        {
            try
            {
                // Günlük reset kontrolü
                _istatistikService.GunlukResetKontrolEt();

                _logger.LogInformation("🎯 Yeni çekim talepleri tespit edildi: {Count} adet", talepler.Count);

                // Sadece izleme modu kontrolü
                if (_settings.Bot.CekimLimitleri.SadeceIzlemeModu)
                {
                    _logger.LogInformation("👁️ SADECE İZLEME MODU - Kayıtlar detayları ile loglanıyor:");

                    foreach (var talep in talepler)
                    {
                        var tutar = talep.GetTutarAsDecimal();
                        _logger.LogInformation("📋 Yakalanan Kayıt: ID={Id}, İsim={Isim}, Tutar={Tutar} ({DecimalTutar:N0} TL), Tarih={Tarih}",
                            talep.Id, talep.Isim, talep.Tutar, tutar, talep.Tarih);
                    }

                    _logger.LogInformation("👁️ İzleme modu aktif - Panele çekme işlemi yapılmadı");

                    // İstatistikleri logla
                    _istatistikService.IstatistikleriLogla();
                    return;
                }

                // Normal mod - limit kontrolü
                if (_istatistikService.LimitKontrolEt())
                {
                    _logger.LogWarning("🚫 Limitler aşıldı, yeni çekimler yapılmayacak");
                    return;
                }

                // Hızlı işlem için paralel olarak çek
                await _paneleCekmeService.ProcessYeniTaleplerAsync(talepler);

                // İstatistikleri logla
                _istatistikService.IstatistikleriLogla();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yeni talepler işlenirken hata oluştu");
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔄 Worker Service başlatılıyor...");
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔄 Worker Service durduruluyor...");
            await base.StopAsync(cancellationToken);
        }
    }
}
