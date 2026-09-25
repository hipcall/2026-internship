---
title: "How to Sync Contacts Between Your CRM and Hipcall Using external_id"
description: "Match records with your own IDs instead of phone numbers, and write a sync that can run twice without creating duplicates."
slug: how-to-sync-contacts-with-external-id
lang: en
locales: [en, tr]
pubDate: 2026-09-25
categories: [developers]
intent: informational
translationKey: how-to-sync-contacts-with-external-id
tags: [api, contacts, crm, dotnet, integrations]
authors: [hipcall-team]
featured: false
draft: true
task: 06
status: review
---

## Overview

Your CRM's customer ID and Hipcall's contact ID do not have to match. When you set up synchronization between the two systems, you need a shared field to link records.

Phone-number matching looks straightforward, but number changes and formatting differences between systems can cause matching problems.

`external_id` removes this issue. You store your own identifier on the Hipcall record and query by it directly. This page covers creating contacts and companies with your own IDs, handling the update rules that differ between `POST` and `PATCH`, and building an idempotent C# sync that produces no duplicates on repeated runs.

## Before you start

Make sure you have these ready:

- .NET 9 SDK installed (`dotnet --version` should output 9.0 or higher).
- A valid Personal Access Token from the Hipcall Developer Portal. The token needs permission to create and update contacts and companies.
- A few test contacts and companies already in your DEMO account, so you can verify both create and update paths.

Set your API token in the terminal:

```bash
export HIPCALL_API_TOKEN="..."
```

## Creating a contact with your own ID

The smallest valid body for `POST /api/v3/contacts` requires one field: `first_name`. Add `external_id` to tag the record with your CRM's identifier:

```bash
curl -X POST "https://use.hipcall.com/api/v3/contacts" \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "first_name": "Can",
    "last_name": "Kaya",
    "external_id": "CUSTOMER-CAN-001",
    "company_id": 80679,
    "phones": [
      { "country": "GB", "number": "+447700900123" }
    ]
  }'
```

The response returns the new record with an internal `id`. Notice that `phones` and `emails` arrays are accepted here during creation.

If you try to create a second contact with the same `external_id`, the API returns `422`:

```json
{
  "errors": {
    "external_id": [
      "has already been taken"
    ]
  }
}
```

The uniqueness constraint on `external_id` prevents creating multiple contacts with the same ID. This means you can reliably match records through this field during synchronization.

## Looking it up by external ID

Query a contact by your own identifier instead of the Hipcall internal ID:

```bash
curl -s "https://use.hipcall.com/api/v3/contacts/by-external-id/CUSTOMER-CAN-001" \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN"
```

If the record exists, you get `200 OK` with the full contact object. If it does not exist, you get `404 Not Found`. Your sync uses these two responses to decide between create and update.

## Updating: what goes where

The API separates scalar fields from arrays when updating records. Use `PATCH` on the main endpoint to update fields like `first_name`, `last_name`, and `company_id`. Manage phones and emails as sub-resources through their own endpoints.

| Operation | Endpoint | Body format |
|---|---|---|
| Update name, company | `PATCH /api/v3/contacts/{id}` | `{ "first_name": "Can", "company_id": 80679 }` |
| Add phone | `POST /api/v3/contacts/{id}/phones` | `{ "phones": [{ "country": "GB", "number": "+447700900123" }] }` |
| Remove phone | `DELETE /api/v3/contacts/{id}/phones/%2B447700900123` | (no body) |
| Add email | `POST /api/v3/contacts/{id}/emails` | `{ "emails": ["name@example.com"] }` |
| Remove email | `DELETE /api/v3/contacts/{id}/emails/name%40example.com` | (no body) |

Because `POST /api/v3/contacts` accepts `phones` and `emails` arrays during creation, you might expect `PATCH /api/v3/contacts/{id}` to accept them as well. If you include a `phones` array in a `PATCH` request, the API rejects it with a `422 Unprocessable Entity` status and returns `"Unexpected field: phones"`.

