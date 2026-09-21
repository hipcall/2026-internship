using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class CallResult
{
    public required string Id { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public bool IsSuccess { get; init; }
}

public sealed class HipcallApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public HipcallApiException(HttpStatusCode statusCode, string responseBody)
        : base($"Hipcall API error {(int)statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

public sealed class HipcallClient
{
    private static readonly HttpClient s_httpClient = new();
    private const string BaseUrl = "https://use.hipcall.com.tr/api/v3";
    private readonly string _apiToken;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public HipcallClient(string? apiToken = null)
    {
        _apiToken = apiToken
            ?? Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN")
            ?? throw new InvalidOperationException(
                "HIPCALL_API_TOKEN environment variable is not set. " +
                "Set it with: set HIPCALL_API_TOKEN=your-key");
    }

    public async Task<CallResult> StartCallAsync(
        int userId,
        string calleeNumber,
        bool? ringUserFirst = null,
        int? numberId = null,
        bool? callMasking = null,
        string? callMaskingName = null,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new Dictionary<string, object>();
        requestBody["callee_number"] = calleeNumber;

        if (ringUserFirst.HasValue)
            requestBody["ring_user_first"] = ringUserFirst.Value;

        if (numberId.HasValue)
            requestBody["number_id"] = numberId.Value;

        if (callMasking.HasValue)
            requestBody["call_masking"] = callMasking.Value;

        if (callMaskingName is not null)
            requestBody["call_masking_name"] = callMaskingName;

        string json = JsonSerializer.Serialize(requestBody, s_jsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{BaseUrl}/users/{userId}/call");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await s_httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        string body = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HipcallApiException(response.StatusCode, body);
        }

        using JsonDocument doc = JsonDocument.Parse(body);
        string callId = doc.RootElement
            .GetProperty("data")
            .GetProperty("id")
            .GetString()
            ?? throw new InvalidOperationException("API returned null call id.");

        return new CallResult
        {
            Id = callId,
            StatusCode = response.StatusCode,
            IsSuccess = true
        };
    }

    public async Task<CallResult> StartCallFromExtensionAsync(
        int extensionId,
        string calleeNumber,
        int? numberId = null,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new Dictionary<string, object>();
        requestBody["callee_number"] = calleeNumber;

        if (numberId.HasValue)
            requestBody["number_id"] = numberId.Value;

        string json = JsonSerializer.Serialize(requestBody, s_jsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{BaseUrl}/extensions/{extensionId}/call");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await s_httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        string body = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HipcallApiException(response.StatusCode, body);
        }

        using JsonDocument doc = JsonDocument.Parse(body);
        string callId = doc.RootElement
            .GetProperty("data")
            .GetProperty("id")
            .GetString()
            ?? throw new InvalidOperationException("API returned null call id.");

        return new CallResult
        {
            Id = callId,
            StatusCode = response.StatusCode,
            IsSuccess = true
        };
    }
}
