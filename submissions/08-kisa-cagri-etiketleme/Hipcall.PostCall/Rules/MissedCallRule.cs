namespace Hipcall.PostCall.Rules;

using Hipcall.PostCall.Models;
using Hipcall.PostCall.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class MissedCallRule : IPostCallRule
{
    public string RuleName => "MissedCallRule";

    private readonly HipcallApiClient _api;
    private readonly IdempotencyStore _idempotency;
    private readonly HipcallSettings _settings;
    private readonly ILogger<MissedCallRule> _logger;

    public MissedCallRule(
        HipcallApiClient api,
        IdempotencyStore idempotency,
        IOptions<HipcallSettings> settings,
        ILogger<MissedCallRule> logger)
    {
        _api = api;
        _idempotency = idempotency;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool Matches(WebhookPayload payload)
    {
        if (payload.Data == null) return false;
        if (payload.Event != "call_hangup") return false;
        if (!payload.Data.IsInbound) return false;
        if (!payload.Data.IsMissedCall) return false;
        return true;
    }

    public async Task ExecuteAsync(WebhookPayload payload, CancellationToken ct = default)
    {
        var call = payload.Data!;
        var idempotencyKey = $"missed:{call.Uuid}";

        if (_idempotency.IsAlreadyProcessed(idempotencyKey))
            return;

        _logger.LogInformation(
            "[MissedCallRule] Cevapsız çağrı tespit edildi — UUID: {Uuid}, Arayan: {Caller}",
            MaskUuid(call.Uuid), MaskPhone(call.CallerNumber));

        bool dispositionOk = false;
        try
        {
            dispositionOk = await _api.WriteDispositionAsync(call.Uuid, _settings.MissedCallDispositionCode, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MissedCallRule] Disposition yazma hatası — UUID: {Uuid}", MaskUuid(call.Uuid));
        }

        try
        {
            int assigneeId = await DetermineAssigneeAsync(call, ct);
            string taskName = $"Geri Arama: {call.CallerNumber} — Cevapsız Çağrı";
            string taskDescription = BuildTaskDescription(call, dispositionOk);
            var dueDate = DateTime.UtcNow.AddMinutes(_settings.TaskDueMinutes);

            int[]? contactIds = call.ContactId.HasValue ? new[] { call.ContactId.Value } : null;
            int[]? companyIds = call.CompanyId.HasValue ? new[] { call.CompanyId.Value } : null;

            await _api.CreateTaskAsync(taskName, taskDescription, assigneeId, dueDate, contactIds, companyIds, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MissedCallRule] Görev açma hatası — UUID: {Uuid}", MaskUuid(call.Uuid));
        }
    }

    private async Task<int> DetermineAssigneeAsync(CallData call, CancellationToken ct)
    {
        if (call.ContactId.HasValue)
        {
            var ownerUserId = await _api.GetContactOwnerAsync(call.ContactId.Value, ct);
            if (ownerUserId.HasValue)
            {
                _logger.LogInformation("[C7] Sorumlu → Kişi Sahibi (UserId: {Id})", ownerUserId.Value);
                return ownerUserId.Value;
            }
        }

        if (call.UserId.HasValue)
        {
            _logger.LogInformation("[C7] Sorumlu → Çağrı Kullanıcısı (UserId: {Id})", call.UserId.Value);
            return call.UserId.Value;
        }

        _logger.LogWarning("[C7] Sorumlu bulunamadı, Yedek Yönetici kullanılıyor (UserId: {Id})", _settings.FallbackUserId);
        return _settings.FallbackUserId;
    }

    private static string BuildTaskDescription(CallData call, bool dispositionWritten)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Cevapsız çağrı tespit edildi.");
        sb.AppendLine($"Arayan: {call.CallerNumber}");
        sb.AppendLine($"Tarih : {FormatDate(call.StartedAt)}");
        sb.AppendLine($"Süre  : {call.CallDuration ?? 0} sn (çalma)");
        sb.AppendLine($"Sebep : {call.MissingCallReason ?? "bilinmiyor"}");
        sb.AppendLine();
        sb.AppendLine(dispositionWritten
            ? "✅ Sonuç kodu otomatik olarak atandı."
            : "⚠️ Sonuç kodu atanamadı (hata loglarını incele).");
        sb.AppendLine();
        sb.AppendLine("Lütfen müşteriye en kısa sürede geri dönüş yapınız.");
        return sb.ToString();
    }

    private static string FormatDate(string? isoDate)
    {
        if (string.IsNullOrEmpty(isoDate)) return "?";
        if (DateTime.TryParse(isoDate, out var dt))
        {
            return dt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        }
        return isoDate;
    }

    private static string MaskUuid(string uuid) =>
        uuid.Length > 8 ? uuid[..8] + "..." : uuid;

    private static string MaskPhone(string? phone) =>
        string.IsNullOrEmpty(phone) || phone.Length < 7 ? (phone ?? "") : phone[..4] + "XXXX" + phone[^2..];
}
