# Blog Yazım Standardı

Bu dizindeki yazılar <https://www.hipcall.com/blog/> için hazırlanıyor. Sen buraya
markdown olarak yazıyorsun; yayına aktarmayı biz yapıyoruz.

**Aktarım kopyala-yapıştır olmalı.** Standarda uyarsan yazın olduğu gibi siteye
taşınır. Uymazsan her yazı için elle düzeltme gerekir ve yayın gecikir.

> ⚠️ **Bu repo herkese açık.** Yazdığın her şeyi internetteki herkes okuyabilir.
> Gerçek API anahtarı, müşteri adı, telefon numarası, e-posta adresi **koyma**.
> DEMO verisi de olsa maskele.

---

## Dizin yapısı

```
blog/
├── en/                          İngilizce yazılar
│   └── how-to-get-a-hipcall-api-key.md
├── tr/                          Türkçe yazılar
│   └── hipcall-api-anahtari-nasil-alinir.md
└── assets/                      Görseller
    └── how-to-get-a-hipcall-api-key/
        └── token-screen.png
```

- Dil dizinleri **sadece** `en/` ve `tr/`.
- Dosya adı = `slug` + `.md`. Başka bir şey olmayacak.
- Uzantı `.md`. (Sitede `.mdx` oluyor, çevirisini biz yapıyoruz.)
- Görsel klasörü yazının slug'ı ile aynı adı taşır.

---

## Dosya adlandırma

| Dil | Kural | Örnek |
|---|---|---|
| EN | küçük harf, tire ayraç, ASCII | `how-to-export-missed-calls-with-the-hipcall-api.md` |
| TR | küçük harf, tire ayraç, **ASCII** — Türkçe karakter yok | `cevapsiz-cagrilari-disari-aktarma.md` |

Türkçe slug'da `ı → i`, `ş → s`, `ğ → g`, `ü → u`, `ö → o`, `ç → c`.

EN ve TR dosya adları **farklı** olacak. Aynı olan tek şey `translationKey`.

---

## Frontmatter

Her yazı bu blokla başlar. Alan sırası da aynı olsun — göz alışkanlığı gözden
geçirmeyi hızlandırıyor.

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

### Alanlar

| Alan | Zorunlu | Kural |
|---|---|---|
| `title` | ✅ | Tırnak içinde. EN'de Başlık Büyük Harf, TR'de normal cümle düzeni. |
| `description` | ✅ | **En fazla 160 karakter.** Sayarak yaz — sınır aşılırsa site build'i kırılır. Arama sonucunda görünen metin budur. |
| `slug` | ✅ | Dosya adıyla aynı (uzantısız). Tırnaksız. |
| `lang` | ✅ | `en` veya `tr` |
| `locales` | ✅ | `[en, tr]` — iki dili de yazdıysan. Tek dil yazdıysan sadece onu koy. |
| `pubDate` | ✅ | `YYYY-MM-DD`. Taslağı bitirdiğin gün. Yayın tarihini biz değiştiririz. |
| `categories` | ✅ | Aşağıdaki listeden. Genelde `developers`. |
| `intent` | ✅ | `informational` \| `navigational` \| `commercial` \| `transactional`. Senin yazıların hep `informational`. |
| `translationKey` | ✅ | EN ve TR dosyada **birebir aynı**. Değeri: İngilizce slug. Site iki yazıyı bununla eşleştiriyor. |
| `tags` | ✅ | 3-6 adet, küçük harf, tire ayraç. |
| `authors` | ✅ | `[hipcall-team]` yaz, **değiştirme.** Yazar atamasını yayına alırken biz yapıyoruz. |
| `featured` | ✅ | Hep `false`. |
| `draft` | ✅ | Hep `true`. Yayın kararı bizim. |
| `task` | ✅ | Ödev numarası: `01`, `02`… Bu alan aktarımda silinir. |
| `status` | ✅ | `draft` (yazıyorum) → `review` (bitti, bakılsın) → `approved` (onaylandı). Aktarımda silinir. |

### Geçerli `categories` değerleri

