if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run -- <user-id> <callee-number> [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  --ring-first          Ring user's device first, then dial callee");
    Console.Error.WriteLine("  --mask                Enable call masking (hide caller number)");
    Console.Error.WriteLine("  --mask-name <name>    Display name for masked calls (max 30 chars)");
    Console.Error.WriteLine("  --number-id <id>      Outbound number ID to use");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Environment:");
    Console.Error.WriteLine("  HIPCALL_API_TOKEN     Required. Your Hipcall API bearer token.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Example:");
    Console.Error.WriteLine("  dotnet run -- 4200 +905551112233 --mask --ring-first");
    return 1;
}

if (!int.TryParse(args[0], out int userId))
{
    Console.Error.WriteLine($"Error: '{args[0]}' is not a valid user ID (expected integer).");
    return 1;
}

string calleeNumber = args[1];

bool ringFirst = false;
bool mask = false;
string? maskName = null;
int? numberId = null;

for (int i = 2; i < args.Length; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--ring-first":
            ringFirst = true;
            break;
        case "--mask":
            mask = true;
            break;
        case "--mask-name" when i + 1 < args.Length:
            maskName = args[++i];
            break;
        case "--number-id" when i + 1 < args.Length:
            if (int.TryParse(args[++i], out int nid))
                numberId = nid;
            break;
        default:
            Console.Error.WriteLine($"Warning: unknown argument '{args[i]}', ignoring.");
            break;
    }
}

HipcallClient client;
try
{
    client = new HipcallClient();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║         Hipcall Click-to-Call Demo              ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine($"  User ID        : {userId}");
Console.WriteLine($"  Callee         : {calleeNumber}");
Console.WriteLine($"  Ring user first: {ringFirst}");
Console.WriteLine($"  Call masking   : {mask}");
if (maskName is not null)
    Console.WriteLine($"  Mask name      : {maskName}");
if (numberId.HasValue)
    Console.WriteLine($"  Number ID      : {numberId.Value}");
Console.WriteLine();
Console.WriteLine("Starting call...");

try
{
    CallResult result = await client.StartCallAsync(
        userId: userId,
        calleeNumber: calleeNumber,
        ringUserFirst: ringFirst,
        numberId: numberId,
        callMasking: mask ? true : null,
        callMaskingName: maskName);

    Console.WriteLine();
    Console.WriteLine($"  ✓ Call queued successfully!");
    Console.WriteLine($"  Status : {(int)result.StatusCode} {result.StatusCode}");
    Console.WriteLine($"  Call ID: {result.Id}");
    Console.WriteLine();
    Console.WriteLine("  ⚠ Note: 201 means the call was QUEUED, not that the");
    Console.WriteLine("    destination answered. Use webhooks or GET /api/v3/calls");
    Console.WriteLine("    to track the actual call outcome.");
}
catch (HipcallApiException ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"  ✗ API Error: {(int)ex.StatusCode} {ex.StatusCode}");
    Console.Error.WriteLine($"  Response: {ex.ResponseBody}");
    return 1;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"  ✗ Network Error: {ex.Message}");
    return 1;
}

return 0;
