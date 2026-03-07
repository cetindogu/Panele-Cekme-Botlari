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
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 1. Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. Configure Serilog - Use code-based sink configuration for better trimming support
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration) // Still read levels and other settings
                .WriteTo.Console() // Explicitly call to ensure it's not trimmed
                .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/log-.txt"), 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
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
                        services.AddSingleton<ICredentialProvider, ObfuscatedCredentialProvider>();
                        services.AddSingleton<IHttpClientService, HttpClientService>();
                        services.AddSingleton<ILoginService, LoginService>();
                        services.AddSingleton<IWithdrawalMonitorService, WithdrawalMonitorService>();
                        services.AddHostedService<WithdrawalMonitorService>(sp => 
                            (WithdrawalMonitorService)sp.GetRequiredService<IWithdrawalMonitorService>());
                    })
                    .Build();

                // 4. Run the application
                var credentialProvider = host.Services.GetRequiredService<ICredentialProvider>();

                // Kullanıcı adını renkli yazdır
                Console.Write("👤 Kullanıcı: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(credentialProvider.GetUsername());
                Console.ResetColor();

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Uygulama çalışırken beklenmedik bir hata oluştu");
            }
            finally
            {
                Log.Information("👋 Uygulama sonlandırıldı.");
                Log.CloseAndFlush();
            }
        }
    }
}
