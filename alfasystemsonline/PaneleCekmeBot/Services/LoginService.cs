using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaneleCekmeBot.Models;

namespace PaneleCekmeBot.Services
{
    public interface ILoginService
    {
        Task<bool> LoginAsync();
        Task<bool> IsLoggedInAsync();
        Task LogoutAsync();
    }

    public class LoginService : ILoginService
    {
        private readonly IHttpClientService _httpClient;
        private readonly ILogger<LoginService> _logger;
        private readonly AppSettings _settings;
        private bool _isLoggedIn = false;

        public LoginService(
            IHttpClientService httpClient,
            ILogger<LoginService> logger,
            IOptions<AppSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<bool> LoginAsync()
        {
            try
            {
                _logger.LogInformation("🔐 Giriş işlemi başlatılıyor...");
                _logger.LogInformation("📍 Login URL: {LoginUrl}", _settings.Login.LoginUrl);
                _logger.LogInformation("👤 Kullanıcı: {Username}", Constants.Username);

                // 1. Login sayfasını al ve token'ı çıkar
                _logger.LogDebug("📥 Login sayfası getiriliyor...");
                var loginPageContent = await _httpClient.GetAsync(_settings.Login.LoginUrl);
                _logger.LogDebug("📄 Login sayfası alındı. İçerik uzunluğu: {Length} karakter", loginPageContent.Length);

                var token = ExtractTokenFromHtml(loginPageContent);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("❌ Login sayfasından token alınamadı");
                    _logger.LogDebug("🔍 Login sayfası içeriği (ilk 500 karakter): {Content}",
                        loginPageContent.Substring(0, Math.Min(500, loginPageContent.Length)));
                    return false;
                }

                _logger.LogInformation("🔑 Token başarıyla alındı: {Token}", token);

                // 2. Login POST isteği gönder (doğru endpoint'e)
                _logger.LogDebug("📤 Login POST isteği hazırlanıyor...");
                var loginEndpoint = _settings.Login.LoginUrl.TrimEnd('/') + "/_giris_kontrol.php";
                _logger.LogDebug("🎯 Login endpoint: {Endpoint}", loginEndpoint);

                var loginData = new Dictionary<string, string>
                {
                    ["login_username"] = Constants.Username,
                    ["login_password"] = "***", // Şifreyi loglamayalım
                    ["login_ga_code"] = "", // Google Authenticator kodu (boş)
                    ["token"] = token
                };
                _logger.LogDebug("📋 Login verileri: {LoginData}", string.Join(", ", loginData.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                var actualLoginData = new Dictionary<string, string>
                {
                    ["login_username"] = Constants.Username,
                    ["login_password"] = _settings.Login.Password,
                    ["login_ga_code"] = "", // Google Authenticator kodu (boş)
                    ["token"] = token
                };

                _logger.LogDebug("🚀 Login AJAX isteği gönderiliyor...");
                var loginResponse = await _httpClient.PostAjaxAsync(loginEndpoint, actualLoginData);
                _logger.LogDebug("📨 Login yanıtı alındı. İçerik uzunluğu: {Length} karakter", loginResponse.Length);

                // 3. Başarılı giriş kontrolü
                _logger.LogDebug("🔍 Giriş yanıtı kontrol ediliyor...");
                _logger.LogDebug("📄 Login yanıtı: {Response}", loginResponse);

                // AJAX yanıtı "Ok" veya "ErrorT" ise başarılı
                var isSuccessful = loginResponse.Trim() == "Ok" || loginResponse.Trim() == "ErrorT";

                if (isSuccessful)
                {
                    _logger.LogInformation("✅ Giriş başarılı!");
                    _isLoggedIn = true;

                    // PHPSESSID cookie'sini kontrol et
                    var sessionId = _httpClient.GetCookie("PHPSESSID");
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _logger.LogInformation("🍪 PHPSESSID cookie alındı: {SessionId}", sessionId);
                        _logger.LogDebug("🔐 Session başarıyla kuruldu");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ PHPSESSID cookie bulunamadı");
                    }

                    // Başarılı giriş sonrası yönlendirme bilgisi
                    if (loginResponse.Contains("index.php"))
                    {
                        _logger.LogDebug("🔄 index.php'ye yönlendirme tespit edildi");
                    }

                    return true;
                }
                else
                {
                    _logger.LogError("❌ Giriş başarısız. Kullanıcı adı veya şifre hatalı olabilir.");
                    _logger.LogWarning("🔍 Login yanıtı analizi (ilk 500 karakter): {Response}",
                        loginResponse.Substring(0, Math.Min(500, loginResponse.Length)));

                    // Hata türünü tespit etmeye çalış
                    if (loginResponse.Contains("password") || loginResponse.Contains("şifre"))
                    {
                        _logger.LogError("🚫 Şifre hatası tespit edildi");
                    }
                    if (loginResponse.Contains("username") || loginResponse.Contains("kullanıcı"))
                    {
                        _logger.LogError("🚫 Kullanıcı adı hatası tespit edildi");
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Giriş işlemi sırasında kritik hata oluştu");
                _logger.LogError("🔍 Hata detayları: {Message}", ex.Message);
                _isLoggedIn = false;
                return false;
            }
        }

        public async Task<bool> IsLoggedInAsync()
        {
            try
            {
                if (!_isLoggedIn)
                {
                    _logger.LogDebug("🔍 Session durumu: Giriş yapılmamış");
                    return false;
                }

                // Session'ın hala geçerli olup olmadığını kontrol et
                var testResponse = await _httpClient.GetAsync(_settings.Login.ListeleUrl);

                // Eğer login sayfasına yönlendirildiyse session süresi dolmuş
                if (testResponse.Contains("login") && testResponse.Contains("password"))
                {
                    _logger.LogWarning("⏰ Session süresi dolmuş, yeniden giriş gerekiyor");
                    _logger.LogDebug("🔍 Session test yanıtı (ilk 300 karakter): {Response}",
                        testResponse.Substring(0, Math.Min(300, testResponse.Length)));
                    _isLoggedIn = false;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Session kontrolü sırasında hata oluştu");
                _isLoggedIn = false;
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                _logger.LogInformation("Çıkış işlemi yapılıyor...");
                _httpClient.ClearCookies();
                _isLoggedIn = false;
                _logger.LogInformation("Çıkış tamamlandı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çıkış işlemi sırasında hata");
            }
            await Task.CompletedTask;
        }

        private string? ExtractTokenFromHtml(string html)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Token input alanını bul
                var tokenInput = doc.DocumentNode
                    .SelectSingleNode("//input[@name='token']");

                if (tokenInput != null)
                {
                    var token = tokenInput.GetAttributeValue("value", "");
                    if (!string.IsNullOrEmpty(token))
                    {
                        return token;
                    }
                }

                // Alternatif olarak hidden input'ları kontrol et
                var hiddenInputs = doc.DocumentNode
                    .SelectNodes("//input[@type='hidden']");

                if (hiddenInputs != null)
                {
                    foreach (var input in hiddenInputs)
                    {
                        var name = input.GetAttributeValue("name", "");
                        if (name.ToLower().Contains("token") || name.ToLower().Contains("csrf"))
                        {
                            var value = input.GetAttributeValue("value", "");
                            if (!string.IsNullOrEmpty(value))
                            {
                                return value;
                            }
                        }
                    }
                }

                _logger.LogWarning("HTML içinde token bulunamadı");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token çıkarılırken hata oluştu");
                return null;
            }
        }
    }
}
