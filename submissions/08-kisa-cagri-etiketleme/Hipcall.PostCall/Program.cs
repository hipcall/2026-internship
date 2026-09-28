using System.Text;
using System.Text.Json;
using Hipcall.PostCall.Models;
using Hipcall.PostCall.Rules;
using Hipcall.PostCall.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(opts => opts.ListenAnyIP(5080));

builder.Services.Configure<HipcallSettings>(
    builder.Configuration.GetSection(HipcallSettings.SectionName));

builder.Services.AddHttpClient<HipcallApiClient>();

builder.Services.AddSingleton<IdempotencyStore>();

builder.Services.AddSingleton<IPostCallRule, MissedCallRule>();
builder.Services.AddSingleton<IPostCallRule, ShortCallTagRule>();

builder.Services.AddSingleton<RuleEngine>();

var app = builder.Build();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

var settings = builder.Configuration
    .GetSection(HipcallSettings.SectionName)
    .Get<HipcallSettings>() ?? new HipcallSettings();

app.MapGet("/", () => Results.Ok("Hipcall PostCall Webhook Alıcısı aktif."));

app.MapPost("/hipcall/events/{secret?}", async (
    string? secret,
    HttpRequest request,
    RuleEngine ruleEngine,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(secret) || !string.Equals(secret, settings.WebhookSecret, StringComparison.Ordinal))
    {
        logger.LogWarning("[GÜVENLİK] Geçersiz veya eksik gizli rota anahtarı.");
        return Results.Unauthorized();
    }

    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var rawBody = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(rawBody))
        return Results.Ok();

    WebhookPayload? payload;
    try
    {
        payload = JsonSerializer.Deserialize<WebhookPayload>(rawBody, jsonOptions);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[JSON] Ayrıştırma hatası");
        return Results.Ok();
    }

    if (payload?.Data == null || string.IsNullOrEmpty(payload.Data.Uuid))
        return Results.Ok();

    if (payload.Event != "call_hangup")
    {
        logger.LogDebug("[Olay] {Event} atlandı (sadece call_hangup işlenir).", payload.Event);
        return Results.Ok();
    }

    logger.LogInformation("[Webhook] call_hangup alındı — UUID: {Uuid}, Yön: {Dir}",
        payload.Data.Uuid.Length > 8 ? payload.Data.Uuid[..8] + "..." : payload.Data.Uuid,
        payload.Data.Direction);

    _ = Task.Run(async () =>
    {
        try
        {
            await ruleEngine.EvaluateAsync(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[RuleEngine] Beklenmeyen hata");
        }
    });

    return Results.Ok();
});

app.Run();

public partial class Program { }
