namespace Hipcall.PostCall.Models;

using System.Text.Json.Serialization;

public sealed class WebhookPayload
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public CallData? Data { get; set; }
}

public sealed class CallData
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    [JsonPropertyName("caller_number")]
    public string? CallerNumber { get; set; }

    [JsonPropertyName("callee_number")]
    public string? CalleeNumber { get; set; }

    [JsonPropertyName("call_duration")]
    public int? CallDuration { get; set; }

    [JsonPropertyName("missing_call")]
    public bool? MissingCall { get; set; }

    [JsonPropertyName("missing_call_reason")]
    public string? MissingCallReason { get; set; }

    [JsonPropertyName("hangup_by")]
    public string? HangupBy { get; set; }

    [JsonPropertyName("record_url")]
    public string? RecordUrl { get; set; }

    [JsonPropertyName("voicemail_id")]
    public int? VoicemailId { get; set; }

    [JsonPropertyName("voicemail_url")]
    public string? VoicemailUrl { get; set; }

    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("answered_at")]
    public string? AnsweredAt { get; set; }

    [JsonPropertyName("bridged_at")]
    public string? BridgedAt { get; set; }

    [JsonPropertyName("ended_at")]
    public string? EndedAt { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("contact_id")]
    public int? ContactId { get; set; }

    [JsonPropertyName("company_id")]
    public int? CompanyId { get; set; }

    [JsonPropertyName("caller_id")]
    public int? CallerId { get; set; }

    [JsonPropertyName("callee_id")]
    public int? CalleeId { get; set; }

    [JsonPropertyName("channel_id")]
    public int? ChannelId { get; set; }

    public bool IsInbound => string.Equals(Direction, "inbound", StringComparison.OrdinalIgnoreCase);

    public bool IsMissedCall => MissingCall == true && VoicemailId == null;
}
