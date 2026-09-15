# Ödev 5 — Insight Card: Ajanın Ekranına Müşteri Bilgisi Basmak

**Tahmini süre:** 3 gün
**Zorluk:** Orta
**Ön koşul:** [`04-webhook-alicisi.md`](04-webhook-alicisi.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

Telefon çalıyor. Ajanın ekranında sadece bir numara var. Ajan CRM'e geçiyor,
numarayı arıyor, kaydı buluyor — bu arada müşteri "alo?" diyor.

Insight Card bu on beş saniyeyi sıfıra indiriyor: çağrı gelir gelmez ajanın
ekranında müşterinin adı, firması, bakiyesi, son siparişi görünüyor. Veri senin
sistemininden geliyor, Hipcall sadece gösteriyor.

Bu, Ödev 4'ün üstüne kuruluyor: webhook gelir → sen müşteriyi bulursun → kartı
basarsın. İki parça, tek akış.

---

## Ön koşullar

- Ödev 4'teki webhook alıcın çalışıyor.
- Ödev 1'deki API anahtarın çalışıyor.
- DEMO'da çağrı alabileceğin bir kurulum: bir dış numara ve sana yönlenen bir akış.
  Yoksa Onur'dan iste.
- Web telefonu (agent ekranı) açık tutabiliyor olman gerekiyor — kartı orada
  göreceksin.

---

## Bölüm A — Kartı elle bas

Önce webhook olmadan, elle dene. Böylece kartın nasıl göründüğünü öğrenirsin.

1. Bir çağrı başlat ve **çağrı devam ederken** `GET /api/v3/calls` ile çağrının
   id'sini bul.
2. `POST /api/v3/calls/{call_id}/cards` ile basit bir kart gönder. En küçük geçerli
   gövde nedir? Bul.
3. Web telefonu ekranında kart göründü mü? Ekran görüntüsü al (numarayı maskele).
4. `GET /api/v3/calls/{call_id}/cards` ile kartı geri oku. Ne dönüyor?
5. **Aynı çağrıya ikinci bir kart** gönder. Ne oluyor — birincinin yerine mi geçiyor,
   altına mı ekleniyor?

---

## Bölüm B — Kart biçimi

Kart, sıralı satırlardan oluşan bir dizi. Her satırın bir `type` alanı var.

1. Hangi satır tipleri destekleniyor? API referansındaki örneği incele ve **her
   tipi tek tek dene**. Çalışanları ve çalışmayanları listele.
2. Bir satırda `label`, `text`, `link` alanları ne işe yarıyor? Hangisi zorunlu?
3. `ios` ve `android` alanları ne için? Web telefonunda etkileri var mı?
4. `user` tipindeki satır ne yapıyor? `user_id` verince ne görünüyor?
5. Tanımadığı bir `type` gönderirsen ne oluyor — hata mı, sessizce atlama mı?
6. Çok uzun bir `text` gönder (500 karakter). Ne oluyor?
7. Kaç satırlık kart makul? 3 satır, 10 satır ve 30 satırlık kartları dene, ekranda
   nasıl göründüğüne bak. Tavsiyeni yaz.

---

## Bölüm C — Zamanlama: kartın en zor tarafı

1. **Çağrı bitmişken** kart göndermeyi dene. Durum kodu ne? Kart görünüyor mu?
2. Cevabın söylediği ile ekranda olan arasında fark var mı? Varsa bu farkı yaz.
3. Çağrı **daha cevaplanmadan** (çalarken) kart gönder. Görünüyor mu?
4. Webhook'tan çağrı id'sini alıp kart basana kadar geçen süreyi ölç. Kendi
   sistemindeki arama 2 saniye sürerse kart zamanında yetişir mi?

C1 ve C2 birlikte önemli: API `200` dönüyor ama kart görünmüyorsa, entegrasyonu
yazan developer bunu nasıl fark edecek? Yazında net bir uyarı ver.

---

## Bölüm D — Uçtan uca: webhook + kart

Ödev 4'teki alıcıyı genişlet.

Akış:

1. `call_init` olayı gelir.
2. Çağrının yönüne bak — müşterinin numarası hangi alanda? (Gelen ve giden çağrıda
   farklı. Bunu ödevde ölç.)
3. Numarayla kendi "CRM"inde ara. Gerçek bir CRM'e gerek yok: 5-10 kayıtlık bir
   sözlük veya JSON dosyası yeterli.
4. Bulduysan kartı bas. Bulamadıysan **hiçbir şey yapma** — boş kart basma.
5. `200` dön.

Ek olarak: Hipcall'ın kendi kişi kayıtlarını da kullanabilirsin.
`GET /api/v3/lookup/by_phone` ne döndürüyor? Kendi CRM'in yerine ya da ona ek olarak
bunu kullanmak mantıklı mı? Görüşünü yaz.

Teslim: `submissions/05-insight-card/Hipcall.InsightCard/`
(Ödev 4'teki projeyi kopyalayıp genişletebilirsin.)

---

## Bölüm E — Community

- Yeni soru: C bölümündeki zamanlama meselesi.
- Eski konuları kapat.

---

## Bölüm F — Teslim edeceklerin

### F1. Çalışma notu

`submissions/05-insight-card.md`

- A–D bölümlerinin çıktıları
- **Satır tipi tablosu**: tip, zorunlu alanlar, ekranda nasıl göründüğü
- En az bir ekran görüntüsü (`blog/assets/` altına koy, numaraları maskele)
- Mermaid `sequenceDiagram`: çağrı → webhook → CRM sorgusu → kart → ajan ekranı
- C bölümünün zamanlama bulguları
- Community linkleri

### F2. Blog — İngilizce

`blog/en/how-to-show-caller-context-with-insight-card.md`

```yaml
title: "How to Show Caller Context on the Agent Screen with Insight Card"
description: "Push your own customer data onto the agent's screen the moment a call starts, using the call_init webhook and the Insight Card API."
slug: how-to-show-caller-context-with-insight-card
lang: en
locales: [en, tr]
categories: [developers]
intent: informational
translationKey: how-to-show-caller-context-with-insight-card
tags: [insight-card, webhooks, dotnet, crm]
task: 05
```

Yapı:

1. **Overview** — 15 saniyelik arama problemi
2. **Before you start** — webhook alıcısı (Ödev 4 yazısına link), API anahtarı
3. **Anatomy of a card** — satır tipleri, tablo, örnek JSON
4. **Sending your first card** — elle, curl + C#
5. **Wiring it to the call_init webhook** — uçtan uca akış, mermaid
6. **Timing matters** — C bölümünün bulguları. Kart ne zaman görünür, ne zaman
   görünmez.
7. **The full example**
8. **When it fails**
9. **Next steps**

### F3. Blog — Türkçe

`blog/tr/insight-card-ile-arayan-bilgisini-ekranda-gosterme.md`

### F4. PR

---

## Kabul kriterleri

- [ ] A5 cevaplanmış: ikinci kart ne yapıyor
- [ ] Satır tipi tablosu dolu, her tip **denenerek** doğrulanmış
- [ ] B5 ve B6 ölçülmüş
- [ ] B7'de kaç satırlık kartın makul olduğu konusunda gerekçeli tavsiye var
- [ ] C1/C2 ölçülmüş ve yazıda uyarı olarak yer alıyor
- [ ] D bölümünde gelen/giden çağrıda müşteri numarasının hangi alanda olduğu
      ölçülmüş
- [ ] Kayıt bulunamadığında boş kart basılmıyor
- [ ] `lookup/by_phone` denenmiş ve hakkında görüş yazılmış
- [ ] Ekran görüntüsünde numara/isim maskeli
- [ ] C# projesi çalışıyor, Ödev 4'ün kurallarını koruyor (tek HttpClient, hızlı 200)
- [ ] EN + TR blog standarda uyuyor

---

## Bilerek söylemediklerim

1. İkinci kartın birinciyi ezip ezmediği (A5)
2. Bitmiş çağrıya kart basınca ne döndüğü ve ne göründüğü (C1/C2)
3. Gelen ve giden çağrıda müşteri numarasının hangi alanda olduğu (D2)
4. Tanınmayan satır tipinin davranışı (B5)

---

## Yaygın hatalar

- **Çağrı bitmeden kartı yetiştirememek.** CRM sorgun yavaşsa kart geç kalır.
  Yazında süre bütçesinden bahset.
- **Boş kart basmak.** Müşteri bulunamadıysa kart basma; ajan boş bir panel görmesin.
- **Kartı bilgiyle doldurmak.** 30 satırlık kart okunmuyor. En kritik 3-5 satır.
- **Ekran görüntüsünde müşteri verisi bırakmak.** Repo herkese açık.
- **`200` aldım diye kartın göründüğünü varsaymak.** Bu ödevin ana konusu.

---

## Teslim

Gözden geçirmede soracağım:

> Ajan telefonu açtığı anda kart ekranında mı? Değilse, developer bunu nasıl fark
> eder ve nereye bakar?

---

**Sonraki ödev:** [`06-kisi-firma-senkronu.md`](06-kisi-firma-senkronu.md)
