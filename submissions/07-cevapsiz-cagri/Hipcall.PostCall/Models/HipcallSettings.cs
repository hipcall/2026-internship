namespace Hipcall.PostCall.Models;

public sealed class HipcallSettings
{
    public const string SectionName = "Hipcall";

    public string ApiBaseUrl { get; set; } = "https://use.hipcall.com.tr/api/v3";

    public string ApiToken { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public string MissedCallDispositionCode { get; set; } = "geri_arama_istendi";

    public int FallbackUserId { get; set; }

    public int TaskDueMinutes { get; set; } = 60;
}
