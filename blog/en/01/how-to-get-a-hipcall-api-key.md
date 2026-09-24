---
title: "How to get a Hipcall API key and make your first request"
description: "Create an API key in the Hipcall dashboard, send your first authenticated request, and read the response."
slug: how-to-get-a-hipcall-api-key
lang: en
locales: [en, tr]
pubDate: 2026-09-16
categories: [developers]
intent: informational
translationKey: how-to-get-a-hipcall-api-key
tags: [api, authentication, getting-started]
authors: [hipcall-team]
featured: false
draft: true
task: 01
status: review
---

## What this guide covers

The Hipcall API lets you connect your cloud phone system to your own software. You can start calls, pull call records, push caller info to an agent's screen, or change how calls are routed, all through HTTP requests.

This page walks you through creating an API key, testing it against the `/profile` endpoint, and handling the most common errors.

## Before you start

You need an account with the Admin or Founder role. Other roles cannot reach the developer settings page.

1. Sign in at `https://use.hipcall.com/`.
2. Go to Settings > Developer in the left sidebar.
3. Open the API tab.
4. Click New.
5. Give the key a name that tells you what it is for (for example, `CRM Integration`).
6. Pick an expiration date. The range is 1 day to 3 years; the default is 1 year.
7. Click Save.

Hipcall shows the full key once, right after you create it. Copy it into a password manager before you leave the page. After that, only a masked version appears in the dashboard.

## Send your first request

Store the key in an environment variable instead of pasting it into your code:

```bash
export HIPCALL_API_TOKEN="SFMyNTY.g2gDbQAAAC..."
```

The API uses a standard `Authorization: Bearer` header. To check that everything works, send a GET request to `/profile`:

```bash
curl -sS -i \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  https://use.hipcall.com/api/v3/profile
```

## Reading the response

A valid key returns `200 OK`. The JSON body contains the user tied to the key, the phone numbers assigned to them, the default Caller ID, and account limits:

```json
{
  "data": {
    "user": {
      "id": 4200,
      "owner": true,
      "suspended": false,
      "state": "available",
      "title": null,
      "email": "alex@example.com",
      "locale": "en_US",
      "timezone": "Europe/London",
      "created_at": "2026-07-21T10:55:30Z",
      "full_name": "Alex Taylor",
      "numbers": [
        {
          "id": 938,
          "name": "Customer Support",
          "number": "+442079460123",
          "country": "GB"
        }
      ],
      "default_number": {
        "id": 938,
        "name": "Customer Support",
        "number": "+442079460123",
        "country": "GB"
      },
      "first_name": "Alex",
      "last_name": "Taylor",
      "phone_prefix": "GB"
    },
    "account": {
      "locale": "en_US",
      "timezone": "Europe/London",
      "contact_center": {
        "b2b": true
      },
      "limits": {
        "max_channel": 5
      }
    },
    "roles": []
  }
}
```

The response headers also include rate limit counters. The default limit is 60 requests per minute.

## When something goes wrong

If the API cannot verify your identity, it returns `401 Unauthorized`. The error message tells you why.

### Missing header

If you forget the `Authorization` header, you get:

```json
{
  "errors": {
    "detail": "Authentication required. Provide either OAuth Bearer token or API token."
  }
}
```

Check that your request includes `Authorization: Bearer <token>` and that the environment variable is set.

### Invalid or expired key

If the key has been deleted or has passed its expiration date:

```json
{
  "errors": {
    "detail": "Not authorized. Check your API token is valid and not expired."
  }
}
```

Open Settings > Developer > API in the dashboard and check the key's status. If it is expired or compromised, delete it and create a new one.

## Keep your key safe

- Do not commit API keys to version control or put them in frontend code.
- In production, pass keys through environment variables, Docker secrets, or a cloud key vault.
- If you think a key has leaked, delete it from the dashboard. The key stops working immediately.

## Parameter reference

The profile endpoint does not take any query parameters or a request body:

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `Authorization` | Header | string | Yes | Bearer token in the format `Bearer <API_TOKEN>`. |

## Next steps

- Try other endpoints in the [Hipcall API Reference](https://use.hipcall.com/api-docs/).
- Move on to listing and filtering call logs.
- Ask questions in the [Hipcall Community](https://community.hipcall.com/).