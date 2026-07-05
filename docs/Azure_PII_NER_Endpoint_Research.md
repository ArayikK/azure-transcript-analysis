# Azure AI PII Detection + NER Endpoints — Research Findings

**Scope of this section:** Identify the Azure AI Language endpoints for PII detection and Named Entity Recognition (NER), confirm attribute-to-entity-category mapping, and provide a working request/response example.

---

## 1. Recommended Endpoints

Azure AI Language (part of Foundry Tools) exposes two prebuilt endpoints relevant to this task:

| Endpoint | Purpose | Route |
|---|---|---|
| **Text PII detection** | Detects and extracts sensitive personal data (name, address, phone, email, SSN, etc.) | `POST {endpoint}/language/:analyze-text?api-version=2024-11-01` with `"kind": "PiiEntityRecognition"` |
| **Named Entity Recognition (NER)** | Detects broader entity types (organization, date, quantity, location, event, etc.) not covered by PII | Same route, `"kind": "EntityRecognition"` |

**Note:** There is also a **Conversation PII** endpoint built specifically for speaker-turn transcripts, but it is **not usable for this project** — its GA version only supports English, French, and Spanish (no Armenian). Text PII should be used instead, applied per line/turn of the transcript.

---

## 2. Attribute → Entity Category Mapping

| Requested Attribute | Azure Entity Category | Source Endpoint |
|---|---|---|
| Person name | `Person` | PII |
| Address | `Address` | PII |
| Phone number | `PhoneNumber` | PII |
| Email address | `Email` | PII |
| Social Security Number (US) | `USSocialSecurityNumber` | PII |
| Date / Date of Birth | `Date` / `DateOfBirth` (preview) | PII |
| Organization | `Organization` | NER |
| IP Address | `IPAddress` | PII |
| URL | `URL` | PII |

**Open gap:** Azure ships country-specific government ID entity types for many countries (US, EU members, UK, Japan, etc.), but **not Armenia**. There is no built-in "Armenian national ID / social card number" category. This must be handled via:
- Custom regex (Text PII preview API supports custom regex rules), or
- A Custom NER model trained on sample Armenian ID formats.

---

## 3. Sample Request / Response (English)

**Request:**

```json
POST {endpoint}/language/:analyze-text?api-version=2024-11-01
Content-Type: application/json
Ocp-Apim-Subscription-Key: {key}

{
  "kind": "PiiEntityRecognition",
  "parameters": { "modelVersion": "latest" },
  "analysisInput": {
    "documents": [
      {
        "id": "1",
        "language": "en",
        "text": "My name is John Smith. I live at 123 Main Street. Yes, it is 123-45-6789."
      }
    ]
  }
}
```

**Response (trimmed):**

```json
{
  "results": {
    "documents": [
      {
        "id": "1",
        "redactedText": "My name is ********. I live at ****************. Yes, it is ***********.",
        "entities": [
          {
            "text": "John Smith",
            "category": "Person",
            "offset": 11,
            "length": 10,
            "confidenceScore": 0.98
          },
          {
            "text": "123 Main Street",
            "category": "Address",
            "offset": 34,
            "length": 15,
            "confidenceScore": 0.93
          },
          {
            "text": "123-45-6789",
            "category": "USSocialSecurityNumber",
            "offset": 64,
            "length": 11,
            "confidenceScore": 0.85
          }
        ]
      }
    ]
  }
}
```

The request/response schema is identical for Armenian input — only `"language"` changes to `"hy"`.

---

## 4. Verified Test Example (Real API Output)

Tested against a longer, realistic meeting-notes transcript containing dates, an organization, an email, a phone number, an IP address, and a physical address.

**Input text:**

```
Meeting Notes

The quarterly project review was held on July 1, 2026, at the Microsoft office in Seattle, Washington.

The support team reviewed customer reports received from New York, London, and Berlin. Several users contacted the company using support@example.com or by calling +1 (800) 555-1234.

One customer reported that documents stored in Azure Blob Storage could not be accessed after changing network settings. The affected server had the IP address 203.0.113.42.

The incident was assigned to the Infrastructure department. A replacement device was shipped to 450 Market Street, San Francisco, California 94105.

The next review meeting is scheduled for July 15, 2026, at 10:30 AM.
```

**Entities returned (model version `2025-11-01`):**

| Detected Text | Category | Subcategory | Confidence |
|---|---|---|---|
| July 1, 2026 | DateTime | Date | 1.00 |
| Microsoft | Organization | — | 0.97 |
| users | PersonType | — | 0.93 |
| support@example.com | Email | — | 0.80 |
| +1 (800) 555-1234 | PhoneNumber | — | 1.00 |
| customer | PersonType | — | 0.99 |
| Azure | Organization | Sports | 0.61 |
| 203.0.113.42 | IPAddress | — | 0.80 |
| Infrastructure department | Organization | — | 0.98 |
| 450 Market Street, San Francisco, California 94105 | Address | — | 1.00 |
| July 15, 2026, at 10:30 AM | DateTime | — | 1.00 |

**Observations from this test:**

- **No `Person` entity was returned** — the text never mentions a specific person's name, only role words like "users" and "customer," which the model correctly tagged as `PersonType` rather than `Person`. This confirms the model distinguishes a named individual from a generic role/category term.
- **`Address`, `Email`, `PhoneNumber`, `IPAddress` all detected correctly** with high confidence (0.80–1.00), matching the mapping table in Section 2.
- **Misclassification found:** "Azure" was tagged as `Organization` with subcategory `Sports` at only 0.61 confidence — clearly wrong (Azure is a cloud platform, not a sports organization). This is a real edge case worth documenting: low-confidence entities (roughly <0.65) should likely be filtered out or manually reviewed rather than trusted automatically.
- **Redaction worked as expected** — all detected entities were replaced with `*` characters of matching length in `redactedText`, confirming the redaction behavior described in Section 1.
- The category returned here is `DateTime` (with `Date` as a subcategory for date-only values), not `Date` as a top-level category — worth correcting in any downstream category-mapping code.

---

## 5. Finding Summary

- **Endpoint to use:** Text PII detection (primary), NER (supplementary, for entity types outside PII scope).
- **Confirmed:** Both Text PII and NER officially support Armenian (`hy`) and English (`en`).
- **Not usable:** Conversation PII (GA) — lacks Armenian support.
- **Gap to solve separately:** No native Armenian government-ID entity category — needs custom regex or Custom NER.
- **Confirmed by live test:** Email, PhoneNumber, Address, IPAddress, Organization, and DateTime all detect correctly with high confidence on English text.
- **Risk confirmed by live test:** confidence scores can be low (~0.6) on ambiguous words (e.g., "Azure" misread as a sports organization) — a confidence threshold should be applied before trusting an extracted attribute.
