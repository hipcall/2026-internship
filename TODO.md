# Blog yazıları: yayın öncesi düzeltmeler (Ödev 01–08)

Bu liste, blog yazılarının siteye elle düzeltme gerekmeden aktarılabilmesi için yapılması gereken düzeltmeleri içeriyor. Her madde ilgili satıra bağlı.

- Linkler `05b056c` commit'ine sabitlendi. Düzeltmeler yapılıp satırlar kaydıkça linkler bozulmaz, hep o anki hali gösterir.
- Linkler Türkçe dosyaya gidiyor. İngilizce dosyada aynı sorun, aksi belirtilmedikçe aynı satır numarasında duruyor.
- Bir maddeyi bitirince `[ ]` yerine `[x]`. Liste bitince bu dosya silinebilir.
- **Öneri** etiketli maddeler yazıyı okuyucu için daha anlaşılır hale getirmek için.

---

## Öncelikli: herkese açık repoda veri

Repo herkese açık. Bu maddeler diğerlerinden önce yapılmalı.

- [x] **Koda gömülü webhook secret'ı.** Ortam değişkeni yoksa kod sabit bir secret'a düşüyor: [04 Program.cs#L23](https://github.com/hipcall/2026-internship/blob/05b056c/submissions/04-webhook-alicisi/Hipcall.WebhookReceiver/Program.cs#L23), [05 Program.cs#L32](https://github.com/hipcall/2026-internship/blob/05b056c/submissions/05-insight-card/Hipcall.InsightCard/Program.cs#L32).
  - Varsayılan değer kaldırılmalı. Değişken tanımlı değilse uygulama hata verip durmalı.
---

## Tüm yazılar için

### Yazılar repoya ihtiyaç duymadan anlaşılır olmalı

Kod örneklerinin dili C# olarak kalıyor. Ama yazıyı okuyan developer bu repoyu bilmiyor; yazıdaki kod eksikse tamamlayabileceği bir yer yok. Blog standardındaki iskelet de (`HAZIRLIK → İSTEK ÖRNEĞİ → BAŞARILI CEVAP → BAŞARISIZ CEVAP → PARAMETRELER`) bir istek örneğinin yanında başarılı ve başarısız cevabın da gösterilmesini istiyor. Bazı yazılarda istekler yalnızca uygulama kodunun içinde geçiyor ve dönen cevap hiç görünmüyor. Önerilen yapı:

1. **Her API çağrısı için C# istek örneği ve cevabı.** Yazının anlattığı her çağrı için isteği gönderen kısa bir C# parçası gösterilmeli: endpoint, HTTP metodu ve gövde görünmeli. Hemen altında başarılı cevap yer almalı (durum kodu ve varsa gövdesi, maskelenmiş JSON olarak).
2. **Hatalar `Hata aldığınızda` bölümünde.** Standarttaki gibi her hata için durum kodu, gövdenin tamamı ve ne yapılması gerektiği yazılmalı.
3. **Öneri: Webhook yazılarında örnek payload (04, 05, 07, 08).** Kuralın kullandığı alanları içeren gerçek bir olay gövdesi JSON bloğu olarak eklenmeli. Payload DEMO'da gerçekten alınmış bir olaydan maskelenerek alınmalı. C# modelindeki alanlar (`bridged_at`, `hangup_by`, `voicemail_id` gibi) okuyucunun gördüğü bu payload'la eşleşmeli.
4. **"The full example" bölümü standarttaki [C# örnekleri](https://github.com/hipcall/2026-internship/blob/05b056c/blog/README.md?plain=1#L173-L185) kurallarına uymalı:** kopyalanıp çalıştırılabilir, eksik `using` ya da yazıda tanımlanmamış sınıf yok, anahtar ortam değişkeninden okunuyor, hata gövdesi yutulmuyor.

---

## 04: Webhook alıcısı

[TR](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/04/dotnet-ile-hipcall-webhook-alicisi-yazma.md) · [EN](https://github.com/hipcall/2026-internship/blob/05b056c/blog/en/04/how-to-build-a-hipcall-webhook-receiver-in-dotnet.md)

- [x] **Tekilleştirmenin gerekçesi.** Yazı tekilleştirmeyi "ağ tekrarları" ile açıklıyor ([#L28](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/04/dotnet-ile-hipcall-webhook-alicisi-yazma.md?plain=1#L28), [#L198](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/04/dotnet-ile-hipcall-webhook-alicisi-yazma.md?plain=1#L198)). Ama aynı yazı Hipcall'ın istekleri tekrar denemediğini de söylüyor ([#L441](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/04/dotnet-ile-hipcall-webhook-alicisi-yazma.md?plain=1#L441)). Bu durumda okuyucu, tekrar gönderim yoksa tekilleştirmenin neden gerektiğini anlayamaz. Gerekçe, [çalışma notunda](https://github.com/hipcall/2026-internship/blob/05b056c/submissions/04-webhook-alicisi.md?plain=1#L250) da yazan gerçek sebeplerle değiştirilmeli:
  - Gece mutabakatı API'den günün çağrılarını çektiğinde, webhook ile zaten kaydedilmiş çağrılar da gelir.
  - Aynı çağrının `call_init`, `call_bridged` ve `call_hangup` olayları aynı `uuid`'yi taşır ve hepsi aynı kaydı güncellemeli.

## 05: Insight Card

[TR](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/05/insight-card-ile-arayan-bilgisini-ekranda-gosterme.md) · [EN](https://github.com/hipcall/2026-internship/blob/05b056c/blog/en/05/how-to-show-caller-context-with-insight-card.md)

- [x] **Öneri: `call_init` payload örneği.** [Webhook entegrasyonu](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/05/insight-card-ile-arayan-bilgisini-ekranda-gosterme.md?plain=1#L120) bölümüne eklenmeli. Okuyucu `direction`, `caller_number` ve `callee_number` alanlarını ve kartı gönderirken kullanılan `uuid`'yi görebilmeli.

## 06: external_id ile senkronizasyon

[TR](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/06/external-id-ile-kisi-senkronizasyonu.md) · [EN](https://github.com/hipcall/2026-internship/blob/05b056c/blog/en/06/how-to-sync-contacts-with-external-id.md)

- [x] **Repo yolu.** [#L205](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/06/external-id-ile-kisi-senkronizasyonu.md?plain=1#L205) satırındaki `submissions/...` yolu yayında hiçbir yere götürmez. Standart da bu repoya link verilmesine izin vermiyor. Cümle kaldırılmalı.

## 07: Cevapsız çağrı takibi

[TR](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/07/cevapsiz-cagrilari-otomatik-kaydetme-ve-takibe-alma.md) · [EN](https://github.com/hipcall/2026-internship/blob/05b056c/blog/en/07/how-to-log-and-follow-up-missed-calls-automatically.md)

- [x] **Öneri: payload örneği.** Kuralın kullandığı alanları gösteren bir `call_hangup` payload'u eklenmeli: `missing_call`, `voicemail_id`, `direction`, `contact_id`, `company_id`, `user_id`. `missing_call` ve `voicemail_id` yazıdaki tabloda var, ama `contact_id`, `company_id` ve `user_id` yazıda hiç görünmüyor.

## 08: Kısa çağrı etiketleme

[TR](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/08/kisa-cagrilari-otomatik-etiketleme.md) · [EN](https://github.com/hipcall/2026-internship/blob/05b056c/blog/en/08/how-to-tag-short-calls-automatically.md)

- [x] **Token düz metin gibi duruyor.** [#L61-L69](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/08/kisa-cagrilari-otomatik-etiketleme.md?plain=1#L61-L69) satırlarında `Bearer HIPCALL_API_TOKEN` yazılmış; gerçek bir token gibi okunuyor. İstek, token'ı ortam değişkeninden okuyan bir C# örneği olarak verilmeli ve başarılı cevap eklenmeli.
- [x] **Kod tek başına derlenmiyor.** [#L93-L173](https://github.com/hipcall/2026-internship/blob/05b056c/blog/tr/08/kisa-cagrilari-otomatik-etiketleme.md?plain=1#L93-L173) arasındaki C# parçaları `IPostCallRule`, `HipcallApiClient`, `HipcallSettings` ve `IsMissedCall`'a bağımlı, ama bunların hiçbiri yazıda tanımlı değil. Ya bu tanımlar yazıya eklenmeli ya da kod onlara ihtiyaç duymayacak şekilde yeniden yazılmalı (bkz. "Tüm yazılar için", 4. madde).
- [x] **Öneri: payload örneği.** `bridged_at`, `ended_at` ve `hangup_by` alanlarını içeren bir `call_hangup` payload'u eklenmeli.

---
