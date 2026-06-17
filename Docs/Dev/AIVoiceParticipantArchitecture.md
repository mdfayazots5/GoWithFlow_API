# AI Voice Participant — Production Design (2026-06-17)

Status: **PHASE 0 DELIVERED (migrations written) — 2026-06-17.** Phase-wise build plan below. Each
phase ships and is verified before the next begins. On-device (APK IV2201) verification is mandatory
before any voice/TTS phase is signed off (§5a) — a green build is never acceptance here.

**OPS EXECUTED (2026-06-17):** Phase 17 migrations applied to **production Supabase** (verified live:
`tblsession` AI columns, `tblsessionmember.isai`, reserved AI user `userid=13` mobile `AI_PARTICIPANT`,
functions `uspinsertaisessionmember` + `uspsetsessionaiconfig`). `npx cap sync android` done — TTS
plugin registered natively. Frontend production AOT build (`vite build`) green; backend `dotnet build`
0/0. **SQL Server parity files NOT applied** (no local SQL Server target; the app runs on PostgreSQL).
**Remaining = user's manual step:** rebuild + install the APK on IV2201 and test (device verification,
§5a). Backend just needs to be running against the same Supabase DB.

Phase 0 artifacts:
`Backend/GoWithFlow.Infrastructure/Migrations/SqlServer/AddAIVoiceParticipant_Phase17.sql`,
`Backend/GoWithFlow.Infrastructure/Migrations/PostgreSQL/AddAIVoiceParticipant_Phase17.sql`.
Decisions applied: 4 nullable AI config columns on `tblSession` (not a child table); reserved system
user resolved by sentinel `MobileNumber = 'AI_PARTICIPANT'` (not a hard-coded id). **OPS PENDING:**
run both migrations against their databases — not applied/verified in the build environment.

Authoring lenses: Product Manager · Solution Architect · Backend Engineer · Database Architect ·
Frontend & Mobile Engineer · Voice & Speech Engineer · Security Architect · QA Architect.

Once a phase reaches stable contract level, its flow contract is mirrored into `ProjectOverview.md`
(Detailed Flow Capture format). This file is the build plan / decision record; `ProjectOverview.md`
remains the system brain.

---

## 1. Problem statement

Today, interview and other multi-role session categories require **at least two human participants**.
A session with `< 2` active members auto-abandons (`uspUpdateSessionMemberLeft`, migration 28). If no
second person joins (e.g. no interviewer), the session cannot proceed.

**Goal:** an **AI Voice Participant** that fills the missing participant role(s) so a candidate can
practice solo — across **all** categories, not just interviews:

| Category | Human role | AI reads role(s) |
|---|---|---|
| Mock Interview | Candidate | Interviewer |
| Grammar Drill | Speaker A | Speaker B |
| Fluency Drill | Speaker A | Speaker B |
| Vocabulary Sprint | Learner | Tutor |
| Repractice Round | Learner | Coach |
| Roleplay | one role | all other roles |

The AI does **not** generate its own answers (not ChatGPT/Siri/Alexa). It only **reads aloud the
predefined scripted lines** for the slot(s) it holds, then hands the turn back to the candidate.

---

## 2. Why this fits the existing engine (no new orchestration needed)

Every session already runs off a **script** whose lines (`tblUtterance`) each carry a `SpeakerLabel`,
and a session has **members** each holding a **slot** (`tblSessionMember.SlotName`) derived from those
labels. The turn engine advances by matching the next line's `SpeakerLabel` → the member holding that
`SlotName` (`ResolveNextTurnAsync`, case-insensitive trim match).

Therefore the AI is simply a **non-human member occupying a slot.** The turn engine is unchanged. The
only new behavior: when the active member is an AI slot, the candidate's client **reads the line via
on-device TTS and auto-advances the turn** instead of recording + scoring. This works identically for
every category because every category's script is multi-speaker with defined slots.

This also removes the "needs 2 people" limitation: an AI member counts toward active membership, so a
solo + AI session is a valid 2-member session.

---

## 3. Approved decisions (2026-06-17)

1. **TTS engine = on-device.** Capacitor on-device text-to-speech. Offline, no server cost, no API
   secrets, never starves the recognizer, aligns with the offline-first voice constitution. Voice
   options limited to device-provided voices (Male/Female per language). Design the config so a cloud
   voice provider could be added later for premium personas **without** breaking the contract.
2. **Activation = chosen at session creation.** An "Enable AI Voice Participant" toggle on the
   create-session form. When on, AI members occupy all non-host slots immediately and the candidate
   can start solo right away — no waiting, no lobby timers.
3. **Role coverage = all non-human slots.** Candidate takes one role; AI reads every other role. A
   true solo session is possible in any category, including multi-role Roleplay.
