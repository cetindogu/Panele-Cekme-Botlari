# Panele Çekme Bot - Kullanım Kılavuzu

## 🚀 Hızlı Başlangıç

### 1. Ayarları Yapılandırın
`appsettings.json` dosyasını açın ve aşağıdaki bilgileri güncelleyin:

```json
{
  "Login": {
    "Username": "GERÇEK_KULLANICI_ADINIZ",
    "Password": "GERÇEK_ŞİFRENİZ"
  }
}
```

### 2. Uygulamayı Çalıştırın
```bash
dotnet run
```

### 3. Logları İzleyin
Uygulama çalışırken aşağıdaki gibi loglar göreceksiniz:

```
🤖 Panele Çekme Bot v1.0
========================
2024-01-15 10:30:15.123 [INF] 🤖 Panele Çekme Bot başlatılıyor...
2024-01-15 10:30:16.456 [INF] 🔐 Giriş yapılıyor... (Deneme: 1/3)
2024-01-15 10:30:17.789 [INF] ✅ Giriş başarılı!
2024-01-15 10:30:18.012 [INF] 🚀 Bot başarıyla başlatıldı ve monitoring başlıyor...
```

## ⚙️ Gelişmiş Ayarlar

### Sadece İzleme Modu (Panele Çekme Yapmadan Test)
```json
{
  "Bot": {
    "CekimLimitleri": {
      "MaxKayitSayisi": 0,  // 0 = Sadece izleme modu
      "MaxToplamTutar": null,
      "ResetDaily": true
    }
  }
}
```

### Tutar Filtresi Ayarlama
```json
{
  "Bot": {
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 10000,   // Minimum 10,000 TL
      "MaxTutar": 50000,   // Maksimum 50,000 TL
      "IgnoreInvalidAmounts": true  // Geçersiz tutarları göz ardı et
    }
  }
}
```

### Polling Aralığını Ayarlama
```json
{
  "Bot": {
    "PollingIntervalMs": 1000  // 1 saniye (dikkatli kullanın!)
  }
}
```

### Proxy Kullanımı
```json
{
  "Proxy": {
    "Enabled": true,
    "Host": "proxy.example.com",
    "Port": 8080,
    "Username": "proxy_user",
    "Password": "proxy_pass"
  }
}
```

### Loglama Seviyesi
```json
{
  "Logging": {
    "LogLevel": {
      "PaneleCekmeBot": "Information"  // Debug, Information, Warning, Error
    }
  }
}
```

## 🎯 Beklenen Davranış

### Sadece İzleme Modu (MaxKayitSayisi = 0)
```
[2024-08-07 00:30:15.123 INF] 🤖 Panele Çekme Bot başlatılıyor...
[2024-08-07 00:30:16.456 INF] 👁️ Mod: SADECE İZLEME MODU (Panele çekme yapılmayacak)
[2024-08-07 00:30:17.789 INF] ✅ Giriş başarılı!
[2024-08-07 00:30:18.012 INF] 🎯 Yeni çekim talepleri tespit edildi: 3 adet
[2024-08-07 00:30:19.345 INF] 👁️ SADECE İZLEME MODU - Kayıtlar detayları ile loglanıyor:
[2024-08-07 00:30:20.678 INF] 📋 Yakalanan Kayıt: ID=12345, İsim=Test User, Tutar=46,700 TL (46700 TL), Tarih=2024-08-07
[2024-08-07 00:30:21.901 INF] 📋 Yakalanan Kayıt: ID=12346, İsim=Test User2, Tutar=25,000 TL (25000 TL), Tarih=2024-08-07
[2024-08-07 00:30:22.234 INF] 📋 Yakalanan Kayıt: ID=12347, İsim=Test User3, Tutar=15,500 TL (15500 TL), Tarih=2024-08-07
[2024-08-07 00:30:22.567 INF] 👁️ İzleme modu aktif - Panele çekme işlemi yapılmadı
```

### Başarılı Çekim (Normal Mod)
```
[2024-08-07 00:30:15.123 INF] 🤖 Panele Çekme Bot başlatılıyor...
[2024-08-07 00:30:16.456 INF] 📁 Log dosyaları: logs/ klasörüne kaydediliyor
[2024-08-07 00:30:17.789 INF] 🔐 Giriş işlemi başlatılıyor...
[2024-08-07 00:30:18.012 INF] ✅ Giriş başarılı!
[2024-08-07 00:30:19.345 INF] 🍪 PHPSESSID cookie alındı: ABC123XYZ
[2024-08-07 00:30:20.678 INF] 💰 Tutar Filtresi Aktif - Min: 10000 TL, Max: 999999 TL
[2024-08-07 00:30:21.901 INF] 🎯 Yeni çekim talepleri tespit edildi: 2 adet
[2024-08-07 00:30:22.234 INF] ✅ İşlem başarılı! ID: 12345, Tutar: 46,700 TL (46700 TL), Süre: 234ms
[2024-08-07 00:30:22.567 INF] ✅ İşlem başarılı! ID: 12346, Tutar: 25,000 TL (25000 TL), Süre: 189ms
[2024-08-07 00:30:22.890 INF] 📊 Talep işleme tamamlandı. Başarılı: 2/2 (%100.0)
```

