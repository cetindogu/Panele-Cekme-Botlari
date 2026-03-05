# TRONPANELE_CEKME Uygulama İstekleri

Bu belge, TRONPANELE_CEKME konsol uygulamasının gereksinimlerini ve çalışma mantığını içermektedir.

## 1. Genel Gereksinimler
- **Teknoloji:** .NET 10 Console Application (C#).
- **Hedef Site:** `https://win.tronpanel.com`
- **Kullanıcı Bilgileri:** Kullanıcı adı kod içerisinde gömülü (obfuscated), şifre `appsettings.json`'dan okunacak.
- **Sayfa:** Çekimler sayfası (`https://win.tronpanel.com/pendings/withdraws`).

## 2. Fonksiyonel Gereksinimler
- **Giriş İşlemi:** Uygulama başladığında kullanıcı adı ve şifre ile siteye giriş yapmalıdır.
- **İzleme:** Çekimler sayfasındaki tablo her 5 saniyede bir (ayarlanabilir) kontrol edilmelidir.
- **Veri Çekme:** Tablo verileri AJAX istekleri ile çekilmelidir.
- **Filtreleme:** 
  - Durumu "Bekleme" (veya "Beklemede") olan satırlar.
  - "İşleme Al" düğmesi bulunan satırlar.
  - "Miktar" bilgisi 10.000 ile 100.000 (ayarlanabilir) arasında olan satırlar.
- **Otomatik İşlem:** Filtreye uyan satırlar için "İşleme Al" düğmesine otomatik basılmalıdır.
  - İşlem başarılı mı kontrol edilmeli (`status: true` cevabı aranmalı).
  - İşleme al endpoint'i: `https://win.tronpanel.com/pendings/proc`
- **Limit Kontrolleri:**
  - Maksimum toplam çekilecek tutar (varsayılan 500.000).
  - Maksimum toplam çekilecek kayıt sayısı (varsayılan 50).
  - Eğer yeni bir işlem toplam tutarı veya kayıt sayısını aşacaksa uygulama sonlanmalıdır.
- **Preview Mode:** 
  - `PreviewMode: true` ise işlem yapmaz, sadece kriterlere uyanları listeler.
  - `PreviewMode: false` ise gerçek işlem yapar.

## 3. Teknik Gereksinimler
- **Yapılandırma:** `appsettings.json` dosyası üzerinden tüm limitler, süreler ve modlar ayarlanabilir olmalıdır.
- **Loglama:** Serilog kullanılmalıdır (Console ve File).
  - Console logları renkli ve okunabilir olmalıdır.
  - Loglar aşırı birikmemeli, özetlenmelidir.
- **Kod Kalitesi:** SOLID prensiplerine uygun, temiz, anlaşılır ve açıklama satırları içeren kod yazılmalıdır.
- **Hata Yönetimi:** Her işlem başarılı/başarısız durumlarıyla loglanmalıdır.

## 4. Akış Şeması
1. `appsettings.json` oku.
2. `https://win.tronpanel.com` login ol.
3. `pendings/withdraws` sayfasına git.
4. Döngü başlat:
   - AJAX ile tablo verilerini çek.
   - Limitleri kontrol et (Toplam tutar, toplam kayıt).
   - Kriterlere uyan satırları bul.
   - `PreviewMode` kontrol et.
   - İşleme al veya listele.
   - Belirlenen süre kadar bekle.
5. Limitler aşılırsa uygulamayı sonlandır.
