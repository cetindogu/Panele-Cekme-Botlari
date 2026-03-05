namespace TRONPANELE_CEKME.Models
{
    public class AppSettings
    {
        public LoginSettings Login { get; set; } = new();
        public WithdrawalSettings Withdrawals { get; set; } = new();
    }

    public class LoginSettings
    {
        public string BaseUrl { get; set; } = "";
        public string LoginUrl { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class WithdrawalSettings
    {
        public string PageUrl { get; set; } = "";
        public string AjaxUrl { get; set; } = "";
        public string ProcessUrl { get; set; } = "";
        public int PollingIntervalMs { get; set; } = 5000;
        public decimal MinAmount { get; set; } = 10000;
        public decimal MaxAmount { get; set; } = 100000;
        public decimal MaxTotalAmount { get; set; } = 500000;
        public int MaxRecordCount { get; set; } = 50;
        public bool PreviewMode { get; set; } = true;
    }

    public class WithdrawalListResponse
    {
        public List<WithdrawalData> Datas { get; set; } = new();
        public string Message { get; set; } = "";
        public bool Status { get; set; }
    }

    public class WithdrawalData
    {
        public long Id { get; set; }
        public string Amount { get; set; } = "0.00";
        public string Userid { get; set; } = "";
        public string Sendername { get; set; } = "";
        public string Bank { get; set; } = "";
        public string Date { get; set; } = "";
        public int Proc { get; set; } // 0: Beklemede, 1: İşlenmiş
        public string Iban { get; set; } = "";
        public string Site { get; set; } = "";
    }

    public class ProcessResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = "";
    }

    public class WithdrawalItem
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public bool CanProcess { get; set; }
    }
}
