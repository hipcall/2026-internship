---
title: "Insight Card ile Arayan Bilgisini Ekranda Gösterme"
description: "call_init webhook'unu ve Insight Card API'sini kullanarak çağrı başladığı an müşteri verilerini doğrudan web telefonu ekranına basın."
slug: insight-card-ile-arayan-bilgisini-ekranda-gosterme
lang: tr
locales: [en, tr]
pubDate: 2026-09-22
categories: [developers]
intent: informational
translationKey: how-to-show-caller-context-with-insight-card
tags: [insight-card, webhooks, dotnet, crm]
authors: [hipcall-team]
featured: false
draft: true
task: 05
status: review
---

## Genel bakış

Temsilci çalan telefonu açtığında ekranda sadece bir numara görürse, CRM sekmesine geçip bu numarayı araması ortalama on beş saniye sürer. Bu sırada müşteri konuşmaya başlamıştır.

Insight Card bu gecikmeyi çözer. Çağrı başladığı an müşterinin adını, şirketini, bakiyesini ve hesap yöneticisini uygulamanızdan alıp web telefonuna yansıtır.

Bu sayfada ASP.NET Core Minimal API ile `call_init` olayını dinlemeyi, numarayı veritabanında sorgulamayı ve canlı çağrı oturumuna Insight Card göndermeyi anlatıyoruz.

## Başlamadan önce

Çalışmaya başlamadan önce şu gereksinimlerin hazır olduğundan emin olun:

- .NET 8 SDK (Çalıştığınız makinede veya sunucuda `dotnet --version` 8.0 veya üstü olmalı).
- Webhook isteklerini alabilmek için dışarıdan erişilebilir bir HTTPS adresi (Örn: ngrok ile 5080 portunu dışarı açın).
- Hipcall Geliştirici Portalından alınmış geçerli bir API Anahtarı.
- Kart görünümünü test edebilmek için tarayıcınızda açık bir Hipcall Web Telefonu oturumu.

API anahtarınızı ortam değişkenlerine ekleyin:

```bash
export HIPCALL_API_TOKEN="SFMyNTY.g2gDbQAAAC..."
```

## Insight Card yapısı ve görsel bileşenler

Insight Card, çağrı penceresi içerisinde alt alta dizilmiş satırlardan oluşur. Üç temel satır tipi desteklenir:

| Satır Tipi | Zorunlu Alanlar | İsteğe Bağlı Alanlar | Açıklama |
|---|---|---|---|
| `title` | `type`, `text` | `link` | Ana kart başlığı. `link` eklendiğinde sağ tarafında tıklanabilir bir ikon çıkar. |
| `shortText` | `type`, `text` | `label`, `link`, `ios`, `android` | İki sütunlu standart veri satırı. Solda soluk `label`, sağda koyu `text` görünür (şirket, bakiye vb. için idealdir). |
| `user` | `type`, `label`, `user_id` | - | Hipcall kullanıcı ID'sini alarak, panodaki temsilcinin gerçek adını ve soyadını karta basar. |

Örnek bir müşteri kartının JSON gövdesi:

```json
{
  "card": [
    {
      "type": "title",
      "text": "Ahmet Y.",
      "link": "https://crm.example.com/customers/102"
    },
    {
      "type": "shortText",
      "label": "Şirket",
      "text": "Örnek Teknoloji A.Ş.",
      "link": "https://crm.example.com/customers/102"
    },
    {
      "type": "shortText",
      "label": "Segment",
      "text": "Kurumsal"
    },
    {
      "type": "shortText",
      "label": "Bakiye",
      "text": "14.250 TL (Açık Fatura)"
    },
    {
      "type": "user",
      "label": "Hesap Yöneticisi",
      "user_id": 4200
    }
  ]
}
```

## REST API üzerinden Insight Card basma

Canlı bir çağrıya kart eklemek için `/api/v3/calls/{call_id}/cards` adresine HTTP POST isteği gönderin:

```bash
curl -X POST "https://use.hipcall.com.tr/api/v3/calls/{call_id}/cards" \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "card": [
      {
        "type": "title",
        "text": "Acme CRM Müşteri Profili",
        "link": "https://crm.example.com/customer/101"
      },
      {
        "type": "shortText",
        "label": "Müşteri",
        "text": "Ahmet Y."
      },
      {
        "type": "shortText",
        "label": "Durum",
        "text": "VIP - Borcu Yok"
      }
    ]
  }'
```

