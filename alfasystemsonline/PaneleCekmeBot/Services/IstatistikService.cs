using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaneleCekmeBot.Models;

namespace PaneleCekmeBot.Services
{
    public interface IIstatistikService
    {
        void KayitGoruldu(List<CekimTalebi> talepler);
        void FiltreGecti(List<CekimTalebi> talepler);
        void CekimBasarili(CekimTalebi talep);
        void CekimBasarisiz(CekimTalebi talep);
        void IstatistikleriLogla();
        bool LimitKontrolEt();
        void GunlukResetKontrolEt();
        BotIstatistikleri GetIstatistikler();
    }

    public class IstatistikService : IIstatistikService
    {
        private readonly ILogger<IstatistikService> _logger;
        private readonly AppSettings _settings;
        private readonly BotIstatistikleri _istatistikler = new();

        public IstatistikService(
            ILogger<IstatistikService> logger,
            IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public void KayitGoruldu(List<CekimTalebi> talepler)
        {
            if (!talepler?.Any() == true) return;

            _istatistikler!.ToplamGorulenKayitSayisi += talepler!.Count;
            
            foreach (var talep in talepler)
            {
                var tutar = talep.GetTutarAsDecimal();
                _istatistikler.ToplamGorulenTutar += tutar;
            }

            _logger.LogDebug("📊 Toplam görülen kayıt güncellendi: +{Count} kayıt", talepler.Count);
        }

        public void FiltreGecti(List<CekimTalebi> talepler)
        {
            if (!talepler?.Any() == true) return;

            _istatistikler.FiltreUyanKayitSayisi += talepler!.Count;
            
            foreach (var talep in talepler)
            {
                var tutar = talep.GetTutarAsDecimal();
                _istatistikler.FiltreUyanToplamTutar += tutar;
            }

            _logger.LogDebug("✅ Filtre geçen kayıt güncellendi: +{Count} kayıt", talepler.Count);
        }

        public void CekimBasarili(CekimTalebi talep)
        {
            _istatistikler.BasariliCekilenKayitSayisi++;
            var tutar = talep.GetTutarAsDecimal();
            _istatistikler.BasariliCekilenToplamTutar += tutar;

            _logger.LogDebug("🎯 Başarılı çekim güncellendi: +1 kayıt, +{Tutar:N0} TL", tutar);
        }

        public void CekimBasarisiz(CekimTalebi talep)
        {
            _istatistikler.BasarisizCekimSayisi++;
            _logger.LogDebug("❌ Başarısız çekim güncellendi: +1 kayıt");
        }

        public void IstatistikleriLogla()
        {
            var ozet = _istatistikler.GetOzet();
            
            // Konsola yazdır
            Console.WriteLine(ozet);
            
            // Loga yazdır
            _logger.LogInformation("📊 BOT İSTATİSTİKLERİ RAPORU");
            _logger.LogInformation("🕐 Çalışma Süresi: {CalismaSuresi}", DateTime.Now - _istatistikler.BaslangicZamani);
            _logger.LogInformation("📅 Günlük Süre: {GunlukSure}", DateTime.Now - _istatistikler.SonResetZamani);
            _logger.LogInformation("👁️ Toplam Görülen: {ToplamGorullen} kayıt ({ToplamGorulenTutar:N0} TL)", 
                _istatistikler.ToplamGorulenKayitSayisi, _istatistikler.ToplamGorulenTutar);
            _logger.LogInformation("✅ Filtre Geçen: {FiltreGecen} kayıt ({FiltreGecentTutar:N0} TL)", 
                _istatistikler.FiltreUyanKayitSayisi, _istatistikler.FiltreUyanToplamTutar);
            _logger.LogInformation("🎯 Başarılı Çekilen: {BasariliCekilen} kayıt ({BasariliCekilenTutar:N0} TL)", 
                _istatistikler.BasariliCekilenKayitSayisi, _istatistikler.BasariliCekilenToplamTutar);
            _logger.LogInformation("❌ Başarısız: {Basarisiz} kayıt", _istatistikler.BasarisizCekimSayisi);
            
            // Başarı oranları
            var filtreBasari = _istatistikler.ToplamGorulenKayitSayisi > 0 ? 
                (_istatistikler.FiltreUyanKayitSayisi * 100.0 / _istatistikler.ToplamGorulenKayitSayisi) : 0;
            var cekimBasari = _istatistikler.FiltreUyanKayitSayisi > 0 ? 
                (_istatistikler.BasariliCekilenKayitSayisi * 100.0 / _istatistikler.FiltreUyanKayitSayisi) : 0;
            var genelBasari = _istatistikler.ToplamGorulenKayitSayisi > 0 ? 
                (_istatistikler.BasariliCekilenKayitSayisi * 100.0 / _istatistikler.ToplamGorulenKayitSayisi) : 0;
                
            _logger.LogInformation("📈 Filtre Başarı Oranı: %{FiltreBasari:F1}", filtreBasari);
            _logger.LogInformation("📈 Çekim Başarı Oranı: %{CekimBasari:F1}", cekimBasari);
            _logger.LogInformation("📈 Genel Başarı Oranı: %{GenelBasari:F1}", genelBasari);
        }

        public bool LimitKontrolEt()
        {
            // Sadece izleme modunda limit kontrolü yapma
            if (_settings.Bot.CekimLimitleri.SadeceIzlemeModu)
            {
                return false;
            }

            var limitAsildi = false;

            // Kayıt sayısı limiti kontrolü
            if (_settings.Bot.CekimLimitleri.MaxKayitSayisi.HasValue && _settings.Bot.CekimLimitleri.MaxKayitSayisi.Value > 0)
            {
                if (_istatistikler.BasariliCekilenKayitSayisi >= _settings.Bot.CekimLimitleri.MaxKayitSayisi.Value)
                {
                    if (!_istatistikler.KayitSayisiLimitiAsildi)
                    {
                        _istatistikler.KayitSayisiLimitiAsildi = true;
                        _logger.LogWarning("🚫 KAYIT SAYISI LİMİTİ AŞILDI! Limit: {Limit}, Mevcut: {Mevcut}",
                            _settings.Bot.CekimLimitleri.MaxKayitSayisi.Value, _istatistikler.BasariliCekilenKayitSayisi);
                        Console.WriteLine($"🚫 UYARI: Kayıt sayısı limiti aşıldı! ({_istatistikler.BasariliCekilenKayitSayisi}/{_settings.Bot.CekimLimitleri.MaxKayitSayisi.Value})");
                    }
                    limitAsildi = true;
                }
            }

            // Toplam tutar limiti kontrolü
            if (_settings.Bot.CekimLimitleri.MaxToplamTutar.HasValue)
            {
                if (_istatistikler.BasariliCekilenToplamTutar >= _settings.Bot.CekimLimitleri.MaxToplamTutar.Value)
                {
                    if (!_istatistikler.ToplamTutarLimitiAsildi)
                    {
                        _istatistikler.ToplamTutarLimitiAsildi = true;
                        _logger.LogWarning("🚫 TOPLAM TUTAR LİMİTİ AŞILDI! Limit: {Limit:N0} TL, Mevcut: {Mevcut:N0} TL", 
                            _settings.Bot.CekimLimitleri.MaxToplamTutar.Value, _istatistikler.BasariliCekilenToplamTutar);
                        Console.WriteLine($"🚫 UYARI: Toplam tutar limiti aşıldı! ({_istatistikler.BasariliCekilenToplamTutar:N0}/{_settings.Bot.CekimLimitleri.MaxToplamTutar.Value:N0} TL)");
                    }
                    limitAsildi = true;
                }
            }

            return limitAsildi;
        }

        public void GunlukResetKontrolEt()
        {
            if (_settings.Bot.CekimLimitleri.ResetDaily && _istatistikler.GunlukResetGerekliMi())
            {
                _logger.LogInformation("🔄 Günlük reset yapılıyor...");
                Console.WriteLine("🔄 Günlük istatistikler sıfırlanıyor...");
                
                // Eski istatistikleri logla
                IstatistikleriLogla();
                
                // Reset yap
                _istatistikler.Reset();
                
                _logger.LogInformation("✅ Günlük reset tamamlandı");
                Console.WriteLine("✅ Günlük reset tamamlandı");
            }
        }

        public BotIstatistikleri GetIstatistikler()
        {
            return _istatistikler;
        }
    }
}
