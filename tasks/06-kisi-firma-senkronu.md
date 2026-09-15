# Ödev 6 — Kişi ve Firma Senkronu: Kendi ID'nizle Çalışmak

**Tahmini süre:** 4 gün
**Zorluk:** Orta–zor
**Ön koşul:** [`05-insight-card.md`](05-insight-card.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

Her entegrasyonun er ya da geç geldiği yer burası: iki sistemde aynı müşteri var,
ikisinde farklı ID ile. Kendi sisteminde `customer_id = 4821`, Hipcall'da
`contact_id = 917`. Bunları eşleştirmeden hiçbir şey yapamazsın.

Yanlış çözüm: telefon numarasıyla eşleştirmek. Numara değişir, aynı numarayı iki
kişi kullanır, biçim farklı yazılır. Bir müşteri numarasını değiştirdiği gün
eşleştirme kopar.

Doğru çözüm: `external_id`. Kendi ID'ni Hipcall kaydına yazıyorsun ve doğrudan
onunla sorguluyorsun.

Bu ödevde iki yönlü, tekrar çalıştırılabilir (idempotent) bir senkron yazacaksın.

---

## Ön koşullar

- Ödev 1–5 tamam.
- DEMO hesabında kişi ve firma oluşturma/güncelleme yetkin olmalı.
- .NET SDK.

---

## Bölüm A — Kişi oluşturma ve okuma

1. `POST /api/v3/contacts` ile bir kişi oluştur. **En küçük geçerli gövde** nedir?
2. Oluştururken telefon ve e-posta da gönderebiliyor musun? Dene.
3. `GET /api/v3/contacts/{id}` ile geri oku. Gönderdiğin her alan döndü mü?
4. `external_id` alanını doldurarak yeni bir kişi oluştur.
5. `GET /api/v3/contacts/by-external-id/{external_id}` ile oku. Çalıştı mı?
6. **Aynı `external_id` ile ikinci bir kişi** oluşturmayı dene. Ne oluyor?
   Durum kodu ve gövdeyi yapıştır. Bu davranış senkron yazarken işine yarar mı,
   yoksa engel mi?
7. `external_id` olmayan bir değerle `by-external-id` sorgusu yap. Ne dönüyor?

---

## Bölüm B — Güncelleme: bir tuzak var

1. `PATCH /api/v3/contacts/{id}` ile kişinin adını değiştir.
2. Aynı `PATCH` ile telefon numarası göndermeyi dene.
   - Kabul edildi mi?
   - Edilmediyse hata gövdesi ne diyor?
3. B2'nin sonucuna göre: telefon ve e-posta nasıl yönetiliyor? İlgili endpoint'leri
   bul ve dene:
   - Telefon ekleme / silme
   - E-posta ekleme / silme
4. Bir kişiye kaç telefon eklenebiliyor? Sınıra kadar ekle, sınırı bul.
5. Aynı numarayı iki kez eklemeyi dene.
6. `PATCH` ile bir alanı **null** yapmayı dene. Siliniyor mu, yok mu sayılıyor?

B2 bu ödevin ilk tuzağı. `POST` ile `PATCH` arasındaki farkı yazında net anlat —
developer'ın bunu kendi kendine bulması gerekmemeli.

---

## Bölüm C — Özel alanlar (custom fields)

1. `GET /api/v3/contacts/custom-fields` ne döndürüyor? DEMO'da tanımlı alan var mı?
   Yoksa panelden bir tane oluştur (menü yolunu not et).
2. Bir alanın `slug` değeri nedir, nerede kullanılıyor?
3. `POST` veya `PATCH` ile özel alan değeri yazmayı dene. Gövdede nereye yazılıyor?
4. Var olmayan bir `slug` ile yazmayı dene. Ne oluyor?
5. Alan tipine uymayan bir değer gönder (sayı alanına metin). Ne oluyor?
6. Kişiyi geri okuduğunda özel alanlar nerede görünüyor?

---

## Bölüm D — Firmalar ve ilişki

1. `POST /api/v3/companies` ile firma oluştur, `external_id` ver.
2. `GET /api/v3/companies/by-external-id/{id}` ile oku.
3. Bir kişiyi firmaya bağla. Hangi alan?
4. Firma silinirse kişiye ne oluyor? (DEMO'da dikkatli dene, kendi oluşturduğun
   kayıtla.)
5. `PATCH /api/v3/contacts/{id}/assign` ne yapıyor? `PATCH /api/v3/contacts/{id}`
   ile atama yapmaktan farkı ne?

---

## Bölüm E — Senkronu yaz

Bir C# konsol uygulaması: `Hipcall.ContactSync`

Girdi: küçük bir JSON dosyası — kendi "CRM"in.

```json
[
  { "customerId": "4821", "firstName": "Ayse", "lastName": "Y.", "phone": "+90555XXXXXXX", "companyId": "77" },
  { "customerId": "4822", "firstName": "Mehmet", "lastName": "K.", "phone": "+90555XXXXXXX", "companyId": "77" }
]
```

Uygulama şunları yapmalı:

- Her kayıt için `external_id` ile Hipcall'da ara.
- Yoksa oluştur, varsa güncelle. (Bu desene **upsert** deniyor.)
- Firmayı da aynı mantıkla senkronla ve kişiyi firmaya bağla.
- Telefonu doğru endpoint'ten yönet (Bölüm B'nin bulgusu).
- **İki kez çalıştırıldığında ikinci çalıştırma hiçbir şeyi bozmamalı ve yeni kayıt
  oluşturmamalı.** Bunu test et ve kanıtını çalışma notuna koy: önce/sonra kayıt
  sayısı.
