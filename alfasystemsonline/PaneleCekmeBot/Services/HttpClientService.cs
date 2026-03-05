using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaneleCekmeBot.Models;

namespace PaneleCekmeBot.Services
{
    public interface IHttpClientService
    {
        Task<string> GetAsync(string url);
        Task<string> PostAsync(string url, string content, string contentType = "application/x-www-form-urlencoded");
        Task<string> PostAsync(string url, Dictionary<string, string> formData);
        Task<string> PostAjaxAsync(string url, Dictionary<string, string> formData);
        void SetCookie(string name, string value, string domain);
        string? GetCookie(string name);
        void ClearCookies();
    }

    public class HttpClientService : IHttpClientService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;
        private readonly ILogger<HttpClientService> _logger;
        private readonly AppSettings _settings;

        public HttpClientService(ILogger<HttpClientService> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
            _cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler()
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            // Proxy ayarları varsa ekle
            if (_settings.Proxy?.Enabled == true)
            {
                var proxy = new WebProxy(_settings.Proxy.Host, _settings.Proxy.Port);
                if (!string.IsNullOrEmpty(_settings.Proxy.Username))
                {
                    proxy.Credentials = new NetworkCredential(_settings.Proxy.Username, _settings.Proxy.Password);
                }
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(_settings.Bot.RequestTimeoutMs)
            };

            // Varsayılan header'ları ayarla
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        }

        public async Task<string> GetAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET isteği başarısız: {Url}", url);
                throw;
            }
        }

        public async Task<string> PostAsync(string url, string content, string contentType = "application/x-www-form-urlencoded")
        {
            try
            {
                _logger.LogDebug("POST isteği gönderiliyor: {Url}", url);
                
                var stringContent = new StringContent(content, Encoding.UTF8, contentType);
                
                // Referer header'ını ekle
                if (!_httpClient.DefaultRequestHeaders.Contains("Referer"))
                {
                    _httpClient.DefaultRequestHeaders.Add("Referer", _settings.Login.LoginUrl);
                }
                
                var response = await _httpClient.PostAsync(url, stringContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogDebug("POST yanıtı alındı. Status: {StatusCode}, Content Length: {Length}", 
                    response.StatusCode, responseContent.Length);
                
                return responseContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST isteği başarısız: {Url}", url);
                throw;
            }
        }

        public async Task<string> PostAsync(string url, Dictionary<string, string> formData)
        {
            var content = string.Join("&", formData.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            return await PostAsync(url, content);
        }

        public async Task<string> PostAjaxAsync(string url, Dictionary<string, string> formData)
        {
            try
            {
                _logger.LogDebug("AJAX POST isteği gönderiliyor: {Url}", url);

                var content = string.Join("&", formData.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

                var stringContent = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded");

                // AJAX için özel header'lar
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = stringContent
                };

                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("Referer", _settings.Login.LoginUrl);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("AJAX POST yanıtı alındı. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                return responseContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AJAX POST isteği başarısız: {Url}", url);
                throw;
            }
        }

        public void SetCookie(string name, string value, string domain)
        {
            try
            {
                var cookie = new Cookie(name, value, "/", domain);
                _cookieContainer.Add(cookie);
                _logger.LogDebug("Cookie eklendi: {Name}={Value} for {Domain}", name, value, domain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cookie eklenirken hata: {Name}={Value}", name, value);
            }
        }

        public string? GetCookie(string name)
        {
            try
            {
                var uri = new Uri(_settings.Login.LoginUrl);
                var cookies = _cookieContainer.GetCookies(uri);
                var cookie = cookies[name];
                return cookie?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cookie alınırken hata: {Name}", name);
                return null;
            }
        }

        public void ClearCookies()
        {
            try
            {
                // CookieContainer'ı temizlemenin doğrudan bir yolu yok, yeni bir tane oluşturmak gerekiyor
                var uri = new Uri(_settings.Login.LoginUrl);
                foreach (Cookie cookie in _cookieContainer.GetCookies(uri))
                {
                    cookie.Expired = true;
                }
                _logger.LogDebug("Tüm cookie'ler temizlendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cookie'ler temizlenirken hata");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
