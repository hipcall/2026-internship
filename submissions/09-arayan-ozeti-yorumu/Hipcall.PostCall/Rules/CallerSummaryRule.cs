namespace Hipcall.PostCall.Rules;

using Hipcall.PostCall.Models;
using Hipcall.PostCall.Services;
using Microsoft.Extensions.Logging;

public sealed class CallerSummaryRule : IPostCallRule
{
    public string RuleName => "CallerSummaryRule";

    private readonly HipcallApiClient _api;
    private readonly LocalCrmService _crm;
    private readonly IdempotencyStore _idempotency;
    private readonly ILogger<CallerSummaryRule> _logger;

    public CallerSummaryRule(
        HipcallApiClient api,
        LocalCrmService crm,
        IdempotencyStore idempotency,
        ILogger<CallerSummaryRule> logger)
    {
        _api = api;
        _crm = crm;
        _idempotency = idempotency;
        _logger = logger;
    }

    public bool Matches(WebhookPayload payload)
    {
        if (payload.Data == null) return false;
        if (payload.Event != "call_hangup") return false;
        if (!payload.Data.IsInbound) return false;

        return true;
    }

    public async Task ExecuteAsync(WebhookPayload payload, CancellationToken ct = default)
    {
        var call = payload.Data!;
        var idempotencyKey = $"summary:{call.Uuid}";
        if (_idempotency.IsAlreadyProcessed(idempotencyKey))
            return;

        CrmCustomer? customer = null;

        // Adım 1: Hipcall'da kayıtlıysa external_id ile bul
        if (call.ContactId.HasValue)
        {
            var externalId = await _api.GetContactExternalIdAsync(call.ContactId.Value, ct);
            if (!string.IsNullOrEmpty(externalId))
            {
                customer = _crm.FindByExternalId(externalId);
            }
        }

        // Adım 2: Bulunamadıysa numaradan bul (Normalizasyon - fallback)
        if (customer == null && !string.IsNullOrEmpty(call.CallerNumber))
        {
            // Webhook'tan callerNumber her zaman E.164 (+90555...) gelir.
            // crm.json içindeki veriler de bu formattaysa eşleşir,
            // değilse burada +90 atılarak eşleştirme yapılabilir.
            customer = _crm.FindByPhone(call.CallerNumber);
        }

        // Adım 3: Müşteri bulunamadıysa hiçbir şey yapma (C4 Kararı)
        if (customer == null)
        {
            _logger.LogInformation("[CallerSummaryRule] Müşteri CRM'de bulunamadı, yorum atlanıyor.");
            return;
        }

        // Adım 4: Yorumu oluştur (C1 & C5 Kararı)
        var summary = BuildSummary(customer);

        // Adım 5: Yorumu yaz
        try
        {
            await _api.WriteCommentAsync(call.Uuid, summary, ct);
            _logger.LogInformation("[CallerSummaryRule] Yorum yazıldı — UUID: {Uuid}", MaskUuid(call.Uuid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CallerSummaryRule] Yorum yazılamadı — UUID: {Uuid}", MaskUuid(call.Uuid));
        }
    }

    private static string BuildSummary(CrmCustomer customer)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("**CRM Özeti:**");
        sb.AppendLine();
        sb.AppendLine($"- **Müşteri:** {customer.FirstName} {customer.LastName}");
        if (!string.IsNullOrEmpty(customer.Company))
            sb.AppendLine($"- **Firma:** {customer.Company}");
        sb.AppendLine($"- **Açık Sipariş:** {customer.OpenOrders}");
        sb.AppendLine($"- **Açık Destek Kaydı:** {customer.OpenTickets}");
        
        var dateStr = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        sb.AppendLine($"- **Bakiye:** {customer.Balance} TL (📅 {dateStr} itibarıyla)");

        return sb.ToString();
    }

    private static string MaskUuid(string uuid) =>
        uuid.Length > 8 ? uuid[..8] + "..." : uuid;
}
