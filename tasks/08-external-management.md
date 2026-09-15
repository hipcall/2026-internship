# Ödev 8 — External Management: Çağrıyı Kendi Servisinizle Yönlendirmek

**Tahmini süre:** 5 gün
**Zorluk:** Zor — programın en zorlu ödevi.
**Ön koşul:** [`07-cagri-sonrasi-otomasyon.md`](07-cagri-sonrasi-otomasyon.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

Şimdiye kadarki her şey çağrının **yanında** oldu: çağrı devam ederken bilgi bastın,
bittikten sonra kaydettin. External Management ise çağrının **içine** giriyor.

Çağrı geldiğinde Hipcall senin servisine soruyor: "bu numara arıyor, ne yapayım?"
Sen cevap veriyorsun: "şu kuyruğa bağla", "önce şunu sor", "PIN iste, doğruysa
destek ekibine, yanlışsa satışa".

Bu, IVR'ı veritabanınla birleştirmek demek. Bakiyesi olan müşteri tahsilata,
VIP müşteri özel ekibe, siparişi kargoda olan kişi doğrudan kargo kaydına.

Zor olmasının sebebi: bir insan telefonda beklerken kod çalışıyor. Yavaşsan müşteri
sessizlik duyuyor. Hata verirsen çağrı düşüyor. Test etmek için her seferinde
telefon açman gerekiyor.

---

## Ön koşullar

- Ödev 1–7 tamam.
- ngrok veya internete açık bir sunucu.
- DEMO'da **gelen çağrı alabileceğin bir dış numara** ve o numaranın akışını
  düzenleme yetkisi. Bu ödevde şart — yoksa Onur'dan iste, ödeve başlama.
- Arayabileceğin bir telefon.

> ⚠️ Bu ödevde gelen çağrı akışını değiştiriyorsun. DEMO'da başkasının kullandığı
> bir numaranın akışını bozma. Kendine ayrılmış numarayı kullan.

---

## Bölüm A — Keşif: panel tarafı

1. External Management kaydını panelde nereden oluşturuyorsun? Menü yolunu ve URL'i
   not et. (İpucu: aradığın yer "Developer" başlığı altında değil.)
2. Oluştururken hangi alanlar isteniyor? Kimlik doğrulama seçeneği var mı? Varsa
   hangi tip?
3. Kaydın bir **durumu** var. Hangi değerleri alabiliyor? Başlangıçta hangisinde?
4. Bir **kayıtlar/logs** sekmesi var. İlk açtığında ne görüyorsun?
5. Logların tutulması için ayrıca açman gereken bir ayar var. Bul, aç, ne işe
   yaradığını yaz. (Bu ödevde en çok kullanacağın araç bu olacak.)
6. Oluşturduğun kaydı bir gelen çağrı akışına nasıl bağlıyorsun? Adımları yaz.

---

## Bölüm B — İlk isteği yakala

Bir ASP.NET Core uygulaması yaz, önce sadece **dinlesin**:

```bash
dotnet new web -n Hipcall.ExternalManagement
```

Gelen isteğin metodunu, yolunu, başlıklarını ve gövdesini konsola dök. Sonra
numarayı ara ve gelen isteği incele.

1. HTTP metodu ne? `GET` mi `POST` mu?
2. Gövde/parametreler ne taşıyor? Her alanı listele:
   - Arayanın numarası hangi alanda?
   - Aranan numara hangi alanda?
   - Çağrıyı benzersiz kılan bir alan var mı?
   - Kaçıncı adımda olduğunu gösteren bir şey var mı?
3. Kimlik doğrulama açtıysan başlıkta ne geliyor?
4. Servisinin cevap vermesi için ne kadar süre var? Kasıtlı olarak 10 saniye beklet,
   sonra 30 saniye. Ne oluyor? Zaman aşımı sınırını **ölç**.
5. Servisin `500` dönerse çağrıya ne oluyor? Arayan ne duyuyor?
6. Servisin geçersiz JSON dönerse ne oluyor?
7. B5 ve B6'yı birkaç kez tekrarla. Kaydın **durumu** değişiyor mu? Değişirse geri
   nasıl alıyorsun?

B4–B7 bu ödevin en kritik ölçümleri. Bir müşterinin canlı sistemi bunlara bağlı.

---

## Bölüm C — Cevap sözleşmesi

Servisinin döndüğü JSON, çağrının ne yapacağını belirliyor. Desteklenen aksiyonları
**deneyerek** çıkar. Başlangıç noktası olarak şunlara bak:

- Çağrıyı bir hedefe bağlama
- Arayandan tuşlama isteme
- Ses dosyası / anons çaldırma

Her aksiyon için:

1. JSON'un tam şekli nedir? Zorunlu alanlar hangileri?
2. Hedef nasıl ifade ediliyor? (Dahili numara mı, id mi, bir başka biçim mi?)
   `GET /api/v3/extensions` çıktısıyla karşılaştır.
3. Aksiyonu bozuk gönderdiğinde ne oluyor? Loglar ne diyor?
4. Birden fazla aksiyonu sırayla verebiliyor musun?

> **Bu bölümde takılacaksın ve bu normal.** Cevap sözleşmesi tam olarak
> dokümante edilmiş değil — zaten bu ödevin çıktısının değeri de burada. İki aracın
> var: panelin **logs** sekmesi (isteğini, cevabını ve hatayı gösteriyor) ve
> mevcut `/developers/` sayfalarındaki örnekler. Üçüncüsü: community'de sor.
> Tahmin edip yazma; ölçtüğünü yaz.

---

## Bölüm D — PIN doğrulama akışını kur

Klasik senaryo, uçtan uca:

1. Çağrı gelir. Servisin arayan numarayı kendi kayıtlarında arar.
2. Numara tanınıyorsa PIN sorar.
3. Arayan PIN'i tuşlar. Hipcall ikinci kez servisine sorar — bu sefer tuşlanan
   değerle.
4. PIN doğruysa bir hedefe, yanlışsa başka bir hedefe bağlar.
5. Numara hiç tanınmıyorsa doğrudan genel kuyruğa bağlar.

Uygulamanın karşılaması gereken durumlar:

- Arayan hiç tuşlamazsa ne olacak?
- Yanlış PIN'de kaç hak verilecek?
- Servisin kendi veritabanı yavaşsa (2-3 saniye) akış ne olacak?
- Aynı çağrı için gelen ardışık istekleri nasıl eşleştiriyorsun? (Durum tutman
  gerekiyor mu, yoksa istek kendi bağlamını taşıyor mu? Bölüm B2'de bunun cevabını
  aramıştın.)

PIN listesi olarak 5 kayıtlık bir sözlük yeterli. Gerçek veritabanı kurma.

Teslim: `submissions/08-external-management/Hipcall.ExternalManagement/`

---

## Bölüm E — Community

Bu ödevde community kullanımı zorunlu, çünkü sözleşmenin bir kısmını tek başına
çıkaramayabilirsin.

- **En az iki soru** aç (`Developers` + `Türkçe`).
- Soruların net olsun: gönderdiğin JSON, aldığın log kaydı, beklediğin davranış.
- Cevapları aldıkça konuları çözüm işaretiyle kapat. Bu konular ileride bu konuyu
  arayan herkesin ilk bulacağı şey olacak.

---

## Bölüm F — Teslim edeceklerin

### F1. Çalışma notu

`submissions/08-external-management.md`

Bu notun diğerlerinden daha kapsamlı olması gerekiyor. İçinde:

- A–D bölümlerinin çıktıları
- **İstek sözleşmesi**: gelen isteğin her alanı, tipi, örneği
- **Cevap sözleşmesi**: desteklediğini doğruladığın her aksiyon, tam JSON şekliyle
- **Ölçümler**: zaman aşımı sınırı, hata davranışı, durum değişimi ve geri alma
- **Çalışmayan denemeler**: neyi denedin, ne oldu. Bu bölümü atlama — bir sonraki
  kişinin aynı çukura düşmesini engelliyor.
- Mermaid `sequenceDiagram`: arayan → Hipcall → servisin → Hipcall → hedef,
  iki adımlı PIN akışıyla
- Community linkleri

### F2. Blog — İngilizce

`blog/en/how-to-route-calls-with-external-management-in-dotnet.md`

```yaml
title: "How to Route Calls with External Management in .NET"
description: "Let your own service decide where each incoming call goes: look up the caller, ask for a PIN, and route on the answer."
slug: how-to-route-calls-with-external-management-in-dotnet
lang: en
locales: [en, tr]
categories: [developers]
intent: informational
translationKey: how-to-route-calls-with-external-management-in-dotnet
tags: [external-management, ivr, dotnet, routing]
task: 08
```

Yapı:

1. **Overview** — hangi iş problemini çözüyor
2. **How it works** — istek/cevap döngüsü, sequence diyagramı
3. **Before you start** — panel kurulumu, ngrok, akışa bağlama
4. **Your first response** — en basit yönlendirme
5. **Asking the caller for input** — tuşlama, ikinci adım
6. **A complete PIN flow** — C# örneği
7. **Timeouts and failures** — B4–B7'nin çıktısı. **Bu bölüm en önemlisi:** servis
   yavaşsa/hata verirse arayan ne yaşıyor, entegrasyon ne zaman kapanıyor, nasıl
   açılıyor.
8. **Debugging with the logs screen** — panelin logs sekmesi nasıl kullanılır
9. **Response reference** — aksiyon tablosu
10. **Next steps**

### F3. Blog — Türkçe

`blog/tr/external-management-ile-cagri-yonlendirme.md`

### F4. PR

---

## Kabul kriterleri

- [ ] A1 ve A5 cevaplı: doğru menü yolu ve log açma ayarı
- [ ] B2'de gelen isteğin **her alanı** tablo hâlinde
- [ ] B4 ölçülmüş: zaman aşımı sınırı, saniye cinsinden
- [ ] B5/B6/B7 ölçülmüş: hata davranışı ve durum değişimi, geri alma dahil
- [ ] C bölümünde en az iki aksiyon **çalışır hâlde** belgelenmiş
- [ ] Çalışmayan denemeler de yazılmış
- [ ] PIN akışı uçtan uca çalışıyor — kanıt: log kayıtları
- [ ] Tuşlama yapılmayan ve yanlış PIN durumları ele alınmış
- [ ] Servis gecikirse ne olacağı düşünülmüş ve yazılmış
- [ ] Community'de en az iki konu açılmış
- [ ] EN + TR blog standarda uyuyor
- [ ] Gerçek numara, gerçek PIN yok

---

## Bilerek söylemediklerim

Bu ödevde **çok şey** söylemedim, diğerlerinden farklı olarak bilerek:

1. Cevap JSON'unun tam şekli
2. Zaman aşımı süresi
3. Hedefin nasıl ifade edildiği
4. Hata sonrası entegrasyonun durumu

Sebebi şu: bu sözleşme bizim tarafımızda da dağınık yazılmış. Sen ölçüp yazdığında
ortaya çıkan belge, elimizdeki en iyi kaynak olacak. O yüzden "çalışmayan
denemeler" bölümü bu ödevde zorunlu.

---

## Yaygın hatalar

- **Yavaş cevap vermek.** Arayan sessizlik duyuyor. Veritabanı sorgunun süresini
  ölç ve yazına bir bütçe cümlesi koy.
- **Hata durumunda cevapsız kalmak.** Servisin her durumda geçerli bir yönlendirme
  dönmeli — "bilmiyorum" bile bir hedefe gitmeli.
- **Tanımadığın aramada çağrıyı düşürmek.** Tanınmayan numara da bir yere bağlanmalı.
- **Durum bilgisini bellekte tutmak.** Uygulaman yeniden başlarsa devam eden çağrılar
  ne olacak? En azından bunu yazında tartış.
- **Logs sekmesini geç keşfetmek.** Bu ödevi elle debug etmeye çalışmak saatler yer.
- **PIN'i loglamak.** PIN paroladır. Log'a yazma, yazıya koyma.

---

## Teslim

Gözden geçirmede beraber bir çağrı yapacağız ve akışı canlı izleyeceğiz.
Soracağım soru:

> Servisin şu anda kapansa, bu numarayı arayan müşteri ne yaşar?

---

## Sonrası

Bu, programın şu anki son ödevi. Sekiz ödevin sonunda ortaya çıkanlar:

- 8 çalışma notu — ölçülmüş, belgelenmiş API davranışı
- 16 blog yazısı (EN + TR), yayına hazır
- 6 çalışan C# örneği
- Community'de kalıcı soru-cevap kaydı

Bunların hiçbiri bizde önceden yoktu.

Bundan sonrası için konuşacağız: yeni ödevler, bir ürün alanında derinleşmek ya da
yazdıklarını gerçek bir müşteri entegrasyonunda kullanmak.
