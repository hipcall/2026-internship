---
title: "Hipcall API ile Kısa Çağrıları Otomatik Etiketleme"
description: "Sekiz saniye süren ve cevaplanmış görünen bir çağrı aslında başarısızdır. Kısa çağrıları webhook üzerinden tespit edin ve inceleme için etiketleyin."
slug: kisa-cagrilari-otomatik-etiketleme
lang: tr
locales: [en, tr]
pubDate: 2026-09-28
categories: [developers]
intent: informational
translationKey: how-to-tag-short-calls-automatically
tags: [api, calls, tags, quality, dotnet]
authors: [hipcall-team]
featured: false
draft: true
task: 08
status: draft
---

## Genel bakış

8 saniye süren ve cevaplanmış bir çağrı raporlarda "başarılı" görünür. Gerçekte 8 saniyede hiçbir iş konuşulmaz. Bu çağrılar yanlış numara, ses gelmemesi veya ajanın hattı erken kapatması gibi sorunlara işaret eder.

Bu kılavuz, webhook üzerinden kısa çağrıları tespit etmeyi ve daha sonra incelenebilmesi için etiketlemeyi açıklar. Ekip liderleri etiketler üzerinden filtreleme yaparak sorunlu çağrıları tek tek aramak zorunda kalmadan bulabilir.

## Başlamadan önce

- Aktif bir Hipcall API anahtarı edinin.
- `call_hangup` webhook olayını karşılayacak altyapıyı kurun.
- Hipcall panelinde Settings > Call Center > Tags (Ayarlar > Çağrı Merkezi > Etiketler) menüsüne giderek gerekli etiketleri oluşturun. API üzerinden etiket oluşturulmaz, var olan etiketler atanır. Konfigürasyon için etiket ID değerlerini not edin.

## Hangi süre alanını kullanmalısınız?

Çağrı kaydı zamanla ilgili birden fazla alan içerir. Doğru alanı seçmek kritik önem taşır.

| Alan | Ölçüm |
|---|---|
| `call_duration` | Anons ve çalma süresi dahil toplam faturalandırılabilir süre |
| `first_touch_duration` | Sisteme girişten ajanın cevaplamasına kadar geçen süre |
| `started_at` | Çağrının sisteme girdiği an |
| `answered_at` | Santralin veya ajanın çağrıyı açtığı an |
| `bridged_at` | Müşteri ile ajanın bağlandığı an |
| `ended_at` | Hattın kapandığı an |

Konuşma süresini ölçmek için `call_duration` değerini kullanmayın. 45 saniye çalıp açılmayan bir çağrının `call_duration` değeri 45 saniyedir. Müşterinin 15 saniye anons dinleyip 6 saniye konuştuğu bir çağrının `call_duration` değeri 21 saniyedir.

Gerçek konuşma süresini hesaplamak için `ended_at` ile `bridged_at` zaman damgaları arasındaki farkı alın.

## Kim kapattı ve neden önemli?

Kısa çağrıları incelerken hattı kimin kapattığı en değerli bilgidir. Bu bilgiyi `hangup_by` alanından alabilirsiniz.

- **contact:** Müşteri kapattı. Yanlış numara veya meşguliyet kaynaklı doğal bir düşmedir.
- **user:** Ajan kapattı. Ajanın yüzüne kapatması veya donanım sorunu anlamına gelir. Kalite yöneticisi için kırmızı alarmdır.

İki ayrı etiket kullanın (örneğin `short-call-agent` ve `short-call-customer`). Tek bir genel etiket basmak, müşteri hatalarıyla kasti ajan kapatmalarını birbirine karıştırır.

## Adım 1: Etiket ekleme

Çağrıya etiket eklemek için `POST /api/v3/calls/{call_id}/tags` endpoint'ini kullanın. İstek gövdesinde etiketin adını değil, `tag_id` değerini gönderin.

```http
POST /api/v3/calls/5c1904ed-ea4d-4209-badd-a985caf0c32a/tags
Authorization: Bearer HIPCALL_API_TOKEN
Content-Type: application/json

{
  "tag_id": 5617
}
```

