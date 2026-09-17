---
title: "How to Get a Hipcall API Key and Make Your First Request"
description: "Create an API key in the dashboard, make your first authenticated request, and understand what comes back."
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
status: draft
---

## Overview

This guide shows you how to create a HipCall API key and make your first authenticated request to the profile endpoint. You will also learn what a successful response looks like and how to handle common authentication errors.

## Before you start

You need a HipCall user account with the **Admin** or **Founder** role to access the developer settings and create an API key. Users with **Standard** or **Authorized** roles cannot access these settings.

1. Log in to your HipCall dashboard at `https://use.hipcall.com.tr/`.
2. Open **Settings > Developer**.
3. Open the **API** section.
4. Click **New** to create an API key.
5. Enter a **Name** and **Expiration date**. Both fields are required.
6. Create the API key and store the full secret key shown on the screen in a secure location.

The default expiration period is one year. You can select an expiration date up to three years in the future.

After the API key is created, HipCall displays the full secret key on the screen. Store it somewhere secure before leaving the page. You cannot view the full key again after the page is closed. The API token list displays a masked version instead.

If you lose the key, delete the token and create a new one.

## Your first request

Store your API key in an environment variable instead of placing it directly in your command.

```bash
export HIPCALL_API_TOKEN="..."
```

For a Turkey DEMO account, use the following profile endpoint:

```text
https://use.hipcall.com.tr/api/v3/profile
```

Send a `GET` request with the API key in the `Authorization` header.

```bash
curl -sS -i \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  https://use.hipcall.com.tr/api/v3/profile
```

You can use the API key exactly as it is displayed in the panel. The `SFMyNTY.` prefixed token format is also supported, so no additional conversion is required.

## Successful response

A valid API key returns a `200 OK` response.

The response contains information under the `data` object, including the authenticated user, permissions, capabilities, account, roles, and subscription information.

```json
{
  "data": {
    "user": {
      "id": 4200,
      "owner": true,
      "suspended": false,
      "state": "available",
      "title": null,
      "email": "ornek@firma.com",
      "locale": "tr_TR",
      "timezone": "Europe/Istanbul",
      "created_at": "2026-07-21T10:55:30",
      "full_name": "Ahmet Y.",
      "numbers": [
        {
          "id": 939,
          "name": "Sub Manager",
          "number": "+90850XXXXXXX",
          "country": "TR"
        },
        {
          "id": 938,
          "name": "Super Manager",
          "number": "+90850XXXXXXX",
          "country": "TR"
        }
      ],
      "default_number": {
        "id": 938,
        "name": "Super Manager",
        "number": "+90850XXXXXXX",
        "country": "TR"
      },
      "first_name": "Ahmet",
      "last_logged_in": "2026-09-15T13:10:07",
      "last_name": "Y.",
      "phone_countries": [
        "AR",
        "TR"
      ],
      "phone_prefix": "TR",
      "avatar_url": null
    },
    "account": {
      "locale": "tr_TR",
      "timezone": "Europe/Istanbul",
      "created_at": "2026-07-21T10:55:30",
      "contact_center": {
        "b2b": true
      },
      "limits": {
        "max_channel": 5
      },
      "slug": null
    },
    "roles": []
  }
}
```


Mask personal and account information before publishing the response.

The response headers also include rate-limit information. In the tested DEMO environment, the API allows **60 requests per minute**.

## When it fails

### Missing API key

A request without an `Authorization` header returns `401 Unauthorized`.

```bash
curl -sS -i \
  https://use.hipcall.com.tr/api/v3/profile
```

The response body is:

```json
{
  "errors": {
    "detail": "Authentication required. Provide either OAuth Bearer token or API token."
  }
}
```

### Invalid or expired API key

An invalid or expired API key also returns `401 Unauthorized`, but the response body is different.

```bash
curl -sS -i \
  -H "Authorization: Bearer bu-anahtar-sahte" \
  https://use.hipcall.com.tr/api/v3/profile
```

The response body is:

```json
{
  "errors": {
    "detail": "Not authorized. Check your API token is valid and not expired."
  }
}
```

### API host

For Turkey DEMO accounts, use `https://use.hipcall.com.tr` as the API host. The profile endpoint is:

```text
https://use.hipcall.com.tr/api/v3/profile
```

## Keeping your key safe

Never publish your API key in a repository, forum post, screenshot, or blog article. API keys, phone numbers, email addresses, and names must be masked before sharing DEMO data.

The full secret key is shown after creation and cannot be viewed again after leaving the page.

If your key is lost or exposed, delete it and create a new one.

## Parameter reference

The profile endpoint does not require a request body. Authentication is provided through the `Authorization` header.

| Parameter       | Type   | Required | Description                                    |
| --------------- | ------ | -------- | ---------------------------------------------- |
| `Authorization` | header | yes      | Bearer token used to authenticate the request. |

## Next steps

Explore the [HipCall API Reference](https://use.hipcall.com/api-docs/) to learn about the available endpoints.

Ask questions or share your integration experience in the [HipCall Community](https://community.hipcall.com/).

You can also use your live API token directly on the interactive [Hipcall API Reference](https://use.hipcall.com.tr/api-docs/) page. Click the "Authorize" button to test endpoints interactively in your browser.