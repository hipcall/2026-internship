namespace Hipcall.PostCall.Services;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Hipcall.PostCall.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class HipcallApiClient
{
    private readonly HttpClient _http;
    private readonly HipcallSettings _settings;
    private readonly ILogger<HipcallApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public HipcallApiClient(HttpClient http, IOptions<HipcallSettings> settings, ILogger<HipcallApiClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        _http.BaseAddress = new Uri(_settings.ApiBaseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<bool> WriteDispositionAsync(string callUuid, string dispositionCode, CancellationToken ct = default)
    {
        var body = new { disposition_code = dispositionCode };
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");

        var response = await _http.PutAsync($"calls/{callUuid}/disposition", content, ct);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("[Disposition] Sonuç kodu yazıldı: {Code} → Çağrı {Uuid}", dispositionCode, MaskUuid(callUuid));
            return true;
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("[Disposition] Hata {Status}: {Body} — Çağrı {Uuid}, Kod: {Code}",
            (int)response.StatusCode, errorBody, MaskUuid(callUuid), dispositionCode);
        return false;
    }

    public async Task<bool> CreateTaskAsync(string name, string description, int assignToUserId,
        DateTime dueDate, int[]? contactIds = null, int[]? companyIds = null, CancellationToken ct = default)
    {
        var body = new Dictionary<string, object>
        {
            ["name"] = name,
            ["description"] = description,
            ["assign_to_user_id"] = assignToUserId,
            ["due_date"] = dueDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        if (contactIds is { Length: > 0 })
            body["contact_ids"] = contactIds;
        if (companyIds is { Length: > 0 })
            body["company_ids"] = companyIds;

        var content = new StringContent(
            JsonSerializer.Serialize(new { data = body }, JsonOpts),
            Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("tasks", content, ct);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("[Task] Görev açıldı: {Name} → Kullanıcı {UserId}", name, assignToUserId);
            return true;
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("[Task] Hata {Status}: {Body}", (int)response.StatusCode, errorBody);
        return false;
    }
    public async Task<bool> AddTagToCallAsync(string callUuid, int tagId, CancellationToken ct = default)
    {
        var body = new { tag_id = tagId };
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"calls/{callUuid}/tags", content, ct);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("[Tag] Etiket eklendi: {TagId} → Çağrı {Uuid}", tagId, MaskUuid(callUuid));
            return true;
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("[Tag] Hata {Status}: {Body} — Çağrı {Uuid}, Tag: {TagId}",
            (int)response.StatusCode, errorBody, MaskUuid(callUuid), tagId);
        return false;
    }

    public async Task<int?> GetContactOwnerAsync(int contactId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"contacts/{contactId}", ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            if (data.TryGetProperty("user_id", out var userIdProp) && userIdProp.ValueKind == JsonValueKind.Number)
                return userIdProp.GetInt32();

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API] Kişi sahibi sorgulanamadı (ContactId: {Id})", contactId);
            return null;
        }
    }

    private static string MaskUuid(string uuid) =>
        uuid.Length > 8 ? uuid[..8] + "..." : uuid;
}
