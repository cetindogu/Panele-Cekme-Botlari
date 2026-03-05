using Newtonsoft.Json;

namespace PaneleCekmeBot.Models
{
    public class CekimTalebi
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("isim")]
        public string Isim { get; set; } = string.Empty;

        [JsonProperty("banka")]
        public string Banka { get; set; } = string.Empty;

        [JsonProperty("tutar")]
        public string Tutar { get; set; } = string.Empty;

        [JsonProperty("tarih")]
        public string Tarih { get; set; } = string.Empty;

        public decimal GetTutarAsDecimal()
        {
            if (string.IsNullOrEmpty(Tutar))
                return 0;

            try
            {
                // Tutar string'ini temizle
                var cleanTutar = Tutar
                    .Replace("TL", "")
                    .Replace("₺", "")
                    .Replace(" ", "")
                    .Trim();

                // Türkiye formatı: 46,700 TL veya 46,700.50 TL
                // Binlik ayırıcı: virgül (,)
                // Ondalık ayırıcı: nokta (.)

                // Eğer hem virgül hem nokta varsa (örn: 46,700.50)
                if (cleanTutar.Contains(",") && cleanTutar.Contains("."))
                {
                    // Son noktadan sonra en fazla 2 karakter varsa ondalık ayırıcı
                    var lastDotIndex = cleanTutar.LastIndexOf('.');
                    if (cleanTutar.Length - lastDotIndex <= 3)
                    {
                        // Virgülleri kaldır (binlik ayırıcı), noktayı ondalık ayırıcı olarak bırak
                        cleanTutar = cleanTutar.Replace(",", "");
                    }
                    else
                    {
                        // Nokta binlik ayırıcı, virgül ondalık ayırıcı
                        cleanTutar = cleanTutar.Replace(".", "").Replace(",", ".");
                    }
                }
                // Sadece virgül varsa (örn: 46,700)
                else if (cleanTutar.Contains(",") && !cleanTutar.Contains("."))
                {
                    // Son virgülden sonra en fazla 2 karakter varsa ondalık ayırıcı
                    var lastCommaIndex = cleanTutar.LastIndexOf(',');
                    if (cleanTutar.Length - lastCommaIndex <= 3 && cleanTutar.Length - lastCommaIndex > 1)
                    {
                        // Virgül ondalık ayırıcı
                        cleanTutar = cleanTutar.Replace(",", ".");
                    }
                    else
                    {
                        // Virgül binlik ayırıcı
                        cleanTutar = cleanTutar.Replace(",", "");
                    }
                }

                if (decimal.TryParse(cleanTutar, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                {
                    return result;
                }
            }
            catch
            {
                // Parse hatası durumunda 0 döndür
            }

            return 0;
        }

        public bool IsValidAmount()
        {
            return GetTutarAsDecimal() > 0;
        }

        public override string ToString()
        {
            return $"ID: {Id}, Yöntem: {Isim}, Banka: {Banka}, Tutar: {Tutar}, Tarih: {Tarih}";
        }
    }
}
