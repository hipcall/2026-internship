namespace Hipcall.PostCall.Services;

using Hipcall.PostCall.Models;
using Hipcall.PostCall.Rules;
using Microsoft.Extensions.Logging;

public sealed class RuleEngine
{
    private readonly IEnumerable<IPostCallRule> _rules;
    private readonly ILogger<RuleEngine> _logger;

    public RuleEngine(IEnumerable<IPostCallRule> rules, ILogger<RuleEngine> logger)
    {
        _rules = rules;
        _logger = logger;
    }

    public async Task EvaluateAsync(WebhookPayload payload, CancellationToken ct = default)
    {
        foreach (var rule in _rules)
        {
            try
            {
                if (!rule.Matches(payload))
                {
                    _logger.LogDebug("[RuleEngine] {Rule} eşleşmedi, atlanıyor.", rule.RuleName);
                    continue;
                }

                _logger.LogInformation("[RuleEngine] {Rule} eşleşti, çalıştırılıyor...", rule.RuleName);
                await rule.ExecuteAsync(payload, ct);
                _logger.LogInformation("[RuleEngine] {Rule} tamamlandı.", rule.RuleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RuleEngine] {Rule} başarısız oldu! Hata: {Message}", rule.RuleName, ex.Message);
            }
        }
    }
}
