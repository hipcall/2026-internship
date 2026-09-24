---
title: "ASP.NET Core ile Hipcall Webhook Alıcısı Oluşturma"
description: "ASP.NET Core ile Hipcall webhook alıcısı kurun, çağrıları tekilleştirerek saklayın ve teslimat garantisi olmadan eksiksiz arşiv oluşturun."
slug: dotnet-ile-hipcall-webhook-alicisi-yazma
lang: tr
locales: [en, tr]
pubDate: 2026-09-22
categories: [developers]
intent: informational
translationKey: how-to-build-a-hipcall-webhook-receiver-in-dotnet
tags: [webhooks, dotnet, cdr, integrations]
authors: [hipcall-team]
featured: false
draft: true
task: 04
status: review
---

## Genel bakış

Çağrı kayıtlarını API üzerinden periyodik olarak çekmek (polling) kota tüketir ve veriyi gecikmeli almanıza neden olur.

Webhook'lar olayları uygulamanıza anında iletir. Bir çağrı başladığında, bağlandığında veya bittiğinde Hipcall santrali uygulamanıza bir HTTP POST gönderir. Çağrı bittiği an görüşme süresi ve ses kaydı bağlantısı doğrudan veritabanınıza ulaşır.

Üretim ortamına hazır bir webhook alıcısının dört görevi vardır:
- Zaman aşımını önlemek için 50 milisaniyenin altında HTTP 200 dönmek.
- Uç noktayı gizli bir rota anahtarıyla korumak.
- Ağ tekrarlarında veriyi UUID ile tekilleştirmek.
- Sunucu kesintilerini telafi etmek için gece mutabakatı yapmak.

Bu sayfada Hipcall panelinde webhook ayarlamayı, ASP.NET Core Minimal API ile bir alıcı yazmayı ve ses dosyalarını arka planda indirmeyi anlatıyoruz.

## Başlamadan önce

Şunlara ihtiyacınız var:
- .NET 8 SDK (`dotnet --version` 8.0 veya üstü olmalı).
- Dışarıdan erişilebilir bir HTTPS adresi. Yerel ortamda port 5080'i dışarı açmak için ngrok kullanın:
  ```bash
  ngrok http 5080
  ```
- Hipcall panelinde entegrasyon ekleme yetkisi.
- Test için çevrimiçi bir Hipcall uygulaması (web veya masaüstü).

## Webhook kurulumu

Webhook'u Hipcall panelinden oluşturun:

1. Ayarlar > Entegrasyonlar > Kataloğa Göz At bölümüne gidin.
2. Web kancası (Webhook) seçeneğine tıklayın.
3. Detayları girin:
   - Ad: `Üretim CDR Alıcısı` gibi bir isim verin.
   - URL: İçinde gizli bir yol bulunan HTTPS adresinizi yazın: `https://your-server.example.com/hipcall/events/whsec_live_xxxxxxxxxxxxxxxx`.
   - Olaylar: `call_init`, `call_bridged` ve `call_hangup` seçeneklerini işaretleyin.
4. Kayıtlar sekmesini açın. Hata Ayıklama Modu'nu etkinleştirdiğinizde sistem iki saat boyunca istek gövdelerini ve HTTP yanıtlarını loglar.

## İlk olayı alma

Yeni bir ASP.NET Core Minimal API projesi oluşturun:

```bash
dotnet new web -n Hipcall.WebhookReceiver
cd Hipcall.WebhookReceiver
```

Gizli anahtarı doğrulayan ve gövdeyi yazdıran bir alıcı hazırlayın:

```csharp
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5080);
});

var app = builder.Build();

var expectedSecret = Environment.GetEnvironmentVariable("HIPCALL_WEBHOOK_SECRET") ?? "whsec_live_xxxxxxxxxxxxxxxx";

app.MapPost("/hipcall/events/{secret?}", async (string? secret, HttpRequest request) =>
{
    if (string.IsNullOrEmpty(secret) || !string.Equals(secret, expectedSecret, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();
    Console.WriteLine($"Webhook başarıyla alındı:\n{body}");
    return Results.Ok();
});

app.Run();
```

Uygulamayı `dotnet run` ile çalıştırıp bir test araması yapın.

### Gövde yapısı

Hipcall, istekleri `application/json` olarak gönderir:

