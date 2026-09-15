# Hipcall 2026 Yazılım Staj Programı

Bu repo, Hipcall 2026 staj programının ödevlerini ve çıktılarını barındırır.

Programın amacı tek cümlede: **Hipcall API'siyle entegrasyon yazan bir developer'ın
işini kolaylaştıran how-to kılavuzları üretmek.** API referansımız var
(<https://use.hipcall.com/api-docs/>), ama "nereden başlayacağım" sorusunun cevabı
yok. Burada üretilen yazılar o boşluğu dolduruyor.

---

## Dizin yapısı

```
.
├── tasks/          Ödevler. Sırayla ilerlenir.
├── submissions/    Ödev çıktıları: çalışma notları, betikler, ölçümler.
└── blog/           Yayına hazırlanan blog yazıları.
    ├── en/         İngilizce
    ├── tr/         Türkçe
    ├── assets/     Görseller
    └── README.md   ← Blog yazım standardı
```

---

## Ödevler

| # | Ödev | Konu |
|---|---|---|
| 01 | [API Anahtarı ve İlk İstek](tasks/01-api-anahtari.md) | Kimlik doğrulama, ilk çağrı, hata cevapları |
| 02 | [Listeleri Çekmek](tasks/02-liste-ve-filtreleme.md) | Sayfalama, arama, sıralama, filtreleme |
| 03 | [Çağrı Başlatma ve Numara Maskeleme](tasks/03-click-to-call.md) | Click-to-call, maskeli çağrı |
| 04 | [Webhook Alıcısı](tasks/04-webhook-alicisi.md) | Olay tabanlı entegrasyon, çağrı kaydı arşivi |
| 05 | [Insight Card](tasks/05-insight-card.md) | Ajan ekranına canlı müşteri bilgisi |
| 06 | [Kişi ve Firma Senkronu](tasks/06-kisi-firma-senkronu.md) | `external_id`, upsert, özel alanlar |
| 07 | [Çağrı Sonrası Otomasyon](tasks/07-cagri-sonrasi-otomasyon.md) | Sonuç kodu, etiket, yorum, görev |
| 08 | [External Management](tasks/08-external-management.md) | Kendi servisinle çağrı yönlendirme |

Bir ödev teslim edilip gözden geçirilmeden sonrakine geçilmez. Her ödev bir
öncekinin üstüne kuruluyor.

### Çıktı

Sekiz ödevin sonunda ortaya çıkan: 8 çalışma notu, 16 blog yazısı (EN + TR),
6 çalışan C# örneği ve community'de kalıcı soru-cevap kaydı.

### Gereçler

- **Dil:** C# / .NET (en az .NET 8). Ödev 1 `curl` ile, sonrakiler C# ile.
- **Ortam:** DEMO hesabı, ngrok (Ödev 4'ten itibaren), gelen çağrı alabilen bir
  numara (Ödev 8).
- **Önceden istenmesi gereken:** Ödev 8 için kendine ayrılmış, akışını
  değiştirebileceğin bir gelen numara. Ödevden **önce** iste.

## Nasıl çalışıyoruz

1. **Ödevi oku.** `tasks/` altındaki dosya, neyin ölçüleceğini ve neyin teslim
   edileceğini satır satır yazar.
2. **DEMO ortamında dene.** Tahmin yürütmek yok — her sonuç ölçülerek bulunur.
3. **Takılınca <https://community.hipcall.com/> üzerinden sor.** Özel mesajla değil.
   Senin takıldığın yerde bir müşteri de takılıyor; forumdaki cevap ikinize birden
   yarıyor.
4. **Çıktıyı yaz.** Çalışma notu `submissions/`, blog yazıları `blog/en/` ve
   `blog/tr/` altına.
5. **PR aç.** Gözden geçiririz, düzeltmeleri birlikte yaparız.
6. **Onaylanan yazıları siteye biz aktarırız.**

---

## Kurallar

**Bu repo herkese açıktır.** İçine yazdığın her şeyi internetteki herkes okuyabilir.

Buraya asla girmeyecek şeyler:

- Gerçek API anahtarı veya parola
- Müşteri adı, telefon numarası, e-posta adresi
- DEMO ortamından alınmış maskelenmemiş veri
- Şirket içi bilgi: ticket numarası, sunucu adı, iç repo yolu

Telefon numarası maskeleme: `+90555XXXXXXX`. İsim: `Ayse Y.`
Anahtar örneği kod bloklarında hep ortam değişkeni: `$HIPCALL_API_TOKEN`.

Anahtarını yanlışlıkla paylaşırsan panik yapma: DEMO panelinden sil, yenisini
oluştur ve durumu bildir.

---

## Yazı standardı

Blog yazıları [`blog/README.md`](blog/README.md) dosyasındaki standarda uyar.
Frontmatter alanları, dosya adlandırma, kod bloğu kuralları, Türkçe terim tablosu
ve gözden geçirme listesi orada.

Standarda uyan bir yazı siteye kopyala-yapıştır aktarılır. Uymayan her yazı elle
düzeltme gerektirir ve yayını geciktirir.

---

## Bağlantılar

- API referansı — <https://use.hipcall.com/api-docs/>
- Developer sayfası — <https://www.hipcall.com/developers/>
- Topluluk forumu — <https://community.hipcall.com/>
- Blog — <https://www.hipcall.com/blog/>