- Her kayıt için ne yaptığını yazdır: `created` / `updated` / `unchanged`.
- Bir kayıt hata verirse **durmamalı**; hatayı raporlayıp diğerlerine devam etmeli.
  Sonunda özet: 2 oluşturuldu, 1 güncellendi, 1 hata.

Teslim: `submissions/06-kisi-firma-senkronu/Hipcall.ContactSync/`

---

## Bölüm F — Community

- Yeni soru: B2'deki `POST` / `PATCH` farkı ya da C bölümündeki özel alan davranışı.
- Eski konuları kapat.

---

## Bölüm G — Teslim edeceklerin

### G1. Çalışma notu

`submissions/06-kisi-firma-senkronu.md`

- A–D bölümlerinin çıktıları
- **Alan tablosu**: kişi oluştururken/güncellerken hangi alan nerede kullanılıyor
- `POST` ve `PATCH` farkının net özeti
- Idempotency kanıtı: iki kez çalıştırma öncesi/sonrası kayıt sayısı
- Mermaid `flowchart`: upsert kararı (ara → var mı? → oluştur/güncelle)
- Community linkleri

### G2. Blog — İngilizce

`blog/en/how-to-sync-contacts-with-external-id.md`

```yaml
title: "How to Sync Contacts Between Your CRM and Hipcall Using external_id"
description: "Match records with your own IDs instead of phone numbers, and write a sync that can run twice without creating duplicates."
slug: how-to-sync-contacts-with-external-id
lang: en
locales: [en, tr]
categories: [developers]
intent: informational
translationKey: how-to-sync-contacts-with-external-id
tags: [api, contacts, crm, dotnet, integrations]
task: 06
```

Yapı:

1. **Overview** — neden telefon numarasıyla eşleştirme kırılır
2. **Before you start**
3. **Creating a contact with your own ID**
4. **Looking it up by external ID**
5. **Updating: what goes where** — `POST` / `PATCH` farkı, telefon ve e-posta
6. **Custom fields**
7. **Companies and the link between them**
8. **Writing an idempotent sync** — upsert deseni, akış diyagramı
9. **The full example**
10. **When it fails**
11. **Next steps**

### G3. Blog — Türkçe

`blog/tr/external-id-ile-kisi-senkronizasyonu.md`

### G4. PR

---

## Kabul kriterleri

- [ ] A6 ölçülmüş: aynı `external_id` ile ikinci kayıt ne oluyor
- [ ] B2 ölçülmüş ve yazıda açıkça anlatılmış
- [ ] B4 ölçülmüş: telefon sayısı sınırı
- [ ] C bölümü tamam, özel alan yazma denenmiş
- [ ] D5 cevaplanmış: `assign` ile normal `PATCH` farkı
- [ ] Senkron iki kez çalıştırılmış, **kanıtı notta**
- [ ] Bir kayıttaki hata senkronu durdurmuyor, sonunda özet basılıyor
- [ ] Telefon doğru endpoint'ten yönetiliyor
- [ ] EN + TR blog standarda uyuyor
- [ ] Örnek veride gerçek numara/isim yok

---

## Bilerek söylemediklerim

1. Aynı `external_id` ikinci kez kullanılınca ne olduğu (A6)
2. `PATCH` ile telefon göndermenin sonucu (B2)
3. Bir kişiye kaç telefon eklenebildiği (B4)
4. Geçersiz özel alan `slug`'ının davranışı (C4)

---

## Yaygın hatalar

- **Telefon numarasıyla eşleştirmek.** Bu ödevin var olma sebebi.
- **Upsert yerine "önce sil, sonra oluştur".** Kaydın geçmişi, etiketleri ve
  çağrıları uçar.
- **Hata veren kayıtta senkronu durdurmak.** 5.000 kayıtlık senkron 3. kayıtta
  durursa kimse kullanamaz.
- **Her çalıştırmada her kaydı güncellemek.** `unchanged` durumunu tespit et;
  gereksiz istek rate limit'i yer.
- **Numarayı E.164 dışında yazmak.** Senkron kaynağında ne varsa değil, `+90…`
  biçiminde gönder.

---

## Teslim

Gözden geçirmede soracağım:

> Bu senkronu saatte bir çalışan bir işe koysam, bir hafta sonra Hipcall'da mükerrer
> kayıt olur mu?

---

**Sonraki ödev:** [`07-cevapsiz-cagri.md`](07-cevapsiz-cagri.md)
