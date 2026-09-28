---
title: "How to Tag Short Calls Automatically with the Hipcall API"
description: "An answered call that lasted eight seconds is not a successful call. Detect short calls from the webhook and tag them for review."
slug: how-to-tag-short-calls-automatically
lang: en
locales: [en, tr]
pubDate: 2026-09-28
categories: [developers]
intent: informational
translationKey: how-to-tag-short-calls-automatically
tags: [api, calls, tags, quality, dotnet]
authors: [hipcall-team]
featured: false
draft: true
task: 08
status: draft
---

## Overview

An answered call that lasts 8 seconds appears as a "successful call" in standard reports. However, no business discussion happens in 8 seconds. These calls usually indicate problems: wrong numbers, audio issues, or agents hanging up prematurely.

This guide explains how to detect short calls using the Hipcall webhook and apply tags for later review. Team leaders can filter calls by these tags to identify systemic issues without manually inspecting every record.

## Before you start

- Have an active Hipcall API key.
- Understand how to receive the `call_hangup` webhook event.
- Create the necessary tags in your Hipcall dashboard (Settings > Call Center > Tags). You must define tags in the UI before using them in the API. Note their IDs for your configuration.

## Which duration field do you actually want?

The call record contains multiple time fields. Choosing the correct one is critical to avoid false positives.

| Field | Measures |
|---|---|
| `call_duration` | Total billable time, including announcements and ringing |
| `first_touch_duration` | Time from entering the system until the agent answers |
| `started_at` | When the call entered the system |
| `answered_at` | When the PBX or agent answered the call |
| `bridged_at` | When the customer and agent are connected |
| `ended_at` | When the call disconnected |

Do not use `call_duration` to measure conversation length. A 45-second call that never connects has a `call_duration` of 45 seconds. An answered call might have a 15-second announcement and a 6-second conversation, resulting in a `call_duration` of 21 seconds.

Calculate the actual conversation duration by finding the difference between `ended_at` and `bridged_at`.

## Who hung up, and why it matters

When reviewing short calls, knowing who ended the conversation is essential. The `hangup_by` field provides this information.

- **contact:** The customer hung up. This is usually a natural drop, such as dialing a wrong number or changing their mind.
- **user:** The agent hung up. This is a critical issue that requires immediate attention from Quality Assurance.

Create two separate tags (e.g., `short-call-agent` and `short-call-customer`). Applying a single generic tag mixes customer errors with intentional agent hangups, destroying the analytical value of the tag.

## Step 1: Adding a tag

Use the `POST /api/v3/calls/{call_id}/tags` endpoint to add a tag. The payload requires the `tag_id`, not the tag name.

```http
POST /api/v3/calls/5c1904ed-ea4d-4209-badd-a985caf0c32a/tags
Authorization: Bearer HIPCALL_API_TOKEN
Content-Type: application/json

{
  "tag_id": 5617
}
```

The API will return `404 Not Found` if you send a non-existent `tag_id`. The endpoint is idempotent; sending the same tag multiple times returns a successful response without creating duplicates.

## Step 2: Wiring it to the webhook

Configure your threshold and tag IDs in `appsettings.json`.

```json
{
  "Hipcall": {
    "ShortCallThresholdSeconds": 10,
    "ShortCallAgentTagId": 5618,
    "ShortCallCustomerTagId": 5617
  }
}
```

Read the threshold from configuration instead of hardcoding it. This allows quick adjustments without deploying new code. Check if the call has a `bridged_at` value to ensure the rule only processes answered calls.

## The full script

This implementation uses C# and evaluates the rule within a webhook receiver.

```csharp
using Hipcall.PostCall.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class ShortCallTagRule : IPostCallRule
{
    public string RuleName => "ShortCallTagRule";

    private readonly HipcallApiClient _api;
    private readonly HipcallSettings _settings;
    private readonly ILogger<ShortCallTagRule> _logger;

    public ShortCallTagRule(
        HipcallApiClient api,
        IOptions<HipcallSettings> settings,
        ILogger<ShortCallTagRule> logger)
    {
        _api = api;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool Matches(WebhookPayload payload)
    {
        if (payload.Data == null) return false;
        if (payload.Event != "call_hangup") return false;
        
        if (payload.Data.IsMissedCall) return false;
        if (string.IsNullOrEmpty(payload.Data.BridgedAt)) return false;

        return true;
    }

    public async Task ExecuteAsync(WebhookPayload payload, CancellationToken ct = default)
    {
        var call = payload.Data!;

        if (!DateTime.TryParse(call.BridgedAt, out var bridgedAt) || 
            !DateTime.TryParse(call.EndedAt, out var endedAt))
        {
            return;
        }

        var talkDurationSeconds = (endedAt - bridgedAt).TotalSeconds;

        if (talkDurationSeconds >= _settings.ShortCallThresholdSeconds)
        {
            return;
        }

        int tagId = call.HangupBy == "user" 
            ? _settings.ShortCallAgentTagId 
            : _settings.ShortCallCustomerTagId;

        if (tagId == 0) return;

        await _api.AddTagToCallAsync(call.Uuid, tagId, ct);
    }
}
```

The `HipcallApiClient` implementation:

```csharp
public async Task<bool> AddTagToCallAsync(string callUuid, int tagId, CancellationToken ct = default)
{
    var body = new { tag_id = tagId };
    var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");

    var response = await _http.PostAsync($"calls/{callUuid}/tags", content, ct);
    if (response.IsSuccessStatusCode)
    {
        return true;
    }

    var errorBody = await response.Content.ReadAsStringAsync(ct);
    _logger.LogError("[Tag] Error {Status}: {Body}", (int)response.StatusCode, errorBody);
    return false;
}
```

## When it fails

**404 Not Found**

```json
{
  "errors": {
    "detail": "Not Found"
  }
}
```
You sent a `tag_id` that does not exist. Verify that the tag is created in the dashboard and the configuration file contains the correct ID.

**422 Unprocessable Entity**

```json
{
  "errors": {
    "tag_id": [
      "Missing field: tag_id"
    ]
  }
}
```
You sent the tag name instead of the ID. Update your request payload to pass the integer `tag_id`.

## Parameter reference

| Parameter | Type | Required | Description |
|---|---|---|---|
| `tag_id` | integer | yes | The unique identifier of the tag. Found in the dashboard URL when editing a tag. |

## Next steps

- Set up a CRM report to group short calls by agent and identify training opportunities.
- Monitor the volume of customer-dropped short calls to find potential routing issues in your IVR tree.
- Analyze the real conversation durations over time to fine-tune your threshold setting.
