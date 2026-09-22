using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5080);
});

var baseEndpoint = Environment.GetEnvironmentVariable("HIPCALL_API_ENDPOINT") ?? "https://use.hipcall.com.tr/api/v3";

builder.Services.AddHttpClient("HipcallClient", client =>
{
    client.BaseAddress = new Uri(baseEndpoint.TrimEnd('/') + "/");
});

var app = builder.Build();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

var expectedSecret = Environment.GetEnvironmentVariable("HIPCALL_WEBHOOK_SECRET") ?? "whsec_live_9a8f2e4c1b0d";
var baseDir = Directory.GetCurrentDirectory();
var customersFilePath = Path.Combine(baseDir, "customers.json");

HashSet<string> processedCalls = [];

var initialCustomers = LoadCustomers(customersFilePath, jsonOptions);
Console.WriteLine("==================================================================");
Console.WriteLine(" [BAŞLATILDI] Hipcall InsightCard Alıcısı Port 5080'de Dinliyor");
Console.WriteLine($" [CRM] {initialCustomers.Count} adet müşteri kaydı yüklendi.");
foreach (var c in initialCustomers)
{
    Console.WriteLine($"   * {c.Phone} -> {c.Name} ({c.Company})");
}
var configuredToken = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN");
if (string.IsNullOrWhiteSpace(configuredToken))
{
    Console.WriteLine(" [UYARI] HIPCALL_API_TOKEN ortam değişkeni tanımlı değil! Kart gönderimleri başarısız olabilir.");
}
else
{
    Console.WriteLine(" [API TOKEN] Ortam değişkeninden token başarıyla yüklendi.");
}
Console.WriteLine("==================================================================");

app.MapGet("/", () =>
{
    var current = LoadCustomers(customersFilePath, jsonOptions);
    return Results.Ok(new
    {
        status = "running",
        service = "Hipcall.InsightCard Webhook & Card Dispatcher",
        customer_count = current.Count,
        listening_port = 5080
    });
});

app.MapGet("/api/customers", () => Results.Ok(LoadCustomers(customersFilePath, jsonOptions)));

app.MapGet("/api/test/lookup", async (string phone, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(phone))
    {
        return Results.BadRequest(new { error = "phone parametresi zorunludur." });
    }

    var token = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN");
    if (string.IsNullOrWhiteSpace(token))
    {
        return Results.Problem("HIPCALL_API_TOKEN ortam değişkeni tanımlı değil.");
    }

    var client = httpClientFactory.CreateClient("HipcallClient");
    var cleanPhone = Uri.EscapeDataString(phone.Trim());
    var requestUrl = $"lookup/by_phone?phone={cleanPhone}";

    using var req = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var sw = Stopwatch.StartNew();
    var response = await client.SendAsync(req);
    sw.Stop();

    var content = await response.Content.ReadAsStringAsync();
    return Results.Text(content, contentType: "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/test/push-card", async (TestPushCardRequest req, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(req.CallId) || string.IsNullOrWhiteSpace(req.Phone))
    {
        return Results.BadRequest(new { error = "call_id ve phone zorunludur." });
    }

    var token = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN");
    if (string.IsNullOrWhiteSpace(token))
    {
        return Results.Problem("HIPCALL_API_TOKEN ortam değişkeni tanımlı değil.");
    }

    var customers = LoadCustomers(customersFilePath, jsonOptions);
    var customer = FindCustomerByPhone(customers, req.Phone);
    if (customer == null)
    {
        return Results.NotFound(new { message = "Müşteri CRM'de bulunamadı. Boş kart basılmadı." });
    }

    var cardPayload = BuildInsightCard(customer);
    var client = httpClientFactory.CreateClient("HipcallClient");

    var cardJson = JsonSerializer.Serialize(cardPayload, jsonOptions);
    using var httpContent = new StringContent(cardJson, Encoding.UTF8, "application/json");

    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"calls/{req.CallId}/cards")
    {
        Content = httpContent
    };
    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var sw = Stopwatch.StartNew();
    var response = await client.SendAsync(requestMessage);
    sw.Stop();

    var responseBody = await response.Content.ReadAsStringAsync();
    return Results.Json(new
    {
        status_code = (int)response.StatusCode,
        duration_ms = sw.ElapsedMilliseconds,
        api_response = responseBody
    });
});