4. **v1 config = Voice (Male/Female) + Speaking speed + Question delay.** Language is **not** a manual
   field — AI TTS language follows the script's hint/target language resolved through the existing
   `buildLanguageCandidates()` fallback chain. Professional/Friendly/Custom personas are deferred
   (they need cloud TTS).
5. **AI turns are never scored.** No `tblVoiceAnalysis` row for an AI turn. Scoring, summary, re-read,
   and listener feedback remain human-turn only and untouched.
6. **The mic rule is preserved.** TTS is speaker output, not a mic consumer; AI turns and human turns
   are sequential. The recognizer stays **idle** during an AI turn and is started only on the human's
   turn — so "the recognizer owns the mic" is never violated and no echo is introduced.

### Defaults applied unless overridden
- AI member is **auto-ready** (`IsReady = 1`); host can start once their own slot is ready.
- If the **host (candidate) leaves**, the session abandons as today — an AI-only session never lingers.
- AI member name = reserved system display name (e.g. "AI Voice Participant"); avatar = a stock AI avatar.

---

## 4. Data model

### 4.1 `tblSessionMember` — add AI flag
- `IsAi BIT NOT NULL DEFAULT(0)` — distinguishes AI-held slots from human members. AI members are
  inserted at create time for every non-host slot when AI is enabled, with `IsReady = 1`, `IsAi = 1`.

### 4.2 Reserved system user
- One `tblUser` row "AI Voice Participant" (well-known UserId, e.g. seeded). AI member rows point their
  `UserId` FK at this row, so `ActiveMemberId`, name, and avatar resolution keep working with **no
  nullable-FK churn** across the existing SPs. One reserved user can back multiple AI slots (the slot
  uniqueness is `SessionId + SlotIndex`, not `UserId`).
- **Rationale (Architect):** chosen over making `tblSessionMember.UserId` nullable, which would ripple
  through `uspInsertSessionMember`, the turn-state FKs, and every member-join query.

### 4.3 AI config on `tblSession` (4 nullable columns)
- `AiEnabled BIT NULL`
- `AiVoiceGender NVARCHAR(8) NULL` — `Male` | `Female`
- `AiSpeechRate DECIMAL(3,2) NULL` — TTS rate multiplier (e.g. 0.75 / 1.00 / 1.25)
- `AiQuestionDelaySec INT NULL` — pause after candidate finishes before AI reads the next line
- **Alternative flagged for Database Architect review:** a child `tblSessionAiConfig` table if SQL
  format review prefers normalization over 4 nullable columns. Columns chosen for v1 simplicity.

Provider parity: every schema change ships in **both** SQL Server and PostgreSQL migrations.

---

## 5. API / SignalR contract changes

### 5.1 `POST /api/sessions` (Create Session) — extend request DTO
Add to `CreateSessionRequestDto`:
- `AiEnabled` (bool, optional, default false)
- `AiVoiceGender` (string, optional) — required when `AiEnabled`; `Male` | `Female`
- `AiSpeechRate` (decimal, optional) — required when `AiEnabled`; whitelist e.g. `[0.75, 1.00, 1.25]`
- `AiQuestionDelaySec` (int, optional) — required when `AiEnabled`; whitelist e.g. `[0, 1, 2, 3, 5]`

When `AiEnabled`, the existing create transaction also inserts AI members for **all non-host slots**
(auto-ready, `IsAi = 1`). `MaxMembers` / distinct-speaker-label validation is unchanged.

### 5.2 New: advance an AI turn
- REST: `POST /api/turns/{sessionId}/advance-ai`
- Hub: `AdvanceAiTurn(sessionId, turnIndex)` on `/hubs/live-session`
- Callable by any active **human** member when the current active member `IsAi`. Backend verifies the
  active slot is AI and `turnIndex` matches, then runs the existing atomic `CompleteAndAdvanceTurnAsync`.
  **No `tblVoiceAnalysis` row is written.** Broadcasts the same `TURN_SHIFT` event.
- **Why a dedicated path:** today's rule is "only the active speaker can shift" (`MemberId == userId`).
  The candidate is not the AI member, so they cannot shift the AI's turn through `CompleteTurn`. This
  adds a safe, explicit path rather than weakening that rule for human turns.

### 5.3 Surface the AI flag on turn state
- `GET /api/turns/{sessionId}/current` response and the `TURN_SHIFT` broadcast → add `isAi` (bool) on
  the active member so clients know to **narrate** (TTS + auto-advance) vs **record** (recognizer).

All new/changed endpoints, DTOs, and SignalR events are documented in `ProjectOverview.md` in the same
phase that ships them (undocumented contract change = drift = QA gate block).

---

## 6. Client behavior (Angular 19 + Capacitor, gated on `SessionCapabilitiesService`)

