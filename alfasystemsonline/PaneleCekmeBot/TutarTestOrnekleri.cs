using PaneleCekmeBot.Models;

namespace PaneleCekmeBot
{
    /// <summary>
    /// Tutar parse fonksiyonunu test etmek için örnek sınıf
    /// Bu dosya sadece test amaçlıdır, ana programa dahil değildir
    /// </summary>
    public static class TutarTestOrnekleri
    {
        public static void TestTutarParsing()
        {
            Console.WriteLine("🧪 Tutar Parse Test Örnekleri");
            Console.WriteLine("================================");

            var testCases = new[]
            {
                // Türkiye formatı örnekleri
                "46,700 TL",           // Beklenen: 46700
                "46,700.50 TL",        // Beklenen: 46700.50
                "₺46,700",             // Beklenen: 46700
                "46700 TL",            // Beklenen: 46700
                "1,234,567 TL",        // Beklenen: 1234567
                "1,234,567.89 TL",     // Beklenen: 1234567.89
                "25,000 TL",           // Beklenen: 25000
                "10,500.25 TL",        // Beklenen: 10500.25
                
                // Edge cases
                "5,500 TL",            // Beklenen: 5500 (filtre altında)
                "100,000 TL",          // Beklenen: 100000
                "999,999.99 TL",       // Beklenen: 999999.99
                
                // Geçersiz formatlar
                "ABC TL",              // Beklenen: 0
                "TL",                  // Beklenen: 0
                "",                    // Beklenen: 0
                "₺",                   // Beklenen: 0
                "46,700,50 TL",        // Beklenen: 46700 (yanlış format)
                
                // Farklı formatlar
                "46700₺",              // Beklenen: 46700
                " 46,700 TL ",         // Beklenen: 46700 (boşluklu)
                "46.700 TL",           // Beklenen: 46700 (nokta binlik ayırıcı)
            };

            foreach (var testCase in testCases)
            {
                var cekimTalebi = new CekimTalebi { Tutar = testCase };
                var result = cekimTalebi.GetTutarAsDecimal();
                var isValid = cekimTalebi.IsValidAmount();
                
                Console.WriteLine($"Input: '{testCase}' → Output: {result} (Valid: {isValid})");
                
                // 10,000 TL filtresi kontrolü
                if (result >= 10000)
                {
                    Console.WriteLine($"  ✅ Filtre geçti (≥10,000 TL)");
                }
                else if (result > 0)
                {
                    Console.WriteLine($"  ❌ Filtre geçmedi (<10,000 TL)");
                }
                else
                {
                    Console.WriteLine($"  ⚠️ Geçersiz tutar");
                }
                Console.WriteLine();
            }
        }

        public static void TestFiltreSenaryolari()
        {
            Console.WriteLine("🎯 Filtre Senaryoları");
            Console.WriteLine("=====================");

            var ornekCekimler = new[]
            {
                new CekimTalebi { Id = "1", Isim = "Test User 1", Tutar = "46,700 TL" },
                new CekimTalebi { Id = "2", Isim = "Test User 2", Tutar = "5,500 TL" },
                new CekimTalebi { Id = "3", Isim = "Test User 3", Tutar = "25,000 TL" },
                new CekimTalebi { Id = "4", Isim = "Test User 4", Tutar = "100,000 TL" },
                new CekimTalebi { Id = "5", Isim = "Test User 5", Tutar = "ABC TL" },
                new CekimTalebi { Id = "6", Isim = "Test User 6", Tutar = "15,750.50 TL" },
            };

            // 10,000 TL minimum filtresi
            var minTutar = 10000m;
            var maxTutar = 999999m;

            Console.WriteLine($"Filtre: {minTutar:N0} - {maxTutar:N0} TL");
            Console.WriteLine();

            foreach (var cekim in ornekCekimler)
            {
                var tutar = cekim.GetTutarAsDecimal();
                var gecerli = tutar >= minTutar && tutar <= maxTutar && cekim.IsValidAmount();

                Console.WriteLine($"ID: {cekim.Id}");
                Console.WriteLine($"  İsim: {cekim.Isim}");
                Console.WriteLine($"  Tutar: {cekim.Tutar} → {tutar:N2} TL");
                Console.WriteLine($"  Sonuç: {(gecerli ? "✅ İşlenecek" : "❌ Filtrelendi")}");
                Console.WriteLine();
            }
        }
    }
}
