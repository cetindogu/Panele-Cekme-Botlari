namespace PaneleCekmeBot.Models
{
    public class AppSettings
    {
        public LoginSettings Login { get; set; } = new();
        public BotSettings Bot { get; set; } = new();
        public ProxySettings? Proxy { get; set; }
    }

    public class LoginSettings
    {
        public string Password { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = "https://alfasystemsonline.com/panelx/";
        public string ListeleUrl { get; set; } = "https://alfasystemsonline.com/panelx/ajax/listele_cekim_havuz.php";
        public string PaneleCekUrl { get; set; } = "https://alfasystemsonline.com/panelx/ajax/panele_cek_islem.php";
    }

    public class BotSettings
    {
        public int PollingIntervalMs { get; set; } = 500;
        public int RequestTimeoutMs { get; set; } = 10000;
        public int MaxRetryCount { get; set; } = 3;
        public bool EnableDetailedLogging { get; set; } = true;
        public TutarFiltresi TutarFiltre { get; set; } = new();
        public CekimLimitleri CekimLimitleri { get; set; } = new();
    }

    public class TutarFiltresi
    {
        public bool Enabled { get; set; } = false;
        public decimal MinTutar { get; set; } = 0;
        public decimal MaxTutar { get; set; } = 999999;
        public bool IgnoreInvalidAmounts { get; set; } = true;
    }

    public class CekimLimitleri
    {
        public int? MaxKayitSayisi { get; set; } = null; // null = sınırsız, 0 = sadece izleme modu
        public decimal? MaxToplamTutar { get; set; } = null; // null = sınırsız
        public bool ResetDaily { get; set; } = true; // Günlük reset

        public bool SadeceIzlemeModu => MaxKayitSayisi == 0;
    }

    public class ProxySettings
    {
        public bool Enabled { get; set; } = false;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