A contact can hold up to six phone numbers. Attempting to add a seventh returns `422` with `"Contact already has 6 phone numbers"`. Adding a number that already exists on the contact also returns `422`.

## Custom fields

Custom field definitions are managed in the panel under Settings > Contact Center > Custom Fields. The API references them by `slug`, which is the machine-readable key generated from the field name (for example, `ozel_alan` for a field named "Özel Alan").

Read or write custom fields through the `custom_fields` object:

```json
{
  "custom_fields": {
    "tier": "enterprise",
    "account_manager": "john.doe"
  }
}
```

Both `POST` and `PATCH` accept `custom_fields`. If a field is marked as required in the panel and you omit it during creation, the API rejects the request:

```json
{
  "errors": {
    "custom_fields": {
      "tier": ["can't be blank"]
    }
  }
}
```

## Companies and the link between them

Companies follow the same `external_id` pattern. Create a company with your own ID, then link contacts to it via `company_id`:

```bash
curl -X POST "https://use.hipcall.com/api/v3/companies" \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "name": "Acme Ltd.", "external_id": "COMPANY-77" }'
```

Look it up with `GET /api/v3/companies/by-external-id/COMPANY-77`, then use the returned `id` as `company_id` when creating or updating a contact.

If you delete a company, contacts that were linked to it have their `company` field set to `null`. The contacts themselves are not deleted.

The `/assign` sub-endpoint (`PATCH /api/v3/contacts/{id}/assign`) changes the contact owner without requiring full update permissions on the contact record. Send `"assign_to_user_id": null` to remove the assignment.

## Writing an idempotent sync

The sync reads a JSON file (your CRM export) and processes each record through this decision tree:

```mermaid
flowchart TD
    Start([Read CRM record]) --> SyncCompany[GET /companies/by-external-id]
    SyncCompany --> HasCompany{Company exists?}
    HasCompany -- No 404 --> CreateCompany[POST /companies] --> GetCompanyId[Get Hipcall company ID]
    HasCompany -- Yes 200 --> GetCompanyId

    GetCompanyId --> SearchContact[GET /contacts/by-external-id]
    SearchContact --> HasContact{Contact exists?}

    HasContact -- No 404 --> CreateContact[POST /contacts]
    CreateContact --> AddPhone[POST /contacts/{id}/phones]
    AddPhone --> Created[Status: created]

    HasContact -- Yes 200 --> Compare{Name or company changed?}
    Compare -- Yes --> Patch[PATCH /contacts/{id}]
    Compare -- No --> PhoneCheck
    Patch --> PhoneCheck{Phone number differs?}

    PhoneCheck -- Yes --> ReplacePhone[DELETE old + POST new phone]
    PhoneCheck -- No --> AnyChange{Any field updated?}
    ReplacePhone --> Updated[Status: updated]

    AnyChange -- Yes --> Updated
    AnyChange -- No --> Unchanged[Status: unchanged]

    Created --> Next([Next record])
    Updated --> Next
    Unchanged --> Next
```

The input file (`crm_data.json`) contains records from your CRM. Each record maps to one contact and optionally one company:

```json
[
  {
    "customerId": "CUSTOMER-CAN-001",
    "firstName": "Can",
    "lastName": "Kaya",
    "phone": "+447700900123",
    "companyId": "4202"
  },
  {
    "customerId": "CUSTOMER-AYSE-002",
    "firstName": "Ayse",
    "lastName": "Y.",
    "phone": "+447700900456",
    "companyId": "4202"
  }
]
```

## The full example

The complete C# console application is in `submissions/06-kisi-firma-senkronu/Hipcall.ContactSync/`. Here is the core upsert logic for a single contact:

