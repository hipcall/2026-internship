using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var crmFilePath = Path.Combine(builder.Environment.ContentRootPath, "crm.json");
List<CrmRecord> crmData = new();

try
{
    if (File.Exists(crmFilePath))
    {
        var json = File.ReadAllText(crmFilePath);
        crmData = JsonSerializer.Deserialize<List<CrmRecord>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CrmRecord>();
        Console.WriteLine($"{crmData.Count} adet müşteri kaydı yüklendi.");
    }
    else
    {
        Console.WriteLine("Uyarı: crm.json dosyası bulunamadı!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Hata: crm.json yüklenirken bir sorun oluştu: {ex.Message}");
}

app.MapPost("/hipcall/external-management", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        
        Console.WriteLine($"\n--- GELEN ISTEK ---");
        Console.WriteLine(body);
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var request = JsonSerializer.Deserialize<HipcallRequest>(body, options);
        
        if (request == null || string.IsNullOrEmpty(request.Caller))
        {
            Console.WriteLine("Geçersiz istek veya arayan numarası eksik.");
            return GetConnectResponse("800");
        }

        // 09. Ödevdeki gibi önce external_id ile bulmayı deneriz (eğer payload'da varsa), 
        // yoksa telefon numarası üzerinden (fallback) eşleştirme yaparız.
        CrmRecord? customer = null;
        
        if (!string.IsNullOrEmpty(request.ContactExternalId))
        {
            customer = crmData.FirstOrDefault(c => c.ExternalId == request.ContactExternalId);
        }

        if (customer == null)
        {
            customer = crmData.FirstOrDefault(c => c.Phone == request.Caller);
        }
        
        if (customer == null)
        {
            Console.WriteLine("Müşteri bulunamadı. Genel kuyruğa (800) yönlendiriliyor.");
            return GetConnectResponse("800");
        }

        string? pinCode = null;
        if (request.Data.ValueKind == JsonValueKind.Object)
        {
            if (request.Data.TryGetProperty("pin_code", out var pinElement))
            {
                pinCode = pinElement.GetString();
            }
        }

        if (string.IsNullOrEmpty(pinCode))
        {
            Console.WriteLine($"Müşteri bulundu ({customer.FirstName} {customer.LastName}). PIN girmesi isteniyor.");
            return GetGatherResponse("https://s3.amazonaws.com/freecodecamp/simonSound1.mp3", "pin_code");
        }
        else
        {
            if (pinCode == customer.Pin)
            {
                Console.WriteLine($"PIN doğru girildi. VIP kuyruğa (801) yönlendiriliyor.");
                return GetConnectResponse("801");
            }
            else
            {
                Console.WriteLine($"PIN yanlış girildi. 10 koduna (Test Kullanıcısı) yönlendiriliyor.");
                return GetConnectResponse("10");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"İstek işlenirken hata oluştu: {ex.Message}");
        return GetConnectResponse("800");
    }
});

IResult GetConnectResponse(string destination)
{
    var response = new HipcallResponse
    {
        Seq = new List<object>
        {
            new
            {
                action = "connect",
                args = new { destination = destination }
            }
        },
        Version = "1"
    };
    
    Console.WriteLine("--- GIDEN CEVAP (Connect) ---");
    Console.WriteLine(JsonSerializer.Serialize(response));
    return Results.Json(response);
}

IResult GetGatherResponse(string askUrl, string variableName)
{
    var response = new HipcallResponse
    {
        Seq = new List<object>
        {
            new
            {
                action = "gather",
                args = new
                {
                    ask = askUrl,
                    max_digits = 4,
                    min_digits = 1,
                    variable_name = variableName
                }
            }
        },
        Version = "1"
    };

    Console.WriteLine("--- GIDEN CEVAP (Gather) ---");
    Console.WriteLine(JsonSerializer.Serialize(response));
    return Results.Json(response);
}

app.Run();

public record CrmRecord(
    [property: JsonPropertyName("external_id")] string ExternalId,
    [property: JsonPropertyName("phone")] string Phone, 
    [property: JsonPropertyName("first_name")] string FirstName, 
    [property: JsonPropertyName("last_name")] string LastName, 
    [property: JsonPropertyName("company")] string Company, 
    [property: JsonPropertyName("balance")] decimal Balance, 
    [property: JsonPropertyName("open_orders")] int OpenOrders, 
    [property: JsonPropertyName("open_tickets")] int OpenTickets,
    [property: JsonPropertyName("pin")] string Pin
);

public record HipcallRequest(
    [property: JsonPropertyName("caller")] string Caller,
    [property: JsonPropertyName("callee")] string Callee,
    [property: JsonPropertyName("uuid")] string Uuid,
    [property: JsonPropertyName("direction")] string Direction,
    [property: JsonPropertyName("external_manager_id")] int ExternalManagerId,
    [property: JsonPropertyName("contact_external_id")] string? ContactExternalId,
    [property: JsonPropertyName("data")] JsonElement Data
);

public record HipcallResponse
{
    [JsonPropertyName("seq")]
    public List<object> Seq { get; init; } = new();

    [JsonPropertyName("version")]
    public string Version { get; init; } = "1";
}
