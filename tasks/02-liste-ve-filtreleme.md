# Ödev 2 — Listeleri Çekmek: Sayfalama, Arama, Sıralama, Filtreleme

**Tahmini süre:** 4 gün
**Zorluk:** Giriş–orta
**Ön koşul:** [`01-api-anahtari.md`](01-api-anahtari.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

Ödev 1'de tek bir kayıt çektin: kendi profilin. Gerçek entegrasyonların neredeyse
tamamı ise **liste** çekiyor — çağrı kayıtları, kişiler, görevler.

Liste çekmek "GET at, gelsin" değil. Developer'ın ilk gün çarptığı duvarlar şunlar:

- 10.000 çağrısı var, istek 10 tane döndürüyor. Kalanı nasıl alacak?
- Toplam kaç kayıt olduğunu nereden bilecek?
- "Sadece cevapsız çağrılar" nasıl süzülür?
- Bir parametreyi yanlış yazdı. API hata mı veriyor, yoksa sessizce yok mu sayıyor?

Son madde en tehlikelisi. Yanlış yazılmış bir filtre sessizce yok sayılırsa,
developer "filtre çalışıyor" sanır ve **eksik veriyle** rapor üretir. Bunu üç ay
sonra fark eder.

Bu ödevde bu dört sorunun cevabını ölçeceksin ve yazacaksın.

### İskelet

Ödev 1'deki iskelet aynen geçerli:

```
HAZIRLIK → İSTEK ÖRNEĞİ → BAŞARILI CEVAP → BAŞARISIZ CEVAP → PARAMETRELER
```

---

## Ön koşullar

- Ödev 1'deki API anahtarın elinde ve çalışıyor.
- DEMO hesabında **veri** olmalı: en az 20-30 çağrı kaydı ve birkaç kişi. Yoksa
  Onur'dan iste — boş listeyle bu ödev yapılmaz.
- .NET SDK kurulu olmalı (`dotnet --version` çalışmalı, en az .NET 8). Kod
  örneklerini C# ile yazacaksın.
- [`blog/README.md`](../blog/README.md) standardını tekrar gözden geçir.

> ⚠️ Bu repo herkese açık. Betikte ve çıktılarda gerçek anahtar, telefon numarası
> veya müşteri adı bulunmayacak.

---

## Bölüm A — Sayfalama

Bu bölümde `GET /api/v3/calls` ile çalışacaksın.

1. Hiç parametre vermeden çağır. Kaç kayıt döndü? Cevabın **en üst düzey yapısı**
   ne? (Dizi mi, nesne mi? İçinde hangi anahtarlar var?)
2. Cevapta `data` dışında ne var? İçindeki her alanın ne anlama geldiğini yaz.
3. `limit` parametresini dene. **Üst sınırı bul:** 50 çalışıyor mu? 100? 101? 1000?
   Sınırı aştığında ne oluyor — kırpıyor mu, hata mı veriyor?
4. `limit=0` ve `limit=-5` gönder. Ne oluyor?
5. `offset` ile ikinci sayfayı çek. Birinci ve ikinci sayfada **aynı kayıt var mı**?
   Varsa sebebini düşün.
6. Toplam kayıt sayısını nereden öğreniyorsun? Bir developer "kaç sayfa çekmem
   lazım" sorusunu hangi formülle cevaplar?

---

## Bölüm B — Arama ve sıralama

1. `q` parametresiyle arama yap. `/api/v3/contacts` üzerinde bir isim ara. Sonra bir
   telefon numarası parçası ara. İkisi de çalışıyor mu?
2. `q` ile hiçbir şeye uymayan bir metin ara. Cevap ne? (Boş liste mi, 404 mü?
   `meta` ne diyor?)
3. `sort` parametresini dene. Biçimi ne? Artan/azalan nasıl belirtiliyor?
4. **Her endpoint aynı alanlarla sıralanamıyor.** `/api/v3/calls` hangi alanlarla,
   `/api/v3/contacts` hangi alanlarla sıralanabiliyor? İkisini karşılaştır.
5. Desteklenmeyen bir alanla sıralamayı dene (`sort=telefon_numarasi.asc` gibi).
   Ne oluyor? **Durum kodunu ve gövdeyi yapıştır.**
6. `sort` parametresini hiç göndermezsen liste hangi sıraya göre geliyor? Varsayılan
   sıralama her endpoint'te aynı mı?

---

## Bölüm C — Filtreleme

Hipcall'da filtreler **köşeli parantez** ile yazılıyor:

```
?alan[operatör]=değer
```

Örnek: `?started_at[gte]=2026-09-01T00:00:00Z`

1. `/api/v3/calls` üzerinde tarih aralığı filtresi kur: belirli iki tarih arasındaki
   çağrıları çek. Hangi operatörler destekleniyor? `eq` çalışıyor mu?
2. Çağrı yönüne göre filtrele. **Kritik soru:** değer olarak ne göndereceksin —
   sayı mı (`1`), yoksa kelime mi (`inbound`)? İkisini de dene, hangisinin
   çalıştığını ölç.
3. Birden fazla değerle filtrele (`in` operatörü). Değerleri nasıl ayırıyorsun?
4. Cevapsız çağrıları süzen filtreyi bul. Adı ne, hangi operatörü alıyor?
5. İki filtreyi aynı anda kullan (tarih + yön). Birlikte çalışıyorlar mı?
6. **En önemli madde:** `/api/v3/contacts` üzerinde de bir tarih filtresi dene,
   örneğin `?created_at[gte]=2026-01-01T00:00:00Z`.
   - Çalışıyor mu?
   - Çalışmıyorsa: hata mı veriyor, yoksa filtreyi **yok sayıp** bütün kişileri mi
     döndürüyor?
   - Cevabı çok dikkatli yaz. Bu ödevin en değerli bulgusu bu olabilir.
7. Var olmayan bir operatör dene: `?started_at[yakin]=...`. Ne oluyor?
8. Bozuk bir tarih gönder: `?started_at[gte]=dün`. Ne oluyor?

C6, C7 ve C8'in cevaplarını yan yana koy. Ortaya çıkan davranış developer için
iyi haber mi, kötü haber mi? Görüşünü yaz.

---

## Bölüm D — Küçük bir araç yaz

Artık parçaları biliyorsun. Şimdi gerçek bir iş yap.

**Görev:** Son 7 günün cevapsız çağrılarını çeken ve CSV'ye yazan bir C# konsol
uygulaması yaz.

```bash
dotnet new console -n Hipcall.MissedCalls
```

Uygulamanın şunları yapması gerekiyor:

- Anahtarı **koddan değil**, ortam değişkeninden okumalı:
  `Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN")`
- `HttpClient` kullanmalı ve **tek bir örnek** üzerinden gitmeli. Her istek için
  `new HttpClient()` açma — soket tükenmesine yol açar, .NET tarafında en sık
  yapılan hata budur.
- Tek sayfa değil, **bütün sayfaları** dolaşmalı.
- Ne zaman duracağını bilmeli. (İpucu: `meta` içindeki bilgiyi kullan; "boş sayfa
  gelene kadar dön" de bir yöntem ama neden daha kırılgan olduğunu yaz.)
- JSON'u `System.Text.Json` ile çözmeli. Üçüncü parti kütüphane ekleme.
- CSV çıktısı: tarih, arayan numara, aranan numara, süre.
- Hata durumunda susmamalı: 401, 422 veya 429 gelirse anlaşılır bir mesajla
  durmalı. `EnsureSuccessStatusCode()` yetmez — o sadece istisna fırlatır,
  cevabın gövdesindeki hata mesajını yutar. Gövdeyi oku ve yazdır.

Uygulama tek dosyada, 120 satırı geçmeyecek. Amaç kütüphane yazmak değil,
**kopyalanıp çalıştırılabilen bir örnek** üretmek.

Teslim yeri: `submissions/02-liste-ve-filtreleme/Hipcall.MissedCalls/`
(`Program.cs` + `.csproj`. `bin/` ve `obj/` klasörlerini commit etme.)

## Bölüm E — Community

Ödev 1'deki kurallar aynen geçerli.

- En az **1 yeni soru** aç (`Developers`, İngilizce) + Türkçe karşılığı.
- Bu ödevde konu doğal olarak şu: Bölüm C'de bulduğun, filtrelerin endpoint'e göre
  değişmesi meselesi. "Şu endpoint'te şu filtre var, bu endpoint'te neden yok?"
- Ödev 1'de açtığın konulara gelen cevaplara **dön ve kapat**. Açık kalan konu
  bırakma.

---

## Bölüm F — Teslim edeceklerin

### F1. Çalışma notu

**Repo:** `hipcall/2026-internship`
**Dosya:** `submissions/02-liste-ve-filtreleme.md`

- A, B, C bölümlerindeki her maddenin komutu + tam çıktısı
- **Bulgular** başlığı: özellikle C6/C7/C8'den çıkan sonuç
- **Karşılaştırma tablosu:** hangi endpoint hangi filtreleri destekliyor. En az
  `/calls`, `/contacts`, `/companies`, `/tasks` için doldur:

  | Endpoint | `q` | `sort` alanları | Bracket filtreler |
  |---|---|---|---|
  | `/api/v3/calls` | | | |
  | `/api/v3/contacts` | | | |
  | `/api/v3/companies` | | | |
  | `/api/v3/tasks` | | | |

- Bir **mermaid** diyagramı: sayfalama döngüsünün akışı (istek → meta oku →
  bitti mi? → sonraki offset). `flowchart` kullan.
- Community konu linkleri

### F2. Blog yazısı — İngilizce

**Dosya:** `blog/en/how-to-export-missed-calls-with-the-hipcall-api.md`

Kurallar [`blog/README.md`](../blog/README.md) dosyasında.

```yaml
---
title: "How to Export Your Missed Calls with the Hipcall API"
description: "Filter calls by date and status, page through every result, and write them to a CSV file with a short script."
slug: how-to-export-missed-calls-with-the-hipcall-api
lang: en
locales: [en, tr]
pubDate: 2026-09-29
categories: [developers]
intent: informational
translationKey: how-to-export-missed-calls-with-the-hipcall-api
tags: [api, calls, reporting, pagination]
authors: [hipcall-team]
featured: false
draft: true
task: 02
status: draft
---
```

Yapı:

1. **Overview** — ne elde edeceğiz (2-3 cümle)
2. **Before you start** — API anahtarı (Ödev 1'in yazısına `/blog/<slug>/` ile link
   ver), gereken izinler
3. **Understanding the response** — `data` + `meta`, sayfalama neden gerekli
4. **Filtering the calls you want** — tarih aralığı + cevapsız filtresi, çalışan
   istek örneği
5. **Paging through every result** — döngü mantığı, mermaid diyagramı
6. **The full example** — Bölüm D'de yazdığın C# uygulaması, çalışır hâlde
7. **When it fails** — 401, 422, 429. Hangi hata ne demek, ne yapmalı.
8. **Parameter reference** — kullandığın parametrelerin tablosu
9. **Next steps**

### F3. Blog yazısı — Türkçe

**Dosya:** `blog/tr/cevapsiz-cagrilari-hipcall-api-ile-disari-aktarma.md`

Aynı içerik, Türkçe, makine çevirisi değil. Kod ve JSON çevrilmez. Terim tablosu
standartta — bu ödevde özellikle şunlar geçecek: cevapsız çağrı, sayfalama, filtre,
istek/cevap, çağrı kaydı.

### F4. Pull request

Tek PR: çalışma notu + C# uygulaması + EN blog + TR blog.

Açıklamasında: ödev linki (`tasks/02-liste-ve-filtreleme.md`), community konu
linkleri, bulgular listesi.

---

## Kabul kriterleri

- [ ] A, B, C bölümlerinin **her maddesi** komut + tam çıktı içeriyor
- [ ] `limit` üst sınırı ölçülmüş (tahmin değil)
- [ ] C2 ölçülmüş: filtre değeri sayı mı kelime mi, denenerek bulunmuş
- [ ] C6/C7/C8 cevapları yan yana konmuş ve yorumlanmış
- [ ] Karşılaştırma tablosu 4 endpoint için dolu
- [ ] Mermaid `flowchart` diyagramı var
- [ ] Uygulama `dotnet run` ile çalışıyor — **ben kendi anahtarımla çalıştıracağım**
- [ ] Anahtar hard-code değil, ortam değişkeninden okunuyor
- [ ] Bütün sayfalar dolaşılıyor, tek sayfayla yetinilmiyor
- [ ] 401/422/429 durumunda anlaşılır mesajla duruluyor, hata gövdesi yazdırılıyor
- [ ] Tek `HttpClient` örneği kullanılıyor
- [ ] `bin/` ve `obj/` commit edilmemiş
- [ ] EN + TR blog var, frontmatter standarda uyuyor, `description` ≤ 160 karakter
- [ ] Blogdaki istek örnekleri kopyalanıp çalıştırılabiliyor
- [ ] Hiçbir yerde gerçek anahtar veya maskelenmemiş müşteri verisi yok
- [ ] Ödev 1'de açtığın community konuları kapatılmış

---

## Bilerek söylemediklerim

1. `limit`in üst sınırı
2. Filtre değerlerinin biçimi — sayı mı, kelime mi (C2)
3. Hangi endpoint'in hangi filtreleri desteklediği (C6)
4. Yanlış parametrenin sessizce yok mu sayıldığı, yoksa hata mı verdiği (C6/C7/C8)

Dördü de API referansında yazılı: <https://use.hipcall.com/api-docs/>. **Ama önce
dene, sonra referansa bak.** Sırası önemli: deneyip sonra okursan, referansın nerede
yetersiz kaldığını görürsün — yazacağın yazının değeri de tam olarak orada.

Denemeden referansa bakarsan, elinde referansın kopyası olur. Onu zaten yazdık.

---

## Yaygın hatalar

- **Tek sayfayla yetinmek.** `limit=100` koyup "hepsi geldi" sanmak. DEMO'da 30 kayıt
  varken çalışır, müşteride 30.000 kayıt varken çalışmaz.
- **`meta`yı görmezden gelmek.** Toplam sayı orada duruyor; boş sayfa bekleyerek
  döngü kurmak fazladan bir istek demek ve veri değişirse yanlış sonuç verir.
- **Sayfalama sırasında verinin değiştiğini unutmak.** Sen 1. sayfayı çekerken yeni
  bir çağrı gelirse, 2. sayfada kayıt tekrarlanabilir veya atlanabilir. Yazında buna
  bir cümle ayır.
- **Hata gövdesini yazmamak.** "422 döner" yetmez. Gövdesi ne diyor? Developer hatayı
  o metinden bulacak.
- **Anahtarı koda gömmek.** Kopyalayan developer onu git'e gönderir.
- **Her istekte yeni `HttpClient` açmak.** Kısa sürede soket tükenir; `using var client = new HttpClient()` bir döngünün içindeyse yanlıştır.
- **Tarihleri yerel saatle yazmak.** Bütün örneklerde UTC ve ISO 8601 kullan
  (`2026-09-01T00:00:00Z`). Saat dilimi karışıklığı en sık gelen destek sorusu.

---

## Teslim

Gözden geçirmede kovalayacağım soru:

> Bu yazıyı okuyan developer, 30.000 çağrısı olan bir müşteri hesabında **eksiksiz**
> rapor üretebilir mi?

"Eksiksiz" kelimesinin altını çiziyorum. Çalışan bir örnek vermek kolay; eksik veri
üretmeyen bir örnek vermek bu ödevin konusu.

---

**Sonraki ödev:** [`03-click-to-call.md`](03-click-to-call.md)
