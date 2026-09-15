# Ödev 1 — API Anahtarı ve İlk İstek

**Tahmini süre:** 3 gün
**Zorluk:** Giriş seviyesi — ama sonraki bütün ödevler bunun üstüne kurulacak.

---

## Neden bu ödev?

Hipcall'ın API referansı var: <https://use.hipcall.com/api-docs/>. Referansta 80'e
yakın endpoint listeli, her biri parametreleriyle yazılı.

Sorun referansta değil. Bir developer bize geldiğinde referansı açıyor, 80 endpoint
görüyor ve "peki ben nereden başlayacağım?" diye soruyor. O sorunun cevabı hiçbir
yerde yazmıyor.

Senin işin o cevabı yazmak.

### Yazacağımız her how-to sayfasının iskeleti

Bundan sonraki bütün ödevlerde bu iskelete uyacaksın:

```
HAZIRLIK → İSTEK ÖRNEĞİ → BAŞARILI CEVAP → BAŞARISIZ CEVAP → PARAMETRELER
```

En kritik bölüm **HAZIRLIK**. Orada "panelde şu menüden şunu al, şunu aç" yazar.
Developer'ı kaybettiğimiz yer tam olarak burası — kod değil, "anahtarı nereden
alacağım" sorusu. Kod örneğini herkes yazabiliyor; HAZIRLIK bölümünü yazmak için
ürünü kullanmış olman gerekiyor.

Bu ödevde o HAZIRLIK bölümünü yazacaksın.

---

## Ön koşullar

- DEMO ortamında hesabın olmalı. Yoksa Onur'dan iste, ödeve başlama.
- `curl` kurulu olmalı (`curl --version` çalışmalı).
- <https://community.hipcall.com/> üzerinde hesap aç.
- `hipcall/2026-internship` reposuna yazma yetkin olmalı. Bütün çıktıların oraya
  gidiyor.
- [`blog/README.md`](../blog/README.md) — blog yazım standardını **baştan sona oku**.
  Yazacağın her yazı ona uyacak.

> ⚠️ `hipcall/2026-internship` **herkese açık bir repo.** Yazdığın her şey
> internette görünür. Gerçek API anahtarı, müşteri adı, telefon numarası veya
> e-posta adresi koyma — DEMO verisi olsa bile maskele.

---

## Bölüm A — Keşif (DEMO ortamında)

Buradaki hiçbir sorunun cevabını bana sorma. DEMO'da kendin bul, **not al**.
Not alırken ekranın adını aynen yaz — "Ayarlar'a gir" değil, menüde ne yazıyorsa o.

1. DEMO hesabında bir API anahtarı oluştur. Hangi menüden hangi menüye gittiğini
   adım adım yaz. Sayfanın URL'ini de not et.
2. Anahtar oluştururken senden hangi alanlar isteniyor? Hangisi zorunlu, hangisi
   opsiyonel? Opsiyonel olanı boş bırakırsan ne oluyor?
3. Kaydettikten sonra ne görüyorsun? Sayfadan çıkıp geri geldiğinde anahtarı
   **tekrar görebiliyor musun**? Liste ekranında anahtar yerine ne yazıyor?
4. Anahtarı silersen ne oluyor? (DEMO'da kendi oluşturduğun anahtarı sil, başkasının
   anahtarına dokunma.)
5. Hesabında ikinci bir kullanıcı varsa, yetkisi kısıtlı bir kullanıcıyla aynı
   ekrana girmeyi dene. Ne görüyorsun? Göremiyorsan **neden** göremediğini yaz —
   bu bilgi dokümanda olmazsa destek bileti olarak bize döner.

---

## Bölüm B — Doğrulama (curl ile, ölçerek)

Bu bölümde tahmin yürütmek yasak. Her maddede **çalıştırdığın komutu ve aldığın
tam çıktıyı** not defterine yapıştır.

### B1. Doğru adresi bul

Ortalıkta iki aday adres dolaşıyor:

```
https://use.hipcall.com/api/v3/profile
https://api.hipcall.com/api/v3/profile
```

İkisini de dene. Hangisi cevap veriyor, hangisi vermiyor? Cevap vermeyen için
`curl` tam olarak ne diyor?

> Bu bir yazım hatası değil, gerçek bir karışıklık. Hangisinin doğru olduğunu
> ölçtükten sonra çalışma notunda **ayrı bir "Bulgular" başlığı** altında yaz.
> Yanlış olan yeri düzeltmek senin ödevin değil; bulduğunu raporlamak senin ödevin.

### B2. Anahtarsız istek

Hiç `Authorization` başlığı göndermeden `GET /api/v3/profile` çağır.

- HTTP durum kodu kaç?
- Cevap gövdesi ne? Tam JSON'u yapıştır.

### B3. Bozuk anahtarla istek

`Authorization: Bearer bu-anahtar-sahte` gönder.