### 6.1 Create-session form
- "Enable AI Voice Participant" toggle. When on, reveal: Voice (Male/Female), Speaking speed
  (slow/normal/fast → rate), Question delay (seconds). Values sent in `POST /api/sessions`.

### 6.2 Session room — AI turn handling
When `activeMember.isAi` is true:
1. **Do not start the recognizer.** Keep the mic idle.
2. Speak the current utterance via on-device TTS at `AiSpeechRate` in the resolved language.
3. Wait `AiQuestionDelaySec`.
4. Call `AdvanceAiTurn(sessionId, turnIndex)`.

When `activeMember.isAi` is false → existing human flow (recognizer + scoring + `CompleteTurn`).

All of the above is gated on `SessionCapabilitiesService`; degrade gracefully if TTS is unavailable
(surface a clear message, never silently stall the turn).

---

## 7. What stays the same
Scoring, completion summary, re-read, listener feedback — human-turn only, untouched. AI members
simply have no voice-analysis rows. The lobby/ready/start/leave/abandon flow is unchanged except that
AI members are auto-ready and count as active.

---

## 8. Phase-wise build plan

Each phase is independently buildable and verifiable. A phase is "done" only when its gate passes and
its contract is mirrored into `ProjectOverview.md`.

### Phase 0 — Schema & seed (Database Architect) — ✅ migrations written (ops apply pending)
- Add `tblSessionMember.IsAi`; add 4 AI config columns to `tblSession`; seed reserved AI system user.
- Migrations for **both** SQL Server and PostgreSQL. No behavior change yet. Both idempotent.
- **Done when:** migrations apply cleanly on both providers; existing sessions unaffected.
- **Status:** SQL files written + `ProjectOverview.md` schema updated. Applying them to the live
  PostgreSQL (Supabase) + local SQL Server is an ops step not runnable in the build env — UNVERIFIED
  until applied. Reserved-user open item resolved (sentinel mobile lookup).

### Phase 1 — Create Session with AI members (Backend Engineer) — ✅ built (build green; DB apply + runtime test pending)
- Extend `CreateSessionRequestDto` + validation (whitelists, required-when-enabled).
- In the create transaction, when `AiEnabled`, insert AI members for all non-host slots (auto-ready).
- **Done when:** creating an AI-enabled session yields a valid lobby with AI member(s) present; build
  green; contract documented.
- **Delivered:** DTO + `CreateSessionRequestValidator.When(AiEnabled)` (gender/rate/delay whitelists);
  `Session`/`SessionMember` entities + EF config (`AiSpeechRate` precision 3,2; `AiVoiceGender` len 8;
  `IsAi`); `ISessionRepository.CreateSessionAsync` now takes `aiMembers` and inserts config + AI members
  in the same transaction via additive SPs `uspInsertAiSessionMember` + `uspSetSessionAiConfig` (both
  providers, in `*_Phase17_Procs.sql`); `SessionService` resolves the reserved AI user by sentinel
  mobile and fills all non-host slots. `dotnet build` = 0 errors/0 warnings. `ProjectOverview.md`
  Create Session contract updated (incl. correction of pre-existing MaxMembers/SessionMode source drift).
- **Pending:** apply Phase 17 + Phase 17_Procs migrations; runtime test that an AI-enabled solo session
  creates a 2-member lobby (host + AI, both ready) — not run in build env.

### Phase 2 — Advance-AI turn path (Backend Engineer + Architect) — ✅ built (build green; runtime test pending)
- Add `POST /api/turns/{sessionId}/advance-ai` + hub `AdvanceAiTurn`; verify active slot `IsAi`; reuse
  `CompleteAndAdvanceTurnAsync`; no voice-analysis write; broadcast `TURN_SHIFT`.
- Add `isAi` to current-turn response + `TURN_SHIFT` payload.
- **Done when:** an AI turn can be advanced by the candidate's client without scoring; turn engine
  bounds/end-of-script behavior matches existing rules; contract documented.
- **Delivered:** `TurnStateResponseDto.IsAi` (computed by slot-match EXISTS on `tblSessionMember.IsAi`);
  `ILiveSessionService.AdvanceAiTurnAsync`; brick-prevention resolve→advance core extracted to the
  shared `AdvanceFromCurrentTurnAsync` (used by both `ShiftTurnAsync` and the new method, so they can't
  diverge); `AdvanceAiTurnRequestDto`; `LiveSessionController` `POST advance-ai`; hub `AdvanceAiTurn`
  (mirrors `CompleteTurn` completion/error handling) + shared `BroadcastTurnShiftAsync` now emits `isAi`
  on every `TURN_SHIFT`. `dotnet build` = 0/0. `ProjectOverview.md` + `ModuleIndex.md` updated.
- **Pending:** runtime test (needs Phase 17 migrations applied + a live AI session): AI turn advances
  with no score row, end-of-script auto-completes, human turns still score normally.

