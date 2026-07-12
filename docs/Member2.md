# Member 2 — Project Presentation Notes

**Role:** PII Extraction (Backend) & Attributes Display (Frontend)

---

## 1. What my part does, in one sentence

Once Azure has looked at the transcript text, **I take Azure's raw findings
and turn them into the 5 clean fields the company actually asked for** —
name, address, Social Security Number, phone number, email — and then
**display those 5 fields nicely** on the website.

---

## 2. Backend: `Services/TranscriptAnalysisService.cs`

Azure doesn't hand us "name: John Smith" directly — it hands us a list of
"entities" it found in the text, each with a category (like `Person` or
`PhoneNumber`) and a confidence score (how sure Azure is, from 0 to 1). My
job is turning that list into our actual output shape.

The logic, step by step:
1. Call `IAzureLanguageService.AnalyzeText(...)` (Member 1's class) to get
   the list of entities Azure found.
2. Loop over every entity. Skip anything with confidence below `0.5` —
   Azure can be wrong on ambiguous words, so low-confidence hits are
   discarded rather than trusted.
3. Map Azure's categories onto our fields:
   | Azure category | Our field |
   |---|---|
   | `Person` | name |
   | `Address` | address |
   | `USSocialSecurityNumber` | socialSecurityNumber |
   | `PhoneNumber` | phoneNumber |
   | `Email` | email |
4. If the same category appears more than once (e.g. two phone numbers
   mentioned), **keep whichever one Azure is most confident about.**
5. Anything Azure never found for a given field stays `null` — we never
   guess or return an empty string, only `null` or a real value.

### A real bug we found and fixed

During live testing against actual Azure (not a fake), we found that when
a caller says an SSN *by itself* on its own line — e.g. the agent asks
"what's your SSN?" and the caller just replies "123-45-6789" — Azure
mislabels it as a `PhoneNumber`, not an SSN. It only gets the `Person` /
context words like "social security number" right when they're in the
*same sentence*. Since most real conversations have the question and
answer on separate lines, this bug would have hit *most* real transcripts.

**Fix:** every `PhoneNumber` entity is re-checked against the pattern
`XXX-XX-XXXX` (3 digits, dash, 2 digits, dash, 4 digits) — a grouping US
phone numbers never use. If it matches, we reclassify it as the SSN
instead. Covered by an automated test
(`Analyze_SsnClassifiedAsPhoneByAzure_IsReclassified`) and confirmed
against the real Azure service.

---

## 3. Frontend: `src/components/AttributesCard.tsx`

This is the card on the results screen showing all 5 extracted fields. It
takes the `ExtractedAttributes` object the backend returns and renders one
row per field:
- If a value is `null` (or empty), it shows a grey **"Not found"** tag.
- If a value is present, it's shown as **copyable text** — one click
  copies it, handy when someone needs to paste a phone number or email
  elsewhere.
- Each field has an icon (person, map pin, ID card, phone, envelope) so
  it's scannable at a glance.

---

## 4. Technologies I used, explained

### Azure PII entity categories & confidence scores
Azure's "PII detection" doesn't just say *where* sensitive text is — it
tags *what kind* of thing it thinks it found (`Person`, `Address`,
`PhoneNumber`, etc.), and how confident it is (0.0–1.0). Treating that
confidence number as a filter (discard anything under 0.5) is what keeps
obviously-wrong guesses out of our results.

### Regex (regular expressions)
A regex is a pattern used to match text — a mini search language. Our SSN
pattern `^\d{3}-\d{2}-\d{4}$` means: "exactly 3 digits, a dash, exactly 2
digits, a dash, exactly 4 digits, nothing else." This is what lets us
tell an SSN apart from a phone number purely by its shape.

### LINQ (C#'s data-query syntax)
Used to loop through and filter Azure's entity list expressively (e.g.
finding "the highest-confidence match in this category") instead of
writing manual loops with counters.

### TypeScript interfaces (shared shape between backend and frontend)
`ExtractedAttributes` is defined once in `frontend/src/types.ts` as a
TypeScript `interface` — it describes exactly what fields exist and their
types (`string | null` for each). This mirrors the backend's C# model
(`Models/ExtractedAttributes.cs`) field-for-field, so the frontend and
backend never disagree about the data's shape.

### Ant Design (antd) — `Descriptions`, `Card`, `Tag`
Ant Design is a library of ready-made UI components. I specifically used:
- **`Card`** — the bordered container with a title ("Extracted attributes").
- **`Descriptions`** — a label/value list layout (like a simple table),
  perfect for "Name: John Smith, Address: ..." style data.
- **`Tag`** — the small colored pill used for the "Not found" placeholder.
- **`Typography.Text copyable`** — makes a value one-click-copyable.

---

## 5. Key files

- `Services/TranscriptAnalysisService.cs` — the mapping/reclassification logic
- `Models/ExtractedAttributes.cs` — the C# shape of the 5 fields
- `frontend/src/components/AttributesCard.tsx` — the display component
- `frontend/src/types.ts` — the matching TypeScript shape
- Tests: `Tests/TranscriptAnalysisTests.cs` (English/Armenian extraction,
  null-attribute handling, SSN reclassification tests)
