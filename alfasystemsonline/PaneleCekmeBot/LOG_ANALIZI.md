# Log Analizi ve İzleme Kılavuzu

## 📁 Log Dosya Yapısı

### Dosya Konumları
```
PaneleCekmeBot/
├── logs/
│   ├── panele-cekme-bot-2024-08-07.log    # Bugünkü loglar
│   ├── panele-cekme-bot-2024-08-06.log    # Dünkü loglar
│   └── panele-cekme-bot-2024-08-05.log    # Önceki günler
```

### Log Format
```
[YYYY-MM-DD HH:mm:ss.fff LEVEL] SourceContext Message
[2024-08-07 00:30:15.123 INF] PaneleCekmeBot.Services.LoginService 🔐 Giriş işlemi başlatılıyor...
```

## 🔍 Log Seviyelerine Göre Analiz

### 1. DEBUG Logları (Detaylı İzleme)
```bash
# Debug loglarını filtreleme
Select-String -Path "logs\*.log" -Pattern "\[DBG\]" | Select-Object -Last 20

# Örnek debug logları:
[DBG] 🔍 Çekim talepleri kontrol ediliyor...
[DBG] 📡 Çekim listesi endpoint'i çağrılıyor: https://...
[DBG] 📨 Çekim listesi yanıtı alındı. Uzunluk: 1234 karakter
[DBG] 🔄 JSON yanıtı parse ediliyor...
[DBG] 📊 Toplam 5 çekim talebi bulundu
[DBG] Tutar filtresi geçti. ID: 12345, Tutar: 46,700 TL (46700), Aralık: 10000-999999
```

### 2. INFORMATION Logları (Genel Bilgi)
```bash
# Başarılı işlemleri görme
Select-String -Path "logs\*.log" -Pattern "\[INF\].*✅" | Select-Object -Last 10

# Örnek information logları:
[INF] 🤖 Panele Çekme Bot başlatılıyor...
[INF] ✅ Giriş başarılı!
[INF] 🍪 PHPSESSID cookie alındı: ABC123XYZ
[INF] 🎯 Yeni çekim talepleri tespit edildi: 3 adet
[INF] ✅ İşlem başarılı! ID: 12345, Tutar: 46,700 TL (46700 TL), Süre: 234ms
```

### 3. WARNING Logları (Uyarılar)
```bash
# Uyarıları görme
Select-String -Path "logs\*.log" -Pattern "\[WRN\]" | Select-Object -Last 10

# Örnek warning logları:
[WRN] ⏰ Session süresi dolmuş, yeniden giriş gerekiyor
[WRN] ⚠️ Çekim başka biri tarafından alınmış. ID: 12345
[WRN] ❓ Beklenmeyen yanıt alındı. ID: 12346, Yanıt: Hata mesajı
```

### 4. ERROR Logları (Hatalar)
```bash
# Hataları görme
Select-String -Path "logs\*.log" -Pattern "\[ERR\]" | Select-Object -Last 10

# Örnek error logları:
[ERR] ❌ Login sayfasından token alınamadı
[ERR] ❌ Giriş başarısız. Kullanıcı adı veya şifre hatalı olabilir.
[ERR] ❌ Panele çekme başarısız (max retry aşıldı). ID: 12345
```

## 📊 Performans Analizi

### Çekim Süreleri
```bash
# Başarılı çekimlerin sürelerini analiz etme
Select-String -Path "logs\*.log" -Pattern "✅.*Süre: (\d+)ms" | 
    ForEach-Object { $_.Matches[0].Groups[1].Value } | 
    Measure-Object -Average -Minimum -Maximum

# Örnek çıktı:
# Count    : 25
# Average  : 245.6
# Minimum  : 123
# Maximum  : 567
```

### Başarı Oranları
```bash
# Günlük başarı oranını hesaplama
$basarili = (Select-String -Path "logs\panele-cekme-bot-2024-08-07.log" -Pattern "✅ İşlem başarılı").Count
$basarisiz = (Select-String -Path "logs\panele-cekme-bot-2024-08-07.log" -Pattern "❌ İşlem başarısız").Count
$oran = ($basarili / ($basarili + $basarisiz)) * 100
Write-Host "Başarı Oranı: %$($oran.ToString('F1'))"
```

## 🎯 Özel Analiz Senaryoları

### 1. Tutar Filtresi Analizi
```bash
# Filtre geçen/geçmeyen tutarları analiz etme
Select-String -Path "logs\*.log" -Pattern "Tutar filtresi geçti.*\((\d+)\)" | 
    ForEach-Object { $_.Matches[0].Groups[1].Value } | 
    Measure-Object -Average -Minimum -Maximum

# Filtre geçmeyen tutarları görme
Select-String -Path "logs\*.log" -Pattern "Tutar filtresi geçmedi" | Select-Object -Last 10
```