### Phase 3 — Create-session UI (Frontend & Mobile + UX) — ✅ built + typechecked; ⚠️ RENDER-UNVERIFIED
- AI toggle + Voice/Speed/Delay controls on the create form; send new fields.
- **Done when:** rendered UI verified (run/verify/screenshot or user confirmation), not just compiled
  (§5a). Mobile design standards obeyed (font caps, no horizontal-scroll).
- **Delivered:** `create-session.component.ts` — AI toggle card (switch) + 3 selects (Voice/Speed/Delay)
  shown when enabled; `aiEnabled/aiVoiceGender/aiSpeechRate/aiQuestionDelay` form controls; payload sends
  `aiEnabled/aiVoiceGender/aiSpeechRate/aiQuestionDelaySec` only when on; AI sessions skip the invite
  screen and route straight to the lobby. `tsc --noEmit` clean. Fonts within mobile caps (≤13px), no
  horizontal scroll (3-col grid of selects).
- **Pending (§5a — blocks sign-off):** RENDERED verification — confirm the toggle reveals the controls,
  layout/fonts look right on a phone width, and an AI session creates + lands in the lobby. NOT yet done
  (no rendered check in this env). **Status: UNVERIFIED — needs visual check.**

### Phase 4 — Session-room AI narration (Voice & Speech + Frontend) — ✅ built + typechecked; ⚠️ ON-DEVICE UNVERIFIED
- On `activeMember.isAi`: skip recognizer, TTS the utterance at configured rate/language, wait delay,
  call `AdvanceAiTurn`. Gate on `SessionCapabilitiesService`; graceful degrade.
- **Done when:** verified **on-device on APK IV2201** end-to-end (candidate hears the AI read each
  scripted line, then takes their turn and is scored normally), recognizer never starved. Until then:
  **UNVERIFIED — needs device check.**
- **Delivered:** installed `@capacitor-community/text-to-speech@8.0.2` (Cap 8 parity); new
  `TtsService` (output-only, gender best-effort, never throws); backend surfaces AI config on the
  turn DTO (joined from `tblSession`); `TurnState.isAi` + AI config on the frontend model;
  `session-room.updateState` triggers `maybeNarrateAiTurn` (once-per-turn guard) → TTS → delay →
  `ws.emit('AdvanceAiTurn', sessionId, turnIndex)`; recognizer suppression is automatic (human is a
  listener on AI turns, so `SpeakerScreenComponent` isn't rendered); TTS/timers cleaned on
  `SESSION_ENDED` + `ngOnDestroy`. Backend `dotnet build` 0/0; frontend `tsc --noEmit` clean.
- **OPS (blocks device test):** `npx cap sync android` to register the TTS plugin in the native
  project, then rebuild + install the APK on IV2201.
- **Pending (§5a — blocks sign-off):** ON-DEVICE verification on IV2201 — AI reads each line aloud,
  human turn still records + scores, recognizer never starved, end-of-script auto-completes.
  **Status: UNVERIFIED — needs device check.** Also note multi-human double-audio limitation (above).

### Phase 5 — Cross-category validation & docs (QA Architect)
- Validate solo + AI across all six categories incl. multi-role Roleplay; confirm summary excludes AI;
  confirm host-leave abandon still fires.
- Mirror all stable flow contracts into `ProjectOverview.md`; update `ModuleIndex.md` if navigation
  changes (new endpoints/columns).

---

## 9. Risk register

| Risk | Owner | Mitigation |
|---|---|---|
| TTS audio bleeds into a live mic (echo) | Voice & Speech | Recognizer kept idle during AI turn; turns are sequential — never concurrent. |
| Device lacks the configured TTS voice/language | Voice & Speech | Resolve via existing language fallback chain; fall back to default device voice; never stall the turn. |
| Candidate can't advance AI turn ("only active speaker can shift") | Backend/Architect | Dedicated `AdvanceAiTurn` path validated against `IsAi`, not weakening the human rule. |
| Nullable-FK ripple from a fake AI member | Architect | Reserved system user backs the FK; no schema churn in turn-state/member SPs. |
| Summary/scoring polluted by AI member | QA | No `tblVoiceAnalysis` for AI turns; summary already aggregates per member. |
| "Compiles but doesn't work on APK" (voice) | QA veto | On-device verification mandatory before Phase 4 sign-off (§5a). |

---

## 10. Open items / [VERIFY]
- Exact Capacitor TTS plugin choice and its voice/locale enumeration API — `[VERIFY]` at Phase 4 start.
- `AiSpeechRate` / `AiQuestionDelaySec` whitelists — proposed values above; confirm during Phase 1.
- Columns-on-`tblSession` vs `tblSessionAiConfig` table — Database Architect to confirm at Phase 0.
- Reserved AI user's well-known id / seed strategy per environment — confirm at Phase 0.