app.MapPost("/hipcall/events/{secret?}", async (string? secret, HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var rawBody = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(rawBody))
    {
        Console.WriteLine("[BİLGİ] Boş gövde alındı.");
        return Results.Ok();
    }

    HipcallWebhookPayload? payload;
    try
    {
        payload = JsonSerializer.Deserialize<HipcallWebhookPayload>(rawBody, jsonOptions);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[JSON AYRIŞTIRMA HATASI] {ex.Message}");
        return Results.Ok();
    }

    if (payload == null || string.IsNullOrEmpty(payload.Event) || payload.Data == null)
    {
        Console.WriteLine("[BİLGİ] Geçersiz veya eksik payload yapısı.");
        return Results.Ok();
    }

    var eventName = payload.Event;
    var data = payload.Data;
    var callUuid = data.Uuid ?? "Bilinmiyor";

    Console.WriteLine($"[WEBHOOK ALINDI] Zaman: {DateTime.Now:HH:mm:ss} | Olay: {eventName} | Çağrı: {callUuid} | Yön: {data.Direction}");

    if (!string.IsNullOrEmpty(secret) && !string.Equals(secret, expectedSecret, StringComparison.Ordinal))
    {
        Console.WriteLine($"[GÜVENLİK UYARISI] Gelen secret ('{secret}') beklenen ('{expectedSecret}') ile uyuşmuyor!");
    }

    if (eventName != "call_init" && eventName != "call_bridged")
    {
        Console.WriteLine($"[ATLANDI] Olay: {eventName} (Insight Card yalnızca call_init/call_bridged anında basılır)");
        return Results.Ok();
    }

    if (string.IsNullOrEmpty(data.Uuid))
    {
        return Results.Ok();
    }

    lock (processedCalls)
    {
        if (processedCalls.Contains(data.Uuid))
        {
            Console.WriteLine($"[BİLGİ] Çağrı {data.Uuid} için daha önce kart basıldı. İkinci istek atlanıyor.");
            return Results.Ok();
        }
    }

    string? targetPhoneNumber;
    if (string.Equals(data.Direction, "inbound", StringComparison.OrdinalIgnoreCase))
    {
        targetPhoneNumber = data.CallerNumber;
    }
    else
    {
        targetPhoneNumber = data.CalleeNumber;
    }

    Console.WriteLine($"[NUMARA ANALİZİ] Yön: {data.Direction} | Arayan: {data.CallerNumber} | Aranan: {data.CalleeNumber} | Hedef Numara: {targetPhoneNumber}");

    if (string.IsNullOrWhiteSpace(targetPhoneNumber))
    {
        Console.WriteLine($"[UYARI] {callUuid} çağrısında hedef müşteri numarası boş!");
        return Results.Ok();
    }

    var currentCustomers = LoadCustomers(customersFilePath, jsonOptions);
    var matchedCustomer = FindCustomerByPhone(currentCustomers, targetPhoneNumber);
    if (matchedCustomer == null)
    {
        Console.WriteLine($"[CRM EŞLEŞMESİ YOK] Numara ({MaskNumber(targetPhoneNumber)}) CRM listesinde bulunamadı. Boş kart basılmadı (güvenli çıkış).");
        return Results.Ok();
    }

    lock (processedCalls)
    {
        processedCalls.Add(data.Uuid);
    }

    Console.WriteLine($"[MÜŞTERİ BULUNDU] {matchedCustomer.Name} ({matchedCustomer.Company}) -> Kart hazırlanıyor...");

    _ = Task.Run(async () =>
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var token = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("[HATA] HIPCALL_API_TOKEN ortam değişkeni tanımlı olmadığı için Hipcall API'sine kart gönderilemedi.");
                return;
            }

            var client = httpClientFactory.CreateClient("HipcallClient");
            var card = BuildInsightCard(matchedCustomer);
            var cardJson = JsonSerializer.Serialize(card, jsonOptions);
            using var cardContent = new StringContent(cardJson, Encoding.UTF8, "application/json");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"calls/{data.Uuid}/cards")
            {
                Content = cardContent
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(requestMessage);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[KART BAŞARIYLA BASILDI] Çağrı: {data.Uuid} | Müşteri: {matchedCustomer.Name} | Süre: {sw.ElapsedMilliseconds} ms | HTTP {(int)response.StatusCode}");
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[KART GÖNDERİM HATASI] Çağrı: {data.Uuid} | HTTP {(int)response.StatusCode} | Cevap: {err}");
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"[İSTİSNA] Kart gönderilirken hata oluştu ({data.Uuid}): {ex.Message}");
        }
    });

    return Results.Ok();
});

