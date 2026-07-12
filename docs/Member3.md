# Member 3 — Project Presentation Notes

**Role:** Speaker Role Detection (Backend) & Conversation Display (Frontend)

---

## 1. What my part does, in one sentence

Given raw transcript text with **no speaker labels at all**, I figure out
**who was probably talking in each line** (Agent or Caller), and then
**display that conversation as a chat-style view** on the website.

This matters because the task's whole premise is: real transcripts often
arrive as plain text with no indication of who said what. My part is what
turns a wall of text into an actual back-and-forth conversation.

---

## 2. Backend: `Services/SpeakerRoleService.cs`

The logic has two modes, chosen automatically based on the input:

**Mode 1 — the transcript already has labels.**
If any line starts with a recognizable label — `Agent:`, `Caller:`,
`Operator:`, `Customer:`, `Client:`, `Speaker 1:`, `Speaker 2:`, or the
Armenian equivalents `Օպերատոր:` / `Հաճախորդ:` — we trust those labels
directly, strip the label off, and use it as the role for that line.
`Agent:`/`Operator:`/`Օպերատոր:` all map to `"Agent"`; `Caller:`/
`Customer:`/`Client:`/`Հաճախորդ:` all map to `"Caller"` — different
wording, same two roles.

**Mode 2 — no labels anywhere.**
We can't know who's who for certain, so we fall back to a simple rule: the
first line is the Agent, and roles **alternate** from there (Agent,
Caller, Agent, Caller...). This matches how a real call transcript
usually reads — one line per turn, back and forth.

### A real bug we found and fixed: mixed-label transcripts

What about a transcript where **only some** lines have labels? E.g.:
```
Agent: Hello, how can I help you?
Caller: My name is John Smith.
I live at 123 Main Street.
Agent: Thank you.
```
That third line has no label. Originally, any unlabeled line defaulted to
`"Speaker 1"` — clearly wrong here, since it's obviously the Caller
continuing their previous turn. **Fix:** an unlabeled line now continues
whichever speaker's turn came right before it, so that third line
correctly stays `"Caller"`. Covered by an automated test
(`Analyze_MixedLabelConversation_UnlabeledLineContinuesPreviousSpeaker`).

---

## 3. Frontend: `src/components/ConversationView.tsx`

This renders the `conversation` array as chat bubbles:
- **Agent** turns are aligned left, **Caller** turns aligned right (a
  familiar messaging-app layout).
- Each bubble shows the role label above it in small grey text.
- Caller bubbles are filled in blue; Agent bubbles are white with a
  border — makes it instantly obvious who's speaking without reading the
  label every time.

---

## 4. Technologies I used, explained

### String parsing / pattern matching
"Parsing" text just means reading it and pulling structure out of it. My
code splits the whole transcript on line breaks, then checks each line's
*start* against a list of known label prefixes (`StartsWith(...)`) — no
AI involved for this part, just straightforward text matching. Detecting
"no labels at all" is done once up front (`hasExplicitLabels`), which
decides whether we go into Mode 1 or Mode 2 for the *whole* transcript.

### Fallback heuristics
A "heuristic" is a reasonable rule-of-thumb guess, used when we can't be
certain. Alternating Agent/Caller when there are no labels is exactly
that — it's not guaranteed correct (a caller might speak twice in a row
in the real recording), but it's the best guess possible from text alone,
and it's what the task itself asked for as the expected fallback.

### Emotion (`@emotion/styled`) — CSS-in-JS
Ant Design doesn't include a chat-bubble component, so this part needed
custom styling. Emotion lets you write actual CSS *inside* a React
component file and attach it to a real HTML element:
```tsx
const Bubble = styled.div<{ right: boolean }>`
  background: ${(p) => (p.right ? '#2f54eb' : '#ffffff')};
`;
```
The `right` prop is just a boolean coming from our own code (is this a
Caller turn?) — Emotion lets the CSS itself change based on that prop, so
one `Bubble` definition handles both left- and right-aligned, differently
colored bubbles without needing two separate components.

---

## 5. Key files

- `Services/SpeakerRoleService.cs` — the label detection / alternating logic
- `Models/ConversationTurn.cs` — the `{ role, text }` shape per line
- `frontend/src/components/ConversationView.tsx` — the chat bubble display
- Tests: `Tests/TranscriptAnalysisTests.cs` — labeled conversation test,
  unlabeled/alternating test, mixed-label test, Armenian-labels test