İşlem başarılı olduğunda API HTTP `201 Created` döner ve kart temsilcinin web telefonu arayüzünde görünür:

![Hipcall Web Telefonunda Insight Card Görünümü](/blog/assets/insight-card-test-page4-1.png)

## Webhook entegrasyonu ve çağrı akışı

Temsilci telefonu yanıtladığı an kartın ekranda hazır olması için sürecin `call_init` olayıyla tetiklenmesi gerekir.

Terminalinizde aşağıdaki `curl` komutunu çalıştırarak yerel alıcınıza sahte bir Hipcall `call_init` olayı gönderebilirsiniz:

```bash
curl -X POST http://localhost:5080/hipcall/events/whsec_live_xxxxxxxxxxxxxxxx \
  -H "Content-Type: application/json" \
  -d '{
  "data": {
    "credited": null,
    "team_touch_at": null,
    "first_touch_duration": null,
    "contact_id": null,
    "callee_id": null,
    "answered_at": null,
    "voicemail_url": null,
    "caller_number": "+90850XXXXXXX",
    "missing_call_reason": null,
    "call_duration": null,
    "callback_time": null,
    "call_flow": [
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
    "ended_at": null,
    "missing_call": null,
    "channel_type": "number",
    "callback_cdr_uuid": null,
    "voicemail_type": null,
    "caller_id": null,
    "started_at": "2026-09-29T14:13:23Z",
    "bridged_at": null,
    "channel_id": 942,
    "caller_type": null,
    "user_id": 4200,
    "hangup_by": null,
    "uuid": "410c92c5-2b61-4dd2-aa75-xxxxxxxxxxxx",
    "record_url": null,
    "number_id": 942,
    "company_id": 80719
  },
  "event": "call_init"
}'
```

### Çağrı yönüne göre numara ayıklama

Aranan hedefin (müşterinin) hangi alanda yer aldığı `direction` (yön) parametresine bağlıdır:

- **Gelen çağrılar (`inbound`):** Arayan taraf dışarıdaki müşteridir. Numarayı `data.caller_number` alanından alın.
- **Giden çağrılar (`outbound`):** Temsilcinin başlattığı çağrıdır. Müşteri numarası `data.callee_number` alanındadır.

### Müşteri bulunamadığında boş kart basımını engelleme

Hipcall teknik olarak boş kartları (`{"card": []}`) kabul eder. Ancak boş bir kart basarsanız ajan arayüzünde içi boş, anlamsız bir gri panel belirir. Veritabanınızda eşleşen bir kayıt yoksa API'ye HTTP POST isteği atmayın; webhook isteğini sadece 200 dönerek sessizce bitirin.

## Minimal API alıcı örneği

Bu ASP.NET Core Minimal API uç noktası, gelen `call_init` olayını karşılar, çağrı yönüne göre numarayı bulur ve işlemi arka plana devredip derhal `200 OK` döner.

