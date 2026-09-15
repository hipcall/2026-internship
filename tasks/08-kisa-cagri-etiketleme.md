# Ödev 8 — Kısa Çağrıları Otomatik Etiketlemek

**Tahmini süre:** 2 gün
**Zorluk:** Orta
**Ön koşul:** [`07-cevapsiz-cagri.md`](07-cevapsiz-cagri.md) teslim edilmiş olmalı.

---

## Neden bu ödev?

Cevaplanmış ama 8 saniye süren bir çağrı, raporda "başarılı çağrı" olarak görünür.
Oysa 8 saniyede hiçbir iş konuşulmaz. O çağrıda bir şey ters gitti: yanlış numara,
ses gelmedi, müşteri yanlış yere düştü, ajan hattı kapattı.

Bu çağrılar toplamda küçük bir yüzde ama tam olarak bakılması gereken yüzde. Sorun
şu ki kimse elle ayıklamıyor — rapora "cevaplandı" diye girip kayboluyorlar.

Bu ödevde onları etiketleyeceksin. Etiket, sonradan filtrelenebilir bir işaret:
ekip lideri haftada bir "short-call" etiketli çağrılara bakıp ne olduğunu
anlayabilir.

Serinin ikincisi:

| Ödev | Koşul | Aksiyon |
|---|---|---|
| 07 | Cevapsız gelen çağrı | Sonuç kodu + takip görevi |
| **08** | **10 saniyeden kısa çağrı** | **Etiket** |
| 09 | Tanıdığın arayan | Yorum olarak müşteri özeti |

---

## Ön koşullar

- Ödev 7'deki `Hipcall.PostCall` projen çalışıyor.
- Kısa çağrı üretebilmelisin: ara, aç, hemen kapat.

---

## Bölüm A — "Kısa" tam olarak ne?

Bu ödevin asıl sorusu bu ve göründüğünden zor.

1. Kısa bir çağrı yap (aç, 5 saniye sonra kapat) ve `GET /api/v3/calls/{id}` ile
   çağrı kaydını incele. Süreyle ilgili **kaç farklı alan** var? Hepsini listele.
2. Her birinin ne ölçtüğünü bul:
   - Çalma süresi mi konuşma süresi mi?
   - Sıfırdan mı başlıyor, çağrı başlangıcından mı?
3. Şu üç çağrıyı üret ve süre alanlarını yan yana koy:
   - 40 saniye çalıp hiç açılmayan çağrı
   - 3 saniye çalıp açılan, 5 saniye konuşulan çağrı
   - 30 saniye çalıp açılan, 60 saniye konuşulan çağrı
4. Tabloya bakarak karar ver: **"10 saniyeden kısa konuşma"yı hangi alan ile
   ölçersin?** Yanlış alanı seçersen ne olur — 40 saniye çalıp açılmayan çağrı da
   "kısa" sayılır mı?
5. Zaman damgalarından (`answered_at`, `bridged_at`, `ended_at`) süreyi kendin
   hesaplayabilir misin? Hesapladığın değerle hazır alan tutuyor mu? Tutmuyorsa
   fark nereden geliyor?
6. `GET /api/v3/calls` üzerinde süreye göre filtre kur (Ödev 2). Hangi operatörler
   destekleniyor? `eq` var mı?

A4 bu ödevin kalbi. Yazında bu soruyu net cevapla — okuyanın aynı hatayı yapmaması
lazım.

---

## Bölüm B — Kim kapattı?

Kısa çağrıyı yorumlarken en değerli bilgi: hattı kim kapattı.

1. Çağrı kaydında bunu söyleyen bir alan var. Bul, hangi değerleri alabildiğini
   listele.
2. Üç deneme yap ve değeri gözlemle:
   - Arayan kapattı
   - Ajan kapattı
   - Sistem kapattı (varsa; nasıl üretebildiğini yaz)
3. "Ajan 5 saniyede kapattı" ile "müşteri 5 saniyede kapattı" aynı şey mi? İkisinin
   iş anlamını yaz.
4. Bu bilgiyi etikete yansıtmalı mısın — tek etiket mi, iki ayrı etiket mi?
   Kararını gerekçelendir.

---

## Bölüm C — Etiket API'si

1. `GET /api/v3/tags` ne döndürüyor? Etiketin alanları neler?
2. Bir çağrıya etiket ekle: `POST /api/v3/calls/{call_id}/tags`. Gövde ne bekliyor —
   etiketin adı mı, id'si mi?
3. Var olmayan bir etiket göndermeyi dene. Yeni etiket **oluşturuluyor mu**, hata mı
   alıyorsun?
4. C3'ün cevabı entegrasyon için kritik: etiketin panelde önceden tanımlı olması
   gerekiyor mu? Gerekiyorsa, entegrasyonu kuran kişiye ne söylemelisin?
5. Aynı etiketi bir çağrıya iki kez ekle. Ne oluyor?
6. `GET /api/v3/calls/{call_id}/tags` ile etiketleri oku.
7. Etiketi kaldır. Hangi endpoint, hangi biçim? Adla mı id ile mi?
8. Bir çağrıya kaç etiket eklenebiliyor? Sınır var mı?
9. Etiketlenen çağrıları `GET /api/v3/calls` üzerinden filtreleyebiliyor musun?
   Deneyip sonucu yaz — **çünkü etiketin tek faydası sonradan bulunabilmesi.**

C9'un cevabı "hayır" ise bu, etiketin değerini değiştirir. Bulgunu dürüstçe yaz ve
alternatif öner (panelde filtreleme, rapor ekranı vb.).

---

## Bölüm D — Kuralı ekle

`Hipcall.PostCall` projesini genişlet.

