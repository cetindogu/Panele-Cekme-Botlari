using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace PaneleCekmeBot.Models
{
    public class CekimListesiResponse
    {
        [JsonProperty("cekim")]
        public CekimData? Cekim { get; set; }

        [JsonProperty("server_time")]
        public long ServerTime { get; set; }
    }

    public class CekimData
    {
        [JsonProperty("yeni")]
        [JsonConverter(typeof(CekimTalebiArrayConverter))]
        public List<CekimTalebi>? Yeni { get; set; }
    }

    public class CekimTalebiArrayConverter : JsonConverter<List<CekimTalebi>>
    {
        public override List<CekimTalebi> ReadJson(JsonReader reader, Type objectType, List<CekimTalebi>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var result = new List<CekimTalebi>();

            if (reader.TokenType == JsonToken.StartArray)
            {
                var arrays = serializer.Deserialize<object[][]>(reader);
                if (arrays != null)
                {
                    foreach (var array in arrays)
                    {
                        if (array.Length >= 5)
                        {
                            var cekimTalebi = new CekimTalebi
                            {
                                Id = array[0]?.ToString() ?? string.Empty,
                                Isim = array[1]?.ToString() ?? string.Empty, // Yöntem
                                Banka = array[2]?.ToString() ?? string.Empty, // Banka
                                Tutar = array[3]?.ToString() ?? string.Empty, // Tutar
                                Tarih = ExtractTimestampFromHtml(array.Length > 4 ? array[4]?.ToString() : null) // Süre HTML
                            };
                            result.Add(cekimTalebi);
                        }
                    }
                }
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, List<CekimTalebi>? value, JsonSerializer serializer)
        {
            // Write işlemi gerekli değil, sadece read yapıyoruz
            throw new NotImplementedException();
        }

        private static string ExtractTimestampFromHtml(string? htmlString)
        {
            if (string.IsNullOrEmpty(htmlString))
                return string.Empty;

            // <span class='islemTarihi_cekim' value='1754518938'></span> formatından timestamp çıkar
            var match = Regex.Match(htmlString, @"value='(\d+)'");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
