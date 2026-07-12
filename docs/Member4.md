# Member 4 — Project Presentation Notes

**Role:** API Endpoint & Validation (Backend) & Forms + API Integration (Frontend)

---

## 1. What my part does, in one sentence

I built **the actual door the outside world knocks on** — the API endpoint
that receives a transcript and sends back the analyzed result — and, on
the frontend, **the form that lets a human actually use it** plus the code
that calls it.

---

## 2. Backend: `Controllers/TranscriptController.cs`

This is `POST /api/transcript/analyze` — the single entry point into the
whole backend. Its job, in order:

1. **Validate the request** and reject anything invalid with a clear
   `400 Bad Request` and a human-readable message:
   - `transcriptText` must not be empty/whitespace.
   - `transcriptText` must be at most 50,000 characters.
   - `language` must be exactly `"en"` or `"hy"` (case-insensitive) —
     anything else (like `"fr"`) is rejected.
2. **Call the other two services** — `SpeakerRoleService.SplitConversation`
   (Member 3) and `TranscriptAnalysisService.ExtractAttributes` (Member 2)
   — and combine their results into one response object.
3. **Handle failures from Azure gracefully**, translating technical
   exceptions into proper HTTP status codes instead of crashing:
   | What went wrong | Status returned |
   |---|---|
   | Azure key is invalid | `401 Unauthorized` |
   | Azure is down / unreachable / times out | `503 Service Unavailable` |
   | Anything else unexpected | `500 Internal Server Error` |
4. Return `200 OK` with the full `{ conversation, extractedAttributes }`
   JSON body on success.

Every error message is written to be genuinely useful to whoever's calling
the API (including our own frontend team) — never a raw stack trace, never
leaking the Azure key or endpoint.

---

## 3. Frontend: forms & talking to the backend

- **`src/components/TranscriptForm.tsx`** — the actual "paste your
  transcript here" form. Validates the input **before** it's ever sent,
  using the *same rules* as the backend (required, max 50,000 characters,
  language must be `en`/`hy`) — so users get instant feedback instead of
  waiting for a round-trip to the server just to be told they left it
  blank.
- **`src/api/client.ts`** — one shared connection setup for every backend
  call, plus a helper that turns whatever error comes back (network
  failure, 400, 401, 503...) into a message the UI can actually show a
  person.
- **`src/hooks/useTranscripts.ts`** — the `useAnalyze()` hook: submits the
  form data to the backend and saves the result to the browsing history
  once it succeeds.

---

## 4. Technologies I used, explained

### REST API basics (verbs, status codes, JSON)
"REST" is just a common convention for web APIs: you send an HTTP
*verb* (`POST` here, for "do something / create something") to a *URL*
(`/api/transcript/analyze`), with data as **JSON** (a text format for
structured data — basically nested key/value pairs), and get back a
*status code* (200 = success, 400 = you sent something wrong, 500 = we
broke) plus more JSON.

### ASP.NET Core routing, controllers & model binding
`[Route("api/transcript")]` and `[HttpPost("analyze")]` are attributes
that tell ASP.NET Core "requests to this exact URL and verb should run
this exact method." **Model binding** is what automatically turns the
incoming JSON body into a real C# `TranscriptRequest` object before my
code even runs — I never manually parse JSON myself.

### Exception handling (`try`/`catch`)
C#'s way of saying "attempt this code, and if something specific goes
wrong, run this other code instead of crashing." `catch (Azure.RequestFailedException ex) when (ex.Status == 401)`
is doubly specific: it only catches *that* exception type, and only when
its status is exactly 401 — different Azure failures get routed to
different HTTP responses.

### Swagger / OpenAPI
An auto-generated, interactive web page (`/swagger` when running locally)
that documents every endpoint and lets you send test requests straight
from the browser — useful for manually trying the API without writing any
code, and it's built automatically from our controller code, not
hand-written separately.

### React Hook Form
Manages what the user has typed and whether each field currently has an
error, without re-rendering the whole page on every keystroke.

### Yup (schema validation)
Describes *what counts as valid data* as a declarative "schema" (e.g.
"`transcriptText` is a required string, max 50,000 characters") rather
than writing manual if-statements — React Hook Form then checks user
input against that schema automatically.

### Axios
The actual library that sends the HTTP request over the network and
hands back the response (or the error) — the "phone call" itself, versus
React Query (below) which decides *when* to make that call.

### React Query (`useMutation`)
Manages the lifecycle of "send data that changes something" — tracks
whether the request is currently in flight (`isPending`), whether it
succeeded (`isSuccess`) or failed (`isError`), and runs a follow-up action
(saving to history) automatically on success.

### Environment variables (`VITE_API_URL`)
The frontend needs to know *where* the backend actually lives — but that
address is different locally (`http://localhost:5266`) versus in
production (`https://azure-transcript-analysis.onrender.com`).
`VITE_API_URL` is read from `frontend/.env.production` at build time, so
the exact same source code produces a build that talks to the right
backend in each environment.

---

## 5. Key files

- `Controllers/TranscriptController.cs` — the endpoint itself
- `Models/TranscriptRequest.cs`, `Models/TranscriptResponse.cs` — request/response shapes
- `frontend/src/components/TranscriptForm.tsx` — the input form
- `frontend/src/api/client.ts` — the Axios setup + error handling
- `frontend/src/hooks/useTranscripts.ts` — the `useAnalyze` mutation hook
- `docs/ApiDocumentation.md` — the full request/response contract
- Tests: `Tests/TranscriptAnalysisTests.cs` — validation tests (empty,
  too-long, unsupported language)
