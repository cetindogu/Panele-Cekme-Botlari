using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TRONPANELE_CEKME.Services
{
    public interface IHttpClientService
    {
        Task<string> GetAsync(string url, bool isAjax = false);
        Task<string> PostAjaxAsync(string url, Dictionary<string, string> data, Dictionary<string, string>? headers = null);
        Task<Stream> PostAjaxStreamAsync(string url, Dictionary<string, string> data, Dictionary<string, string>? headers = null);
        string? GetCookie(string name);
    }

    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;
        private readonly ILogger<HttpClientService> _logger;
        private readonly IStatisticsService _stats;

        public HttpClientService(ILogger<HttpClientService> logger, IStatisticsService stats)
        {
            _logger = logger;
            _stats = stats;
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                AllowAutoRedirect = false, // Handle redirects manually to see what's happening
                UseCookies = true,
                MaxConnectionsPerServer = 100, // Connection pool limitini artır
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate // GZip desteği ekle
            };

            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Zaman aşımını düşük tut
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01"); // Varsayılan olarak JSON bekle
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
            _httpClient.DefaultRequestHeaders.ConnectionClose = false; // Keep-alive'ı zorla
        }

        public async Task<string> GetAsync(string url, bool isAjax = false)
        {
            _stats.IncrementRequest();
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (isAjax)
                {
                    request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    request.Headers.Remove("Accept");
                    request.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                }

                var response = await _httpClient.SendAsync(request);
                
                if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.MovedPermanently || response.StatusCode == HttpStatusCode.SeeOther || response.StatusCode == HttpStatusCode.TemporaryRedirect)
                {
                    var location = response.Headers.Location;
                    if (location != null)
                    {
                        var redirectUrl = location.IsAbsoluteUri ? location.ToString() : new Uri(new Uri(url), location).ToString();
                        _logger.LogInformation("↪️ Redirecting to: {Url}", redirectUrl);
                        return await GetAsync(redirectUrl, isAjax);
                    }
                }
                
                // Allow 401 for login page just in case, but usually we expect success
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    response.EnsureSuccessStatusCode();
                }
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET isteği sırasında hata oluştu: {Url}", url);
                throw;
            }
        }

        public async Task<string> PostAjaxAsync(string url, Dictionary<string, string> data, Dictionary<string, string>? headers = null)
        {
            _stats.IncrementRequest();
            try
            {
                var content = new FormUrlEncodedContent(data);
                
                // Add AJAX header for this request
                 var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                 request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                 
                 // Add extra headers if provided
                 if (headers != null)
                 {
                     foreach (var header in headers)
                     {
                         if (request.Headers.Contains(header.Key)) request.Headers.Remove(header.Key);
                         request.Headers.Add(header.Key, header.Value);
                     }
                 }
                 
                 var response = await _httpClient.SendAsync(request);
                
                if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.MovedPermanently || response.StatusCode == HttpStatusCode.SeeOther || response.StatusCode == HttpStatusCode.TemporaryRedirect)
                {
                    var location = response.Headers.Location;
                    if (location != null)
                    {
                        var redirectUrl = location.IsAbsoluteUri ? location.ToString() : new Uri(new Uri(url), location).ToString();
                        _logger.LogInformation("↪️ POST Redirecting to: {Url}", redirectUrl);
                        // Usually after POST, we follow redirect with GET
                        return await GetAsync(redirectUrl);
                    }
                }
                
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST AJAX isteği sırasında hata oluştu: {Url}", url);
                throw;
            }
        }

        public async Task<Stream> PostAjaxStreamAsync(string url, Dictionary<string, string> data, Dictionary<string, string>? headers = null)
        {
            _stats.IncrementRequest();
            var content = new FormUrlEncodedContent(data);
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (request.Headers.Contains(header.Key)) request.Headers.Remove(header.Key);
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            // HttpCompletionOption.ResponseHeadersRead: Gövdenin tamamını beklemeden stream'i açar
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        public string? GetCookie(string name)
        {
            var cookies = _cookieContainer.GetAllCookies();
            return cookies.FirstOrDefault(c => c.Name == name)?.Value;
        }
    }
}
