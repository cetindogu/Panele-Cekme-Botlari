using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TRONPANELE_CEKME.Models;
using TRONPANELE_CEKME.Services;

namespace TRONPANELE_CEKME
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            try
            {
                Log.Information("🚀 TRONPANELE_CEKME Başlatılıyor...");

                // 3. Configure DI container
                var host = Host.CreateDefaultBuilder(args)
                    .UseSerilog()
                    .ConfigureServices((context, services) =>
                    {
                        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
                        services.AddSingleton<IHttpClientService, HttpClientService>();
                        services.AddSingleton<ILoginService, LoginService>();
                        services.AddSingleton<IWithdrawalMonitorService, WithdrawalMonitorService>();
                    })
                    .Build();

                // 4. Run the application
                using var scope = host.Services.CreateScope();
                var loginService = scope.ServiceProvider.GetRequiredService<ILoginService>();
                var monitorService = scope.ServiceProvider.GetRequiredService<IWithdrawalMonitorService>();

                // 5. Perform Login
                if (await loginService.LoginAsync())
                {
                    Log.Information("✅ Giriş başarılı. İzleme başlatılıyor...");
                    
                    var cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (s, e) =>
                    {
                        Log.Information("🛑 Uygulama kapatılıyor...");
                        cts.Cancel();
                        e.Cancel = true;
                    };

                    await monitorService.StartMonitoringAsync(cts.Token);
                }
                else
                {
                    Log.Fatal("❌ Giriş yapılamadı! Uygulama sonlandırılıyor.");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Uygulama çalışırken beklenmedik bir hata oluştu");
            }
            finally
            {
                Log.Information("👋 Uygulama sonlandırıldı. Herhangi bir tuşa basarak çıkın.");
                Log.CloseAndFlush();
                if (!Console.IsInputRedirected)
                {
                    Console.ReadKey();
                }
            }
        }
    }
}
