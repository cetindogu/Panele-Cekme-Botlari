using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TRONPANELE_CEKME.Models;

namespace TRONPANELE_CEKME.Services
{
    public interface ILoginService
    {
        Task<bool> LoginAsync();
    }

    public class LoginService : ILoginService
    {
        private readonly IHttpClientService _httpClient;
        private readonly ILogger<LoginService> _logger;
        private readonly ICredentialProvider _credentialProvider;
        private readonly LoginSettings _settings;

        public LoginService(
            IHttpClientService httpClient,
            ILogger<LoginService> logger,
            ICredentialProvider credentialProvider,
            IOptions<AppSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _credentialProvider = credentialProvider;
            _settings = settings.Value.Login;
        }

        public async Task<bool> LoginAsync()
        {
            try
            {
                var username = _credentialProvider.GetUsername();
                _logger.LogInformation("🔐 Giriş işlemi başlatılıyor...");

                // 1. Get login page to extract CSRF token if exists
                var loginPageContent = await _httpClient.GetAsync(_settings.LoginUrl);
                _logger.LogDebug("Login sayfası içeriği: {Content}", loginPageContent.Substring(0, Math.Min(500, loginPageContent.Length)));
                var token = ExtractTokenFromHtml(loginPageContent);
                _logger.LogInformation("🔑 Alınan token: {Token}", token ?? "Bulunamadı");

                // 2. Prepare login data
                var loginData = new Dictionary<string, string>
                {
                    ["email"] = username,
                    ["password"] = _settings.Password,
                    ["_token"] = token ?? ""
                };

                // 3. Send login POST request
                var loginEndpoint = _settings.LoginUrl;
                var loginResponse = await _httpClient.PostAjaxAsync(loginEndpoint, loginData);

                // Check if login was successful. 
                // Since this might be a redirect or a JSON response, let's be flexible.
                // If it's a redirect to the dashboard or contains success indicators.
                var isSuccessful = !loginResponse.Contains("Giriş Yap") && 
                                  (loginResponse.Contains("dashboard") || 
                                   loginResponse.Contains("withdraws") || 
                                   string.IsNullOrEmpty(loginResponse)); // Redirects often return empty body with 302

                if (isSuccessful)
                {
                    _logger.LogInformation("✅ Giriş başarılı!");
                    return true;
                }

                _logger.LogError("❌ Giriş başarısız! Yanıt: {Response}", loginResponse);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Giriş işlemi sırasında hata oluştu");
                return false;
            }
        }

        private string? ExtractTokenFromHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            // Look for name="_token" or name="token"
            var tokenInput = doc.DocumentNode.SelectSingleNode("//input[@name='_token']") ?? 
                             doc.DocumentNode.SelectSingleNode("//input[@name='token']");
            return tokenInput?.GetAttributeValue("value", string.Empty);
        }
    }
}
