using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

string? token = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Error: HIPCALL_API_TOKEN environment variable is not set.");
    Console.Error.WriteLine("Set it with: export HIPCALL_API_TOKEN=\"your-key\"");
    return 1;
}

const string baseUrl = "https://use.hipcall.com.tr/api/v3/calls";
const int pageSize = 100;
string csvFile = $"missed-calls-{DateTime.UtcNow:yyyy-MM-dd}.csv";

string from = DateTime.UtcNow.AddDays(-7).Date.ToString("yyyy-MM-ddT00:00:00Z");
string to = DateTime.UtcNow.Date.ToString("yyyy-MM-ddT23:59:00Z");

using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

var allCalls = new List<JsonElement>();
int offset = 0;
int totalCount;

Console.WriteLine($"Fetching missed calls from {from} to {to} ...");

do
{
    string url = $"{baseUrl}?missing_call%5Beq%5D=true"
               + $"&started_at%5Bgte%5D={Uri.EscapeDataString(from)}"
               + $"&started_at%5Blte%5D={Uri.EscapeDataString(to)}"
               + $"&limit={pageSize}&offset={offset}";

    HttpResponseMessage response = await client.GetAsync(url);
    string body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"Error: API returned {(int)response.StatusCode} {response.StatusCode}");
        Console.Error.WriteLine(body);
        return 1;
    }

    using JsonDocument doc = JsonDocument.Parse(body);
    JsonElement root = doc.RootElement;

    JsonElement meta = root.GetProperty("meta");
    totalCount = meta.GetProperty("count").GetInt32();
    int currentLimit = meta.GetProperty("limit").GetInt32();

    JsonElement data = root.GetProperty("data");
    foreach (JsonElement call in data.EnumerateArray())
    {
        allCalls.Add(call.Clone());
    }

    int fetched = allCalls.Count;
    Console.WriteLine($"  Page {(offset / pageSize) + 1}: got {data.GetArrayLength()} records ({fetched}/{totalCount})");

    offset += currentLimit;

} while (offset < totalCount);

using (var writer = new StreamWriter(csvFile, false, System.Text.Encoding.UTF8))
{
    writer.WriteLine("date;caller_number;callee_number;duration_seconds");

    foreach (JsonElement call in allCalls)
    {
        string date = call.TryGetProperty("started_at", out JsonElement startedAt)
            ? startedAt.GetString() ?? ""
            : "";

        string callerNumber = call.TryGetProperty("caller_number", out JsonElement caller)
            ? caller.GetString() ?? ""
            : "";

        string calleeNumber = call.TryGetProperty("callee_number", out JsonElement callee)
            ? callee.GetString() ?? ""
            : "";

        string duration = call.TryGetProperty("call_duration", out JsonElement dur)
            ? dur.ToString()
            : "0";

        writer.WriteLine($"\"{date}\";\"{callerNumber}\";\"{calleeNumber}\";{duration}");
    }
}

Console.WriteLine();
Console.WriteLine($"Done. {allCalls.Count} missed call(s) written to {csvFile}");
return 0;
