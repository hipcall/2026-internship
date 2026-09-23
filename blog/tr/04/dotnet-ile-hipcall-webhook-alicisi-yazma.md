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

Çağrı kayıtlarını veri ambarınıza, CRM yazılımınıza veya muhasebe sisteminize aktarırken liste endpoint'lerini periyodik olarak sorgulamak (polling), hem API kota sınırlarını tüketir hem de çağrı verilerinin sisteminize onlarca saniye gecikmeyle ulaşmasına neden olur.

Webhook mimarisi bu süreci anlık ve verimli hale getirir. Bir çağrı başladığında, temsilciye bağlandığında veya sonlandığında Hipcall santrali doğrudan uygulamanıza bir HTTP POST isteği gönderir. Böylece çağrı bittiği milisaniyede görüşme süresi, sonlanma nedeni ve ses kaydı bağlantısı sisteminize gecikmesiz akar.

Üretim ortamında kesintisiz çalışan bir webhook altyapısı kurmak birkaç temel mühendislik adımını bir araya getirmeyi gerektirir:
- Santral dağıtıcısını bekletmemek için **50 milisaniyenin altında hızlı yanıt vermek** ve ağır işleri (ses kaydı indirme, veritabanı yazma) arka plana devretmek.
- İmzalanmamış webhook uç noktalarını **gizli URL rota anahtarı** ile güvenceye almak.
- Ağ dalgalanmalarına karşı kayıtları **UUID ile tekilleştirmek (idempotency)**.
- Canlı webhook akışını, olası ağ kesintilerine karşı **gece mutabakat servisi** ile destekleyerek sıfır veri kaybı garantisi sağlamak.

Bu rehberde, Hipcall panelinde webhook yapılandırmayı, bu mimari ilkeleri uygulayan güvenilir bir ASP.NET Core Minimal API alıcısı kurmayı ve ses dosyalarını asenkron arşivlemeyi adım adım uyguluyoruz.

## Başlamadan önce

Çalışmaya başlamadan önce şu gereksinimleri hazırlayın:

