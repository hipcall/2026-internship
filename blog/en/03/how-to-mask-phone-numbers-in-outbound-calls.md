---
title: "How to Mask Phone Numbers in Outbound Calls with the Hipcall API"
description: "Connect two people without either seeing the other's number. Start a masked call from your app and understand what the API does and does not promise."
slug: how-to-mask-phone-numbers-in-outbound-calls
lang: en
locales: [en, tr]
pubDate: 2026-09-18
categories: [developers]
intent: informational
translationKey: how-to-mask-phone-numbers-in-outbound-calls
tags: [api, calls, privacy, click-to-call]
authors: [hipcall-team]
featured: false
draft: true
task: 03
status: review
---

## Overview

A courier needs to call a customer about a delivery. If the courier dials from a personal phone, the customer sees the courier's number and can call it back after the delivery is over. The same problem shows up in marketplaces, real estate, and field service: two people need to talk, but neither should keep the other's number.

Hipcall handles this with a single API call. Your app sends an HTTP POST, the PBX connects both sides through a company number, and neither party sees the other's real phone number.

This page covers starting an outbound call, enabling masking, choosing which company number the customer sees, and understanding what the API response actually tells you.

## Before you start

You need three things:

- An API key. See [How to get a Hipcall API key](/developers/how-to-get-a-hipcall-api-key/) if you do not have one.
- A registered device for the agent. The agent's Hipcall app (web, desktop, or mobile) must be online. If the device is offline, the API accepts the request but the call never connects.
- At least one outbound number. List yours with `GET /api/v3/numbers`.

Set the API key:

```bash
export HIPCALL_API_TOKEN="SFMyNTY.g2gDbQAAAC..."
```

## Starting a call

Send a POST to `/users/{user_id}/call` with the customer's number in E.164 format:

```bash
curl -X "POST" "https://use.hipcall.com/api/v3/users/4200/call" \
  -H "accept: application/json" \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "callee_number": "+442079460123",
    "ring_user_first": true
  }'
```

### Which side rings first (ring_user_first)

`ring_user_first` is required. It controls the order:

- `true` (recommended): The agent's app rings first. When the agent picks up, the PBX dials the customer. The customer never lands on a silent line.
- `false`: The PBX dials the customer right away. If the agent is not ready, the call drops.

### Choosing the outbound number (number_id)

`number_id` sets which company number the customer sees on their phone:

```bash
curl -X "POST" "https://use.hipcall.com/api/v3/users/4200/call" \
  -H "accept: application/json" \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "callee_number": "+442079460123",
    "number_id": 943,
    "ring_user_first": true
  }'
```

If you leave `number_id` out, the API uses the agent's default number from their profile. You can list available numbers with `GET /api/v3/numbers`.

Two things people mix up: `callee_number` is who you are calling; `number_id` is which of your own numbers appears on their screen.

When calling through `/extensions/{extension_id}/call`, `number_id` is required. Through `/users/{user_id}/call`, it is optional.

The API can accept numbers without a country code, like `02079460123`. In these cases, it interprets the number based on the default country tied to the user or account (e.g., the UK). However, in production, always send the full E.164 format (`+44...`). It avoids routing surprises and failed calls when accounts span multiple countries.

## Turning on masking

Add `call_masking: true` to hide the customer's number from the agent. Add `call_masking_name` to show a label instead of zeros:

```bash
curl -X "POST" "https://use.hipcall.com/api/v3/users/4200/call" \
  -H "accept: application/json" \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "callee_number": "+442079460123",
    "call_masking": true,
    "call_masking_name": "Order #1042",
    "number_id": 943,
    "ring_user_first": true
  }'
```

### What each side sees

- The agent sees `0000000000` if you omit `call_masking_name`, or the label you set (e.g. "Order #1042"). The real number never appears on the agent's screen.
- The customer sees the company number you picked with `number_id`. The agent's personal number is never shown.

### Call records keep the real numbers

Masking only affects what the agent sees during the call. The call detail records (`GET /api/v3/calls`) still contain the full, unmasked numbers. This is by design: billing, legal compliance, and reporting need the real data.

## What the 201 response means

When the API accepts your request, it returns `201 Created` with a call ID:

```json
{
  "data": {
    "id": "19d354e6-2be8-4c4e-bd49-fd12545f71a4"
  }
}
```

```mermaid
sequenceDiagram
    participant App as Your App (CRM)
    participant API as Hipcall API
    participant Agent as Agent Device
    participant Customer as Customer Phone

    App->>API: POST /users/{id}/call (ring_user_first: true)
    API-->>App: 201 Created (data.id: UUID)
    Note over App,API: Call queued. Not connected yet.
    API->>Agent: Ring agent device
    Agent-->>API: Agent answers
    API->>Customer: Dial customer number
    Customer-->>API: Customer answers
    Note over Agent,Customer: Both sides bridged. Call in progress.
```

