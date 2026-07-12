# Member 5 — Project Presentation Notes

**Role:** Testing & Documentation (Backend) & Code Quality Tooling (Frontend) & Deployment Verification

---

## 1. What my part does, in one sentence

I make sure **everyone else's claims are actually true** — that the code
works the way it's supposed to, in an automated and repeatable way — and I
write down what was tested and found, so the team (and anyone else) can
trust the results without re-checking everything by hand.

---

## 2. Backend: `Tests/TranscriptAnalysisTests.cs`

**11 automated tests**, run with one command: `dotnet test`. All 11 pass.

The key trick: these tests boot the **entire real API** in memory
(`WebApplicationFactory<Program>`) — real controller, real validation,
real speaker-role logic — but swap out the one class that talks to real
Azure (`IAzureLanguageService`) for a `FakeAzureLanguageService` that
returns exactly the entities the test asks for. This is only possible
*because* the rest of the team built everything behind interfaces and
used Dependency Injection (see Member 1's doc) — the fake and the real
thing are interchangeable from the app's point of view.

Why fake Azure instead of using the real thing in tests: it makes tests
**fast** (no network round-trip), **free** (no billing), **deterministic**
(same result every run, not dependent on Azure's mood that day), and
**runnable offline** (no internet or API key needed to run the test suite
at all — useful for anyone checking out the repo fresh).

What's actually covered:
- English transcript → correct name/address/phone/email/SSN extraction
- Armenian transcript → correct extraction *and* correct Agent/Caller
  role mapping from Armenian labels
- Missing attributes correctly come back as `null`, never guessed
- Empty transcript → `400`
- Unsupported language (`"fr"`) → `400`
- Transcript over 50,000 characters → `400` (boundary test)
- Labeled conversation → labels respected, stripped from the text
- Unlabeled conversation → alternates Agent/Caller correctly
- Mixed-label conversation → unlabeled line continues previous speaker
- The SSN-mislabeled-as-phone bug → correctly reclassified
- Long-text chunking → splits under Azure's limit, loses no content

## 3. Backend: `docs/TestResults.md` and `docs/ApiDocumentation.md`

Two different kinds of testing get written up separately, on purpose:
- **`TestResults.md`** documents both the 11 automated tests *and* what
  actually happened when we ran real transcripts (English and Armenian)
  against the **real** Azure service — including the real bugs we found
  (the SSN mislabeling, a transient Azure hang) and the fixes applied.
  Nothing in it is invented — every result is something that was actually
  observed.
- **`ApiDocumentation.md`** is written for someone who has *never seen the
  backend code* — it describes the endpoint, request/response shapes, and
  every error case, so the frontend team (or anyone else) could integrate
  with it without reading a single line of C#.

---

## 4. Frontend: code quality tooling

I set up three tools that catch problems *before* they're ever committed:

- **ESLint** — reads the code (without running it) and flags likely
  mistakes: unused variables, missing dependencies in a React hook, etc.
- **Prettier** — auto-formats code style (spacing, quotes) so the whole
  team's code looks consistent without anyone manually matching a style
  guide.
- **Husky + lint-staged** — a **git pre-commit hook**: every time anyone
  runs `git commit`, ESLint and Prettier automatically run on just the
  files being committed. If they can auto-fix something, they do, and the
  fix is included in the commit; if there's a real problem, the commit is
  blocked until it's fixed. This is the frontend's version of the same
  idea as the backend's automated tests: **catch problems automatically,
  before they become the team's problem.**

---

## 5. Deployment verification

Once the site was deployed live (Render for the backend, Netlify for the
frontend — see Member 1's doc for how), I ran the actual checks that prove
it works, the same way the automated tests prove the code works locally:

- Confirmed the live backend boots correctly in production mode and
  responds to real requests.
- Verified **CORS is actually locked down**, not just configured to look
  locked down: sent a request pretending to come from a random website
  (correctly rejected — no CORS headers came back at all) and a request
  from our real Netlify URL (correctly allowed).
- Ran a full request through the live public URLs end-to-end and
  confirmed the extracted attributes were correct.

---

## 6. Technologies I used, explained

### Unit testing vs. integration testing
A **unit test** checks one small piece of logic in isolation (e.g. "does
this exact input to the chunking function produce the right output?"). An
**integration test** checks that multiple pieces work correctly *together*
— our tests are mostly integration tests, because they go through the
real controller, real validation, and real service wiring, all at once.

### xUnit
The testing framework used to write and run the C# tests. `[Fact]` marks
a method as "this is a test, run it." `Assert.Equal(...)` / `Assert.Null(...)`
etc. are how a test states what it expects and fails loudly if reality
doesn't match.

### Mocking / fakes
A "fake" is a stand-in for a real dependency that behaves predictably for
testing purposes — our `FakeAzureLanguageService` never calls the real
Azure service; it just returns whatever entities a specific test tells it
to, every time, instantly.

### `WebApplicationFactory<T>`
A tool from ASP.NET Core specifically for integration tests — it spins up
your *entire real application* in memory (no separate server process
needed) and gives your test a working `HttpClient` to send real requests
to. `ConfigureTestServices` is how we swap in the fake Azure service
*after* the app's normal startup has already wired everything else up.

### ESLint & Prettier
See section 4 above — a "linter" (catches likely bugs) and a "formatter"
(enforces consistent style), respectively. Different jobs: Prettier
doesn't look for bugs, ESLint doesn't care about spacing.

### Git hooks (Husky)
Git lets you attach scripts to specific moments (like "right before a
commit is created"). Husky is the tool that installs such a script;
`lint-staged` makes it run tools only on the files actually being
committed, not the whole project every time (which would be slow).

### `curl` / manual API testing
A command-line tool for sending HTTP requests directly, without a
browser or any UI — this is how the deployment verification checks were
actually run: real `POST` requests to the real live URLs, checking the
real status codes and response bodies that came back.

---

## 7. Key files

- `Tests/TranscriptAnalysisTests.cs` — all 11 automated tests
- `docs/TestResults.md` — automated + live-Azure test results, findings, fixes
- `docs/ApiDocumentation.md` — the API contract for integrators
- `frontend/eslint.config.js`, `frontend/.prettierrc.json` — quality tool configs
- `.husky/pre-commit` — the git hook that runs them automatically
