# Panele Çekme Bot

Bu proje, belirli bir web sitesine otomatik giriş yaparak çekim taleplerini sürekli kontrol eden ve yeni talepleri hızlıca "panele çeken" bir C# .NET Core 8.0 uygulamasıdır.

## 🚀 Özellikler

- **Otomatik Login**: Kullanıcı adı, şifre ve dinamik token ile güvenli giriş
- **Sürekli Monitoring**: 7/24 çalışarak yeni çekim taleplerini tespit eder
- **Hızlı İşlem**: Milisaniye seviyesinde hızlı panele çekme işlemi
- **Session Yönetimi**: PHPSESSID cookie'si ile oturum yönetimi
- **Paralel İşlem**: Birden fazla talebi aynı anda işleyebilir
- **Retry Mekanizması**: Başarısız istekleri otomatik olarak tekrar dener
- **Detaylı Loglama**: Hem konsol hem dosya tabanlı sistematik loglama
- **Tutar Filtresi**: Minimum ve maksimum tutar aralığı belirleme
- **Çekim Limitleri**: Kayıt sayısı ve toplam tutar limitleri
- **Gerçek Zamanlı İstatistikler**: Anlık performans ve başarı takibi
- **Ayar Onaylama**: Başlangıçta tüm ayarları onaylama sistemi
- **Dosya Loglama**: Günlük log dosyaları ve otomatik arşivleme
- **Proxy Desteği**: Opsiyonel proxy kullanımı

## 📋 Gereksinimler

- .NET 8.0 SDK
- Windows 10/11 veya Windows Server
- İnternet bağlantısı

## ⚙️ Kurulum

1. **Projeyi klonlayın veya indirin**
2. **Bağımlılıkları yükleyin:**
   ```bash
   dotnet restore
   ```

3. **Ayarları yapılandırın:**
   `appsettings.json` dosyasını düzenleyin:
   ```json
   {
     "Login": {
       "Username": "KULLANICI_ADINIZ",
       "Password": "SIFRENIZ"
     }
   }
   ```

## 🔧 Konfigürasyon

### appsettings.json Ayarları

```json
{
  "Login": {
    "Username": "kullanici_adiniz",
    "Password": "sifreniz",
    "LoginUrl": "https://alfasystemsonline.com/panelx/",
    "ListeleUrl": "https://alfasystemsonline.com/panelx/ajax/listele_cekim_havuz.php",
    "PaneleCekUrl": "https://alfasystemsonline.com/panelx/ajax/panele_cek_islem.php"
  },
  "Bot": {
    "PollingIntervalMs": 500,
    "RequestTimeoutMs": 10000,
    "MaxRetryCount": 3,
    "EnableDetailedLogging": true,
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 100,
      "MaxTutar": 5000,
      "IgnoreInvalidAmounts": true
    }
  },
  "Proxy": {
    "Enabled": false,
    "Host": "proxy.example.com",
    "Port": 8080,
    "Username": "proxy_user",
    "Password": "proxy_pass"
  }
}
```

### Ayar Açıklamaları

- **PollingIntervalMs**: Çekim kontrolü aralığı (milisaniye)
- **RequestTimeoutMs**: HTTP istek timeout süresi
- **MaxRetryCount**: Başarısız istekler için maksimum deneme sayısı
- **EnableDetailedLogging**: Detaylı log çıktısı
- **TutarFiltre.Enabled**: Tutar filtresini aktif/pasif yapar
- **TutarFiltre.MinTutar**: Minimum tutar (TL)
- **TutarFiltre.MaxTutar**: Maksimum tutar (TL)
- **TutarFiltre.IgnoreInvalidAmounts**: Geçersiz tutarları göz ardı et
- **CekimLimitleri.MaxKayitSayisi**: Maksimum çekilebilecek kayıt sayısı (null = sınırsız, 0 = sadece izleme modu)
- **CekimLimitleri.MaxToplamTutar**: Maksimum çekilebilecek toplam tutar (null = sınırsız)
- **CekimLimitleri.ResetDaily**: Günlük limit sıfırlama

## 🚀 Çalıştırma

### 1. Uygulamayı Başlatın
```bash
dotnet run
```

### 2. Ayarları Onaylayın
Uygulama başladığında tüm ayarlarınızı gösterecek ve onayınızı isteyecek:
```
🔧 PANELE ÇEKME BOT AYARLARI
============================

🔐 GİRİŞ AYARLARI:
   👤 Kullanıcı Adı: your_username
   🔑 Şifre: ********

💰 TUTAR FİLTRESİ:
   ✅ Durum: Aktif
   💵 Min Tutar: 10,000 TL
   💰 Max Tutar: 999,999 TL

🎯 ÇEKİM LİMİTLERİ:
   👁️ Mod: SADECE İZLEME MODU (Panele çekme yapılmayacak)
   📊 Max Kayıt Sayısı: 0 (Sadece kayıt yakalama ve loglama)
   💎 Max Toplam Tutar: 500,000 TL
   🔄 Günlük Reset: Aktif

Bu ayarları onaylıyor musunuz? (E/H):
```

