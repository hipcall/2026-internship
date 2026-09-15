# Ödev 7 — Çağrı Sonrası İşleri Otomatikleştirmek

**Tahmini süre:** 3 gün
**Zorluk:** Orta
**Ön koşul:** [`06-kisi-firma-senkronu.md`](06-kisi-firma-senkronu.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

Çağrı bitti. Şimdi birinin şunları yapması gerekiyor: sonucu işaretle, etiketle,
notu yaz, gerekiyorsa takip görevi aç.

Çağrı merkezlerinde buna "wrap-up" deniyor ve ajanın gününün ciddi bir kısmını
yiyor. Her çağrıdan sonra 40 saniye × günde 80 çağrı = bir saat. Bunun çoğu
otomatikleştirilebilir: sonucu sistem zaten biliyor, notu kendi sisteminden
çekebilirsin, görevi kural ile açabilirsin.

Bu ödevde çağrı kaydını zenginleştiren dört API'yi öğreneceksin ve `call_hangup`
webhook'una bağlayacaksın.

---

## Ön koşullar

- Ödev 4'teki webhook alıcın çalışıyor.
- DEMO hesabında çağrı sonuç kodları (disposition) tanımlı olmalı. Yoksa panelden
  oluştur — menü yolunu not et.

---

## Bölüm A — Çağrı sonuç kodu (disposition)

1. `GET /api/v3/dispositions` ne döndürüyor? Her kaydın alanlarını listele.
2. Bir sonuç kodunun hem `id`'si hem `code`'u var. İkisi ne işe yarıyor?
3. Bitmiş bir çağrıya sonuç kodu yaz: `PUT /api/v3/calls/{call_id}/disposition`.
   En küçük geçerli gövde nedir?
4. `disposition_id` ve `disposition_code` alanlarının **ikisini birden** gönder.
   Ne oluyor?
5. **Hiçbirini** gönderme. Ne oluyor?
6. `GET /api/v3/calls/{call_id}/disposition` ile geri oku.
7. Aynı çağrının sonucunu **değiştir**. Kabul ediliyor mu?
8. Çok eski bir çağrının (birkaç gün önceki) sonucunu değiştirmeyi dene. Ne oluyor?
   Hata alıyorsan gövdesi ne diyor?
9. Gelen çağrıya, sadece giden çağrılar için tanımlı bir sonuç kodu yazmayı dene.

A8 ve A9 bu ödevin iki tuzağı. İkisi de dokümanda görünmüyor ve ikisi de üretimde
"neden 422 alıyorum" biletine dönüşüyor. Bulgularını net yaz.

---

## Bölüm B — Etiket ve yorum

1. `GET /api/v3/tags` ne döndürüyor?
2. Bir çağrıya etiket ekle: `POST /api/v3/calls/{call_id}/tags`. Gövde ne?
3. Var olmayan bir etiket adı göndermeyi dene. Yeni etiket oluşturuluyor mu, hata mı?
4. Aynı etiketi iki kez ekle.
5. Etiketi kaldır. Hangi endpoint, hangi biçim?
6. `POST /api/v3/calls/{call_id}/comments` ile yorum ekle. Uzunluk sınırı var mı?
7. Yorumlar kimin adına yazılıyor? API anahtarı bir kullanıcıya mı bağlı? Panelde
   yorumu kim yazmış görünüyor?

B7 önemli: entegrasyonun yazdığı notların panelde kim tarafından yazılmış göründüğü,
müşterinin soracağı ilk sorulardan biri.

---

## Bölüm C — Görev açma

1. `POST /api/v3/tasks` ile görev oluştur. Zorunlu alanlar neler?
2. Görevi bir kullanıcıya ata. Hangi alan?
3. Görevi bir kişiye/firmaya bağlayabiliyor musun?
4. Son tarih (due date) alanı var mı? Biçimi ne? Saat dilimi nasıl yorumlanıyor?
5. `GET /api/v3/tasks` ile listele, filtreleri dene (Ödev 2'deki bilgi burada işine
   yarayacak).
6. `PATCH` ile görevi tamamlandı işaretle.

---

## Bölüm D — Kuralı yaz

Ödev 4'teki alıcıyı genişlet: `call_hangup` olayı geldiğinde kurallar çalışsın.

En az üç kural yaz:

| Koşul | Aksiyon |
|---|---|
| Cevapsız gelen çağrı | Sonuç kodu yaz + kişiye atanmış takip görevi aç |
| 10 saniyeden kısa çağrı | Etiket ekle |
| Kendi sisteminde kaydı olan arayan | Yorum olarak müşteri özetini yaz |

Kurallar:

- Kurallar veriden ayrı olsun: bir `IRule` arayüzü veya basit bir strateji listesi.
  Yeni kural eklemek için `if` zincirine dokunmak gerekmesin.
- Her aksiyon ayrı ayrı hata verebilir. Biri patlarsa diğerleri çalışmaya devam
  etmeli, ama hata **yutulmamalı** — loglanmalı.
- Aynı olay iki kez gelirse aksiyonlar iki kez uygulanmamalı (Ödev 4'teki
  idempotency bilgisi).
- Aksiyonlar HTTP cevabını bekletmemeli.

Teslim: `submissions/07-cagri-sonrasi-otomasyon/Hipcall.PostCall/`

---

## Bölüm E — Community

- Yeni soru: A8 (düzenleme penceresi) veya B7 (yorumu kim yazmış görünüyor).
- Eski konuları kapat.

---

## Bölüm F — Teslim edeceklerin

### F1. Çalışma notu

`submissions/07-cagri-sonrasi-otomasyon.md`

- A–C bölümlerinin çıktıları
- A8 ve A9 bulguları ayrı başlık altında
- Kural tablosu ve her kuralın hangi API çağrılarına dönüştüğü
- Mermaid `flowchart`: `call_hangup` → kural değerlendirme → aksiyonlar
- Community linkleri

### F2. Blog — İngilizce

`blog/en/how-to-automate-post-call-work-with-the-hipcall-api.md`

```yaml
title: "How to Automate Post-Call Work with the Hipcall API"
description: "Set dispositions, add tags and notes, and open follow-up tasks automatically when a call ends, driven by the call_hangup webhook."
slug: how-to-automate-post-call-work-with-the-hipcall-api
lang: en
locales: [en, tr]
categories: [developers]
intent: informational
translationKey: how-to-automate-post-call-work-with-the-hipcall-api
tags: [api, calls, dispositions, tasks, dotnet]
task: 07
```

Yapı:

1. **Overview** — wrap-up süresi problemi
2. **Before you start**
3. **Setting the call disposition** — `id` vs `code`, kurallar, düzenleme penceresi
4. **Tags and notes**
5. **Opening a follow-up task**
6. **Driving it from the webhook** — kural motoru, diyagram
7. **The full example**
8. **When it fails** — A8/A9'un çıktısı burada
9. **Parameter reference**
10. **Next steps**

### F3. Blog — Türkçe

`blog/tr/cagri-sonrasi-islemleri-otomatiklestirme.md`

### F4. PR

---

## Kabul kriterleri

- [ ] A2 cevaplanmış: `id` ve `code` ne zaman hangisi
- [ ] A4 ve A5 ölçülmüş
- [ ] A8 ölçülmüş: eski çağrının sonucu değiştirilebiliyor mu, sınır ne
- [ ] A9 ölçülmüş: yön uyuşmazlığı
- [ ] B3 ve B4 ölçülmüş
- [ ] B7 cevaplanmış: yorumu kim yazmış görünüyor
- [ ] C4 cevaplanmış: son tarih biçimi ve saat dilimi
- [ ] En az üç kural yazılmış ve kurallar koddan ayrılmış
- [ ] Bir aksiyonun hatası diğerlerini durdurmuyor, hata loglanıyor
- [ ] Aynı olay iki kez gelince aksiyonlar tekrarlanmıyor
- [ ] EN + TR blog standarda uyuyor

---

## Bilerek söylemediklerim

1. Aynı anda hem `disposition_id` hem `disposition_code` gönderince ne olduğu (A4)
2. Eski bir çağrının sonucunun değiştirilip değiştirilemediği (A8)
3. Yön uyuşmazlığının sonucu (A9)
4. Var olmayan etiket adının davranışı (B3)

---

## Yaygın hatalar

- **Sonuç kodunu sabit sayıyla kodlamak.** `disposition_id: 7` başka bir hesapta
  başka bir şeydir. Örneklerde `code` kullanmayı tercih et ve nedenini yaz.
- **Saat dilimini belirtmemek.** Son tarihte UTC/ISO 8601 kullan.
- **Kural motorunu `if/else` yığınına çevirmek.** Müşteri dördüncü kuralı isteyince
  kod okunamaz hâle gelir.
- **Aksiyon hatasını yutmak.** `catch { }` boş bırakılırsa görev açılmadığını kimse
  fark etmez.
- **Her çağrıya yorum yazmak.** Gürültü. Kural, ne zaman yazılacağını da söylemeli.

---

## Teslim

Gözden geçirmede soracağım:

> Müşteri "şu durumda da şunu yap" dediğinde, kaç satır kod değişiyor?

---

**Sonraki ödev:** [`08-external-management.md`](08-external-management.md)