- Durum kodu B2 ile aynı mı?
- Cevap gövdesi B2 ile **aynı mı, farklı mı**? Farklıysa farkı yaz.

### B4. Doğru anahtarla istek

Bölüm A'da oluşturduğun anahtarla çağır.

```bash
curl -sS -i \
  -H "Authorization: Bearer ANAHTARIN" \
  https://ADRES/api/v3/profile
```

- Durum kodu kaç?
- Gövdede hangi alanlar dönüyor? Hepsini listele.

### B5. Anahtarın biçimi

A3'te ekrandan kopyaladığın anahtarı dikkatle incele.

- `SFMyNTY.` ile mi başlıyor?
- Farklı yerlerdeki örneklerde anahtar hep aynı biçimde mi yazılmış?
- Ekrandan kopyaladığın hâliyle istek çalışıyor mu? Başına `SFMyNTY.` ekleyince ne
  oluyor? **İkisini de dene, gör.**

Bu maddenin cevabını sana vermiyorum çünkü müşteri de aynı şeyi yaşayacak.

### B6. Cevap başlıkları

B4'teki isteğin cevap başlıklarına bak (`curl -i` çıktısının üst kısmı).

- `X-RateLimit-` ile başlayan başlık var mı?
- Varsa dakikada kaç isteğe izin var?
- **Yoksa** bu ne anlama geliyor? Limit yok mu, yoksa limit var ama bu ortamda
  kapalı mı? Emin olamıyorsan emin olmadığını yaz — uydurma.

> ⚠️ Limiti test etmek için arka arkaya yüzlerce istek atma. DEMO paylaşımlı bir
> ortam. Merak ediyorsan önce Onur'a sor.

---

## Bölüm C — Sorularını community'de sor

Takıldığın her yerde <https://community.hipcall.com/> üzerinden soracaksın. Bana
Slack'ten soru yazma. Sebebi şu: senin takıldığın yerde bir müşteri de takılıyor.
Forumdaki soru-cevap, yazacağın blog yazısından daha çok developer'a ulaşabilir.

**Kurallar:**

- En az **1 soru** aç: `Developers` kategorisi, İngilizce.
- Aynı soruyu `Türkçe` kategorisinde de aç (birebir çeviri değil, Türkçe düşünülmüş
  hâli).
- Soru formatı şu olmalı:
  1. Ne yapmaya çalışıyordum
  2. Ne denedim (komutun tamamı)
  3. Ne bekliyordum
  4. Ne oldu (çıktının tamamı)
- **Anahtarını asla paylaşma.** Ne foruma, ne ekran görüntüsüne, ne çalışma notuna.
  Maskele: `Bearer SFMy...a3f9`. Yanlışlıkla paylaşırsan panik yapma, anahtarı
  DEMO'dan sil ve yenisini oluştur — zaten o yüzden silinebiliyor.
- Cevap gelince konuyu kapat: işe yarayan cevabı "çözüm" olarak işaretle.

Açtığın konuların linklerini çalışma notuna ekle.

---

## Bölüm D — Teslim edeceklerin

### D1. Çalışma notu

**Repo:** `hipcall/2026-internship`
**Dosya:** `submissions/01-api-anahtari.md`

Ham bulgular. Cilalamana gerek yok, eksiksiz olsun. İçinde şunlar olacak:

- Bölüm A'nın 5 sorusunun cevabı
- Bölüm B'nin 6 maddesinin komut + tam çıktısı
- **Bulgular** başlığı: dokümanla gerçeğin uyuşmadığı her nokta
- Community konu linkleri
- Bir **mermaid** diyagramı: anahtarın oluşturulmasından ilk başarılı isteğe kadarki
  akış. Örnek iskelet (kopyalama, kendi ölçtüğünü çiz):

  ```mermaid
  sequenceDiagram
      participant D as Developer
      participant P as Hipcall Panel
      participant A as Hipcall API
      D->>P: Anahtar oluştur
      P-->>D: Anahtar (bir kez gösterilir)
      D->>A: GET /api/v3/profile + Bearer
      A-->>D: 200 + profil
  ```

### D2. Blog yazısı — İngilizce

**Dosya:** `blog/en/how-to-get-a-hipcall-api-key.md`

Frontmatter ve biçim kuralları [`blog/README.md`](../blog/README.md) dosyasında.
Yazmaya başlamadan önce oku — özellikle `description` karakter sınırını ve
`translationKey` kuralını.

Bu yazı için frontmatter:

```yaml
---
title: "How to Get a Hipcall API Key and Make Your First Request"
description: "Create an API key in the dashboard, make your first authenticated request, and understand what comes back."
slug: how-to-get-a-hipcall-api-key
lang: en
locales: [en, tr]
pubDate: 2026-09-22
categories: [developers]
intent: informational
translationKey: how-to-get-a-hipcall-api-key
tags: [api, authentication, getting-started]
authors: [hipcall-team]
featured: false
draft: true
task: 01
status: draft
---
```