```json
{
  "event": "call_hangup",
  "data": {
    "uuid": "9a266251-d2a3-44fc-b422-9486ddf880c7",
    "direction": "outbound",
    "caller_number": "+90850XXXXXXX",
    "callee_number": "+90530XXXXXXX",
    "call_duration": 14,
    "missing_call": false,
    "hangup_by": "contact",
    "record_url": "https://storage.hipcall.com.tr/recordings/1412/2026/09/21/9a266251-d2a3-44fc-b422-9486ddf880c7.mp3?X-Amz-Expires=604800...",
    "started_at": "2026-09-21T10:37:07Z",
    "answered_at": "2026-09-21T10:37:07Z",
    "ended_at": "2026-09-21T10:37:21Z"
  }
}
```

### İstek başlıklarının incelenmesi

Gelen HTTP başlıkları şu şekildedir:

```http
Host: your-server.example.com
User-Agent: Hipcall-Webhook/1.0
Content-Type: application/json
Accept-Encoding: gzip
X-Forwarded-For: 31.192.211.2
X-Forwarded-Proto: https
```

Hipcall webhook istekleri `X-Signature` gibi HMAC imza başlıkları içermez.

## Olayların taşıdığı veriler

Tek bir çağrı birden fazla olay tetikler.

| Olay | Tetiklenme Anı | Öne Çıkan Alanlar | Kullanım Amacı |
|---|---|---|---|
| `call_init` | Santralde çağrı başladığında | `uuid`, `direction`, `caller_number`, `started_at` | Oturum başlangıcını tespit etme. |
| `call_bridged` | Temsilci bağlandığında | `uuid`, `direction`, `user_id`, `call_flow` | Temsilcinin katıldığını doğrulama. |
| `call_hangup` | Çağrı kapandığında | `uuid`, `call_duration`, `hangup_by`, `record_url` | Görüşme özeti ve ses arşivi oluşturma. |

### Tıklayıp arama (Click-to-Call) akışı

API üzerinden arama başlatıldığında Hipcall telefonu temsilciyi otomatik yanıtlar. Temsilci hemen santrale bağlandığı için, müşteri tarafı çalarken `call_init` ve `call_bridged` olayları peş peşe ulaşır.

### Ses kaydı bağlantısı

`call_hangup` olayındaki `data.record_url`, 7 gün geçerli geçici bir AWS S3 bağlantısıdır (`X-Amz-Expires=604800`).

Ses dosyasını webhook geldiği anda arka planda indirin ve kurumunuzun kendi depolama alanına kaydedin. Geçici bağlantıyı veritabanına yazıp bırakmayın.

## Güvenilir mimari tasarımı

Üretim ortamı için şu dört ilkeyi uygulayın:

```mermaid
flowchart TD
    A["Gelen Webhook İsteği"] --> B{"Gizli URL Şifresini Doğrula"}
    B -- "Geçersiz" --> C["401 Unauthorized"]
    B -- "Geçerli" --> D["Gövdeyi Ayrıştır ve UUID Kontrolü Yap"]
    D --> E["Anında HTTP 200 OK Dön (< 50 ms)"]

    subgraph BG ["Arka Plan Asenkron İşleme"]
        F["calls.json Dosyasına Tekil Kayıt Yaz (Upsert)"]
        F --> G{"record_url Var mı?"}
        G -- "Evet" --> H["MP3 Dosyasını recordings Klasörüne İndir"]
        G -- "Hayır" --> I["Tamamlandı"]
        H --> I
    end

    D -.->|Asenkron Görev| F

    subgraph REC ["Gece Mutabakat Görevi"]
        J["Zamanlanmış Görev: Gece 02:00"] --> K["Hipcall REST API: GET /api/v3/calls"]
        K --> L["UUID Küme Farkını Hesapla"]
        L --> M["Kaçan Çağrıları ve Sesleri Arşive Ekle"]
    end
```

### 1. Hızlı cevap verin, ağır işleri arka plana devredin

Hipcall yanıtı 15 saniye içinde bekler. Alıcı ses kaydı indirmek için beklerse bağlantı zaman aşımına uğrar.

İşlem sırası:
1. Gizli anahtarı doğrulayın (1 ms).
2. JSON gövdesini çözün.
3. Veriyi kuyruğa alın.
4. Anında HTTP 200 OK dönün (50 ms altı).
5. Ses indirme ve dosyaya yazma işlemini arka planda yapın.

### 2. Tekilleştirme (Idempotency)

Ağ sorunları aynı çağrı olayını tekrar gönderebilir. Veri kirliliğini önlemek için:
- Tekilleştirme anahtarı olarak sadece `data.uuid` kullanın.
- Zaman damgası veya telefon numarası üzerinden tekilleştirme yapmayın.
- Gelen kayıtlarla mevcut kaydı güncelleyin (upsert).