`call-center`, `customer-experience`, `sales`, `support`, `tech`, `remote-teams`,
`news`, `developers`

Listede olmayan bir değer yazarsan site build'i kırılır.

---

## Yazının iskeleti

Her how-to yazısı bu sırayı izler:

```
HAZIRLIK → İSTEK ÖRNEĞİ → BAŞARILI CEVAP → BAŞARISIZ CEVAP → PARAMETRELER
```

Başlıklara dönüşmüş hâli:

| # | EN | TR |
|---|---|---|
| 1 | `## Overview` | `## Genel bakış` |
| 2 | `## Before you start` | `## Başlamadan önce` |
| 3 | `## Step 1: …` | `## Adım 1: …` |
| 4 | `## The full script` *(varsa)* | `## Betiğin tamamı` |
| 5 | `## When it fails` | `## Hata aldığınızda` |
| 6 | `## Parameter reference` | `## Parametre listesi` |
| 7 | `## Next steps` | `## Sonraki adımlar` |

**`## Before you start` bölümünü atlamak yok.** Panelde neyi nereden alacağı,
hangi iznin gerektiği orada yazar. Developer'ı en çok orada kaybediyoruz.

**`## When it fails` bölümünü atlamak yok.** Developer'ın zamanının çoğu hata
ayıklamakla geçiyor. Her hata için: durum kodu + gövdenin tamamı + ne yapmalı.

---

## Biçim kuralları

### Başlıklar

- Yazı gövdesinde `#` (H1) **kullanma** — o, `title` alanından geliyor.
- Bölümler `##`, alt bölümler `###`. Daha derine inme.

### Kod blokları

Her blok dil etiketli olacak:

````
```bash
curl -sS -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  https://use.hipcall.com/api/v3/profile
```
````

- `bash`, `json`, `csharp`, `http` — hangisiyse onu yaz. C# örneklerinde `csharp`
  kullan, `c#` veya `cs` değil.
- Etiketsiz blok bırakma; renklendirme çalışmaz.
- **Kod blokları çevrilmez.** Türkçe yazıda da komut, JSON ve HTTP başlıkları
  İngilizce kalır. Sadece `# yorum satırları` çevrilir.

### Anahtarlar ve gizli veri

Kod örneklerinde anahtar **hiçbir zaman** düz yazılmaz:

```bash
# doğru
export HIPCALL_API_TOKEN="..."
curl -H "Authorization: Bearer $HIPCALL_API_TOKEN" ...

# yanlış
curl -H "Authorization: Bearer SFMyNTY.g2gDbQAAACQ0M2Jl..." ...
```

Çıktı yapıştırırken telefon, e-posta, isim maskelenir:

```json
{ "caller_number": "+90555XXXXXXX", "full_name": "Ayse Y." }
```

### C# örnekleri

Kod örneklerinin dili C#. Yazıya koyduğun her örnek şunları sağlamalı:

- Kopyalanıp çalıştırılabilir olmalı — eksik `using`, tanımsız değişken bırakma.
- Anahtar ortam değişkeninden okunmalı, koda gömülmemeli.
- `HttpClient` tek örnek üzerinden kullanılmalı; döngü içinde `new HttpClient()` yok.
- `async`/`await` doğru kullanılmalı; `.Result` ve `.Wait()` yok.
- Üçüncü parti JSON kütüphanesi yok — `System.Text.Json` yeterli.
- Hata cevabının gövdesi yutulmamalı.

Uzun örnekleri yazının içine gömmek yerine önemli parçayı göster, tamamını
`submissions/` altındaki projeye bırak ve oraya işaret et.

### Tablolar

Parametre listelerinde tablo kullan:

| Parametre | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `limit` | integer | hayır | Sayfa başına kayıt. 1–100. Varsayılan 10. |

### Mermaid diyagramları

Destekleniyor, kullan. Akış için `flowchart`, adım sırası için `sequenceDiagram`:

````
```mermaid
flowchart TD
    A[İstek] --> B{meta.count > offset + limit}
    B -- evet --> C[offset += limit]
    C --> A
    B -- hayır --> D[Bitti]
```
````

Diyagram içindeki metni yazının diline göre yaz.