### 2. Session Yönetimi İzleme
```bash
# Session yenileme sıklığını kontrol etme
Select-String -Path "logs\*.log" -Pattern "Session süresi dolmuş" | 
    Select-Object LineNumber, Line

# Cookie bilgilerini izleme
Select-String -Path "logs\*.log" -Pattern "PHPSESSID cookie alındı" | 
    Select-Object -Last 5
```

### 3. Hata Türü Analizi
```bash
# En sık karşılaşılan hataları bulma
Select-String -Path "logs\*.log" -Pattern "\[ERR\]" | 
    Group-Object { ($_.Line -split " ", 4)[3] } | 
    Sort-Object Count -Descending | 
    Select-Object Count, Name -First 5
```

## 📈 Monitoring ve Alerting

### Kritik Durumlar
```bash
# Son 1 saatte çok fazla hata var mı?
$sonSaat = (Get-Date).AddHours(-1)
$hatalar = Select-String -Path "logs\panele-cekme-bot-$(Get-Date -Format 'yyyy-MM-dd').log" -Pattern "\[ERR\]" | 
    Where-Object { [DateTime]::ParseExact(($_.Line -split " ")[0..1] -join " ", "yyyy-MM-dd HH:mm:ss.fff", $null) -gt $sonSaat }

if ($hatalar.Count -gt 10) {
    Write-Host "⚠️ UYARI: Son 1 saatte $($hatalar.Count) hata tespit edildi!" -ForegroundColor Red
}
```

### Performans İzleme
```bash
# Yavaş çekimleri tespit etme (>1000ms)
Select-String -Path "logs\*.log" -Pattern "Süre: (\d{4,})ms" | 
    ForEach-Object { 
        $sure = $_.Matches[0].Groups[1].Value
        "$($_.Line) - YAVAŞ: ${sure}ms"
    }
```

## 🔧 Log Temizleme ve Bakım

### Eski Log Dosyalarını Temizleme
```bash
# 30 günden eski logları silme
Get-ChildItem -Path "logs\" -Filter "*.log" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | 
    Remove-Item -Force

# Log dosya boyutlarını kontrol etme
Get-ChildItem -Path "logs\" -Filter "*.log" | 
    Select-Object Name, @{Name="SizeMB";Expression={[math]::Round($_.Length/1MB,2)}} | 
    Sort-Object SizeMB -Descending
```

### Log Arşivleme
```bash
# Eski logları sıkıştırma
$eskiLoglar = Get-ChildItem -Path "logs\" -Filter "*.log" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) }

foreach ($log in $eskiLoglar) {
    Compress-Archive -Path $log.FullName -DestinationPath "$($log.FullName).zip"
    Remove-Item $log.FullName
}
```

## 📋 Günlük Kontrol Listesi

### Her Gün Yapılması Gerekenler
1. **Hata Kontrolü**: `Select-String -Path "logs\*.log" -Pattern "\[ERR\]" | Select-Object -Last 10`
2. **Başarı Oranı**: Günlük başarı oranını hesapla
3. **Performans**: Ortalama çekim sürelerini kontrol et
4. **Disk Alanı**: Log dosyalarının disk kullanımını kontrol et

### Haftalık Kontroller
1. **Trend Analizi**: Haftalık performans trendlerini analiz et
2. **Log Temizleme**: Eski log dosyalarını temizle
3. **Sistem Sağlığı**: Genel sistem sağlığını değerlendir

### Aylık Kontroller
1. **Kapsamlı Analiz**: Aylık detaylı performans raporu
2. **Optimizasyon**: Ayarları optimize et
3. **Arşivleme**: Eski logları arşivle

## 🚨 Acil Durum Senaryoları

### Uygulama Çökmesi
```bash
# Son çökme zamanını bulma
Select-String -Path "logs\*.log" -Pattern "\[FTL\]" | Select-Object -Last 1

# Çökme öncesi son logları inceleme
Select-String -Path "logs\*.log" -Pattern "\[ERR\]|\[FTL\]" | Select-Object -Last 20
```

### Performans Düşüşü
```bash
# Son 100 çekimin ortalama süresini hesaplama
$sonCekimler = Select-String -Path "logs\*.log" -Pattern "✅.*Süre: (\d+)ms" | Select-Object -Last 100
$ortalama = ($sonCekimler | ForEach-Object { $_.Matches[0].Groups[1].Value } | Measure-Object -Average).Average
Write-Host "Son 100 çekimin ortalama süresi: $($ortalama.ToString('F1'))ms"
```
