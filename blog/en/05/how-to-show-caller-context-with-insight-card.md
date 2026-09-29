---
title: "How to Show Caller Context on the Agent Screen with Insight Card"
description: "Push your own customer data onto the agent's screen the moment a call starts, using the call_init webhook and the Insight Card API."
slug: how-to-show-caller-context-with-insight-card
lang: en
locales: [en, tr]
pubDate: 2026-09-22
categories: [developers]
intent: informational
translationKey: how-to-show-caller-context-with-insight-card
tags: [insight-card, webhooks, dotnet, crm]
authors: [hipcall-team]
featured: false
draft: true
task: 05
status: review
---

## Overview

When an incoming call rings and the screen shows only an unfamiliar number, the representative switches to the CRM tab and searches for it. That manual search takes about fifteen seconds. By the time the record loads, the caller has already started speaking.

Insight Card removes this delay. The moment a call starts, it displays the customer's name, company, open balance, and account owner inside the web phone. The data comes from your database; Hipcall surfaces it before the agent answers.

This page covers intercepting `call_init` webhooks with ASP.NET Core Minimal API, looking up the caller in a local CRM, and sending an Insight Card to the active call session.

## Before you start

Before implementing the integration, make sure you have:

- The .NET 8 SDK installed on your workstation or server (`dotnet --version` should output 8.0 or higher).
- A secure, publicly accessible HTTPS endpoint (such as an ngrok tunnel on port 5080) to receive incoming webhook events.
- A valid Personal Access Token generated in the Hipcall Developer Portal.
- An active Hipcall Web Phone session open in your browser to verify card rendering.

Set your API token in your terminal environment:

```bash
export HIPCALL_API_TOKEN="SFMyNTY.g2gDbQAAAC..."
```

## Insight Card structure and visual components

An Insight Card is rendered as a vertical stack of structured rows inside the active call window. Three core row types are supported:

| Row Type | Required Fields | Optional Fields | Description |
|---|---|---|---|
| `title` | `type`, `text` | `link` | The prominent card header. Setting `link` renders an external link icon that opens the CRM record in a new tab. |
| `shortText` | `type`, `text` | `label`, `link`, `ios`, `android` | Standard two-column data item. Displays a muted label on the left and bold text on the right (for company, tier, balance). |
| `user` | `type`, `label`, `user_id` | - | Resolves a Hipcall user ID to display the account owner's full name. |

Example payload for a structured customer card:

```json
{
  "card": [
    {
      "type": "title",
      "text": "Jane Doe",
      "link": "https://crm.example.com/customers/102"
    },
    {
      "type": "shortText",
      "label": "Company",
      "text": "Acme Global Ltd.",
      "link": "https://crm.example.com/customers/102"
    },
    {
      "type": "shortText",
      "label": "Segment",
      "text": "Enterprise"
    },
    {
      "type": "shortText",
      "label": "Balance",
      "text": "£14,250 (Open Invoice)"
    },
    {
      "type": "user",
      "label": "Account Owner",
      "user_id": 4200
    }
  ]
}
```

## Creating an Insight Card via the REST API

To attach a card to an active call session, send an HTTP POST request to `/api/v3/calls/{call_id}/cards`:

```bash
curl -X POST "https://use.hipcall.com/api/v3/calls/{call_id}/cards" \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "card": [
      {
        "type": "title",
        "text": "Acme CRM Customer Profile",
        "link": "https://crm.example.com/customer/101"
      },
      {
        "type": "shortText",
        "label": "Customer",
        "text": "Jane Doe"
      },
      {
        "type": "shortText",
        "label": "Status",
        "text": "VIP - Up to Date"
      }
    ]
  }'
```

On success, the API returns HTTP `201 Created` and renders the card inside the agent's web phone interface:

![Insight Card rendered on Hipcall Web Phone](/blog/assets/insight-card-test-page4-1.png)

## Webhook integration and call lifecycle

To ensure the card is ready the moment the representative answers, the pipeline triggers on the `call_init` webhook event.

You can simulate a Hipcall `call_init` event locally using `curl`:

```bash
curl -X POST http://localhost:5080/hipcall/events/whsec_live_xxxxxxxxxxxxxxxx \
  -H "Content-Type: application/json" \
  -d '{
  "data": {
    "credited": null,
    "team_touch_at": null,
    "first_touch_duration": null,
    "contact_id": null,
    "callee_id": null,
    "answered_at": null,
    "voicemail_url": null,
    "caller_number": "+90850XXXXXXX",
    "missing_call_reason": null,
    "call_duration": null,
    "callback_time": null,
    "call_flow": [
      {
        "action": "init",
        "detail": {
          "id": null,
          "type": "contact"
        },
        "timestamp": 1790691203
      }
    ],
    "direction": "outbound",
    "callee_number": "+90530XXXXXXX",
    "voicemail_id": null,
    "callback_user_id": null,
    "callee_type": "contact",
    "ended_at": null,
    "missing_call": null,
    "channel_type": "number",
    "callback_cdr_uuid": null,
    "voicemail_type": null,
    "caller_id": null,
    "started_at": "2026-09-29T14:13:23Z",
    "bridged_at": null,
    "channel_id": 942,
    "caller_type": null,
    "user_id": 4200,
    "hangup_by": null,
    "uuid": "410c92c5-2b61-4dd2-aa75-xxxxxxxxxxxx",
    "record_url": null,
    "number_id": 942,
    "company_id": 80719
  },
  "event": "call_init"
}'
```

### Determining the customer number by call direction

The location of the target customer number depends on the `direction` parameter:

