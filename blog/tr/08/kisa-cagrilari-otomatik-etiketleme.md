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

Ekip liderlerinin sorunlu çağrıları tek tek aramak zorunda kalmaması için kısa çağrıları webhook üzerinden tespit edip etiketleyin.

## Başlamadan önce

- Aktif bir Hipcall API anahtarı edinin.
- `call_hangup` webhook olayını karşılayacak altyapıyı kurun.
- Hipcall panelinde Ayarlar > Çağrı Merkezi > Etiketler menüsüne giderek gerekli etiketleri oluşturun. API üzerinden etiket oluşturulmaz, var olan etiketler atanır. Konfigürasyon için etiket ID değerlerini not edin.

## Hangi süre alanını kullanmalısınız?

Çağrı kaydı zamanla ilgili birden fazla alan içerir. Doğru alanı seçin. Yanlış alan hatalı rapor üretir.

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

Kısa çağrıları incelerken hattı kimin kapattığına bakın. Bu bilgiyi `hangup_by` alanından alabilirsiniz.

- **contact:** Müşteri kapattı. Yanlış numara veya meşguliyet kaynaklı doğal bir düşmedir.
- **user:** Ajan kapattı. Ajanın yüzüne kapatması veya donanım sorunu anlamına gelir. Kalite yöneticisi için kırmızı alarmdır.

İki ayrı etiket kullanın (örneğin `short-call-agent` ve `short-call-customer`). Tek bir genel etiket basmak, müşteri hatalarıyla kasti ajan kapatmalarını birbirine karıştırır.

## Adım 1: Etiket ekleme

Çağrıya etiket eklemek için `POST /api/v3/calls/{call_id}/tags` endpoint'ini kullanın. İstek gövdesinde etiketin adını değil, `tag_id` değerini gönderin.

```csharp
var apiToken = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN") 
    ?? throw new InvalidOperationException("HIPCALL_API_TOKEN ortam değişkeni bulunamadı.");

client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);

var tagBody = new { tag_id = 5635 };
var tagContent = new StringContent(JsonSerializer.Serialize(tagBody), Encoding.UTF8, "application/json");

var response = await client.PostAsync($"calls/{callUuid}/tags", tagContent);
```

İşlem başarılı olduğunda API `200 OK` (veya `201 Created`) döner ve atanan etiketin detaylarını verir:

```json
{
  "data": {
    "id": 5635,
    "name": "test-kisa-cagri-1",
    "description": "test",
    "color": "#ef4444",
    "color_name": "red"
  }
}
```

Var olmayan bir `tag_id` gönderirseniz API `404 Not Found` döner. Bu endpoint idempotent'tir. Aynı etiketi iki kez göndermek hata fırlatmaz, başarılı döner ancak mükerrer kayıt oluşturmaz.

## Adım 2: Webhook kuralları

Kuralı işletmeden önce çağrının `bridged_at` ve `ended_at` değerlerine sahip olduğunu doğrulayın, cevapsız çağrıları es geçin.

Uygulamanızı test etmek için aşağıdaki `curl` komutu ile altı saniye konuşulmuş sahte bir `call_hangup` olayını yerel sunucunuza gönderebilirsiniz:

```bash
curl -X POST http://localhost:5000/hipcall/events/whsec_live_xxxxxxxxxxxxxxxx \
  -H "Content-Type: application/json" \
  -d '{
  "data": {
    "credited": false,
    "team_touch_at": null,
    "first_touch_duration": 10,
    "contact_id": null,
    "callee_id": null,
    "answered_at": "2026-09-29T14:13:33Z",
    "voicemail_url": null,
    "caller_number": "+90850XXXXXXX",
    "missing_call_reason": null,
    "call_duration": 12,
    "callback_time": null,
    "call_flow": [
      {
        "action": "hangup",
        "detail": {
          "hangup_by": "contact"
        },
        "timestamp": 1790691215
      },
      {
        "action": "bridge",
        "detail": {
          "id": 4200,
          "type": "user"
        },
        "timestamp": 1790691205
      },
      {
        "action": "init",
        "detail": {
          "id": null,
          "type": "contact"
        },
        "timestamp": 1790691203
      }
    ],
    "direction": "outbound",
    "callee_number": "+90530XXXXXXX",
    "voicemail_id": null,
    "callback_user_id": null,
    "callee_type": "contact",
    "ended_at": "2026-09-29T14:13:35Z",
    "missing_call": false,
    "channel_type": "number",
    "callback_cdr_uuid": null,
    "voicemail_type": null,
    "caller_id": 4200,
    "started_at": "2026-09-29T14:13:23Z",
    "bridged_at": "2026-09-29T14:13:33Z",
    "channel_id": 942,
    "caller_type": "user",
    "user_id": 4200,
    "hangup_by": "contact",
    "uuid": "410c92c5-2b61-4dd2-aa75-d3601ae51277",
    "record_url": "https://storage.hipcall.com.tr/recordings/...masked...",
    "number_id": 942,
    "company_id": 80719
  },
  "event": "call_hangup"
}'
```