### 3. Gece mutabakatı

Yalnızca webhook kullanan bir sistemde sunucu yeniden başlatmaları sırasında veri kaçabilir.

Eksiksiz bir arşiv için:
- Her gece çalışan bir görev oluşturun.
- API'den günün çağrılarını çekin:
  ```http
  GET /api/v3/calls?started_at[gte]=...&started_at[lte]=...&limit=100
  ```
- API'deki UUID'ler ile kendi veritabanınızı karşılaştırın.
- Eksik çağrıları ve ses kayıtlarını API üzerinden tamamlayın.

### 4. İmzasız uç noktaları koruma

HMAC başlığı olmadığı için alıcı adresinizi korumanız gerekir:
1. Gizli URL yolu: URL'nizde gizli bir anahtar bulundurun (`/hipcall/events/whsec_live_...`). Anahtar yoksa HTTP 401 Unauthorized dönün.
2. IP beyaz listesi: Güvenlik duvarınızda (Nginx, Cloudflare) sadece Hipcall çıkış IP adresine (`31.192.211.2`) izin verin.

## Örnek uygulamanın tamamı

Bu ASP.NET Core Minimal API kodu gizli anahtar doğrulaması, tekilleştirilmiş dosya kaydı ve arka planda ses indirme içerir.

```csharp
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5080);
});

builder.Services.AddHttpClient();

var app = builder.Build();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

var expectedSecret = Environment.GetEnvironmentVariable("HIPCALL_WEBHOOK_SECRET") ?? "whsec_live_xxxxxxxxxxxxxxxx";
var baseDir = Directory.GetCurrentDirectory();
var callsFilePath = Path.Combine(baseDir, "calls.json");
var fileLock = new object();

Dictionary<string, CallRecord> storedCalls = new(StringComparer.OrdinalIgnoreCase);

if (File.Exists(callsFilePath))
{
    try
    {
        var existingJson = File.ReadAllText(callsFilePath);
        var existingList = JsonSerializer.Deserialize<List<CallRecord>>(existingJson, jsonOptions);
        if (existingList != null)
        {
            foreach (var call in existingList)
            {
                if (!string.IsNullOrEmpty(call.Uuid))
                {
                    storedCalls[call.Uuid] = call;
                }
            }
        }
    }
    catch
    {
    }
}

app.MapGet("/", () => Results.Ok("Hipcall Webhook Receiver aktif."));

app.MapPost("/hipcall/events/{secret?}", async (string? secret, HttpRequest request, IHttpClientFactory httpClientFactory) =>
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

    HipcallWebhookPayload? payload;
    try
    {
        payload = JsonSerializer.Deserialize<HipcallWebhookPayload>(rawBody, jsonOptions);
    }
    catch
    {
        return Results.Ok();
    }

    if (payload == null || string.IsNullOrEmpty(payload.Event))
    {
        return Results.Ok();
    }

    if (payload.Event != "call_hangup" && payload.Event != "call_init" && payload.Event != "call_bridged")
    {
        return Results.Ok();
    }

    var data = payload.Data;
    if (data == null || string.IsNullOrEmpty(data.Uuid))
    {
        return Results.Ok();
    }

    lock (fileLock)
    {
        var record = new CallRecord
        {
            Uuid = data.Uuid,
            Direction = data.Direction,
            CallerNumber = CallRecord.MaskNumber(data.CallerNumber),
            CalleeNumber = CallRecord.MaskNumber(data.CalleeNumber),
            CallDuration = data.CallDuration,
            MissingCall = data.MissingCall,
            HangupBy = data.HangupBy,
            RecordUrl = data.RecordUrl,
            StartedAt = data.StartedAt,
            AnsweredAt = data.AnsweredAt,
            EndedAt = data.EndedAt,
            LastEvent = payload.Event,
            UpdatedAt = DateTime.UtcNow.ToString("o")
        };

        storedCalls[data.Uuid] = record;

        try
        {
            var serialized = JsonSerializer.Serialize(storedCalls.Values.ToList(), jsonOptions);
            File.WriteAllText(callsFilePath, serialized);
        }
        catch
        {
        }
    }

    if (!string.IsNullOrEmpty(data.RecordUrl))
    {
        string audioUrl = data.RecordUrl;
        string callUuid = data.Uuid;
        _ = Task.Run(async () =>
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    await Task.Delay(attempt == 1 ? 2500 : 3000);
                    var response = await client.GetAsync(audioUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string recDir = Path.Combine(baseDir, "recordings");
                        Directory.CreateDirectory(recDir);
                        string filePath = Path.Combine(recDir, $"{callUuid}.mp3");
                        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                        await response.Content.CopyToAsync(fs);
                        return;
                    }
                }
            }
            catch
            {
            }
        });
    }

    return Results.Ok();
});

app.Run();

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
    public bool? MissingCall { get; set; }
    public string? MissingCallReason { get; set; }
    public string? HangupBy { get; set; }
    public string? RecordUrl { get; set; }
    public string? StartedAt { get; set; }
    public string? AnsweredAt { get; set; }
    public string? EndedAt { get; set; }
}

public class CallRecord
{
    public string? Uuid { get; set; }
    public string? Direction { get; set; }
    public string? CallerNumber { get; set; }
    public string? CalleeNumber { get; set; }
    public int? CallDuration { get; set; }
    public bool? MissingCall { get; set; }
    public string? HangupBy { get; set; }
    public string? RecordUrl { get; set; }
    public string? StartedAt { get; set; }
    public string? AnsweredAt { get; set; }
    public string? EndedAt { get; set; }
    public string? LastEvent { get; set; }
    public string? UpdatedAt { get; set; }

    public static string? MaskNumber(string? number)
    {
        if (string.IsNullOrEmpty(number) || number.Length <= 6)
            return number;
        return number[..6] + new string('X', number.Length - 6);
    }
}
```

