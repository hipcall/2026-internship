# Ödev 4 — Webhook Alıcısı: Çağrı Kayıtlarını Kendi Sisteminize Akıtmak

**Tahmini süre:** 4 gün
**Zorluk:** Orta
**Ön koşul:** [`03-click-to-call.md`](03-click-to-call.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

Şimdiye kadar hep **sen sorduğun** için veri geldi. Webhook bunun tersi: olay
olduğunda **Hipcall sana** haber veriyor.

Bu, en çok istenen entegrasyon tipi. Müşteri çağrı kayıtlarını kendi veri
ambarında, kendi CRM'inde, kendi arşivinde istiyor — ve her 5 dakikada bir API'yi
yoklayarak değil, çağrı biter bitmez.

Webhook'un zor tarafı istek almak değil. Zor tarafı şu soru: **teslimat garantisi
yoksa veriyi nasıl eksiksiz tutarsın?** Bu ödevde o sorunun cevabını yazacaksın.

---

## Ön koşullar

- Ödev 1'deki anahtar çalışıyor, Ödev 3'ten çağrı başlatmayı biliyorsun.
- .NET SDK kurulu.
- İnternete açılabilen bir yerel sunucu. [ngrok](https://ngrok.com/) kullan:
  `ngrok http 5080`
- DEMO hesabında en az birkaç test çağrısı yapabilecek durumda ol.

---

## Bölüm A — Panelde webhook kur

1. DEMO panelinde webhook entegrasyonunu nereden oluşturuyorsun? Menü yolunu ve
   sayfa URL'ini not et.
2. Oluştururken hangi alanlar isteniyor?
3. **Olaylar (events) sekmesi:** kaç farklı olaya abone olunabiliyor? Hepsini
   listele. Ne zaman tetiklendiklerini tahmin et, sonra test ederek doğrula.
4. **Kayıtlar (logs) sekmesi:** ne gösteriyor? İlk açtığında boş mu?
5. Log kaydının tutulması için ayrıca bir şey açman gerekiyor mu? Bir ayar
   arayacaksın — bulunca ne işe yaradığını yaz.
6. URL doğrulaması var mı? Rastgele bir adres (`https://example.com/yok`) girmeyi
   dene, kaydediyor mu?

---

## Bölüm B — Alıcıyı yaz

ASP.NET Core minimal API ile başla:

```bash
dotnet new web -n Hipcall.WebhookReceiver
```

İlk sürüm sadece şunu yapsın: gelen gövdeyi olduğu gibi konsola yazdır ve `200`
dön.

```csharp
app.MapPost("/hipcall/events", async (HttpRequest req) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    Console.WriteLine(body);
    return Results.Ok();
});
```

ngrok adresini panele gir, bir test çağrısı yap.

1. Kaç istek geldi? Tek çağrı için kaç olay tetiklendi?
2. Gövdenin yapısı ne? En üst düzeyde hangi anahtarlar var?
3. `Content-Type` ne? Gövde JSON mu, form mu?
4. İstek başlıklarına bak. **İmza başlığı var mı?** (`X-Signature`, `X-Hub-Signature`
   benzeri bir şey.) Varsa yaz, yoksa "yok" yaz — bu bulgunun sonuçlarını Bölüm D'de
   tartışacağız.

---

## Bölüm C — Olayları tanı

En az üç olay tipini gerçekten tetikle ve gövdelerini yan yana koy:

1. Bir çağrı başlat (Ödev 3'teki yöntemle) → hangi olay(lar) geldi?
2. Çağrıyı cevapla ve kapat → hangi olay(lar) geldi?
3. Cevapsız bırak → fark ne?
4. API ile bir kişi oluştur → olay geliyor mu?

Her olay için **alan alan** bir tablo çıkar:

| Alan | Tip | Örnek | Açıklama |
|---|---|---|---|

Özellikle şu soruların cevabı lazım:

5. Çağrıyı benzersiz kılan alan hangisi? Aynı çağrının farklı olaylarında bu alan
   aynı kalıyor mu?
6. Çağrının yönünü (gelen/giden) nereden anlıyorsun?
7. Ses kaydı URL'i hangi olayda geliyor? O URL'i tarayıcıda açmayı dene. Bir saat
   sonra tekrar açmayı dene. Ne oluyor?
8. Cevapsız çağrıyı hangi alan(lar)dan tespit edersin?

C7 önemli: URL'i veritabanına kaydedip üç gün sonra kullanmayı planlayan bir
developer ne yapmalı? Cevabını yazında bir uyarı kutusu olarak ver.

---

## Bölüm D — Teslimat garantisi: asıl konu

Bu bölüm bu ödevin kalbi. Ölç, sonra yaz.

1. Alıcın `500` dönsün (kodda geçici olarak hata fırlat). Bir çağrı yap.
   - Hipcall isteği **tekrar deniyor mu**? Kaç kez, hangi aralıkla?
   - Panelde logs sekmesinde ne görünüyor?
2. Alıcın 30 saniye beklesin sonra cevap versin. Zaman aşımı var mı?
3. Arka arkaya birkaç kez `500` dön. Entegrasyonun **durumu** değişiyor mu?
   Panelde bir şey fark ediyor musun?
4. Durum değiştiyse: eski hâline nasıl döndürüyorsun?

Şimdi bulgularını birleştir ve şu soruyu cevapla:

> Teslimat garantisi ve imza doğrulaması olmayan bir webhook'la, **eksiksiz** ve
> **güvenilir** bir çağrı kaydı arşivi nasıl kurulur?

Cevabın en az şu üç başlığı içermeli:

- **Hızlı cevap ver, işi sonraya bırak.** Neden? İşlem sırası ne olmalı?
- **Idempotency.** Aynı olay iki kez gelirse ne olmalı? Hangi alan üzerinden
  tekilleştirirsin?
- **Mutabakat (reconciliation).** Kaçan olayları nasıl yakalarsın? Ödev 2'de
  yazdığın liste çekme kodu burada işe yarayacak.

Bir de dördüncüsü var, onu da düşün: imza yoksa, alıcı endpoint'ine sahte istek
gönderen birini nasıl engellersin? En az iki yöntem öner.

---

## Bölüm E — Alıcıyı tamamla

İlk sürümü gerçek bir alıcıya çevir:

- Gövdeyi tiplenmiş sınıflara çöz (`System.Text.Json`, `JsonSerializerOptions` ile
  snake_case eşlemesi).
- Sadece ilgilendiğin olayı işle; tanımadığın olayı **hata verme**, sessizce geç.
  (Yarın yeni bir olay tipi eklenirse alıcın çökmemeli.)
- Çağrı kaydını sakla. Veritabanı kurmana gerek yok — SQLite veya düz bir JSON
  dosyası yeterli. Önemli olan **tekilleştirme**: aynı çağrı iki kez gelirse ikinci
  kayıt oluşmamalı.
- Ses kaydı varsa arka planda indir. İndirme işlemi HTTP cevabını **bekletmemeli**.
- `200`'ü hızlı dön.
- Basit bir paylaşılan gizli anahtar kontrolü ekle (Bölüm D'deki önerilerinden biri):
  örneğin URL'de tahmin edilemez bir yol parçası.

Teslim: `submissions/04-webhook-alicisi/Hipcall.WebhookReceiver/`

---

## Bölüm F — Community

- Yeni soru: D bölümündeki bulgular. "Tekrar deneme yoksa eksiksizliği nasıl
  sağlarım?" — bu sorunun forumda cevabı olması bizim için değerli.
- Eski konuları kapat.

---

## Bölüm G — Teslim edeceklerin

### G1. Çalışma notu

`submissions/04-webhook-alicisi.md`

- A–D bölümlerinin çıktıları
- **Olay tablosu**: her olay tipi, ne zaman tetiklendiği, taşıdığı alanlar
- D bölümünün cevabı — bu ödevin en önemli parçası
- Mermaid `flowchart`: olay geldi → doğrula → kuyruğa al → 200 dön → arka planda işle
- Community linkleri

### G2. Blog — İngilizce

`blog/en/how-to-build-a-hipcall-webhook-receiver-in-dotnet.md`

```yaml
title: "How to Build a Hipcall Webhook Receiver in .NET"
description: "Receive call events in ASP.NET Core, store call records without duplicates, and keep your archive complete when a delivery fails."
slug: how-to-build-a-hipcall-webhook-receiver-in-dotnet
lang: en
locales: [en, tr]
categories: [developers]
intent: informational
translationKey: how-to-build-a-hipcall-webhook-receiver-in-dotnet
tags: [webhooks, dotnet, cdr, integrations]
task: 04
```

Yapı:

1. **Overview**
2. **Before you start** — ngrok, .NET
3. **Setting up the webhook** — panelde kurulum (Bölüm A)
4. **Receiving your first event** — minimal alıcı
5. **What each event carries** — olay tablosu
6. **Making it reliable** — Bölüm D'nin cevabı: hızlı 200, idempotency, mutabakat,
   endpoint'i koruma
7. **The full example** — C# projesi
8. **When it fails** — 500 dönerse ne oluyor, entegrasyon nasıl kapanıyor, nasıl
   geri açılıyor
9. **Next steps**

6. bölüm bu yazının varlık sebebi. Kısa geçme.

### G3. Blog — Türkçe

`blog/tr/dotnet-ile-hipcall-webhook-alicisi-yazma.md`

### G4. PR

---

## Kabul kriterleri

- [ ] Panel kurulum adımları menü adlarıyla yazılmış
- [ ] Olay tablosu en az 3 olay için alan alan dolu
- [ ] B4 cevaplanmış: imza başlığı var mı yok mu
- [ ] C7 ölçülmüş: ses kaydı URL'i bir saat sonra çalışıyor mu
- [ ] D1–D4 **gerçekten denenmiş**, çıktılar yapıştırılmış
- [ ] D bölümünün üç başlığı (hızlı cevap / idempotency / mutabakat) cevaplanmış
- [ ] Sahte istek engelleme için en az iki yöntem önerilmiş
- [ ] Alıcı tanımadığı olay tipinde çökmüyor
- [ ] Aynı olay iki kez gelince ikinci kayıt oluşmuyor — **test ederek göster**
- [ ] Ses kaydı indirme HTTP cevabını bekletmiyor
- [ ] EN + TR blog standarda uyuyor
- [ ] Gerçek numara/isim yok

---

## Bilerek söylemediklerim

1. İmza başlığının olup olmadığı (B4)
2. Tekrar deneme davranışı (D1)
3. Arka arkaya hata verince entegrasyona ne olduğu (D3)
4. Ses kaydı URL'inin ömrü (C7)

Bunlar dokümanda yazmıyor ve müşteri bunları **üretimde** öğreniyor. Yazının değeri
tam olarak burada.

---

## Yaygın hatalar

- **`200`'ü iş bittikten sonra dönmek.** Ses kaydını indirip sonra cevap verirsen
  zaman aşımına düşersin ve olay kaybolur.
- **Tanımadığı olayda çökmek.** `switch` ifadesinin `default` dalı hata fırlatmasın.
- **`uuid` yerine zaman damgasıyla tekilleştirmek.** İki çağrı aynı saniyede
  başlayabilir.
- **Webhook'u tek doğruluk kaynağı saymak.** Mutabakat olmadan arşiv eksik kalır.
- **ngrok adresini yazıya koymak.** Örneklerde `https://your-server.example.com`
  kullan.
- **Gövdeyi loglarken maskelememek.** Telefon numarası log dosyasında da PII'dir.

---

## Teslim

Gözden geçirmede soracağım:

> Bu alıcı bir hafta boyunca çalışsa, Hipcall'daki çağrı sayısı ile senin
> veritabanındaki kayıt sayısı **birebir tutar mı**? Tutmazsa fark nereden gelir ve
> bunu nasıl kapatırsın?

---

**Sonraki ödev:** [`05-insight-card.md`](05-insight-card.md)