Eşik değerini ve etiket ID'lerini koda gömmeyin. Dinamik değer kullanmak yeniden derleme yapmadan değişiklik imkanı verir. Bunları `appsettings.json` dosyasından okuyun:

```json
{
  "Hipcall": {
    "ShortCallThresholdSeconds": 10,
    "ShortCallAgentTagId": 5618,
    "ShortCallCustomerTagId": 5635
  }
}
```

## Minimal API alıcı örneği

Bu ASP.NET Core uygulaması webhook olayını karşılar. Süreleri hesaplar, eşiğin altındaysa kapatan tarafa göre uygun etiketi API'ye gönderir.

```csharp
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var apiToken = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN") 
    ?? throw new InvalidOperationException("HIPCALL_API_TOKEN ortam değişkeni bulunamadı.");

builder.Services.AddHttpClient("HipcallClient", client =>
{
    client.BaseAddress = new Uri("https://use.hipcall.com.tr/api/v3/");
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
});

var app = builder.Build();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true
};

var expectedSecret = Environment.GetEnvironmentVariable("HIPCALL_WEBHOOK_SECRET") 
    ?? throw new InvalidOperationException("HIPCALL_WEBHOOK_SECRET ortam değişkeni bulunamadı.");

app.MapPost("/hipcall/events/{secret?}", async (
    string? secret,
    HttpRequest request,
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(secret) || !string.Equals(secret, expectedSecret, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var rawBody = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(rawBody))
    {
        return Results.Ok();
    }

    var payload = JsonSerializer.Deserialize<WebhookPayload>(rawBody, jsonOptions);
    if (payload?.Data == null || payload.Event != "call_hangup")
    {
        return Results.Ok();
    }

    var call = payload.Data;
    
    if (call.MissingCall || string.IsNullOrEmpty(call.BridgedAt) || string.IsNullOrEmpty(call.EndedAt))
    {
        return Results.Ok();
    }

    if (!DateTime.TryParse(call.BridgedAt, out var bridgedAt) || 
        !DateTime.TryParse(call.EndedAt, out var endedAt))
    {
        return Results.Ok();
    }

    var thresholdSeconds = config.GetValue<int>("Hipcall:ShortCallThresholdSeconds", 10);
    var agentTagId = config.GetValue<int>("Hipcall:ShortCallAgentTagId", 5618);
    var customerTagId = config.GetValue<int>("Hipcall:ShortCallCustomerTagId", 5635);

    var talkDuration = (endedAt - bridgedAt).TotalSeconds;

    if (talkDuration < thresholdSeconds)
    {
        _ = Task.Run(async () =>
        {
            var tagId = call.HangupBy == "user" ? agentTagId : customerTagId;
            if (tagId == 0) return;

            var client = httpClientFactory.CreateClient("HipcallClient");
            var body = new { tag_id = tagId };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"calls/{call.Uuid}/tags", content);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    logger.LogError("Etiket eklenemedi: {Uuid}, Hata: {Error}", call.Uuid, err);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Etiket eklenirken hata: {Uuid}", call.Uuid);
            }
        });
    }

    return Results.Ok();
});

app.Run();

record WebhookPayload(string Event, CallData? Data);
record CallData(string Uuid, bool MissingCall, string? BridgedAt, string? EndedAt, string? HangupBy);
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

- Temsilci bazında kısa çağrı raporları oluşturarak eğitim ihtiyaçlarını belirleyin.
- Müşteri kaynaklı kısa çağrıların hacmini izleyerek santral menünüzdeki (IVR) olası yönlendirme hatalarını tespit edin.
- Zaman içindeki gerçek konuşma sürelerini analiz ederek eşik değerinizi en uygun şekilde güncelleyin.
