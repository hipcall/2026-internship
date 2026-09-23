---
title: "How to Get a Hipcall API Key and Make Your First Request"
description: "Generate an API key in the dashboard, make your first authenticated request to the profile endpoint, and inspect the response."
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

## Overview

The Hipcall API allows you to integrate cloud telephony into your internal systems, CRM platforms, and custom business automations. Through the API, you can originate calls, export call detail records (CDRs), stream caller context to agent screens, and manage telephony routing programmatically.

In this guide, you will learn how to create a Personal Access Token in the Hipcall dashboard and send your first authenticated HTTP request to the `/profile` endpoint.

## Before you start

Creating an API key requires a user account with either the **Admin** or **Founder** role. Standard and restricted user roles do not have permission to access developer credentials.

1. Sign in to your Hipcall dashboard at `https://use.hipcall.com/`.
2. Open **Settings > Developer** in the left sidebar.
3. Select the **API** tab.
4. Click the **New** button.
5. Enter a descriptive **Name** (for example, `CRM Integration`).
6. Select an **Expiration date** between 1 day and 3 years (the default is 1 year).
7. Click **Save**.

Immediately upon creation, Hipcall displays the complete raw secret key on your screen. Copy and store this value in a secure password manager before navigating away. For security purposes, the raw token is never shown again; only a masked prefix remains visible in your token list.

## Making your first request

Store your API key in an environment variable rather than hardcoding it in scripts:

```bash
export HIPCALL_API_TOKEN="SFMyNTY.g2gDbQAAAC..."
```

Authentication uses standard HTTP Bearer tokens transmitted via the `Authorization` header. To test connectivity, issue a `GET` request to the `/profile` endpoint:

```bash
curl -sS -i \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  https://use.hipcall.com/api/v3/profile
```

## Successful response

A valid token returns an HTTP `200 OK` status code.

The JSON response provides information regarding the token owner, assigned phone numbers, default Caller ID settings, and account service limits:

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

Response headers contain rate-limiting metrics. Standard developer environments permit **60 requests per minute**.

## When it fails

If an issue occurs during authentication, the API returns HTTP `401 Unauthorized`. The response payload explains the specific reason:

### 1. Missing Authorization header

Sending a request without the `Authorization` header produces an immediate rejection:

```json
{
  "errors": {
    "detail": "Authentication required. Provide either OAuth Bearer token or API token."
  }
}
```

**Resolution:** Confirm that the `Authorization: Bearer <token>` header is present in the request and that your environment variable is loaded.

### 2. Invalid or expired token

Supplying a deleted, revoked, or expired token results in a descriptive error:

```json
{
  "errors": {
    "detail": "Not authorized. Check your API token is valid and not expired."
  }
}
```

**Resolution:** Inspect token status and expiration dates in the Hipcall dashboard under Settings > Developer > API. Revoke any compromised token and generate a replacement.

## Security best practices

- Do not commit API credentials to version control systems or embed them into frontend client code.
- Provide credentials to production applications using environment variables, container secrets, or cloud key vaults.
- If a secret is exposed, immediately delete the token in the dashboard to invalidate access.

## Parameter reference

The profile endpoint accepts no query parameters or request body:

| Parameter | Location | Type | Required | Description |
|---|---|---|---|---|
| `Authorization` | Header | string | Yes | Bearer token authentication in the format `Bearer <API_TOKEN>`. |

## Next steps

- Explore interactive endpoint documentation in the [Hipcall API Reference](https://use.hipcall.com/api-docs/).
- Learn how to filter and export call logs in our call management guides.
- Connect with other developers in the [Hipcall Community](https://community.hipcall.com/).