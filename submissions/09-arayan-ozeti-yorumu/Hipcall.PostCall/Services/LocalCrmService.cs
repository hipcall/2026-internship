namespace Hipcall.PostCall.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

public class CrmCustomer
{
    [JsonPropertyName("external_id")] public string? ExternalId { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("last_name")] public string? LastName { get; set; }
    [JsonPropertyName("company")] public string? Company { get; set; }
    [JsonPropertyName("balance")] public decimal Balance { get; set; }
    [JsonPropertyName("open_orders")] public int OpenOrders { get; set; }
    [JsonPropertyName("open_tickets")] public int OpenTickets { get; set; }
}

public class LocalCrmService
{
    private readonly List<CrmCustomer> _customers = new();

    public LocalCrmService()
    {
        var filePath = "crm.json";
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                _customers = JsonSerializer.Deserialize<List<CrmCustomer>>(json) ?? new();
            }
            catch
            {
                // json boş veya hatalıysa boş liste kalsın
            }
        }
    }

    public CrmCustomer? FindByExternalId(string externalId)
    {
        return _customers.FirstOrDefault(c => c.ExternalId == externalId);
    }

    public CrmCustomer? FindByPhone(string phone)
    {
        return _customers.FirstOrDefault(c => c.Phone == phone);
    }
}
