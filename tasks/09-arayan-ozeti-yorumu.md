# Ödev 9 — Arayanın Özetini Çağrı Kaydına Yazmak

**Tahmini süre:** 2 gün
**Zorluk:** Orta
**Ön koşul:** [`08-kisa-cagri-etiketleme.md`](08-kisa-cagri-etiketleme.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

Ödev 5'te arayanın bilgisini ajanın ekranına bastın. O bilgi çağrı bitince kayboldu —
kart canlı çağrıya ait, kalıcı değil.

Bu ödevde aynı bilgiyi **kalıcı** hâle getireceksin: çağrı kaydına bir yorum olarak.
Üç ay sonra o çağrıyı açan kişi, arayanın o gün hangi siparişi beklediğini,
bakiyesinin ne olduğunu görecek.

Aradaki fark önemli ve yazında bunu anlatman gerekiyor: **Insight Card anlıktır,
yorum kayıttır.** Biri ajanın o anki işine yarar, diğeri sonradan bakan herkese.

Serinin sonuncusu:

| Ödev | Koşul | Aksiyon |
|---|---|---|
| 07 | Cevapsız gelen çağrı | Sonuç kodu + takip görevi |
| 08 | 10 saniyeden kısa çağrı | Etiket |
| **09** | **Tanıdığın arayan** | **Yorum olarak müşteri özeti** |

---

## Ön koşullar

- Ödev 8'deki `Hipcall.PostCall` projen çalışıyor, iki kural ayakta.
- Ödev 6'daki senkron mantığı elinde (`external_id`).
- Küçük bir "CRM": 5-10 kayıtlık JSON dosyası yeterli. Ödev 5'te kullandığını
  tekrar kullanabilirsin.

---

## Bölüm A — Arayan kim? Üç yol

Arayanı tanımanın üç yolu var ve üçü farklı şeyler döndürüyor. Üçünü de ölç.

1. **Hipcall zaten eşleştirmiş mi?** Çağrı kaydında kişi ve firmaya işaret eden
   alanlar var. Bul. Ne zaman dolu, ne zaman boş?
2. Bir çağrı yap, arayan numarası Hipcall'da kayıtlı **olmayan** bir numara olsun.
   A1'deki alanlar ne oluyor?
3. Aynı numarayı Hipcall'a kişi olarak ekle, tekrar ara. Şimdi dolu mu?
4. **`GET /api/v3/lookup/by_phone`** ne döndürüyor? A1'deki alanlardan farkı ne?
   Hangi durumda birini, hangi durumda diğerini kullanırsın?
5. **Kendi CRM'in.** Numara ile kendi kayıtlarında ara. Ödev 6'da `external_id` ile
   eşleştirme yapmıştın — burada numaradan mı gidiyorsun, yoksa önce Hipcall'daki
   kişiyi bulup `external_id`'sinden mi? İkisini karşılaştır, hangisinin daha
   güvenilir olduğunu gerekçelendir.
6. Numara biçimi: gelen çağrıda numara hangi biçimde geliyor? Kendi CRM'indeki
   kayıtla eşleşiyor mu? Eşleşmiyorsa **normalizasyonu nerede yapacaksın?**

A5 ve A6 bu ödevin iki gerçek problemi. A6'yı hafife alma — entegrasyonların
sessizce çalışmama sebebi çoğu zaman bu.

---

## Bölüm B — Yorum API'si

1. `POST /api/v3/calls/{call_id}/comments` ile yorum ekle. Gövde ne bekliyor?
2. Uzunluk sınırı var mı? Uzun bir metin göndererek bul.
3. Biçimlendirme destekleniyor mu? Satır sonu, markdown, HTML — üçünü de dene,
   panelde nasıl göründüğüne bak.
4. `GET /api/v3/calls/{call_id}/comments` ile oku. Dönen alanlar neler?
5. **Yorum kimin adına yazılıyor?** Panelde "kim yazdı" olarak ne görünüyor?
   API anahtarı bir kullanıcıya bağlı mı?
6. B5'in sonucu müşteri için ne anlama geliyor? Panelde entegrasyonun yazdığı
   notlarla insanların yazdığı notlar ayırt edilebiliyor mu? Edilemiyorsa ne
   önerirsin?
7. Aynı çağrıya birden çok yorum eklenebiliyor mu? Sıralama nasıl?
8. Yorum silinebiliyor veya düzenlenebiliyor mu? API'de karşılığı var mı?

B5 ve B6 bu ödevin en önemli bulgusu. Müşterinin soracağı ilk soru bu olacak.

---

## Bölüm C — Özeti tasarla

Teknik iş kolay; asıl zor olan **ne yazılacağı.**

1. Yorumun içinde ne olmalı? En fazla 5 madde seç ve her birini neden seçtiğini
   yaz. Adaylar: müşteri adı, firma, müşteri numarası, bakiye, açık sipariş, son
   sipariş durumu, üyelik seviyesi, açık destek kaydı, son çağrı tarihi.
2. Yorumda **olmaması** gereken şeyler neler? En az üç örnek. (İpucu: çağrı kaydı
   yıllarca duruyor ve çok kişi görüyor.)
3. Kaç satır makul? Panelde gerçekten dene: 3 satırlık ve 20 satırlık yorum nasıl
   görünüyor?
4. Kendi sisteminde kaydı **bulunamayan** arayan için ne yapacaksın? Yorum yazmak
   mı, hiçbir şey yapmamak mı? Kararını gerekçelendir.
5. Bilgi zamanla eskiyor: yorum çağrı anındaki bakiyeyi yazıyor, üç ay sonra bakan
   kişi onu güncel sanabilir. Bunu nasıl önlersin? (Basit bir çözüm var.)

C2 ve C5 yazının en faydalı bölümleri olacak; çoğu entegrasyon ikisini de atlıyor.

---

## Bölüm D — Kuralı ekle

`Hipcall.PostCall` projesini genişlet — üçüncü ve son kural.

Akış:

1. `call_hangup` geldi.
2. Gelen çağrı mı?
3. Arayanı kendi sisteminde bul (A bölümündeki yöntemin).
4. Bulduysan özeti oluştur (C bölümündeki tasarımın) ve yorum olarak yaz.
5. Bulamadıysan C4'teki kararını uygula.
6. `200` dön.

Kurallar:

- Üç kural artık yan yana çalışıyor. **Birinin hatası diğer ikisini durdurmamalı.**
  Bunu test et: CRM sorgusunu kasten patlat, Ödev 7 ve 8'in kuralları çalışmaya
  devam ediyor mu?
- Kendi CRM sorgun yavaşsa HTTP cevabı beklemeyecek.
- İdempotency: aynı olay iki kez gelirse ikinci yorum yazılmamalı.
- Üç kural da tek bir yerde kayıtlı olmalı; dördüncüsünü eklemek tek satır olmalı.

Teslim: `submissions/09-arayan-ozeti-yorumu/Hipcall.PostCall/`

Bu, serinin tamamlanmış hâli: bir webhook alıcısı, üç kural, ayrıştırılmış yapı.

---

## Bölüm E — Community

- Yeni soru: B5/B6 (yorumu kim yazmış görünüyor) veya A6 (numara normalizasyonu).
- Eski konuları kapat.

---

## Bölüm F — Teslim edeceklerin

### F1. Çalışma notu

`submissions/09-arayan-ozeti-yorumu.md`

- A bölümünün üç yol karşılaştırması: tablo hâlinde, hangisi ne zaman dolu
- A6: numara biçimi ve normalizasyon kararı
- B bölümünün çıktıları, özellikle B5/B6
- C bölümünün tasarım kararları, gerekçeleriyle
- Mermaid `flowchart`: üç kuralın birlikte değerlendirilmesi
- **Kural motorunun son hâli**: dördüncü kuralı eklemek için ne yapmak gerekiyor?
- Community linkleri

### F2. Blog — İngilizce

`blog/en/how-to-write-a-caller-summary-on-every-call-record.md`

```yaml
title: "How to Write a Caller Summary on Every Call Record"
description: "Match the caller to your own records and leave a permanent note on the call — what the Insight Card shows live, kept for whoever opens the record later."
slug: how-to-write-a-caller-summary-on-every-call-record
lang: en
locales: [en, tr]
categories: [developers]
intent: informational
translationKey: how-to-write-a-caller-summary-on-every-call-record
tags: [api, calls, comments, crm, dotnet]
task: 09
```

Yapı:

1. **Overview** — kart anlıktır, yorum kayıttır
2. **Before you start**
3. **Three ways to identify the caller** — A bölümü, karşılaştırma tablosu
4. **Phone number formats** — A6, normalizasyon
5. **Writing the comment** — B bölümü, kim yazmış görünüyor
6. **What belongs in the summary — and what doesn't** — C bölümü. Bu yazının
   ayırt edici bölümü.
7. **Wiring it to the webhook** — üçüncü kural, üçünün birlikte çalışması
8. **The full example** — serinin tamamlanmış projesi
9. **When it fails**
10. **Next steps**

### F3. Blog — Türkçe

`blog/tr/arayan-ozetini-cagri-kaydina-yazma.md`

### F4. PR

---

## Kabul kriterleri

- [ ] A1–A4 ölçülmüş, üç yol tablo hâlinde karşılaştırılmış
- [ ] A5 gerekçelendirilmiş
- [ ] A6 ölçülmüş: numara biçimi ve normalizasyon çözümü uygulanmış
- [ ] B2 ve B3 ölçülmüş
- [ ] B5 cevaplanmış: yorumu kim yazmış görünüyor
- [ ] B6'da entegrasyon notlarının ayırt edilmesi için bir öneri var
- [ ] B8 ölçülmüş
- [ ] C1'de en fazla 5 madde seçilmiş, her biri gerekçeli
- [ ] C2'de en az üç "yazılmamalı" örneği var
- [ ] C4 ve C5 cevaplanmış
- [ ] CRM sorgusu patlatıldığında diğer iki kural çalışmaya devam ediyor — **test
      ederek göster**
- [ ] Aynı olay iki kez gelince ikinci yorum yazılmıyor
- [ ] Dördüncü kural eklemek tek satır — notta gösterilmiş
- [ ] Örnek verilerde gerçek isim, numara, bakiye yok
- [ ] EN + TR blog standarda uyuyor

---

## Bilerek söylemediklerim

1. Çağrı kaydındaki kişi/firma alanlarının ne zaman dolduğu (A1–A3)
2. Gelen numaranın biçimi (A6)
3. Yorumun panelde kimin adına göründüğü (B5)
4. Yorumun silinip düzenlenebildiği (B8)

---

## Yaygın hatalar

- **Numara biçimini varsaymak.** Kendi CRM'inde `0555…`, çağrıda `+90555…` ise
  hiçbir eşleşme olmaz ve kod hatasız çalışır — en sinsi hata tipi.
- **Yoruma hassas veri yazmak.** Kart ekranda 30 saniye durur, yorum kayıtta
  yıllarca. TC kimlik, kart numarası, adres, sağlık bilgisi yazma.
- **Tarih yazmamak.** Bakiye üç ay sonra yanlış bilgi hâline gelir; özete "şu tarih
  itibarıyla" ibaresi koy.
- **Bulunamayan arayan için boş yorum yazmak.** "Müşteri bulunamadı" notu kimseye
  yaramaz, kaydı kirletir.
- **Üç kuralı tek metoda yığmak.** Bu ödevin sonunda kod okunabilir olmalı.
- **CRM sorgusunu cevaptan önce beklemek.** Yavaş sorgu webhook'u zaman aşımına
  düşürür.

---

## Teslim

Bu, üç ödevlik serinin sonu. Gözden geçirmede beraber bir çağrı yapıp üç kuralın da
çalıştığını izleyeceğiz. Soracağım:

> Üç ay sonra bu çağrı kaydını açan bir ajan, o gün ne olduğunu anlayabilir mi?

---

**Sonraki ödev:** [`10-external-management.md`](10-external-management.md)