This is the most important part: a 201 means the PBX accepted the command. It does not mean the customer's phone rang, or that anyone picked up. Even if the agent's device is turned off, you still get a 201. The PBX tries to reach the agent, fails, and drops the call silently.

Do not mark calls as "connected" in your CRM based on this response alone. Use webhooks or poll `GET /api/v3/calls` and check `bridged_at` and `call_duration` to know whether the call actually went through.

## The full script

This class wraps the call endpoint. It uses a single `HttpClient`, reads the token from the environment, and keeps the API error message when something fails:

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public sealed class HipcallClient
{
    private static readonly HttpClient s_httpClient = new();
    private const string BaseUrl = "https://use.hipcall.com/api/v3";
    private readonly string _apiToken;

    public HipcallClient()
    {
        _apiToken = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN")
            ?? throw new InvalidOperationException("HIPCALL_API_TOKEN environment variable is not set.");
    }

    public async Task<string> StartCallAsync(
        int userId,
        string calleeNumber,
        bool ringUserFirst = true,
        int? numberId = null,
        bool? callMasking = null,
        string? callMaskingName = null)
    {
        var body = new Dictionary<string, object>
        {
            ["callee_number"] = calleeNumber,
            ["ring_user_first"] = ringUserFirst
        };

        if (numberId.HasValue)
            body["number_id"] = numberId.Value;

        if (callMasking.HasValue)
            body["call_masking"] = callMasking.Value;

        if (!string.IsNullOrEmpty(callMaskingName))
            body["call_masking_name"] = callMaskingName;

        string json = JsonSerializer.Serialize(body);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/users/{userId}/call");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await s_httpClient.SendAsync(request).ConfigureAwait(false);
        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"API Error {(int)response.StatusCode}: {responseBody}");
        }

        using JsonDocument doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("data")
            .GetProperty("id")
            .GetString() ?? throw new InvalidOperationException("API returned a null call ID.");
    }
}
```

To run the application:

```csharp
var client = new HipcallClient();
string callId = await client.StartCallAsync(
    userId: 4200,
    calleeNumber: "+442079460123",
    ringUserFirst: true,
    numberId: 943,
    callMasking: true,
    callMaskingName: "Order #1042"
);

Console.WriteLine($"Call queued. Call ID: {callId}");
```

### Notes on this code

- Create a single `HttpClient` and reuse it. Opening a new one per request inside a loop exhausts sockets.
- When the API returns an error, print the full response body. Using only `EnsureSuccessStatusCode()` hides the API's error message.
- Masking and `number_id` are optional parameters. If you leave them out, the user's profile defaults apply.

## When it fails

### 422 Unprocessable Entity

Missing a required field. For example, leaving out `ring_user_first`:

```json
{
  "errors": {
    "ring_user_first": [
      "can't be blank"
    ]
  }
}
```

Both `callee_number` and `ring_user_first` must be in the request body.

### 404 Not Found

The user ID or extension ID does not exist:

```json
{
  "errors": {
    "user_id": ["No result for user_id: 21212212"]
  }
}
```

Verify the ID with `GET /api/v3/users` or `GET /api/v3/extensions`.

### Agent device offline (silent drop)

If the agent's Hipcall app is closed or disconnected:

- The API still returns `201 Created` because the command was queued.
- The PBX tries to reach the agent, gets no answer, and terminates the call without ever dialing the customer.
- To find out why a call did not connect, check the `call_hangup` webhook for `hangup_by: "system"`, or look at the agent's device status in the dashboard logs.

## Parameter reference

| Parameter | Type | Required | Description |
|---|---|---|---|
| `callee_number` | string | yes | Customer's phone number in E.164 format (e.g. `+442079460123`). |
| `ring_user_first` | boolean | yes | Ring the agent first (`true`, recommended) or dial the customer directly (`false`). |
| `number_id` | integer | no | ID of the company number shown to the customer. Uses the agent's default if omitted. |
| `call_masking` | boolean | no | Hides the customer's number on the agent's screen, showing `0000000000` instead. |
| `call_masking_name` | string | no | Replaces the zeros with a label (max 30 characters). |

## Next steps

- Set up a webhook receiver to get notified when calls connect, bridge, or hang up.
- Explore more parameters in the [Hipcall API Reference](https://use.hipcall.com/api-docs/).
- Ask questions in the [Hipcall Community](https://community.hipcall.com/).