- **Inbound calls (`inbound`):** The caller is the external customer. Extract the number from `data.caller_number`.
- **Outbound calls (`outbound`):** The representative initiates the call. Extract the customer number from `data.callee_number`.

### Handling missing customers silently

Hipcall accepts empty card arrays (`{"card": []}`). However, posting an empty card causes the web phone to display an unnecessary blank box. If your database query finds no matching profile, do not send an HTTP request; acknowledge the webhook and complete the execution silently.

## Minimal API receiver example

The following ASP.NET Core Minimal API acknowledges incoming `call_init` webhooks within 50 ms, identifies the caller, and pushes card generation to a background task to prevent webhook timeouts.

```csharp
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(5080));
builder.Services.AddHttpClient();
var app = builder.Build();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var expectedSecret = Environment.GetEnvironmentVariable("HIPCALL_WEBHOOK_SECRET") 
    ?? throw new InvalidOperationException("HIPCALL_WEBHOOK_SECRET environment variable is missing.");
var apiToken = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN") 
    ?? throw new InvalidOperationException("HIPCALL_API_TOKEN environment variable is missing.");

app.MapPost("/hipcall/events/{secret?}", async (string? secret, HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    if (!string.Equals(secret, expectedSecret, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    HipcallWebhookPayload? payload;
    try
    {
        payload = await JsonSerializer.DeserializeAsync<HipcallWebhookPayload>(request.Body, jsonOptions);
    }
    catch
    {
        return Results.Ok();
    }

    if (payload?.Event == "call_init" && payload.Data?.Uuid != null)
    {
        string? targetPhone = string.Equals(payload.Data.Direction, "inbound", StringComparison.OrdinalIgnoreCase)
            ? payload.Data.CallerNumber
            : payload.Data.CalleeNumber;

        if (!string.IsNullOrWhiteSpace(targetPhone))
        {
            _ = Task.Run(() => ProcessInsightCardAsync(payload.Data.Uuid, targetPhone, httpClientFactory, apiToken, jsonOptions));
        }
    }

    return Results.Ok();
});

app.Run();

async Task ProcessInsightCardAsync(string uuid, string phone, IHttpClientFactory clientFactory, string token, JsonSerializerOptions options)
{
    // Simulate CRM lookup
    if (phone != "+442079460123") return; 

    var cardData = new InsightCardRoot
    {
        Card =
        [
            new InsightCardItem { Type = "title", Text = "Jane Doe", Link = "https://crm.example.com/customers/102" },
            new InsightCardItem { Type = "shortText", Label = "Company", Text = "Acme Global Ltd." }
        ]
    };

    var client = clientFactory.CreateClient();
    using var content = new StringContent(JsonSerializer.Serialize(cardData, options), Encoding.UTF8, "application/json");
    using var req = new HttpRequestMessage(HttpMethod.Post, $"https://use.hipcall.com/api/v3/calls/{uuid}/cards")
    {
        Content = content
    };
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    await client.SendAsync(req);
}

public class HipcallWebhookPayload
{
    public string? Event { get; set; }
    public CallDataPayload? Data { get; set; }
}

public class CallDataPayload
{
    public string? Uuid { get; set; }
    public string? Direction { get; set; }
    public string? CallerNumber { get; set; }
    public string? CalleeNumber { get; set; }
    public int? CallDuration { get; set; }
    public string? RecordUrl { get; set; }
    public string? HangupBy { get; set; }
    public string? VoicemailId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? BridgedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class InsightCardRoot
{
    public List<InsightCardItem> Card { get; set; } = [];
}

public class InsightCardItem
{
    public string Type { get; set; } = "shortText";
    public string? Label { get; set; }
    public string? Text { get; set; }
    public string? Link { get; set; }
    public int? UserId { get; set; }
}
```

## When it fails

### 1. HTTP 422 Unprocessable Entity and strict validation

The Insight Card API enforces strict schema validation on row objects. If attributes not defined for a row type (such as a `user_id` property on a `shortText` row) are included—even with `null` values—the API rejects the payload:

```text
HTTP 422 Unprocessable Entity
shortText type only allows fields: type, text, label, link, android, ios. Invalid fields found: user_id in card item 2
```

Configure your JSON serializer with `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. This strips unassigned fields from outgoing payloads.

### 2. Posting cards to terminated calls

If a card request arrives after a call ends, Hipcall still returns HTTP 201 and attaches the card to the session history. The agent never sees it because the dialog is closed. Context must be pushed while the session is alive.

### 3. Latency budget considerations

Telephony ring times typically range from 5 to 15 seconds. Factoring in webhook transit (~150 ms) and the card API request (~200 ms), a 2-second database lookup yields an overall dispatch time of roughly 2.4 seconds, ensuring the card is ready before the representative picks up. Lookups exceeding 3 to 4 seconds cause cards to pop up after the conversation starts.

## Parameter reference

Supported Insight Card row types and their allowed attributes:

| Row Type | Supported Fields | Description |
|---|---|---|
| `title` | `type`, `text`, `link` | Card header with clickable external CRM link. |
| `shortText` | `type`, `text`, `label`, `link`, `ios`, `android` | Key-value data row, external URL, or mobile deep link. |
| `user` | `type`, `label`, `user_id` | Displays internal account owner via Hipcall user ID. |

## Next steps

- Transition from local memory lookups to an indexed PostgreSQL database or Redis cache for large customer datasets.
- Use Hipcall's `GET /api/v3/lookup/by_phone` endpoint as a fallback data source when an incoming caller does not match records in your primary CRM.
- Add `ios` and `android` deep link schemes to customer cards to let mobile agents open native CRM records directly.
- Share your integration feedback or custom Insight Card designs in the [Hipcall Community](https://community.hipcall.com/).