- Bilgisayarınızda veya sunucunuzda **.NET 8 SDK** kurulu olmalıdır (`dotnet --version` çıktısı `8.0` veya üstü).
- **Dışarıdan erişilebilir bir HTTPS adresi.** Yerel geliştirme ortamında test yapabilmek için [ngrok](https://ngrok.com/) aracılığıyla 5080 portunu dış dünyaya açın:
  ```bash
  ngrok http 5080
  ```
- Hipcall panelinde entegrasyon ekleme yetkisine sahip bir hesap.
- Test aramaları yapabilmek için çevrim içi durumda olan **kayıtlı bir temsilci cihazı** (Hipcall web telefonu veya masaüstü uygulaması).

## Webhook kurulumu

Webhook entegrasyonunu Hipcall yönetim panelinde oluşturun:

1. **Ayarlar > Entegrasyonlar > Kataloğa Göz At** sayfasına gidin.
2. Entegrasyon kataloğundan **Web kancası (Webhook)** seçeneğini belirleyin.
3. Gerekli alanları doldurun:
   - **Ad (Zorunlu):** Entegrasyon için tanımlayıcı bir isim girin (Örn: `Çağrı Kaydı Alıcısı`).
   - **URL (Zorunlu):** Gizli yol parçasını içeren genel HTTPS adresinizi yazın: `https://your-server.example.com/hipcall/events/whsec_live_xxxxxxxxxxxxxxxx`.
   - **Olaylar:** Abone olmak istediğiniz çağrı olaylarını seçin: `call_init`, `call_bridged` ve `call_hangup`.
4. **Kayıtlar** sekmesini açın. Webhook isteklerinin loglanması **Hata Ayıklama Modu** ile yönetilir. Geliştirme esnasında açıldığında, sistem iki saat boyunca gelen istek gövdelerini ve HTTP yanıt kodlarını kaydeder; ardından log tutmayı otomatik kapatıp günlükleri temizler.

## İlk olayı alma

Yeni bir ASP.NET Core Minimal API projesi oluşturun:

```bash
dotnet new web -n Hipcall.WebhookReceiver
cd Hipcall.WebhookReceiver
```

Panelde tanımladığımız gizli anahtarı doğrulayan ve gelen gövdeyi konsola yazdıran temel bir alıcıyla başlayın:

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

Uygulamayı `dotnet run` komutuyla başlatın ve panelden veya telefonunuzdan bir test araması gerçekleştirin.

### Gövde yapısı

Hipcall istekleri `Content-Type: application/json` başlığıyla gönderir. Tüm olaylar iki anahtarlı standart bir zarf yapısı taşır:

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

Gelen HTTP başlıkları incelendiğinde şu yapı görülür:

```http
Host: your-server.example.com
User-Agent: Hipcall-Webhook/1.0
Content-Type: application/json
Accept-Encoding: gzip
X-Forwarded-For: 31.192.211.2
X-Forwarded-Proto: https
```

Gelen başlıklarda `X-Signature` veya `X-Hub-Signature` gibi bir HMAC imza başlığı yer almaz. Güvenlik, URL içindeki gizli anahtar ve IP filtreleme ile sağlanır.

## Olayların taşıdığı veriler

Tek bir çağrı oturumu yaşam döngüsü boyunca üç temel webhook olayı tetikler:

| Olay | Tetiklenme Anı | Öne Çıkan Alanlar | Kullanım Amacı |
|---|---|---|---|
| `call_init` | Santralde çağrı oturumu başladığında | `uuid`, `direction`, `caller_number`, `started_at` | Çağrı başlangıcını tespit etme ve CRM eşleştirmesi yapma. |
| `call_bridged` | Temsilci santral köprüsüne bağlandığında | `uuid`, `direction`, `user_id`, `call_flow` | Temsilci bacağının bağlandığını teyit etme. |
| `call_hangup` | Çağrı taraflardan biri tarafından kapatıldığında | `uuid`, `call_duration`, `hangup_by`, `record_url` | Muhasebeleştirme ve kalıcı ses arşivi oluşturma. |

### Tıklayıp arama (Click-to-Call) akışı

API veya panel üzerinden click-to-call ile arama başlatıldığında Hipcall web telefonu temsilci bacağını otomatik yanıtlar. Temsilci anında santral köprüsüne bağlandığı için, karşı tarafın telefonu henüz çalarken `call_init` ve `call_bridged` olayları 1-2 saniye arayla peş peşe ulaşır.

### Ses kaydı bağlantısı ve kalıcı depolama

`call_hangup` olayında dönen `data.record_url` alanı, AWS S3 üzerinde barındırılan geçici imzalı bir Presigned URL'dir (`X-Amz-Expires=604800` parametresi ile 7 gün geçerlidir).

Kurumsal sistemlerde kalıcı ve kesintisiz bir ses arşivi oluşturmanın en doğru yolu, bu geçici bağlantıyı olduğu gibi veritabanına yazmak yerine, webhook geldiği anda ses dosyasını asenkron bir arka plan göreviyle indirip kurumun kendi kalıcı depolama alanına (yerel disk, özel S3 kovası vb.) kaydetmektir.

## Güvenilir mimari tasarımı

Üretim seviyesinde bir webhook alıcısı kurarken dört temel tasarım kuralı uygulanır:

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

Hipcall alıcıdan yanıtı en fazla 15 saniye içinde bekler. Alıcı HTTP isteğini bekletip ses kaydı indirmeye veya veritabanı kilitlerine girdiğinde bağlantı zaman aşımına uğrayabilir.

En sağlıklı işlem sırası:
1. Gizli anahtar doğrulamasını gerçekleştirin (yaklaşık 1 ms).
2. JSON gövdesini çözün.
3. Veriyi belleğe veya iş kuyruğuna alın.
4. **Anında HTTP `200 OK` cevabı dönün** (< 50 ms).
5. Dosya yazma ve ses indirme işlemlerini arka plan görevinde tamamlayın.

### 2. Tekilleştirme (Idempotency)

Ağ dalgalanmaları veya servis güncellemeleri nedeniyle aynı çağrı oturumunun birden fazla kez iletilmesi durumunda veri kirliliğini önlemek için:
- Tekilleştirme anahtarı olarak daima değişmez olan `data.uuid` alanını kullanın.
- Birden fazla çağrı aynı saniyede başlayabileceği için asla zaman damgası veya telefon numarası üzerinden tekilleştirme yapmayın.
- Var olan kaydı güncelleme (upsert) yaklaşımını uygulayın.

### 3. Gece mutabakatı (Reconciliation)

Yalnızca webhook dinleyen bir sistem, sunucu yeniden başlatmaları veya ağ kesintileri nedeniyle zaman içinde küçük veri kaçakları yaşayabilir.

Eksiksiz arşiv garantisi sağlamak için:
- Her gece çalışan zamanlanmış bir arka plan görevi (Windows Görev Zamanlayıcısı veya cron) kurun.
- Günün çağrılarını çekmek için Hipcall REST API'sine istek atın:
  ```http
  GET /api/v3/calls?started_at[gte]=...&started_at[lte]=...&sort=started_at.asc&limit=100
  ```
- API'den gelen UUID listesi ile yerel veritabanınızdaki UUID listesini karşılaştırarak küme farkını alın.
- Eksik kalan çağrıları ve ses kayıtlarını API üzerinden indirip arşivi kuruşu kuruşuna eşitleyin.

### 4. İmzasız uç noktaları koruma

HMAC imza başlığı bulunmadığı için alıcı adresinizi iki yöntemle koruyun:
1. **Gizli URL yolu:** URL rotasına tahmin edilemez bir anahtar yerleştirin (`/hipcall/events/whsec_live_xxxxxxxxxxxxxxxx`) ve bu anahtarı taşımayan tüm istekleri doğrudan HTTP `401 Unauthorized` ile reddedin.
2. **IP beyaz listesi:** Güvenlik duvarınızda (Nginx veya Cloudflare) gelen istekleri yalnızca Hipcall santralinin çıkış IP adresine (`31.192.211.2`) izin verecek şekilde sınırlandırın.

## Örnek uygulamanın tamamı

Aşağıda snake_case model eşlemesi, gizli anahtar doğrulaması, tanınmayan olay toleransı, tekilleştirilmiş dosya kaydı ve asenkron ses indirme özelliklerini içeren eksiksiz ASP.NET Core Minimal API kodu yer almaktadır:

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

Hipcall webhook altyapısındaki hata durumlarını tanımak beklenmeyen kesintilerin önüne geçer:

### 1. HTTP 500 yanıtı ve tek gönderimli model
Sunucunuz iç hata verip `500 Internal Server Error` döndürdüğünde:
- Telefon görüşmesi kesintiye uğramaz; santraldeki arama akışı ile webhook dağıtımı birbirinden bağımsızdır.
- Hipcall entegrasyon kayıtlarına `500` yazar.
- Hipcall başarısız istekleri otomatik tekrar denemez (at-most-once iletim modeli). Bu nedenle alıcınızda gelen yükü kuyruğa aldıktan hemen sonra 200 dönmek ve kaçan kayıtları gece mutabakat servisiyle tamamlamak esastır.

### 2. Zaman aşımı durumu
Alıcınızın yanıt süresi 15 saniyeyi aştığında Hipcall TCP bağlantısını sonlandırır ve olayı başarısız kabul ederek düşürür.

### 3. Başarısız yanıtlar ve "Kırık" durumu
Alıcınız 1 saat içinde 4 kez 200 dışı başarısız yanıt döndüğünde ya da 15 saniyelik zaman aşımına uğradığında:
- Hipcall santral kaynaklarını korumak amacıyla entegrasyon durumunu otomatik olarak **Kırık** durumuna getirir.
- Durum Kırık olduğunda, siz onu panelden tekrar Aktif konuma getirene kadar santral yeni webhook istekleri göndermez.
- **Eski hâline döndürme:** Panelde entegrasyonu açın, **Düzenle** butonuna tıklayın, durum anahtarını tekrar **Aktif** yapıp **Kaydet** butonuna basın.

Ayrıntılı yapılandırma ve güncel rehberler için Hipcall'ın resmi [Webkancaları nelerdir ve nasıl ayarlanır?](https://yardim.hipcall.com/gelistirme-araclari/webkancalari-nelerdir-ve-nasil-ayarlanir/) dokümanına göz atabilirsiniz. Bu rehberdeki kurallar Hipcall Webkancaları v1 altyapısını kapsamaktadır; ilerleyen dönemde [Standard Webhooks](https://www.standardwebhooks.com/) standardına uygun v2 sürümü geliştirilecektir.

## Parametre listesi

Çağrı olaylarında `data` nesnesi içinde iletilen temel alanlar:

| Parametre | Tip | Örnek | Açıklama |
|---|---|---|---|
| `uuid` | string | `"9a266251-d2a3-44fc-b422-9486ddf880c7"` | Çağrının sistem genelindeki tekil kimliği. |
| `direction` | string | `"outbound"` | Çağrı yönü (`"inbound"` veya `"outbound"`). |
| `caller_number` | string | `"+90850XXXXXXX"` | Arayan tarafın numarası (E.164 formatında). |
| `callee_number` | string | `"+90530XXXXXXX"` | Aranan hedef numara (E.164 formatında). |
| `call_duration` | integer | `14` | Toplam konuşma süresi (saniye cinsinden). |
| `missing_call` | boolean | `false` | Gelen yanıtsız çağrılarda `true`. |
| `hangup_by` | string | `"contact"` | Çağrıyı sonlandıran taraf (`"user"`, `"contact"`, `"system"`). |
| `record_url` | string/null | `"https://storage.hipcall.com.tr/..."` | AWS S3 ses indirme bağlantısı. |
| `started_at` | string | `"2026-09-21T10:37:07Z"` | Çağrının santralde başladığı an (UTC). |
| `answered_at` | string/null | `"2026-09-21T10:37:07Z"` | Çağrının yanıtlandığı an (UTC). |
| `ended_at` | string/null | `"2026-09-21T10:37:21Z"` | Çağrının kapandığı an (UTC). |

## Sonraki adımlar

- Ani çağrı yoğunluklarında HTTP alıcısı ile veritabanı arasına RabbitMQ veya Redis Streams gibi bir mesaj kuyruğu yerleştirin.
- Dosya tabanlı `calls.json` yapısından PostgreSQL veya SQL Server veritabanına geçin ve `uuid` kolonuna `UNIQUE` indeks ekleyin.
- Hipcall'ın `GET /api/v3/calls` REST API'sini kullanan gece mutabakat servisini Windows Görev Zamanlayıcısı'na (Task Scheduler) bağlayarak sıfır veri kaybını garanti altına alın.
- Entegrasyon sorularınızı veya webhook mimarisi deneyimlerinizi [Hipcall Topluluk](https://community.hipcall.com/) platformunda paylaşın.