Yazının iskeleti:

1. **Overview** — ne yapacağız, kime lazım (2-3 cümle)
2. **Before you start** — panelde adım adım anahtar oluşturma. Bölüm A'nın çıktısı
   burada.
3. **Your first request** — çalışan `curl` örneği
4. **Successful response** — gerçek JSON (DEMO verisi, maskelenmiş)
5. **When it fails** — 401'ler, gövdeleriyle. Bölüm B2/B3'ün çıktısı.
6. **Keeping your key safe** — bir kez gösterilir, sızarsa sil ve yenile
7. **Next steps** — API referansına ve community'ye link

### D3. Blog yazısı — Türkçe

**Dosya:** `blog/tr/hipcall-api-anahtari-nasil-alinir.md`

Aynı içerik, Türkçe. **Makine çevirisi istemiyorum.** Standarttaki terim tablosuna
uy; kod blokları, JSON çıktıları ve HTTP başlıkları çevrilmez.

Frontmatter'da değişenler: `lang: tr`, `slug` Türkçe (ASCII), `title` ve
`description` Türkçe. `translationKey` İngilizce yazıyla **aynı kalır**.

### D4. Pull request

Hepsi tek PR: çalışma notu + EN blog + TR blog.

PR açıklamasında şunlar olsun:

- Hangi ödev olduğu (`tasks/01-api-anahtari.md` linki)
- Açtığın community konularının linkleri
- Bulgular listen (B1 ve B5'ten çıkanlar)

Yazılar `status: draft` ile başlar. Gözden geçirmeden sonra `review`, onaydan sonra
`approved` yaparız ve siteye biz aktarırız.

---

## Kabul kriterleri

Teslimi şu listeye göre kontrol edeceğim:

- [ ] Çalışma notu var, Bölüm A'nın 5 sorusu da cevaplı
- [ ] Bölüm B'nin 6 maddesinin her biri **komut + tam çıktı** içeriyor
- [ ] B1'in sonucu ölçülmüş (tahmin değil), iki adres de denenmiş
- [ ] B5 denenmiş ve sonucu yazılmış
- [ ] Mermaid diyagramı var ve gerçekten ölçtüğün akışı gösteriyor
- [ ] Community'de en az 2 konu açılmış (1 EN + 1 TR), linkleri notta
- [ ] EN ve TR blog yazıları var, frontmatter standarda uyuyor
- [ ] `description` iki dosyada da 160 karakterin altında
- [ ] `translationKey` iki dosyada aynı; `authors: [hipcall-team]` ve `draft: true` bozulmamış
- [ ] Yazılarda **çalışan** curl örneği var (ben kopyalayıp çalıştıracağım)
- [ ] Hiçbir yerde gerçek API anahtarı yok — notta, blogda, forumda, PR'da
- [ ] Yazıda kalan tek bir "TODO" veya "buraya bakılacak" yok

---

## Bilerek söylemediklerim

Bu ödevde üç soruyu kasten cevapsız bıraktım. Üçü de gerçek bir developer'ın ilk
saatinde karşılaştığı şeyler:

1. Anahtar hangi biçimde kullanılıyor (B5)
2. Doğru API adresi hangisi (B1)
3. Limitler bu ortamda açık mı (B6)

Bunları bulman 10 dakika sürebilir, 2 saat de sürebilir. Ne kadar sürdüğünü not al —
**çünkü müşteriye de o kadar sürecek.** Yazacağın blog yazısının değeri tam olarak
o süreyi sıfıra indirmesinde.

---

## Yaygın hatalar

- **Referansı kopyalamak.** Blog yazısı endpoint listesi değil. Tek bir işi baştan
  sona anlatıyor.
- **"Kolayca", "basitçe", "sadece" demek.** Okuyan takılırsa bu kelimeler onu aptal
  gibi hissettirir. Kullanma.
- **Ekran görüntüsüne güvenmek.** Panel değişir, görsel eskir. Menü adlarını metin
  olarak da yaz.
- **Hata durumlarını atlamak.** Developer'ın çoğu zamanı hata ayıklamakla geçiyor;
  o yüzden iskelette "BAŞARISIZ CEVAP" ayrı bir başlık.
- **DEMO verisini maskelemeden yapıştırmak.** Telefon numarası, e-posta, isim —
  hepsini değiştir.

---

## Teslim

Bitince bana haber ver. Birlikte oturup gözden geçireceğiz. Gözden geçirmede
kovalayacağım tek soru şu:

> Bu yazıyı hiç Hipcall görmemiş bir developer'a versem, 15 dakikada ilk isteğini
> atabilir mi?

Cevap "hayır" ise yazı eksik, sen değil.

---

**Sonraki ödev:** [`02-liste-ve-filtreleme.md`](02-liste-ve-filtreleme.md) — bu ödev
teslim edilince başla.