Var olmayan bir `tag_id` gönderirseniz API `404 Not Found` döner. Bu endpoint idempotent'tir. Aynı etiketi iki kez göndermek hata fırlatmaz, başarılı döner ancak mükerrer kayıt oluşturmaz.

## Adım 2: Webhook kuralları

Eşik değerini ve etiket ID'lerini `appsettings.json` dosyasından okuyun.

```json
{
  "Hipcall": {
    "ShortCallThresholdSeconds": 10,
    "ShortCallAgentTagId": 5618,
    "ShortCallCustomerTagId": 5617
  }
}
```

Eşiği koda gömmeyin. Dinamik değer kullanmak yeniden derleme yapmadan değişiklik imkanı verir. Kuralı işletmeden önce çağrının `bridged_at` değerine sahip olduğunu doğrulayın, cevapsız çağrıları es geçin.

## C# Minimal API uygulaması

Bu uygulama C# kullanır ve kuralı webhook alıcısında değerlendirir.

```csharp
using Hipcall.PostCall.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class ShortCallTagRule : IPostCallRule
{
    public string RuleName => "ShortCallTagRule";

    private readonly HipcallApiClient _api;
    private readonly HipcallSettings _settings;
    private readonly ILogger<ShortCallTagRule> _logger;

    public ShortCallTagRule(
        HipcallApiClient api,
        IOptions<HipcallSettings> settings,
        ILogger<ShortCallTagRule> logger)
    {
        _api = api;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool Matches(WebhookPayload payload)
    {
        if (payload.Data == null) return false;
        if (payload.Event != "call_hangup") return false;
        
        if (payload.Data.IsMissedCall) return false;
        if (string.IsNullOrEmpty(payload.Data.BridgedAt)) return false;

        return true;
    }

    public async Task ExecuteAsync(WebhookPayload payload, CancellationToken ct = default)
    {
        var call = payload.Data!;

        if (!DateTime.TryParse(call.BridgedAt, out var bridgedAt) || 
            !DateTime.TryParse(call.EndedAt, out var endedAt))
        {
            return;
        }

        var talkDurationSeconds = (endedAt - bridgedAt).TotalSeconds;

        if (talkDurationSeconds >= _settings.ShortCallThresholdSeconds)
        {
            return;
        }

        int tagId = call.HangupBy == "user" 
            ? _settings.ShortCallAgentTagId 
            : _settings.ShortCallCustomerTagId;

        if (tagId == 0) return;

        await _api.AddTagToCallAsync(call.Uuid, tagId, ct);
    }
}
```

`HipcallApiClient` metodunun içeriği:

```csharp
public async Task<bool> AddTagToCallAsync(string callUuid, int tagId, CancellationToken ct = default)
{
    var body = new { tag_id = tagId };
    var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");

    var response = await _http.PostAsync($"calls/{callUuid}/tags", content, ct);
    if (response.IsSuccessStatusCode)
    {
        return true;
    }

    var errorBody = await response.Content.ReadAsStringAsync(ct);
    _logger.LogError("[Tag] Error {Status}: {Body}", (int)response.StatusCode, errorBody);
    return false;
}
```

## Hata aldığınızda

**404 Not Found**

```json
{
  "errors": {
    "detail": "Not Found"
  }
}
```
Panelde bulunmayan bir `tag_id` gönderdiniz. Etiketin panelde oluşturulduğunu ve ID değerinin konfigürasyonda doğru tanımlandığını kontrol edin.

**422 Unprocessable Entity**

```json
{
  "errors": {
    "tag_id": [
      "Missing field: tag_id"
    ]
  }
}
```
Etiketin adını gönderdiniz. İstek gövdesini sayısal `tag_id` alanını içerecek şekilde düzenleyin.

## Parametre listesi

| Parametre | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `tag_id` | integer | evet | Etiketin benzersiz kimliği. Panelin etiket düzenleme ekranındaki URL üzerinden bulunabilir. |

## Sonraki adımlar

- Cevapsız çağrıları yakalamayı [Cevapsız Çağrıları Yönetme](/tr/developers/cevapsiz-cagri-nasil-islenir) yazısından okuyun.
- Önceki görüşme özetlerini [Arayan Geçmişi Özetleri](/tr/developers/arayan-gecmisi-ozetleri) üzerinden inceleyin.