app.Run();

static List<CrmCustomer> LoadCustomers(string path, JsonSerializerOptions options)
{
    if (!File.Exists(path)) return [];
    try
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<CrmCustomer>>(json, options) ?? [];
    }
    catch
    {
        return [];
    }
}

static CrmCustomer? FindCustomerByPhone(List<CrmCustomer> customers, string phone)
{
    var normalizedTarget = NormalizePhone(phone);
    return customers.FirstOrDefault(c => NormalizePhone(c.Phone) == normalizedTarget);
}

static string NormalizePhone(string? phone)
{
    if (string.IsNullOrWhiteSpace(phone))
        return string.Empty;

    var sb = new StringBuilder();
    foreach (var ch in phone)
    {
        if (char.IsDigit(ch))
            sb.Append(ch);
    }

    var digits = sb.ToString();
    if (digits.StartsWith("90") && digits.Length == 12)
        return digits[2..];
    if (digits.StartsWith("0") && digits.Length == 11)
        return digits[1..];

    return digits;
}

static string? MaskNumber(string? number)
{
    if (string.IsNullOrEmpty(number) || number.Length <= 6)
        return number;
    return number[..6] + new string('X', number.Length - 6);
}

static InsightCardRoot BuildInsightCard(CrmCustomer customer)
{
    List<InsightCardItem> items =
    [
        new InsightCardItem
        {
            Type = "title",
            Text = customer.Name,
            Link = customer.CrmUrl
        },
        new InsightCardItem
        {
            Type = "shortText",
            Label = "Şirket",
            Text = customer.Company,
            Link = customer.CrmUrl
        },
        new InsightCardItem
        {
            Type = "shortText",
            Label = "Segment",
            Text = customer.Segment
        },
        new InsightCardItem
        {
            Type = "shortText",
            Label = "Bakiye",
            Text = customer.Balance
        }
    ];

    if (customer.AccountOwnerId.HasValue)
    {
        items.Add(new InsightCardItem
        {
            Type = "user",
            Label = "Müşteri Yöneticisi",
            UserId = customer.AccountOwnerId.Value
        });
    }

    return new InsightCardRoot
    {
        Card = items
    };
}

public class CrmCustomer
{
    public string? Phone { get; set; }
    public string? Name { get; set; }
    public string? Company { get; set; }
    public string? Segment { get; set; }
    public string? Balance { get; set; }
    public string? CrmUrl { get; set; }
    public int? AccountOwnerId { get; set; }
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
    public string? Android { get; set; }
    public string? Ios { get; set; }
    public int? UserId { get; set; }
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
    public string? StartedAt { get; set; }
}

public class TestPushCardRequest
{
    public string? CallId { get; set; }
    public string? Phone { get; set; }
}