### Görseller

Zorunlu değil. Gerekiyorsa:

1. Dosyayı `blog/assets/<slug>/` altına koy.
2. Yazıda normal markdown ile referans ver:
   `![Token ekranı](../assets/how-to-get-a-hipcall-api-key/token-screen.png)`
3. `alt` metnini boş bırakma — ekran okuyucular ve SEO için gerekli.

Bunu siteye özel görsel bileşenine çevirmeyi biz yapıyoruz.

**Ekran görüntüsü tek başına yeterli değil.** Panel arayüzü değişir, görsel eskir.
Menü adlarını metin olarak da yaz.

### Bağlantılar

- Site içi: `/blog/<slug>/` veya `/developers/` gibi kök yollar.
- Dış: tam URL.
- API referansı: <https://use.hipcall.com/api-docs/>
- Forum: <https://community.hipcall.com/>
- **Bu repoya link verme.** Yayınlanan yazı stajyer repo'sunu tanımaz.

### Satır düzeni

Paragrafları elle sarma (80 karaktere bölme). Bir paragraf = bir satır. Markdown
editörleri ve çeviri araçları böyle daha iyi çalışıyor.

---

## Dil ve üslup

### İkisi için ortak

- **"Kolayca", "basitçe", "sadece", "yalnızca birkaç adımda" yasak.** Okuyan
  takılırsa bu kelimeler onu aptal gibi hissettirir.
- Emir kipi kullan: "Anahtarı oluşturun" / "Create the key". "Oluşturmanız gerekmektedir" değil.
- Tarihler UTC ve ISO 8601: `2026-09-01T00:00:00Z`.
- Rakip firma adı geçmez.
- Ürün adı **Hipcall** — "HipCall", "hipcall" değil.
- İç bilgi geçmez: ticket numarası, iç repo yolu, sunucu adı, çalışan adı.

### Türkçe yazarken

- **Makine çevirisi kabul edilmiyor.** Yazıyı Türkçe düşünerek yaz. Cümle sırası
  İngilizcesinden farklı olabilir, olmalı.
- Terim tutarlılığı — bir yazıda bir terimi hep aynı karşılıkla kullan:

  | İngilizce | Türkçe |
  |---|---|
  | API key | API anahtarı |
  | endpoint | endpoint *(çevirme)* |
  | request / response | istek / cevap |
  | webhook | webhook *(çevirme)* |
  | pagination | sayfalama |
  | filter | filtre |
  | missed call | cevapsız çağrı |
  | contact | kişi |
  | company | firma |
  | call record / CDR | çağrı kaydı |
  | dashboard / panel | panel |

- Panel menü adlarını **DEMO'da göründüğü dilde** yaz, parantez içinde diğerini
  ver: `Settings > Developer (Ayarlar > Geliştirici)`.

---

## Teslim akışı

1. Yazıyı `blog/en/` ve `blog/tr/` altına yaz, `status: draft`.
2. Bitince `status: review` yap, PR aç.
3. Gözden geçiririz, yorum bırakırız.
4. Düzeltmeler bitince `status: approved` yapılır ve biz siteye aktarırız.

`draft: true` ve `authors: [hipcall-team]` alanlarına dokunma — ikisi de yayın
kontrolü için.

---

## Gözden geçirmede bakacağım şeyler

- [ ] `description` 160 karakterin altında mı
- [ ] `translationKey` iki dosyada birebir aynı mı
- [ ] `categories` geçerli bir değer mi
- [ ] Slug ASCII mi, dosya adıyla aynı mı
- [ ] Gövdede H1 var mı (olmamalı)
- [ ] Kod blokları dil etiketli mi
- [ ] Komutlar kopyalanıp çalışıyor mu — **çalıştıracağım**
- [ ] `Before you start` ve `When it fails` bölümleri var mı
- [ ] Anahtar, telefon, e-posta, isim sızmış mı
- [ ] Türkçe metin makine çevirisi gibi mi okunuyor
- [ ] "Kolayca / basitçe / sadece" geçiyor mu
- [ ] Yarım kalmış "TODO" var mı
