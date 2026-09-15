# Ödev 7 — Cevapsız Çağrıyı Kaydetmek ve Takibe Almak

**Tahmini süre:** 2 gün
**Zorluk:** Orta
**Ön koşul:** [`06-kisi-firma-senkronu.md`](06-kisi-firma-senkronu.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

Cevapsız çağrı, çağrı merkezinin en pahalı olayı. Arayan bir müşteri, bir sipariş,
bir şikâyet — ve kimse geri dönmezse kaybediliyor.

Çoğu ekipte bu iş elle yürüyor: birisi gün sonunda rapora bakıyor, listeyi çıkarıyor,
ajanlara dağıtıyor. Arada geçen süre 6-8 saat, bazen ertesi gün.

Bu ödevde otomatikleştireceksin: çağrı cevapsız kapandığı anda sonuç kodu yazılacak
ve sorumlu kişiye takip görevi açılacak. Toplam gecikme: birkaç saniye.

Bu, üç parçalı bir serinin ilki. Üçü de aynı webhook alıcısının üstüne kuruluyor:

| Ödev | Koşul | Aksiyon |
|---|---|---|
| **07** | **Cevapsız gelen çağrı** | **Sonuç kodu + takip görevi** |
| 08 | 10 saniyeden kısa çağrı | Etiket |
| 09 | Tanıdığın arayan | Yorum olarak müşteri özeti |

---

## Ön koşullar

- Ödev 4'teki webhook alıcın çalışıyor ve `call_hangup` olayını alıyor.
- DEMO hesabında çağrı sonuç kodları (disposition) tanımlı olmalı. Yoksa panelden
  oluştur — menü yolunu not et.
- Cevapsız bırakabileceğin bir gelen numara.

---

## Bölüm A — Cevapsızı tanı

Önce şu soruyu cevapla: bir çağrının cevapsız olduğunu **nereden** anlıyorsun?

1. Birkaç çağrı üret ve `GET /api/v3/calls/{id}` ile karşılaştır:
   - Cevaplanmış gelen çağrı
   - Hiç açılmayan gelen çağrı
   - Ajan açtı ama müşteri hemen kapattı
   - Sesli mesaja düşen çağrı
2. Hangi alan(lar) bu durumları ayırıyor? En az iki aday alan bulacaksın. İkisini de
   yaz ve farklarını açıkla.
3. Cevapsızlığın **sebebini** taşıyan bir alan var. Hangi değerleri alabiliyor?
   Her değeri üretmeye çalış, üretebildiklerini listele.
4. `answered_at` ve `bridged_at` alanları ne zaman dolu, ne zaman boş? İkisi
   arasındaki fark ne?
5. Giden çağrı da "cevapsız" olabilir mi? Dene. Bu ödevde neden sadece **gelen**
   cevapsızla ilgileniyoruz?
6. `GET /api/v3/calls` üzerinde cevapsızları süzen filtreyi kur (Ödev 2'deki bilgi).
   Filtreyle gelen sonuç, A2'de bulduğun alana göre elle ayıkladığınla aynı mı?

A2 bu ödevin ilk tuzağı: yanlış alanı seçersen sesli mesaja düşen çağrılar ya
sayılmaz ya da iki kez sayılır.

---

## Bölüm B — Sonuç kodu yaz

1. `GET /api/v3/dispositions` ne döndürüyor? Her kaydın alanlarını listele.
2. Bir sonuç kodunun hem `id`'si hem `code`'u var. Entegrasyonda hangisini
   kullanmalısın? Gerekçeni yaz. (İpucu: aynı kodu iki farklı hesapta çalıştırmayı
   düşün.)
3. Cevapsız bir çağrıya sonuç kodu yaz: `PUT /api/v3/calls/{call_id}/disposition`.
   En küçük geçerli gövde nedir?
4. `disposition_id` ve `disposition_code` alanlarının **ikisini birden** gönder.
   Ne oluyor?
5. **Hiçbirini** gönderme. Ne oluyor?
6. `GET /api/v3/calls/{call_id}/disposition` ile geri oku.
7. Aynı çağrının sonucunu **değiştir**. Kabul ediliyor mu?
8. Birkaç gün önceki bir çağrının sonucunu değiştirmeyi dene. Ne oluyor? Hata
   alıyorsan gövdesi tam olarak ne diyor?
9. Gelen çağrıya, sadece giden çağrılar için tanımlı bir sonuç kodu yazmayı dene.

B8 ve B9 bu ödevin iki tuzağı. İkisi de dokümanda görünmüyor ve ikisi de üretimde
"neden 422 alıyorum" biletine dönüşüyor.

---

## Bölüm C — Takip görevi aç

1. `POST /api/v3/tasks` ile görev oluştur. Zorunlu alanlar neler?
2. Görevi bir kullanıcıya ata. Hangi alan?
3. Görevi bir kişiye veya firmaya bağlayabiliyor musun? Bağlarsan panelde nerede
   görünüyor?
4. Son tarih (due date) alanı var mı? Biçimi ne? Saat dilimi nasıl yorumlanıyor?
   **Yerel saatle mi UTC ile mi** yorumlandığını ölçerek bul.
5. `GET /api/v3/tasks` ile listele, filtreleri dene.
6. `PATCH` ile görevi tamamlandı işaretle.
7. **Görev kime atanmalı?** Cevapsız çağrıda bunu belirlemenin birkaç yolu var:
   - Çağrının yönlendiği kullanıcı (`user_id`)
   - Arayanın Hipcall'daki kişi kaydının sahibi
   - Sabit bir kullanıcı

   Üçünü de çağrı kaydındaki alanlardan çıkarmayı dene. Hangisi her zaman dolu,
   hangisi bazen boş? Boş kaldığında ne yapacaksın?

C7 gerçek bir tasarım kararı ve yazının en faydalı bölümü olacak.

---

## Bölüm D — Kuralı yaz

Ödev 4'teki alıcıyı genişlet.

Akış:

1. `call_hangup` geldi.
2. Gelen çağrı mı? Değilse çık.
3. Cevapsız mı? (A2'deki alanı kullan.) Değilse çık.
4. Sonuç kodunu yaz.
5. Sorumluyu belirle (C7), takip görevi aç.
6. `200` dön.

Kurallar:

- Aksiyonlar HTTP cevabını bekletmemeli.
- Sonuç kodu yazma başarısız olursa görev yine de açılmalı — **ve tersi**. Biri
  patlarsa diğeri çalışmaya devam etmeli, ama hata **yutulmamalı**, loglanmalı.
- Aynı olay iki kez gelirse ikinci görev açılmamalı (Ödev 4'teki idempotency).
- Sonuç kodu sabit sayıyla değil, koduyla seçilmeli (B2'nin gerekçesi).

Teslim: `submissions/07-cevapsiz-cagri/Hipcall.PostCall/`

Bu proje 08 ve 09'da da büyüyecek; baştan genişlemeye uygun kur.

---

## Bölüm E — Community

- Yeni soru: B8 (düzenleme penceresi) veya C7 (sorumlu kimdir).
- Eski konuları kapat.

---

## Bölüm F — Teslim edeceklerin

### F1. Çalışma notu

`submissions/07-cevapsiz-cagri.md`

- A–C bölümlerinin çıktıları
- **Cevapsızlık tablosu**: dört senaryo (A1) × ilgili alanlar. Hangi alan hangi
  durumda ne değer alıyor?
- B8 ve B9 bulguları ayrı başlık altında
- C7'nin cevabı: sorumlu belirleme stratejisi ve boş kalma durumları
- Mermaid `flowchart`: `call_hangup` → gelen mi? → cevapsız mı? → sonuç kodu +
  görev
- Community linkleri

### F2. Blog — İngilizce

`blog/en/how-to-log-and-follow-up-missed-calls-automatically.md`

```yaml
title: "How to Log and Follow Up Missed Calls Automatically"
description: "Detect a missed call from the webhook, write its disposition, and open a follow-up task for the right person — within seconds of the hangup."
slug: how-to-log-and-follow-up-missed-calls-automatically
lang: en
locales: [en, tr]
categories: [developers]
intent: informational
translationKey: how-to-log-and-follow-up-missed-calls-automatically
tags: [api, calls, dispositions, tasks, dotnet]
task: 07
```

Yapı:

1. **Overview** — 8 saatlik gecikme problemi
2. **Before you start** — webhook alıcısı (Ödev 4'ün yazısına link), sonuç kodları
3. **Telling a missed call from an answered one** — A bölümü, alan tablosu
4. **Writing the disposition** — `id` vs `code`, kurallar
5. **Opening the follow-up task** — sorumlu kimdir, son tarih ve saat dilimi
6. **Wiring it to the webhook** — akış diyagramı
7. **The full example** — C# projesi
8. **When it fails** — B8/B9 ve sorumlu bulunamama durumu
9. **Parameter reference**
10. **Next steps** — serinin 08 ve 09'una işaret et

### F3. Blog — Türkçe

`blog/tr/cevapsiz-cagrilari-otomatik-kaydetme-ve-takibe-alma.md`

### F4. PR

---

## Kabul kriterleri

- [ ] A1'deki dört senaryo gerçekten üretilmiş, tablo dolu
- [ ] A2 cevaplanmış: en az iki aday alan ve farkları
- [ ] A3'te cevapsızlık sebebinin alabildiği değerler listelenmiş
- [ ] A6'da filtre sonucu elle ayıklamayla karşılaştırılmış
- [ ] B2'de `id` / `code` seçimi gerekçelendirilmiş
- [ ] B4, B5 ölçülmüş
- [ ] B8 ölçülmüş: eski çağrının sonucu değiştirilebiliyor mu, sınır ne
- [ ] B9 ölçülmüş: yön uyuşmazlığı
- [ ] C4 ölçülmüş: son tarihin saat dilimi yorumu
- [ ] C7 cevaplanmış, boş kalma durumu ele alınmış
- [ ] Bir aksiyonun hatası diğerini durdurmuyor, hata loglanıyor
- [ ] Aynı olay iki kez gelince ikinci görev açılmıyor — **test ederek göster**
- [ ] Sonuç kodu sabit `id` ile değil `code` ile seçiliyor
- [ ] EN + TR blog standarda uyuyor

---

## Bilerek söylemediklerim

1. Cevapsızlığı hangi alanın kesin olarak söylediği (A2)
2. Aynı anda hem `disposition_id` hem `disposition_code` gönderince ne olduğu (B4)
3. Eski bir çağrının sonucunun değiştirilip değiştirilemediği (B8)
4. Son tarihin hangi saat dilimine göre yorumlandığı (C4)

---

## Yaygın hatalar

- **Sonuç kodunu sabit sayıyla kodlamak.** `disposition_id: 7` başka bir hesapta
  başka bir şeydir.
- **Sesli mesaja düşen çağrıyı cevapsız saymamak** (ya da tersi). A bölümünün
  varlık sebebi.
- **Giden çağrıları da işlemek.** Ajanın açmadığı giden çağrı için müşteriye görev
  açmak anlamsız.
- **Sorumlu boşsa görevi hiç açmamak.** Sahipsiz çağrı en çok takip edilmesi gereken
  çağrıdır — bir yedek sorumlu belirle.
- **Saat dilimini belirtmemek.** Son tarihte UTC/ISO 8601 kullan.
- **Aksiyon hatasını yutmak.** `catch { }` boş bırakılırsa görev açılmadığını kimse
  fark etmez.

---

## Teslim

Gözden geçirmede beraber bir çağrıyı cevapsız bırakacağız. Soracağım:

> Görev kimde açıldı, ne zaman açıldı, ve içinde ajanın geri dönmek için ihtiyaç
> duyduğu her şey var mı?

---

**Sonraki ödev:** [`08-kisa-cagri-etiketleme.md`](08-kisa-cagri-etiketleme.md)
