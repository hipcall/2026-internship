namespace Hipcall.PostCall.Services;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

public sealed class IdempotencyStore
{
    private readonly ConcurrentDictionary<string, DateTime> _processed = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<IdempotencyStore> _logger;

    public IdempotencyStore(ILogger<IdempotencyStore> logger)
    {
        _logger = logger;
    }

    public bool IsAlreadyProcessed(string key)
    {
        if (_processed.TryAdd(key, DateTime.UtcNow))
        {
            return false;
        }

        _logger.LogInformation("[Idempotency] Tekrar tespit edildi, atlanıyor: {Key}", key);
        return true;
    }

    public int Count => _processed.Count;
}