### Tutar Filtresi Çalışması
```
[2024-08-07 00:30:23.123 DBG] 🔍 Çekim talepleri kontrol ediliyor...
[2024-08-07 00:30:23.456 DBG] 📡 Çekim listesi endpoint'i çağrılıyor
[2024-08-07 00:30:23.789 DBG] 📨 Çekim listesi yanıtı alındı. Uzunluk: 1234 karakter
[2024-08-07 00:30:24.012 DBG] 🔄 JSON yanıtı parse ediliyor...
[2024-08-07 00:30:24.345 DBG] 📊 Toplam 3 çekim talebi bulundu
[2024-08-07 00:30:24.678 DBG] Tutar filtresi geçti. ID: 12345, Tutar: 46,700 TL (46700), Aralık: 10000-999999
[2024-08-07 00:30:24.901 DBG] Tutar filtresi geçmedi. ID: 12346, Tutar: 5,500 TL (5500), Aralık: 10000-999999
[2024-08-07 00:30:25.234 DBG] Geçersiz tutar göz ardı ediliyor. ID: 12347, Tutar: ABC TL
```

### Log Dosyası İzleme
```
# Windows'ta log dosyasını canlı izleme
Get-Content -Path "logs\panele-cekme-bot-2024-08-07.log" -Wait -Tail 50

# Belirli bir seviyedeki logları filtreleme
Select-String -Path "logs\*.log" -Pattern "\[ERR\]" | Select-Object -Last 10
```

### Başka Biri Tarafından Alınmış
```
[WRN] ⚠️ Çekim başka biri tarafından alınmış. ID: 12347
```

### Session Süresi Dolmuş
```
[WRN] Session geçersiz, yeniden giriş yapılıyor...
[INF] ✅ Giriş başarılı!
```

## 🛠️ Sorun Giderme

### 1. Giriş Başarısız
- Kullanıcı adı ve şifrenizi kontrol edin
- İnternet bağlantınızı kontrol edin
- Site erişilebilir mi kontrol edin

### 2. JSON Parse Hatası
- Site yapısı değişmiş olabilir
- Endpoint URL'lerini kontrol edin

### 3. Çok Fazla İstek Hatası
- PollingIntervalMs değerini artırın (örn: 3000)
- Proxy kullanmayı düşünün

### 4. Session Sürekli Süresi Doluyor
- Çok fazla paralel istek olabilir
- MaxRetryCount değerini azaltın

### 5. Tutar Filtresi Çalışmıyor
- TutarFiltre.Enabled = true olduğundan emin olun
- MinTutar ve MaxTutar değerlerini kontrol edin
- Tutar formatını kontrol edin (TL, ₺ sembolleri otomatik temizlenir)
- Debug loglarını kontrol edin: "Tutar filtresi geçti/geçmedi" mesajları

### 6. Log Dosyaları Oluşmuyor
- logs/ klasörünün var olduğundan emin olun
- Uygulama yazma iznine sahip olduğundan emin olun
- Disk alanının yeterli olduğunu kontrol edin

## 🔧 Performance Optimizasyonu

### Hızlı Çekim İçin (Çok Yüksek Tutar)
```json
{
  "Bot": {
    "PollingIntervalMs": 1000,
    "RequestTimeoutMs": 5000,
    "MaxRetryCount": 1,
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 25000,
      "MaxTutar": 999999
    }
  }
}
```

### Güvenli Kullanım İçin (Yüksek Tutar)
```json
{
  "Bot": {
    "PollingIntervalMs": 3000,
    "RequestTimeoutMs": 15000,
    "MaxRetryCount": 3,
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 10000,
      "MaxTutar": 50000
    }
  }
}
```

## 🚨 Önemli Uyarılar

1. **IP Ban Riski**: Çok sık istek göndermeyin
2. **Kullanıcı Bilgileri**: appsettings.json dosyasını güvenli tutun
3. **Monitoring**: Uygulamayı sürekli izleyin
4. **Backup**: Önemli ayarlarınızı yedekleyin

## 📞 Acil Durum

Uygulama donmuşsa:
1. `Ctrl+C` ile durdurun
2. Logları kontrol edin
3. Ayarları gözden geçirin
4. Yeniden başlatın

## 🎮 Test Modu

Gerçek işlem yapmadan test etmek için:
1. Sahte endpoint URL'leri kullanın
2. Loglama seviyesini Debug yapın
3. PollingIntervalMs'i yükseltin

## 📈 İstatistikler

Uygulama çalışırken şu bilgileri takip edebilirsiniz:
- Toplam tespit edilen çekim sayısı
- Başarılı çekim sayısı
- Ortalama yanıt süresi
- Session yenileme sayısı
