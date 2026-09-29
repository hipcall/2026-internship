namespace Hipcall.PostCall.Rules;

using Hipcall.PostCall.Models;

public interface IPostCallRule
{
    string RuleName { get; }

    bool Matches(WebhookPayload payload);

    Task ExecuteAsync(WebhookPayload payload, CancellationToken ct = default);
}