### 3. Bot Çalışmasını İzleyin

### Production Build
```bash
dotnet build -c Release
dotnet run -c Release
```

### Windows Service Olarak Çalıştırma
```bash
# Publish
dotnet publish -c Release -o ./publish

# Service olarak kurulum (yönetici yetkisi gerekli)
sc create "PaneleCekmeBot" binPath="C:\path\to\publish\PaneleCekmeBot.exe"
sc start "PaneleCekmeBot"
```

## 📊 Loglama Sistemi

Uygulama hem konsola hem de dosyaya detaylı loglar üretir:

### Log Seviyeleri
- **Debug**: Detaylı debug bilgileri (session kontrolü, HTTP istekleri)
- **Information**: Genel bilgi mesajları (giriş, çekim işlemleri)
- **Warning**: Uyarı mesajları (session süresi dolması, filtre)
- **Error**: Hata mesajları (giriş başarısız, çekim hatası)
- **Fatal**: Kritik hatalar (uygulama çökmesi)

### Log Dosyaları
- **Konum**: `logs/` klasörü
- **Format**: `panele-cekme-bot-YYYY-MM-DD.log`
- **Rotasyon**: Günlük dosyalar
- **Saklama**: 30 gün
- **Boyut Limiti**: 10MB (aşılırsa yeni dosya)

### Log Örnekleri
```
[2024-08-07 00:30:15.123 INF] 🤖 Panele Çekme Bot başlatılıyor...
[2024-08-07 00:30:16.456 INF] 🔐 Giriş işlemi başlatılıyor...
[2024-08-07 00:30:17.789 INF] ✅ Giriş başarılı!
[2024-08-07 00:30:18.012 INF] 🍪 PHPSESSID cookie alındı: ABC123XYZ
[2024-08-07 00:30:19.345 INF] 💰 Tutar Filtresi Aktif - Min: 10000 TL, Max: 999999 TL
[2024-08-07 00:30:20.678 INF] 🎯 Yeni çekim talepleri tespit edildi: 2 adet
[2024-08-07 00:30:21.901 INF] ✅ İşlem başarılı! ID: 12345, Tutar: 46,700 TL (46700 TL), Süre: 234ms
```

### Detaylı Loglama İçeriği
- **Giriş İşlemleri**: Token alma, POST verileri, cookie bilgileri
- **Session Yönetimi**: Session kontrolü, yenileme işlemleri
- **Çekim Monitoring**: JSON yanıtları, parse işlemleri
- **Tutar Filtresi**: Filtre geçen/geçmeyen tutarlar
- **Panele Çekme**: İstek/yanıt detayları, başarı/başarısızlık
- **Hata Yönetimi**: Exception detayları, retry işlemleri

## 🔒 Güvenlik

- **Session Yönetimi**: PHPSESSID cookie'si ile güvenli oturum
- **Token Kontrolü**: Her login'de dinamik token kullanımı
- **Proxy Desteği**: IP gizleme için proxy kullanımı
- **Rate Limiting**: Aşırı istek gönderimini önleme

## ⚠️ Önemli Notlar

1. **Kullanıcı Bilgileri**: `appsettings.json` dosyasındaki kullanıcı bilgilerini mutlaka güncelleyin
2. **Polling Aralığı**: Çok düşük değerler IP ban riskine neden olabilir
3. **Proxy Kullanımı**: Yüksek trafikte proxy kullanımı önerilir
4. **Monitoring**: Uygulamayı sürekli izleyin ve logları kontrol edin

## 🛠️ Geliştirme

### Proje Yapısı
```
PaneleCekmeBot/
├── Models/
│   ├── AppSettings.cs
│   ├── CekimTalebi.cs
│   └── CekimListesiResponse.cs
├── Services/
│   ├── HttpClientService.cs
│   ├── LoginService.cs
│   ├── CekimMonitoringService.cs
│   ├── PaneleCekmeService.cs
│   └── PaneleCekmeBotWorker.cs
├── Program.cs
├── appsettings.json
└── README.md
```

### Kullanılan Teknolojiler
- **.NET 8.0**: Ana framework
- **Microsoft.Extensions.Hosting**: Background service
- **HtmlAgilityPack**: HTML parsing
- **Newtonsoft.Json**: JSON işlemleri
- **Microsoft.Extensions.Logging**: Loglama

## 📞 Destek

Herhangi bir sorun yaşarsanız:
1. Log dosyalarını kontrol edin
2. Ayarları doğrulayın
3. İnternet bağlantısını kontrol edin
4. Proxy ayarlarını kontrol edin (kullanıyorsanız)

## ⚖️ Lisans

Bu proje eğitim amaçlı geliştirilmiştir. Kullanım sorumluluğu kullanıcıya aittir.
