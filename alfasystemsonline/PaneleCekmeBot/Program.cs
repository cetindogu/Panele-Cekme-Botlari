using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaneleCekmeBot.Models;
using PaneleCekmeBot.Services;
using Serilog;
using Serilog.Events;

namespace PaneleCekmeBot
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            // Serilog konfigürasyonu
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/panele-cekme-bot-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10_000_000,
                    rollOnFileSizeLimit: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Console.WriteLine("🤖 Panele Çekme Bot v1.0");
            Console.WriteLine("========================");

            try
            {
                Log.Information("🚀 Uygulama başlatılıyor...");
                Log.Information("📁 Log dosyaları: logs/ klasörüne kaydediliyor");

                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "💥 Uygulama kritik hata ile sonlandı");
                Console.WriteLine($"💥 Kritik hata: {ex.Message}");
                Console.WriteLine("Detaylar için logs/ klasöründeki log dosyalarını kontrol edin.");
                Environment.Exit(1);
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Konfigürasyon
                    services.Configure<AppSettings>(context.Configuration);

                    // Servisler
                    services.AddSingleton<IHttpClientService, HttpClientService>();
                    services.AddSingleton<ILoginService, LoginService>();
                    services.AddSingleton<ICekimMonitoringService, CekimMonitoringService>();
                    services.AddSingleton<IPaneleCekmeService, PaneleCekmeService>();
                    services.AddSingleton<IAyarOnaylamaService, AyarOnaylamaService>();
                    services.AddSingleton<IIstatistikService, IstatistikService>();

                    // Worker Service
                    services.AddHostedService<PaneleCekmeBotWorker>();
                })
                .UseSerilog()
                .UseConsoleLifetime();
    }
}
