namespace Hipcall.PostCall.Rules;

using Hipcall.PostCall.Models;
using Hipcall.PostCall.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class ShortCallTagRule : IPostCallRule
{
    public string RuleName => "ShortCallTagRule";

    private readonly HipcallApiClient _api;
    private readonly HipcallSettings _settings;
    private readonly ILogger<ShortCallTagRule> _logger;

    public ShortCallTagRule(
        HipcallApiClient api,
        IOptions<HipcallSettings> settings,
        ILogger<ShortCallTagRule> logger)
    {
        _api = api;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool Matches(WebhookPayload payload)
    {
        if (payload.Data == null) return false;
        if (payload.Event != "call_hangup") return false;
        
        // Sadece cevaplanmış çağrılar için (MissedCall olmayan ve BridgedAt dolu olan)
        if (payload.Data.IsMissedCall) return false;
        if (string.IsNullOrEmpty(payload.Data.BridgedAt)) return false;

        return true;
    }

    public async Task ExecuteAsync(WebhookPayload payload, CancellationToken ct = default)
    {
        var call = payload.Data!;

        if (!DateTime.TryParse(call.BridgedAt, out var bridgedAt) || 
            !DateTime.TryParse(call.EndedAt, out var endedAt))
        {
            _logger.LogWarning("[ShortCallTagRule] Tarih alanları parse edilemedi — UUID: {Uuid}", MaskUuid(call.Uuid));
            return;
        }

        var talkDurationSeconds = (endedAt - bridgedAt).TotalSeconds;

        if (talkDurationSeconds >= _settings.ShortCallThresholdSeconds)
        {
            _logger.LogInformation("[ShortCallTagRule] Çağrı süresi eşiğin üzerinde ({Duration} sn) — UUID: {Uuid}", 
                talkDurationSeconds, MaskUuid(call.Uuid));
            return;
        }

        _logger.LogInformation(
            "[ShortCallTagRule] Kısa çağrı tespit edildi — UUID: {Uuid}, Süre: {Duration} sn",
            MaskUuid(call.Uuid), talkDurationSeconds);

        int tagId = call.HangupBy == "user" 
            ? _settings.ShortCallAgentTagId 
            : _settings.ShortCallCustomerTagId;

        if (tagId == 0)
        {
            _logger.LogWarning("[ShortCallTagRule] Etiket ID tanımlı değil, atlanıyor — UUID: {Uuid}", MaskUuid(call.Uuid));
            return;
        }

        try
        {
            await _api.AddTagToCallAsync(call.Uuid, tagId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ShortCallTagRule] Etiket ekleme hatası — UUID: {Uuid}", MaskUuid(call.Uuid));
        }
    }

    private static string MaskUuid(string uuid) =>
        uuid.Length > 8 ? uuid[..8] + "..." : uuid;
}
