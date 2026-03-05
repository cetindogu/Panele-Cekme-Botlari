using System.Text;

namespace TRONPANELE_CEKME.Services
{
    /// <summary>
    /// Kimlik bilgilerini sağlayan servis arayüzü.
    /// SOLID - Interface Segregation & Dependency Inversion prensiplerine uygundur.
    /// </summary>
    public interface ICredentialProvider
    {
        string GetUsername();
    }

    /// <summary>
    /// Kullanıcı adını kod içerisinde gizlenmiş (obfuscated) şekilde saklayan implementasyon.
    /// </summary>
    public class ObfuscatedCredentialProvider : ICredentialProvider
    {
        // Kodlanmış kullanıcı adı.
        // Decompile edildiğinde düz metin olarak arama yapıldığında bulunamaz.
        private static readonly string _obfuscatedUsername = "NzQnOjsVMzQmIXs2Ojg=";
        private static readonly byte _key = 0x55;

        public string GetUsername()
        {
            try
            {
                byte[] data = Convert.FromBase64String(_obfuscatedUsername);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)(data[i] ^ _key);
                }
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception)
            {
                // Hata durumunda boş string dönüyoruz, loglama ana serviste yapılacak.
                return string.Empty;
            }
        }
    }
}
