# Member 1 — Project Presentation Notes

**Role:** Team Lead — Azure Connection, Project Setup & Deployment Infrastructure

> This document is longer than the other four on purpose. As team lead you're
> expected to know the whole project end-to-end, not just your own slice —
> so this file contains the full project story (useful for the "whole
> project" presentation in a week), your own hands-on part in detail, AND a
> short summary of what each teammate built (so you can introduce their
> sections, or step in if someone can't present). Their own .md files have
> the full depth on their parts — this is just enough for you to speak to it.

---

## 1. What this project actually is (30-second version)

A company gets phone-call transcripts (in English or Armenian) with no
speaker labels and no structure — just raw text. We built a system that:
1. Takes that raw text,
2. Figures out who was talking when (Agent vs Caller),
3. Pulls out personal information mentioned in it (name, address, phone,
   email, SSN) using a real Azure AI service,
4. Returns all of that as clean structured data,
5. And gives a human a web page to paste text in and see the result.

That's it. Everything else in this document is "how."

---

## 2. The whole pipeline, end to end

```
Browser (React app on Netlify)
   │  user pastes transcript, clicks Analyze
   ▼
POST /api/transcript/analyze  (our ASP.NET Core API, on Render)
   │
   ├─► SpeakerRoleService   — splits text into Agent/Caller turns   (Member 3)
   │
   └─► TranscriptAnalysisService — asks Azure to find PII, maps it  (Member 2)
            │
            ▼
       Azure AI Language Service (Microsoft's cloud, does the actual
       "find the name/address/phone/email/SSN in this text" work)
   │
   ▼
JSON response: { conversation: [...], extractedAttributes: {...} }
   │
   ▼
Browser renders chat bubbles + an attributes card
```

Two separate deployed things, talking over the internet:
- **Frontend** (the website): `https://stellular-travesseiro-c7c11b.netlify.app`
- **Backend** (the API): `https://azure-transcript-analysis.onrender.com`

---

## 3. My part — Azure connection & project setup

I set up the foundation everyone else built on:

- Created the **Azure AI Language resource** (this is the actual Azure cloud
  service that does PII detection — a "Cognitive Services" resource named
  `transcript-language-service`).
- Set up the **ASP.NET Core Web API project** structure (Controllers/,
  Services/, Models/, Program.cs) that the rest of the team built inside.
- Wrote **`Services/AzureLanguageService.cs`** — the *only* class in the
  whole backend allowed to talk to Azure directly. Everyone else calls
  Azure indirectly through this class (via its `IAzureLanguageService`
  interface), so if we ever swap Azure for another provider, this is the
  only file that changes.
  - Uses the official `Azure.AI.TextAnalytics` SDK's `TextAnalyticsClient`.
  - Reads the endpoint + key from configuration (never hardcoded — see
    "Secrets" below).
  - **Chunking**: Azure's PII API only accepts 5,120 characters per
    document and 5 documents per request, but our API promises callers up
    to 50,000 characters. So this class silently splits long transcripts
    into ≤5,000-character pieces (cutting only at line breaks, never
    mid-sentence), sends them in batches, and merges all the results back
    into one list. Callers never notice this happens.
  - **Resilience**: during live testing we saw one Azure request just hang
    for 2+ minutes with no response. I added a 20-second timeout with 2
    retries (exponential backoff) so a stuck request now fails fast into
    our existing error handling instead of hanging the whole app.
- Registered every service for **Dependency Injection** in `Program.cs`
  (explained below) so the whole app wires together automatically.

## 4. My part — deploying the whole thing publicly

This turned into the most interesting story in the project, so it's worth
telling in the presentation. Short version: **we tried the "obvious" Azure
option first, hit a wall that had nothing to do with our code, and pivoted
to a simpler stack.**

**Attempt 1 — Azure App Service (didn't work, and here's exactly why):**
Azure account setup gives you $200 free credit as a "Free Trial"
subscription. What isn't obvious: Free Trial subscriptions have a
`spendingLimit` flag that hard-blocks **any** VM-backed compute service
(App Service, VMs, Container Apps — anything that needs a virtual machine
under the hood) at **zero quota**, regardless of tier or region. We
diagnosed this precisely:
```
$ az rest --method get --url ".../subscriptions/<id>?api-version=2021-01-01"
quotaId: FreeTrial_2014-09-01
spendingLimit: On
```
Quota increase requests don't fix it either — Azure's own quota page says
so explicitly ("quota need not be requested for this SKU"). The only real
fix is removing the spending limit (adding a payment card, which does
**not** charge you — it just lifts the block; you still use the $200
credit first). We chose not to do that and pivoted instead.

**Attempt 2 — Render + Netlify (this is what's actually live):**
- **Render** hosts the backend. It builds a **Docker container** from our
  code and runs it — no VM quota concept at all, free tier available.
- **Netlify** hosts the frontend as static files with a CDN — genuinely
  free, drag-and-drop simple.

What I built to make this work:
- **`Dockerfile`** (repo root) — a two-stage build: one stage compiles the
  C# code with the full .NET SDK, the second stage copies just the
  compiled output into a much smaller "runtime-only" image. This is
  standard practice — you don't ship your compiler with your app.
- **`.dockerignore`** — keeps irrelevant files (node_modules, bin/obj, git
  history) out of what gets sent to Docker, so builds are fast.
- **Configurable CORS** in `Program.cs` — locally the API accepts requests
  from any origin (easy dev), but in production it only accepts requests
  from our real Netlify URL, set via an `AllowedOrigins` environment
  variable. Verified with real cross-origin tests: a request pretending to
  come from a random site gets silently rejected by the browser; a request
  from our real frontend URL is allowed.
- **HTTPS redirect fix** — only runs locally now. Render already handles
  HTTPS itself at its own edge before traffic reaches our container;
  redirecting again inside the app would have caused an infinite redirect
  loop.
- Verified the whole thing **before** deploying, by running the published
  build locally with the exact same settings Render would use (Production
  mode, secrets via environment variables, custom port) — caught nothing
  broken, but this is good practice: test production configuration before
  it's actually public.

---

## 5. Technologies, explained (you need to know all of these)

### Azure AI Language Service (a.k.a. Cognitive Services)
Microsoft's pre-built AI, reachable as a web API. You send it text, it
sends back structured findings (in our case: "this text contains a person
named X at this position, with 92% confidence"). We don't train any AI
ourselves — we're a *consumer* of Microsoft's already-trained model. Key
research finding: it supports Armenian and English both.

### API / endpoint / API key
An "endpoint" is just a URL your code sends requests to. An "API key" is
like a password proving you're allowed to use that URL — it must **never**
appear in code you commit to git, only in a secrets/config store.

### NuGet & SDKs
NuGet is .NET's package manager (like an app store for code libraries). An
"SDK" (`Azure.AI.TextAnalytics` here) is a library someone wrote that
wraps the raw web API in convenient C# method calls, so we call
`client.RecognizePiiEntitiesBatchAsync(...)` instead of hand-building HTTP
requests ourselves.

### ASP.NET Core Web API
The framework our whole backend is built on — Microsoft's toolkit for
building an API. It handles turning incoming HTTP requests into C# method
calls, and C# return values back into HTTP responses (usually JSON).

### Dependency Injection (DI)
Instead of a class creating its own dependencies directly, they're handed
to it from outside (usually via the constructor). E.g. `TranscriptController`
doesn't build an `AzureLanguageService` itself — it just says "I need
something that implements `ITranscriptAnalysisService`," and `Program.cs`
decides what actual object to hand it. This is *why* our automated tests
can swap in a "fake Azure" without touching a single line of production
code (see Member 5's part).

### Docker & containers
A "container" packages your app plus everything it needs to run (runtime,
libraries, config) into one portable unit that runs identically anywhere.
A "Dockerfile" is the recipe for building that package. This is what let
us skip Azure App Service entirely — any host that runs Docker containers
works, including Render.

### PaaS (Platform as a Service)
A hosting model where you give the platform your code/container and it
handles servers, scaling, and networking for you. Render and Netlify are
both PaaS — as opposed to Azure App Service or a raw VM, which require you
to manage more of the infrastructure yourself (and, in our case, hit a
quota wall we didn't cause).

### CORS (Cross-Origin Resource Sharing)
A browser security rule: a website running at domain A cannot call an API
at domain B unless that API explicitly says "requests from domain A are
allowed." Our backend explicitly allows only our real Netlify domain —
this is what stops some *other* random website from quietly using our
Azure-backed API for free.

### HTTPS / TLS termination
HTTPS encrypts traffic between browser and server. On platforms like
Render, the encryption is handled at the platform's "edge" (their own
servers) — our container only ever sees plain HTTP internally. That's why
our app's own HTTPS-redirect logic had to be turned off in production.

### Environment variables vs. secrets vs. config files
- `appsettings.json` — checked into git, only ever holds placeholders.
- `dotnet user-secrets` — real values, stored outside the project folder,
  used only for local development.
- **Environment variables** (set in the Render dashboard) — how the real
  Azure key reaches the app once it's actually deployed. Same principle
  as user-secrets: real values live outside of git, always.

### Cloud subscriptions & quotas
A "quota" is a cap on how much of a resource (like virtual machines) your
account can use. Azure's Free Trial has a **spending limit** flag that
zeroes out compute quota specifically to prevent accidental billing —
this was the real blocker, not anything wrong with our setup or code.

### Git & GitHub
Version control — tracks every change to the code over time, lets
multiple people work on different parts without overwriting each other
(each teammate's work landed via their own branch and pull request — see
`NAVIGATION.txt` for the branch names), and is what both Render and our
own history are built on.

---

## 6. What each teammate built (for your reference — see their .md for depth)

- **Member 2 — PII extraction (+ Attributes display in the frontend).**
  Takes what Azure found and maps it into our 5 fields (name, address,
  SSN, phone, email), including a real bug fix: Azure sometimes reports a
  spoken-alone SSN as a phone number, so Member 2 added a pattern check to
  reclassify it correctly. On the frontend, built the card that displays
  these 5 fields.

- **Member 3 — Speaker role detection (+ Conversation display in the
  frontend).** Figures out who's talking — uses explicit labels
  (`Agent:`, `Caller:`, Armenian equivalents) when present, and falls back
  to alternating Agent/Caller when the transcript has no labels at all,
  including a fix so an unlabeled line correctly continues the *previous*
  speaker instead of resetting. On the frontend, built the chat-bubble
  view of the conversation.

- **Member 4 — API endpoint (+ Forms & API calls in the frontend).**
  Built the actual `POST /api/transcript/analyze` endpoint: validation,
  wiring the other services together, and turning Azure/network failures
  into proper HTTP error codes. On the frontend, built the transcript
  submission form and the code that actually calls our API.

- **Member 5 — Testing & documentation (+ code quality tooling on the
  frontend, + verifying our live deployment).** Wrote the 11 automated
  tests that run without needing real Azure access, plus the docs
  describing what was tested against the *real* Azure service. On the
  frontend, set up the tools that auto-check code quality before every
  commit. Also ran the checks that confirmed our live, deployed site
  actually works correctly end-to-end.

---

## 7. Presentation notes

- **Tomorrow (frontend-only session):** each teammate can present their
  own frontend slice from their own .md; you can open with section 1–2 of
  this document (what the project is + the pipeline diagram) as context.
- **Next week (whole project):** this document, read start to finish,
  covers everything you personally need to speak to. Hand off to each
  teammate for their own section using the summaries in section 6, or
  present solo using their .md files as your speaking notes.
