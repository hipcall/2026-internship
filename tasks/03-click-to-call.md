# Ödev 3 — Çağrı Başlatma ve Numara Maskeleme

**Tahmini süre:** 3 gün
**Zorluk:** Orta
**Ön koşul:** [`02-liste-ve-filtreleme.md`](02-liste-ve-filtreleme.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

İlk iki ödevde sadece **okudun**. Bu ödevde ilk kez bir şey **yaptıracaksın**:
gerçek bir telefon çalacak.

Click-to-call, entegrasyon isteyen müşterilerin bir numaralı talebi. Senaryo hep
aynı: CRM'de müşteri kartı açık, yanında bir "Ara" düğmesi, tıklayınca çağrı
başlıyor. Ajan numara tuşlamıyor, yanlış tuşlamıyor, zaman kaybetmiyor.

İkinci bir senaryo daha var ve az biliniyor: **numara maskeleme.** Pazaryeri,
kurye, emlak, ikinci el araç — alıcı ve satıcının konuşması gerekiyor ama
birbirinin numarasını görmemesi gerekiyor. Hipcall bunu tek parametreyle çözüyor.
Bu özelliğin yazılmış bir kılavuzu yok.

---

## Ön koşullar

- Ödev 1'deki API anahtarın çalışıyor.
- DEMO hesabında **sana ait bir dahili/cihaz** olmalı ve kayıtlı (registered)
  durumda olmalı. Yoksa çağrı başlamaz.
- Arayabileceğin ikinci bir telefon numarası (kendi cep telefonun olabilir).
- .NET SDK kurulu.

> ⚠️ Gerçek çağrı başlatıyorsun ve bu **ücretli**. Test ederken kendi numaranı ara,
> rastgele numara arama. Günde 5-10 test yeter; döngü içinde çağrı başlatma.

---

## Bölüm A — Hazırlık: kimin çağrısı?

1. `GET /api/v3/users` ile kullanıcıları listele. Kendi kullanıcı id'ini bul.
2. `GET /api/v3/numbers` ile hesaptaki dış numaraları listele. Kaç tane var?
   Her birinin `id` ve `number` alanını not et.
3. `GET /api/v3/extensions` ile dahilileri listele. `target_type` alanı ne
   döndürüyor? Bir dahili neyi işaret edebiliyor?
4. Çağrı başlatmanın **iki** yolu var:
   - `POST /api/v3/users/{user_id}/call`
   - `POST /api/v3/extensions/{extension_id}/call`

   İkisinin arasındaki farkı yaz. Hangi durumda hangisini kullanırsın?

---

## Bölüm B — İlk çağrını başlat

1. `POST /api/v3/users/{user_id}/call` çağır. Gövdede en az `callee_number` olacak.
   Numarayı **E.164** biçiminde yaz: `+905551112233`.
2. Ne oldu? Telefonun mu çaldı, aradığın numara mı çaldı? **Sırayı dikkatle
   gözlemle** ve yaz.
3. Cevabın durum kodu ne? Gövdesinde ne dönüyor?
4. Cevap geldiği anda çağrı bağlanmış oluyor mu? Cevabın **ne söylediğini**, **ne
   söylemediğini** ayrı ayrı yaz. (Bu ayrım blog yazının en önemli cümlesi olacak.)
5. `ring_user_first` parametresini değiştirerek tekrar dene. Davranış nasıl
   değişiyor?
6. `number_id` göndermeden ve göndererek dene. Aradığın kişi ekranında hangi numarayı
   görüyor? Parametreyi hiç göndermezsen hangi numara kullanılıyor?

---

## Bölüm C — Hata durumları

Her madde için durum kodu + gövdenin tamamı.

1. Var olmayan bir `user_id` ile dene.
2. `callee_number` göndermeden dene.
3. Numarayı E.164 olmadan gönder: `05551112233` ve `555 111 22 33`. Kabul ediliyor
   mu? Ediliyorsa hangi ülkeye göre yorumlanıyor?
4. Cihazı kapalı / kayıtsız bir kullanıcı için çağrı başlat. Hata mı alıyorsun,
   yoksa çağrı kabul edilip sessizce mi düşüyor?
5. Gövdeye uydurma bir alan ekle (`"foo": "bar"`). Ne oluyor?

C4'ün cevabı önemli: eğer hata gelmiyorsa, entegrasyonu yazan developer çağrının
başladığını nasıl anlayacak? Bu soruyu yazında cevapla.

---

## Bölüm D — Numara maskeleme

`call_masking` ve `call_masking_name` parametreleri.

1. `call_masking: true` ile bir çağrı başlat. Aradığın telefonda hangi numara
   görünüyor? Maskesiz çağrıdan farkı ne?
2. `call_masking_name` ne işe yarıyor? Değer verip vermediğinde ne değişiyor?
3. Maskelenmiş çağrı bittikten sonra `GET /api/v3/calls` ile kaydına bak. Çağrı
   kaydında gerçek numara duruyor mu, maskeli numara mı?
4. Bu özelliği hangi iş senaryosunda kullanırsın? En az iki örnek yaz. (Kendi
   bulduğun örnekler olsun.)

---

## Bölüm E — C# örneği

Küçük bir sınıf yaz: `HipcallClient`.

```csharp
public sealed class HipcallClient
{
    public Task<CallResult> StartCallAsync(int userId, string calleeNumber, ...);
}
```

Kurallar:

- Tek `HttpClient`, `IHttpClientFactory` veya `static readonly` alan.
- Anahtar ortam değişkeninden.
- `System.Text.Json` — üçüncü parti JSON kütüphanesi yok.
- Hata cevabını yutma: 4xx durumunda gövdeyi oku, çağırana anlamlı bir istisna
  veya sonuç nesnesi döndür.
- `async`/`await` doğru kullanılacak: `.Result` veya `.Wait()` yok.
- Maskeleme parametreleri opsiyonel olacak.

Teslim: `submissions/03-click-to-call/Hipcall.ClickToCall/`

---

## Bölüm F — Community

- Yeni bir soru aç (`Developers` + `Türkçe`). Bu ödevde doğal konu: B4'teki ayrım —
  "201 aldım ama çağrı bağlandı mı?"
- Önceki ödevlerde açtığın konuları kapat.

---

## Bölüm G — Teslim edeceklerin

### G1. Çalışma notu

`submissions/03-click-to-call.md`

- A–D bölümlerinin her maddesi: komut/kod + tam çıktı
- **Çağrı akışı diyagramı** (mermaid `sequenceDiagram`): istek → ajanın telefonu →
  karşı taraf → bağlanma. Gerçekte gözlemlediğin sırayı çiz.
- Maskeli ve maskesiz çağrının karşılaştırması
- Community konu linkleri

### G2. Blog — İngilizce

`blog/en/how-to-mask-phone-numbers-in-outbound-calls.md`

```yaml
title: "How to Mask Phone Numbers in Outbound Calls with the Hipcall API"
description: "Connect two people without either seeing the other's number. Start a masked call from your app and understand what the API does and does not promise."
slug: how-to-mask-phone-numbers-in-outbound-calls
lang: en
locales: [en, tr]
categories: [developers]
intent: informational
translationKey: how-to-mask-phone-numbers-in-outbound-calls
tags: [api, calls, privacy, click-to-call]
task: 03
```

Diğer frontmatter alanları [`blog/README.md`](../blog/README.md) standardındaki gibi.

Yapı:

1. **Overview** — hangi iş problemini çözüyor (pazaryeri/kurye örneği)
2. **Before you start** — anahtar, dahili, dış numara
3. **Starting a call** — temel çağrı, C# örneği
4. **Turning on masking** — `call_masking`, `call_masking_name`
5. **What the response means** — 201 ne demek, ne demek değil
6. **When it fails** — C bölümünün çıktısı
7. **Parameter reference** — tablo
8. **Next steps**

### G3. Blog — Türkçe

`blog/tr/giden-aramalarda-numara-maskeleme.md`

### G4. PR

Tek PR: çalışma notu + C# projesi + iki blog.

---

## Kabul kriterleri

- [ ] A–D bölümlerinin her maddesi cevaplı, çıktılar yapıştırılmış
- [ ] B4 cevaplanmış: 201'in ne söylediği ve ne söylemediği ayrı ayrı yazılmış
- [ ] C3 ölçülmüş: E.164 olmayan numaraya ne oluyor
- [ ] C4 ölçülmüş ve yorumlanmış
- [ ] `sequenceDiagram` gerçekte gözlemlenen sırayı gösteriyor
- [ ] D bölümünde maskeleme için en az iki iş senaryosu yazılmış
- [ ] C# projesi `dotnet run` ile çalışıyor
- [ ] Tek `HttpClient`, anahtar ortam değişkeninden, `.Result`/`.Wait()` yok
- [ ] Hata gövdesi yutulmuyor
- [ ] EN + TR blog standarda uyuyor
- [ ] Gerçek telefon numarası hiçbir yerde yok (maskelenmiş: `+90555XXXXXXX`)

---

## Bilerek söylemediklerim

1. Çağrının hangi tarafı önce çalıyor (B2)
2. `201`'in gerçekte neyi garanti ettiği (B4)
3. E.164 olmayan numaranın nasıl yorumlandığı (C3)
4. Maskeli çağrının kaydında hangi numaranın durduğu (D3)

Dördü de "dene ve gör" ile 15 dakikada bulunur. Dördü de dokümanda yazmıyor.

---

## Yaygın hatalar

- **Test için rastgele numara aramak.** Kendi numaranı ara.
- **Döngü içinde çağrı başlatmak.** Her çağrı para. Bir hata döngüsü faturaya döner.
- **201'i "çağrı bağlandı" sanmak.** Bu ödevin ana konusu.
- **Numarayı yerel biçimde göndermek.** Örneklerinde hep E.164 kullan: `+90…`.
- **Maskelemeyi "numarayı gizler" diye özetlemek.** Ne olduğunu, kimin ne gördüğünü
  ve kayıtta ne kaldığını yaz.
- **`.Result` ile async kodu bloklamak.** Kopyalayan developer'ın uygulaması kilitlenir.

---

## Teslim

Gözden geçirmede soracağım:

> Bu yazıyı okuyan developer, "Ara" düğmesini bugün ekleyebilir mi — ve çağrının
> başlayıp başlamadığını nasıl anlayacağını biliyor mu?

---

**Sonraki ödev:** [`04-webhook-alicisi.md`](04-webhook-alicisi.md)