```csharp
var getResp = await client.GetAsync($"contacts/by-external-id/{record.CustomerId}");
if (getResp.IsSuccessStatusCode)
{
    // Contact exists. Compare fields and PATCH if anything changed.
    var body = await getResp.Content.ReadFromJsonAsync<HipcallResponse<Contact>>();
    var contact = body?.Data ?? throw new Exception("Empty contact response.");
    contactId = contact.Id;

    var patchReq = new Dictionary<string, object>();
    if (contact.FirstName != record.FirstName)
        patchReq["first_name"] = record.FirstName ?? "";
    if (contact.LastName != record.LastName)
        patchReq["last_name"] = record.LastName ?? "";
    if (companyId.HasValue && contact.Company?.Id != companyId.Value)
        patchReq["company_id"] = companyId.Value;

    if (patchReq.Count > 0)
    {
        // Phones and emails are NOT included here. PATCH rejects them.
        await client.PatchAsJsonAsync($"contacts/{contactId}", patchReq);
        status = "updated";
    }
}
else if (getResp.StatusCode == HttpStatusCode.NotFound)
{
    // Contact does not exist. Create it.
    var postReq = new Dictionary<string, object>
    {
        ["first_name"] = record.FirstName ?? "",
        ["last_name"] = record.LastName ?? "",
        ["external_id"] = record.CustomerId ?? ""
    };
    var postResp = await client.PostAsJsonAsync("contacts", postReq);
    // ...
    status = "created";
}
```

Phone numbers are handled separately through the sub-resource endpoint:

```csharp
if (!phoneExists)
{
    var phoneReq = new
    {
        phones = new[] { new { country = "GB", number = targetPhone } }
    };
    await client.PostAsJsonAsync($"contacts/{contactId}/phones", phoneReq);
}
```

Each record prints its result. If a record throws an exception, the sync catches it, logs the error, and continues with the next record:

```text
--- Hipcall Contact & Company Sync starting (6 records) ---

Processing: [CUSTOMER-CAN-001] Can Kaya
   [=] No changes (record is current)
Status: unchanged

Processing: [CUSTOMER-MEHMET-003] Mehmetii Demir
   [+] Fields updated: Name ('Mehmeti' -> 'Mehmetii')
Status: updated

Processing: [CUSTOMER-ERROR-004]  Invalid Record
ERROR (CUSTOMER-ERROR-004): Contact creation error: {"errors":{"first_name":["String length is smaller than minLength: 1"]}}

================ SUMMARY ================
Summary: 0 created, 1 updated, 4 unchanged, 1 error.
=========================================
```

Running the same data a second time produces no creates and no updates:

```text
Summary: 0 created, 0 updated, 5 unchanged, 1 error.
```

Record count before the first run: 21. After the first run: 25. After the second run: 25. No duplicates.

## When it fails

| Status code | Error body | Cause | Fix |
|---|---|---|---|
| `422` | `{"errors":{"external_id":["has already been taken"]}}` | You tried to create a contact with an `external_id` that already exists. | Query by `external_id` first and update instead. |
| `422` | `{"errors":{"phones":["Unexpected field: phones"]}}` | You included `phones` in a `PATCH` request body. | Use `POST /contacts/{id}/phones` to add numbers. |
| `422` | `{"errors":{"phones":["Contact already has 6 phone numbers"]}}` | The contact reached the six-phone limit. | Remove an old number before adding a new one. |
| `422` | `{"errors":{"first_name":["String length is smaller than minLength: 1"]}}` | `first_name` was empty or missing. | Validate before sending. `first_name` is required and must have at least one character. |
| `422` | `{"errors":{"custom_fields":{"tier":["can't be blank"]}}}` | A required custom field was not provided. | Include all required custom fields in the body, or remove the requirement in the panel. |
| `404` | `{"errors":{"detail":"Not Found"}}` | The `external_id` does not match any record. | This is expected during upsert. Create the record. |

## Next steps

- Add email synchronization using the same sub-resource pattern (`POST /contacts/{id}/emails`).
- Schedule the sync to run on a timer (for example, every hour) and verify that repeated runs produce zero duplicates.