## Hata aldığınızda

### 1. HTTP 500 yanıtı

Sunucunuz `500 Internal Server Error` döndürdüğünde:
- Telefon görüşmesi etkilenmez. Webhook dağıtımı ile telefon trafiği bağımsızdır.
- Hipcall hatayı loglara yazar.
- Hipcall istekleri tekrar denemez (at-most-once). Bu yüzden 200 dönmek ve kaçan kayıtları gece tamamlamak zorunludur.

### 2. Zaman aşımı

Alıcınız 15 saniyeden geç yanıt verirse Hipcall bağlantıyı keser ve olayı başarısız sayar.

### 3. Kırık durumu

Alıcınız bir saat içinde 4 kez başarısız yanıt (200 dışı kod veya 15 saniye zaman aşımı) verirse:
- Hipcall entegrasyonu Kırık (Broken) duruma alır.
- Siz tekrar Aktif konuma getirene kadar yeni webhook göndermez.
- Düzeltmek için paneli açın, durumu Aktif yapıp kaydedin.

Daha fazla bilgi için [Hipcall API Referansı](https://use.hipcall.com.tr/api-docs/) sayfasına bakın.

## Parametre listesi

Çağrı olaylarında `data` içindeki temel alanlar:

| Parametre | Tip | Örnek | Açıklama |
|---|---|---|---|
| `uuid` | string | `"9a266251-d2a3-44fc-b422-9486ddf880c7"` | Çağrının tekil kimliği. |
| `direction` | string | `"outbound"` | `"inbound"` veya `"outbound"`. |
| `caller_number` | string | `"+90850XXXXXXX"` | Arayan numara. |
| `callee_number` | string | `"+90530XXXXXXX"` | Aranan numara. |
| `call_duration` | integer | `14` | Toplam konuşma süresi (saniye). |
| `missing_call` | boolean | `false` | Yanıtsız gelen çağrılarda `true`. |
| `hangup_by` | string | `"contact"` | Kapatan taraf (`"user"`, `"contact"`, `"system"`). |
| `record_url` | string/null | `"https://storage.hipcall.com.tr/..."` | AWS S3 ses indirme bağlantısı. |
| `started_at` | string | `"2026-09-21T10:37:07Z"` | Çağrının başlama anı (UTC). |
| `answered_at` | string/null | `"2026-09-21T10:37:07Z"` | Çağrının yanıtlanma anı (UTC). |
| `ended_at` | string/null | `"2026-09-21T10:37:21Z"` | Çağrının kapanma anı (UTC). |

## Sonraki adımlar

- HTTP alıcısı ile veritabanı arasına RabbitMQ ekleyin.
- `calls.json` yerine `uuid` kolonu eşsiz olan bir PostgreSQL veritabanına geçin.
- Gece mutabakatını `GET /api/v3/calls` ile otomatikleştirin.
- Sorularınızı [Hipcall Topluluk](https://community.hipcall.com/) platformunda paylaşın.
