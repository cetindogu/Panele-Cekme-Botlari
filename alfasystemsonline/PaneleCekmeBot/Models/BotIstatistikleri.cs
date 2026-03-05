namespace PaneleCekmeBot.Models
{
    public class BotIstatistikleri
    {
        public DateTime BaslangicZamani { get; set; } = DateTime.Now;
        public DateTime SonResetZamani { get; set; } = DateTime.Now;
        
        // Toplam görülen kayıtlar
        public int ToplamGorulenKayitSayisi { get; set; } = 0;
        public decimal ToplamGorulenTutar { get; set; } = 0;
        
        // Tutar filtresine uyan kayıtlar
        public int FiltreUyanKayitSayisi { get; set; } = 0;
        public decimal FiltreUyanToplamTutar { get; set; } = 0;
        
        // Başarıyla çekilen kayıtlar
        public int BasariliCekilenKayitSayisi { get; set; } = 0;
        public decimal BasariliCekilenToplamTutar { get; set; } = 0;
        
        // Başarısız çekimler
        public int BasarisizCekimSayisi { get; set; } = 0;
        
        // Limit kontrolleri
        public bool KayitSayisiLimitiAsildi { get; set; } = false;
        public bool ToplamTutarLimitiAsildi { get; set; } = false;
        
        public void Reset()
        {
            SonResetZamani = DateTime.Now;
            ToplamGorulenKayitSayisi = 0;
            ToplamGorulenTutar = 0;
            FiltreUyanKayitSayisi = 0;
            FiltreUyanToplamTutar = 0;
            BasariliCekilenKayitSayisi = 0;
            BasariliCekilenToplamTutar = 0;
            BasarisizCekimSayisi = 0;
            KayitSayisiLimitiAsildi = false;
            ToplamTutarLimitiAsildi = false;
        }
        
        public bool GunlukResetGerekliMi()
        {
            return SonResetZamani.Date < DateTime.Now.Date;
        }
        
        public string GetOzet()
        {
            var calismaSuresi = DateTime.Now - BaslangicZamani;
            var gunlukSure = DateTime.Now - SonResetZamani;
            
            return $@"
📊 BOT İSTATİSTİKLERİ
==================
🕐 Çalışma Süresi: {calismaSuresi:dd\.hh\:mm\:ss}
📅 Günlük Süre: {gunlukSure:hh\:mm\:ss}

📈 KAYIT İSTATİSTİKLERİ
👁️ Toplam Görülen: {ToplamGorulenKayitSayisi:N0} kayıt ({ToplamGorulenTutar:N0} TL)
✅ Filtre Geçen: {FiltreUyanKayitSayisi:N0} kayıt ({FiltreUyanToplamTutar:N0} TL)
🎯 Başarılı Çekilen: {BasariliCekilenKayitSayisi:N0} kayıt ({BasariliCekilenToplamTutar:N0} TL)
❌ Başarısız: {BasarisizCekimSayisi:N0} kayıt

📊 BAŞARI ORANLARI
Filtre Başarı: %{(ToplamGorulenKayitSayisi > 0 ? (FiltreUyanKayitSayisi * 100.0 / ToplamGorulenKayitSayisi) : 0):F1}
Çekim Başarı: %{(FiltreUyanKayitSayisi > 0 ? (BasariliCekilenKayitSayisi * 100.0 / FiltreUyanKayitSayisi) : 0):F1}
Genel Başarı: %{(ToplamGorulenKayitSayisi > 0 ? (BasariliCekilenKayitSayisi * 100.0 / ToplamGorulenKayitSayisi) : 0):F1}";
        }
    }
}
