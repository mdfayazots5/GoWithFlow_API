# Question & Answer Category — Phase-Wise Implementation Plan

> Status: **Phases 1–3 CODE COMPLETE (build-verified) — UNVERIFIED ON DEVICE.** Created 2026-06-18.
> Remaining to ship: on-device APK verification (IV2201) of the blind Q&A flow + report render; then Phase 1.b leftovers
> (Q&A prompt-template seed, ExcelTemplateStandard.md Q&A section). Phase 3 required no code — data routes by construction.
> Owner roles: Product Manager (Priya) + Chief Architect (Ravi) + Frontend/Mobile (Kenji) +
> Voice & Speech (Noor) + QA (Grace). Scope tier: **T3 (new category + new AI-driven flow)**.
> This file is the build contract for the feature. ProjectOverview.md / ModuleIndex.md are updated
> as each phase ships — this plan is the working tracker, NOT a replacement for the system brain.

---

## 1. Goal (confirmed with user)

Replace the `Repractice Round` **category** with a new **`Question & Answer`** category in which an
**AI speaker (Interviewer) asks questions aloud** and the **candidate answers by speaking** — like a
real interview. The candidate must **not see the question text or any model answer on screen**; they
listen, then explain the answer in their own words. Each spoken answer is captured, converted to text,
and routed into the **existing Session Report**. Built to help candidates (Hyderabad / Telangana /
Andhra Pradesh and similar) practise realistic interview communication, confidence, and technical
explanation.

## 2. Confirmed decisions (user verdicts — 2026-06-18)

| # | Decision | Verdict |
|---|---|---|
| 1 | Remove "Re-Practice" | **Category only.** Retire `Repractice Round` from selectable upload categories; keep existing tagged scripts readable (legacy alias). **Mistake Repractice MODULE stays fully intact.** |
| 2 | Answer capture model | **Per-question, turn by turn.** AI asks one question → candidate answers → recognizer captures that answer → next question. |
| 3 | Cloud AI scoring | **No new scoring build.** Q&A answers flow into the **existing Report module** as-is; admin uses the report's current evaluation/export path. |
| 4 | Blind-answer screen | **Hide question text + model answer; keep the candidate's own live transcript visible** (so they know the mic is working). |

## 3. Reused existing building blocks (no reinvention)

- **Phase 17 AI Voice Participant** — AI holds a session slot (`IsAi=1`), reads its scripted line via
  on-device TTS; config `AiVoiceGender` / `AiSpeechRate` / `AiQuestionDelaySec`; AI turns are not scored.
- **Facilitator Role Awareness** — facilitator turns (Interviewer) are read-only, performer turns
  (Candidate) are captured + scored. Q&A: Interviewer = AI/facilitator, Candidate = performer.
- **Recognizer + voice-analysis → Session Report** — existing per-turn transcript + report pipeline.
- **Category infrastructure** — upload validation, `MapCategoryToSessionMode`, `FacilitatorRoles`,
  Excel template standard, prompt-data, certificates, legacy-alias maps.

## 4. The single net-new rule

**Blind answer mode.** On a Q&A Candidate turn the Speaker screen must render a *Listen → Speak* state
that shows **neither the question text nor any model answer**, but **does** show the candidate's own
live transcript. Recommended derivation: from `Category = 'Question & Answer'` (automatic), not a
separate togglable field — to be confirmed at Phase 1 start.

Mic-ownership rule preserved: **AI narrates first, recognizer listens after — never concurrent.**

---

## 5. Phase-wise build plan

### Phase 1 — Category swap (Backend + DB) — ✅ DONE 2026-06-18 (build-verified; not device-verified)
- [x] Add `Question & Answer`; retire `Repractice Round` from selectable upload categories.
      → `SessionModeType.QuestionAnswer = 7` (Domain enum); `RepracticeRound = 6` kept for legacy data.
      → `ScriptService.ValidateUploadRequest`: removed `Repractice Round`/`Repetition`, added `Question & Answer`.
      → Admin UI `script-upload.component.ts`: `<select>` option + `categoryTemplateOptions` + static speaker/min/max/mandatory maps.
- [x] Q&A speaker labels: **Interviewer (AI/facilitator) / Candidate (performer)** — `ScriptRepository.GetPromptDataForCategoryAsync` tuple (`Interviewer / Candidate`, 16–40, E required).
- [x] `MapCategoryToSessionMode` → `Question & Answer` → `QuestionAnswer`; `Repractice Round`/`Repetition` mapping KEPT (legacy data loads). `MapSessionMode` adds `QuestionAnswer => "Question & Answer"`.
- [x] `FacilitatorRoles.IsFacilitator` → `Question & Answer` → `Interviewer` = facilitator (read-only, no score); Candidate = scored performer.
- [x] Frontend type/maps: `session.model.ts` union + `create-session.component.ts` `categoryModeMap`/`derivedMaxMembers` (had `?? cat`/`?? 2` fallbacks; added explicit Q&A entries).
- [x] Update `ProjectOverview.md` (category list, facilitator table, SessionMode derivation note) and `ModuleIndex.md` (Q&A keywords).
- [x] **Builds clean:** backend `dotnet build` 0 errors; frontend `tsc --noEmit` exit 0.
- [x] **Phase 1.b — Q&A prompt-template seed DRAFTED (2026-06-18), not yet applied to a DB:** canonical Q&A Claude prompt authored for BOTH providers (parity):
      → PostgreSQL: `Backend/Docs/PostgreSQLMigration/37_add_question_answer_prompt_template.sql` (`ON CONFLICT DO NOTHING`).
      → SQL Server: `Backend/Docs/SqlServerSeed/37_add_question_answer_prompt_template.sql` (idempotent `IF NOT EXISTS`; apostrophes doubled).
      → **APPLIED to PostgreSQL (active provider, Supabase) 2026-06-18** — inserted as `prompttemplateid = 7`, version 1, prompttext length 5487; verified by re-select. SQL Server script remains unapplied (run it only if SQL Server becomes the active provider). Retired `Repractice Round` seed row from migration 19 is intentionally left in place (harmless).
