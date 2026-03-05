# Tutar Filtresi Örnekleri

## 🎯 Temel Kullanım

### Sadece Yüksek Tutarlar (10,000 TL ve üzeri)
```json
{
  "Bot": {
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 10000,
      "MaxTutar": 999999,
      "IgnoreInvalidAmounts": true
    }
  }
}
```

### Orta-Yüksek Tutarlar (5,000-20,000 TL arası)
```json
{
  "Bot": {
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 5000,
      "MaxTutar": 20000,
      "IgnoreInvalidAmounts": true
    }
  }
}
```

### Sadece Küçük Tutarlar (100-500 TL arası)
```json
{
  "Bot": {
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 100,
      "MaxTutar": 500,
      "IgnoreInvalidAmounts": true
    }
  }
}
```

## 🔧 Gelişmiş Senaryolar

### Tüm Tutarları İşle (Filtre Kapalı)
```json
{
  "Bot": {
    "TutarFiltre": {
      "Enabled": false
    }
  }
}
```

### Geçersiz Tutarları da İşle
```json
{
  "Bot": {
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 100,
      "MaxTutar": 5000,
      "IgnoreInvalidAmounts": false  // Geçersiz tutarları da işler
    }
  }
}
```

## 📊 Tutar Format Örnekleri

Bot aşağıdaki Türkiye tutar formatlarını otomatik olarak tanır ve temizler:

### Desteklenen Türkiye Formatları
- `46,700 TL` → 46700 (binlik ayırıcı virgül)
- `46,700.50 TL` → 46700.50 (binlik virgül, ondalık nokta)
- `₺46,700` → 46700
- `46700 TL` → 46700 (ayırıcısız)
- `46,700.5 TL` → 46700.5
- `1,234,567 TL` → 1234567 (çoklu binlik ayırıcı)

### Geçersiz Formatlar
- `ABC TL` → 0 (geçersiz)
- `TL` → 0 (geçersiz)
- `""` → 0 (boş)
- `null` → 0 (null)

## 🎮 Test Senaryoları

### Senaryo 1: Yüksek Tutar Avcısı (Varsayılan)
```json
{
  "Bot": {
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 10000,
      "MaxTutar": 999999,
      "IgnoreInvalidAmounts": true
    }
  }
}
```
**Sonuç**: Sadece 10,000 TL ve üzeri yüksek tutarlar işlenir.

### Senaryo 2: Çok Yüksek Tutarlar
```json
{
  "Bot": {
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 25000,
      "MaxTutar": 100000,
      "IgnoreInvalidAmounts": true
    }
  }
}
```
**Sonuç**: Sadece 25,000-100,000 TL arası çok yüksek tutarlar işlenir.

### Senaryo 3: Orta Seviye Tutarlar
```json
{
  "Bot": {
    "TutarFiltre": {
      "Enabled": true,
      "MinTutar": 5000,
      "MaxTutar": 15000,
      "IgnoreInvalidAmounts": true
    }
  }
}
```
**Sonuç**: 5,000-15,000 TL arası orta seviye tutarlar işlenir.

## 📈 Performans İpuçları

### Hızlı İşlem İçin
- Dar tutar aralığı seçin (örn: 10,000-20,000 TL)
- `IgnoreInvalidAmounts: true` kullanın
- Polling aralığını düşürün

### Güvenli İşlem İçin
- Geniş tutar aralığı seçin (örn: 5,000-50,000 TL)
- `IgnoreInvalidAmounts: false` kullanın
- Polling aralığını yükseltin

## 🚨 Dikkat Edilmesi Gerekenler

1. **MinTutar > MaxTutar**: Bu durumda hiçbir çekim işlenmez
2. **Çok Dar Aralık**: Çok az çekim yakalayabilirsiniz
3. **Çok Geniş Aralık**: Çok fazla çekim yakalayıp sistem yükü oluşturabilir
4. **Geçersiz Tutarlar**: `IgnoreInvalidAmounts: false` dikkatli kullanın

## 🔍 Debug İpuçları

Tutar filtresi çalışmasını izlemek için:

```json
{
  "Logging": {
    "LogLevel": {
      "PaneleCekmeBot": "Debug"
    }
  }
}
```

Bu ayarla şu logları göreceksiniz:
```
[DBG] Tutar filtresi geçti. ID: 12345, Tutar: 46,700 TL (46700), Aralık: 10000-999999
[DBG] Tutar filtresi geçmedi. ID: 12346, Tutar: 5,500 TL (5500), Aralık: 10000-999999
[DBG] Geçersiz tutar göz ardı ediliyor. ID: 12347, Tutar: ABC TL
```