```csharp
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(5080));
builder.Services.AddHttpClient();
var app = builder.Build();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var expectedSecret = Environment.GetEnvironmentVariable("HIPCALL_WEBHOOK_SECRET") 
    ?? throw new InvalidOperationException("HIPCALL_WEBHOOK_SECRET ortam değişkeni bulunamadı.");
var apiToken = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN") 
    ?? throw new InvalidOperationException("HIPCALL_API_TOKEN ortam değişkeni bulunamadı.");

app.MapPost("/hipcall/events/{secret?}", async (string? secret, HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    if (!string.Equals(secret, expectedSecret, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    HipcallWebhookPayload? payload;
    try
    {
        payload = await JsonSerializer.DeserializeAsync<HipcallWebhookPayload>(request.Body, jsonOptions);
    }
    catch
    {
        return Results.Ok();
    }

    if (payload?.Event == "call_init" && payload.Data?.Uuid != null)
    {
        string? targetPhone = string.Equals(payload.Data.Direction, "inbound", StringComparison.OrdinalIgnoreCase)
            ? payload.Data.CallerNumber
            : payload.Data.CalleeNumber;

        if (!string.IsNullOrWhiteSpace(targetPhone))
        {
            _ = Task.Run(() => ProcessInsightCardAsync(payload.Data.Uuid, targetPhone, httpClientFactory, apiToken, jsonOptions));
        }
    }

    return Results.Ok();
});

app.Run();

async Task ProcessInsightCardAsync(string uuid, string phone, IHttpClientFactory clientFactory, string token, JsonSerializerOptions options)
{
    // Örnek CRM Sorgusu
    if (phone != "+90530XXXXXXX") return; 

    var cardData = new InsightCardRoot
    {
        Card =
        [
            new InsightCardItem { Type = "title", Text = "Ahmet Y.", Link = "https://crm.example.com/customers/102" },
            new InsightCardItem { Type = "shortText", Label = "Şirket", Text = "Örnek Teknoloji A.Ş." }
        ]
    };

    var client = clientFactory.CreateClient();
    using var content = new StringContent(JsonSerializer.Serialize(cardData, options), Encoding.UTF8, "application/json");
    using var req = new HttpRequestMessage(HttpMethod.Post, $"https://use.hipcall.com.tr/api/v3/calls/{uuid}/cards")
    {
        Content = content
    };
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    await client.SendAsync(req);
}

public class HipcallWebhookPayload
{
    public string? Event { get; set; }
    public CallDataPayload? Data { get; set; }
}

public class CallDataPayload
{
    public string? Uuid { get; set; }
    public string? Direction { get; set; }
    public string? CallerNumber { get; set; }
    public string? CalleeNumber { get; set; }
    public int? CallDuration { get; set; }
    public string? RecordUrl { get; set; }
    public string? HangupBy { get; set; }
    public string? VoicemailId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? BridgedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class InsightCardRoot
{
    public List<InsightCardItem> Card { get; set; } = [];
}

public class InsightCardItem
{
    public string Type { get; set; } = "shortText";
    public string? Label { get; set; }
    public string? Text { get; set; }
    public string? Link { get; set; }
    public int? UserId { get; set; }
}
```

## Hata aldığınızda

### 1. HTTP 422 Unprocessable Entity ve katı şema doğrulaması

Insight Card API'si çok katı bir şema (schema) kullanır. Örneğin `shortText` satırına `user_id` alanı gönderirseniz (değeri `null` bile olsa), API isteği reddeder:

```text
HTTP 422 Unprocessable Entity
shortText type only allows fields: type, text, label, link, android, ios. Invalid fields found: user_id in card item 2
```

C# nesnelerinizi serileştirirken JSON ayarlarına `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` ekleyin. Bu ayar, içi boş olan özelliklerin JSON gövdesine yazılmasını engeller.

### 2. Çağrı bittikten sonra kart yollamak

Gecikmeli bir API isteği atarsanız ve çağrı o sırada kapanmışsa, Hipcall kartı kabul eder ve veritabanına işler (`201 Created` döner). Ancak ekran kapanmış olduğu için ajan kartı göremez.

### 3. Süre bütçesi (Latency)

Ortalama bir telefon 5-15 saniye çalar. Webhook ağ süresi (~150 ms) ve kart basım süresi (~200 ms) düşüldüğünde, kendi veritabanınızda 2 saniyelik bir sorgu yapmanız son derece güvenlidir. Ancak CRM sorgunuz 3-4 saniyeyi aşıyorsa kartlar ancak temsilci telefonu açtıktan sonra ekrana düşer.

## Parametre listesi

Desteklenen satır tipleri ve izin verilen alanlar:

| Satır Tipi | Desteklenen Alanlar | Açıklama |
|---|---|---|
| `title` | `type`, `text`, `link` | Tıklanabilir bağlantıya sahip ana başlık satırı. |
| `shortText` | `type`, `text`, `label`, `link`, `ios`, `android` | Veri çiftleri, web bağlantıları veya mobil uygulama linkleri (deep link). |
| `user` | `type`, `label`, `user_id` | Hipcall kullanıcı kimliğini alıp personel adını basar. |

## Sonraki adımlar

- Hızlı aramalar için PostgreSQL veritabanı veya Redis önbellek (cache) mimarisine geçiş yapın.
- Arayan müşteri kendi CRM sisteminizde bulunmadığında ikinci (fallback) adım olarak Hipcall `GET /api/v3/lookup/by_phone` metodunu sorgulayın.
- Mobil cihazdan çalışan temsilcilerin kendi şirket uygulamanıza doğrudan geçiş yapması için `ios` ve `android` link yapılandırmalarını (deep link) karta ekleyin.
- Sorularınızı [Hipcall Topluluk](https://community.hipcall.com/) platformunda paylaşın.
