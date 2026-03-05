using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaneleCekmeBot.Models;

namespace PaneleCekmeBot.Services
{
    public interface IAyarOnaylamaService
    {
        Task<bool> AyarlariOnaylaAsync();
        void AyarlariLogla();
    }

    public class AyarOnaylamaService : IAyarOnaylamaService
    {
        private readonly ILogger<AyarOnaylamaService> _logger;
        private readonly AppSettings _settings;

        public AyarOnaylamaService(
            ILogger<AyarOnaylamaService> logger,
            IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public Task<bool> AyarlariOnaylaAsync()
        {
            Console.WriteLine();
            Console.WriteLine("🔧 PANELE ÇEKME BOT AYARLARI");
            Console.WriteLine("============================");
            Console.WriteLine();

            // Login ayarları
            Console.WriteLine("🔐 GİRİŞ AYARLARI:");
            Console.WriteLine($"   👤 Kullanıcı Adı: {Constants.Username}");
            Console.WriteLine($"   🔑 Şifre: {new string('*', _settings.Login.Password.Length)}");
            Console.WriteLine($"   🌐 Login URL: {_settings.Login.LoginUrl}");
            Console.WriteLine($"   📡 Listele URL: {_settings.Login.ListeleUrl}");
            Console.WriteLine($"   🎯 Panele Çek URL: {_settings.Login.PaneleCekUrl}");
            Console.WriteLine();

            // Bot ayarları
            Console.WriteLine("🤖 BOT AYARLARI:");
            Console.WriteLine($"   ⏱️ Polling Aralığı: {_settings.Bot.PollingIntervalMs:N0} ms");
            Console.WriteLine($"   ⏰ Request Timeout: {_settings.Bot.RequestTimeoutMs:N0} ms");
            Console.WriteLine($"   🔄 Max Retry: {_settings.Bot.MaxRetryCount}");
            Console.WriteLine($"   📝 Detaylı Loglama: {(_settings.Bot.EnableDetailedLogging ? "Aktif" : "Pasif")}");
            Console.WriteLine();

            // Tutar filtresi
            Console.WriteLine("💰 TUTAR FİLTRESİ:");
            if (_settings.Bot.TutarFiltre.Enabled)
            {
                Console.WriteLine($"   ✅ Durum: Aktif");
                Console.WriteLine($"   💵 Min Tutar: {_settings.Bot.TutarFiltre.MinTutar:N0} TL");
                Console.WriteLine($"   💰 Max Tutar: {_settings.Bot.TutarFiltre.MaxTutar:N0} TL");
                Console.WriteLine($"   🚫 Geçersiz Tutarları Göz Ardı Et: {(_settings.Bot.TutarFiltre.IgnoreInvalidAmounts ? "Evet" : "Hayır")}");
            }
            else
            {
                Console.WriteLine($"   ❌ Durum: Pasif (Tüm tutarlar işlenecek)");
            }
            Console.WriteLine();

            // Çekim limitleri
            Console.WriteLine("🎯 ÇEKİM LİMİTLERİ:");
            if (_settings.Bot.CekimLimitleri.MaxKayitSayisi.HasValue)
            {
                if (_settings.Bot.CekimLimitleri.MaxKayitSayisi == 0)
                {
                    Console.WriteLine($"   👁️ Mod: SADECE İZLEME MODU (Panele çekme yapılmayacak)");
                    Console.WriteLine($"   📊 Max Kayıt Sayısı: 0 (Sadece kayıt yakalama ve loglama)");
                }
                else
                {
                    Console.WriteLine($"   📊 Max Kayıt Sayısı: {_settings.Bot.CekimLimitleri.MaxKayitSayisi:N0} kayıt");
                }
            }
            else
            {
                Console.WriteLine($"   📊 Max Kayıt Sayısı: Sınırsız");
            }

            if (_settings.Bot.CekimLimitleri.MaxToplamTutar.HasValue)
            {
                Console.WriteLine($"   💎 Max Toplam Tutar: {_settings.Bot.CekimLimitleri.MaxToplamTutar:N0} TL");
            }
            else
            {
                Console.WriteLine($"   💎 Max Toplam Tutar: Sınırsız");
            }

            Console.WriteLine($"   🔄 Günlük Reset: {(_settings.Bot.CekimLimitleri.ResetDaily ? "Aktif" : "Pasif")}");
            Console.WriteLine();

            // Proxy ayarları
            Console.WriteLine("🌐 PROXY AYARLARI:");
            if (_settings.Proxy?.Enabled == true)
            {
                Console.WriteLine($"   ✅ Durum: Aktif");
                Console.WriteLine($"   🖥️ Host: {_settings.Proxy.Host}");
                Console.WriteLine($"   🔌 Port: {_settings.Proxy.Port}");
                Console.WriteLine($"   👤 Kullanıcı: {(!string.IsNullOrEmpty(_settings.Proxy.Username) ? _settings.Proxy.Username : "Yok")}");
            }
            else
            {
                Console.WriteLine($"   ❌ Durum: Pasif");
            }
            Console.WriteLine();

            // Onay alma
            Console.WriteLine("⚠️ UYARI: Bu ayarlarla bot çalışmaya başlayacak!");
            Console.WriteLine("📁 Tüm ayarlar log dosyasına kaydedilecek.");
            Console.WriteLine();
            Console.Write("Bu ayarları onaylıyor musunuz? (E/H): ");

            var onay = Console.ReadLine()?.ToUpper().Trim();
            
            if (onay == "E" || onay == "EVET" || onay == "Y" || onay == "YES")
            {
                Console.WriteLine();
                Console.WriteLine("✅ Ayarlar onaylandı! Bot başlatılıyor...");
                Console.WriteLine();
                
                // Ayarları logla
                AyarlariLogla();
                
                return Task.FromResult(true);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("❌ Ayarlar onaylanmadı. Bot durduruluyor...");
                Console.WriteLine("💡 Ayarları değiştirmek için appsettings.json dosyasını düzenleyin.");
                Console.WriteLine();
                return Task.FromResult(false);
            }
        }

        public void AyarlariLogla()
        {
            _logger.LogInformation("🔧 BOT AYARLARI ONAYLANDI");
            _logger.LogInformation("========================");
            
            // Login ayarları
            _logger.LogInformation("🔐 Giriş Ayarları:");
            _logger.LogInformation("   👤 Kullanıcı: {Username}", Constants.Username);
            _logger.LogInformation("   🌐 Login URL: {LoginUrl}", _settings.Login.LoginUrl);
            _logger.LogInformation("   📡 Listele URL: {ListeleUrl}", _settings.Login.ListeleUrl);
            _logger.LogInformation("   🎯 Panele Çek URL: {PaneleCekUrl}", _settings.Login.PaneleCekUrl);
            
            // Bot ayarları
            _logger.LogInformation("🤖 Bot Ayarları:");
            _logger.LogInformation("   ⏱️ Polling Aralığı: {PollingInterval} ms", _settings.Bot.PollingIntervalMs);
            _logger.LogInformation("   ⏰ Request Timeout: {RequestTimeout} ms", _settings.Bot.RequestTimeoutMs);
            _logger.LogInformation("   🔄 Max Retry: {MaxRetry}", _settings.Bot.MaxRetryCount);
            _logger.LogInformation("   📝 Detaylı Loglama: {DetailedLogging}", _settings.Bot.EnableDetailedLogging);
            
            // Tutar filtresi
            _logger.LogInformation("💰 Tutar Filtresi:");
            _logger.LogInformation("   ✅ Aktif: {Enabled}", _settings.Bot.TutarFiltre.Enabled);
            if (_settings.Bot.TutarFiltre.Enabled)
            {
                _logger.LogInformation("   💵 Min Tutar: {MinTutar:N0} TL", _settings.Bot.TutarFiltre.MinTutar);
                _logger.LogInformation("   💰 Max Tutar: {MaxTutar:N0} TL", _settings.Bot.TutarFiltre.MaxTutar);
                _logger.LogInformation("   🚫 Geçersiz Tutarları Göz Ardı Et: {IgnoreInvalid}", _settings.Bot.TutarFiltre.IgnoreInvalidAmounts);
            }
            
            // Çekim limitleri
            _logger.LogInformation("🎯 Çekim Limitleri:");
            if (_settings.Bot.CekimLimitleri.SadeceIzlemeModu)
            {
                _logger.LogInformation("   👁️ Mod: SADECE İZLEME MODU (Panele çekme yapılmayacak)");
                _logger.LogInformation("   📊 Max Kayıt Sayısı: 0 (Sadece kayıt yakalama ve loglama)");
            }
            else
            {
                _logger.LogInformation("   📊 Max Kayıt Sayısı: {MaxKayit}",
                    _settings.Bot.CekimLimitleri.MaxKayitSayisi?.ToString("N0") ?? "Sınırsız");
            }
            _logger.LogInformation("   💎 Max Toplam Tutar: {MaxTutar}",
                _settings.Bot.CekimLimitleri.MaxToplamTutar?.ToString("N0") + " TL" ?? "Sınırsız");
            _logger.LogInformation("   🔄 Günlük Reset: {DailyReset}", _settings.Bot.CekimLimitleri.ResetDaily);
            
            // Proxy ayarları
            _logger.LogInformation("🌐 Proxy Ayarları:");
            _logger.LogInformation("   ✅ Aktif: {ProxyEnabled}", _settings.Proxy?.Enabled ?? false);
            if (_settings.Proxy?.Enabled == true)
            {
                _logger.LogInformation("   🖥️ Host: {ProxyHost}", _settings.Proxy.Host);
                _logger.LogInformation("   🔌 Port: {ProxyPort}", _settings.Proxy.Port);
                _logger.LogInformation("   👤 Kullanıcı: {ProxyUser}", 
                    !string.IsNullOrEmpty(_settings.Proxy.Username) ? _settings.Proxy.Username : "Yok");
            }
            
            _logger.LogInformation("========================");
            _logger.LogInformation("✅ Ayarlar onaylandı ve loglandı - {Timestamp}", DateTime.Now);
        }
    }
}