- [x] **Phase 1.b — ExcelTemplateStandard.md §6.7 Question & Answer DRAFTED (2026-06-18):** new section (purpose, blind-by-design note, session behaviour, metadata, speaker labels, column rules, content rules, 8-row sample); ToC + §5.3 category-code table + §7.1 note updated; `Repractice Round` marked legacy.
- **Gate:** Architecture + QA — **PASSED at build level.** No contract drift; legacy `Repractice Round` data still loads/derives. `HideScriptText` derivation deferred to Phase 2 (blind-answer UI).

### Phase 2 — AI-as-Interviewer + blind answer — ⚠️ CODE COMPLETE 2026-06-18, **UNVERIFIED ON DEVICE**
**Decisions (user, 2026-06-18):** AI auto-forced as Interviewer (no toggle); host always Candidate; question hidden on BOTH turns.
- [x] Q&A auto-forces AI on (`SessionService.CreateSessionAsync`: `aiEnabled = dto.AiEnabled || isQuestionAnswer`); host seated in first non-facilitator slot; AI fills every other slot (incl. Interviewer). Safe AI defaults when host gave none (Female / 1.00 / 2s).
- [x] `HideScriptText` flag added to `TurnStateResponseDto` (backend, set in `GetCurrentTurnAsync` from category) → frontend `TurnState.hideScriptText` (passes through `getCurrentTurn`; carried in optimistic TURN_SHIFT via spread).
- [x] **speaker-screen** (candidate answer turn): hides grammar tag + utterance text + hint when `hideScriptText`; shows "Answer in your own words" prompt; voice-recorder still runs (transcript shown, `expectedText` fed to engine but NEVER rendered).
- [x] **listener-screen** (AI Interviewer turn — candidate is listener): hides grammar tag + question text; shows "Listen — interviewer is asking your question".
- [x] Mic rule preserved: AI narrates via TTS (Phase 17), recognizer runs on the candidate's own turn — no concurrent mic added.
- [x] **Builds clean:** backend 0 warn/0 err; frontend `tsc --noEmit` exit 0.
- [ ] **MANDATORY BEFORE SIGN-OFF — on-device APK verification (IV2201):** (a) Q&A session auto-creates with AI=Interviewer/human=Candidate; (b) AI reads question via TTS, candidate hears it, NO question text on screen; (c) candidate answer turn shows recorder + own transcript, NO model answer; (d) transcript captured into report. Green build is NOT acceptance (§5a). **Currently UNVERIFIED — needs device check.**
- **Known limitation (for Phase 3):** scoring still runs the recognizer against the scripted model answer (`expectedText`); for freeform answers the pronunciation/fluency number is noisy. The meaningful evaluation is the report's existing AI path over the transcript — acceptable per decision #3 (reuse report as-is). Revisit if per-answer scoring needs to ignore `expectedText`.

### Phase 3 — Report — ✅ NO CODE NEEDED 2026-06-18 (data routes by construction; render-verify pending)
- [x] Confirmed by reading the render path: `LiveSessionRepository` (≈line 598) builds the review from **all** utterances incl. facilitator turns; `SessionReviewTurnDto` carries `IsFacilitatorTurn`, `EnglishText`, `TranscribedText`.
- [x] `session-review.component.ts` (≈line 84) renders every turn:
      → **Interviewer (facilitator) turn** shows `englishText` = the **question** (revealed in the report). ✅
      → **Candidate turn** shows `englishText` (model answer, for comparison) + `transcribedText` = the **actual spoken answer**. ✅
- [x] Scoreboard already excludes AI + facilitator (Phase 17 / Facilitator Role Awareness) — Q&A leaderboard = the candidate only.
- [x] **No new scoring code** — matches decision #3. Existing report's AI-evaluation/export path is the meaningful evaluation over the transcript.
- [ ] **Render-verify (folds into the Phase 2 on-device check):** open a completed Q&A session's review and confirm the question shows on Interviewer turns and the transcript shows on Candidate turns. UNVERIFIED until then.
- **Gate:** QA — PASSED at code level (data present + rendered by existing component). Visual confirmation pending with Phase 2 device check.

---

## 6. Verification gate (mandatory — §5a)

Phases 2–3 are voice/native + UI. A green `tsc`/build is **never** acceptance. Verify on the real APK
(device IV2201) using `Backend/Docs/Testing/` harness + test credentials before sign-off. If it cannot
be verified in-environment, report **UNVERIFIED — needs device check** — never emit a false COMPLETE.

## 7. Open items to confirm at build start

- `HideScriptText` derivation: category-derived (recommended) vs separate session field.
- Exact Q&A min/max row rules + Excel sample for the upload template.
- Whether Q&A forces single-candidate or allows multiple candidates per AI interviewer.

## 8. Risks / regression guards

- **Repractice ambiguity** — never touch the Mistake Repractice MODULE; only the category list.
- **Legacy data** — existing `Repractice Round` scripts must still load (alias kept).
- **Mic contention** — never add a second concurrent mic consumer during recognition.
- **Category fan-out** — adding/removing a category touches upload validation, SessionMode map,
  FacilitatorRoles, template standard, certificates, alias maps — change all, miss none.