Akış:

1. `call_hangup` geldi.
2. Çağrı **cevaplanmış** mı? Cevaplanmamışsa bu kural çalışmaz — o Ödev 7'nin işi.
3. Konuşma süresi eşiğin altında mı? (A4'teki alan.)
4. Etiketi ekle. (B4'teki kararına göre tek ya da iki etiket.)
5. `200` dön.

Kurallar:

- **Eşik sabit kodlanmayacak.** `appsettings.json` veya ortam değişkeninden gelsin.
  10 saniye bir başlangıç; müşteri 15 isteyecek.
- Ödev 7'deki kural bozulmayacak. İki kural yan yana çalışmalı ve biri diğerini
  engellememeli.
- Kurallar artık ikiye çıktı. Kod `if` zincirine dönüşmeye başladıysa **şimdi**
  düzenle: her kural kendi sınıfında, ortak bir arayüzün arkasında.
- İdempotency korunacak: aynı olay iki kez gelirse etiket iki kez eklenmemeli.
  (C5'in cevabı burada işine yarayacak.)

Teslim: `submissions/08-kisa-cagri-etiketleme/Hipcall.PostCall/`

---

## Bölüm E — Community

- Yeni soru: A4 (hangi süre alanı) veya C9 (etiketle filtreleme).
- Eski konuları kapat.

---

## Bölüm F — Teslim edeceklerin

### F1. Çalışma notu

`submissions/08-kisa-cagri-etiketleme.md`

- **Süre alanları tablosu**: alan adı, ne ölçüyor, üç senaryoda aldığı değer
- A4'ün cevabı ve gerekçesi
- B bölümünün bulguları: kim kapattı, iş anlamı
- C bölümünün çıktıları, özellikle C3, C5, C9
- Mermaid `flowchart`: iki kuralın yan yana değerlendirilmesi
- Community linkleri

### F2. Blog — İngilizce

`blog/en/how-to-tag-short-calls-automatically.md`

```yaml
title: "How to Tag Short Calls Automatically with the Hipcall API"
description: "An answered call that lasted eight seconds is not a successful call. Detect short calls from the webhook and tag them for review."
slug: how-to-tag-short-calls-automatically
lang: en
locales: [en, tr]
categories: [developers]
intent: informational
translationKey: how-to-tag-short-calls-automatically
tags: [api, calls, tags, quality, dotnet]
task: 08
```

Yapı:

1. **Overview** — "cevaplandı" görünen başarısız çağrı problemi
2. **Before you start**
3. **Which duration field do you actually want?** — A bölümü, alan tablosu. Bu
   yazının en değerli bölümü.
4. **Who hung up, and why it matters** — B bölümü
5. **Adding a tag** — C bölümü, etiketin önceden tanımlı olması meselesi
6. **Wiring it to the webhook** — kural, eşiğin ayarlanabilir olması
7. **The full example**
8. **When it fails**
9. **Next steps** — serinin 07 ve 09'una link

### F3. Blog — Türkçe

`blog/tr/kisa-cagrilari-otomatik-etiketleme.md`

### F4. PR

---

## Kabul kriterleri

- [ ] A1'de bütün süre alanları listelenmiş
- [ ] A3'teki üç çağrı gerçekten üretilmiş, tablo dolu
- [ ] A4 cevaplanmış ve gerekçelendirilmiş
- [ ] A5 ölçülmüş: elle hesapla hazır alan tutuyor mu
- [ ] B1 ve B2 ölçülmüş
- [ ] B4'te tek/çift etiket kararı gerekçelendirilmiş
- [ ] C3 ölçülmüş: olmayan etiket oluşturuluyor mu
- [ ] C5 ve C8 ölçülmüş
- [ ] C9 ölçülmüş ve sonucu dürüstçe yazılmış
- [ ] Eşik `appsettings.json` veya ortam değişkeninden okunuyor
- [ ] Ödev 7'nin kuralı hâlâ çalışıyor, iki kural birbirini engellemiyor
- [ ] Kurallar ayrı sınıflarda, ortak arayüz arkasında
- [ ] Aynı olay iki kez gelince etiket tekrarlanmıyor
- [ ] EN + TR blog standarda uyuyor

---

## Bilerek söylemediklerim

1. Hangi alanın konuşma süresini verdiği (A4)
2. Hattı kimin kapattığını söyleyen alanın değerleri (B1)
3. Var olmayan etiketin davranışı (C3)
4. Etikete göre filtreleme yapılıp yapılamadığı (C9)

---

## Yaygın hatalar

- **Yanlış süre alanını seçmek.** Çalma süresini konuşma süresi sanmak bu ödevdeki
  bir numaralı hata; cevapsız çağrıların hepsi "kısa" olarak etiketlenir.
- **Eşiği koda gömmek.** Müşteri 15 saniye isteyince yeni sürüm çıkmak gerekir.
- **Cevaplanmamış çağrıları da etiketlemek.** Onlar Ödev 7'nin konusu; iki kural
  aynı çağrıya iki farklı hikâye yazmasın.
- **Etiketi çağrıyı yorumlamadan eklemek.** "short-call" etiketine bakan kişi
  nedenini de görmek isteyecek; kim kapattı bilgisi olmadan etiket yarım.
- **Sonradan filtrelenemeyen bir işaret bırakmak.** C9'u ölçmeden yazma.

---

## Teslim

Gözden geçirmede soracağım:

> Ekip lideri cuma günü "bu haftaki kısa çağrıları göster" dese, kaç tıklamada
> listeyi görüyor?

---

**Sonraki ödev:** [`09-arayan-ozeti-yorumu.md`](09-arayan-ozeti-yorumu.md)
