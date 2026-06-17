# Session Room — UX Audit & Redesign Plan (2026-06-06)

Status: **ALL PHASES (0–5) IMPLEMENTED & build-verified 2026-06-06.**
Scope: the live Session Room only (`Frontend/src/app/modules/live-session/*`) —
`session-room`, `speaker-screen`, `listener-screen`, `voice-recorder`, and the
`SessionPreferencesService` / `VoiceBroadcastService` it depends on.

Perspective applied silently: Product Manager, Full-Stack Dev, UX Architect, Mobile Experience.

---

## 1. Components inspected (source of truth for this audit)

| File | Role |
|---|---|
| `session-room/session-room.component.ts` | Shell: top bar, settings panel, presence toasts, alert banner, routing speaker/listener |
| `speaker-screen/speaker-screen.component.{ts,html}` | Speaker turn: utterance, hint, recorder, feedback, Done/Try Again/Skip |
| `listener-screen/listener-screen.component.ts` | Listener turn: speaker identity, utterance card, quick feedback |
| `voice/voice-recorder/voice-recorder.component.html` | Mic button + waveform + interim transcript + volume bar |
| `core/services/session-preferences.service.ts` | 3 global localStorage prefs |
| `core/services/voice-broadcast.service.ts` | WebRTC peer audio — **hard-disabled on native** |
| `core/services/audio-archive.service.ts` | Per-turn clip capture/upload |

---

## 2. Current UX problems

1. **No device adaptation.** The entire room is locked to `max-w-[480px]` centered
   (`session-room.component.ts:167`). On tablet/desktop it is a narrow column floating
   in a dark void; there is **no tablet or desktop layout at all**.
2. **No visual hierarchy.** Almost every text node is
   `text-[11px] font-black uppercase italic tracking-widest`. Everything shouts equally,
   so nothing leads. The result reads as "technical/loud", not friendly, and is hard to scan.
3. **The most important element is not dominant.** For a speaker, the utterance they must
   read should own the screen. Instead it competes with the grammar tag, progress bar,
   hint toggle, pronunciation note, and skip button — all styled at similar weight.
4. **Non-functional features shown on mobile (the reported bug).**
   - "Hear Speaker's Voice" toggle is rendered on every platform, but
     `VoiceBroadcastService.startBroadcast()` returns early on
     `Capacitor.isNativePlatform()` (`voice-broadcast.service.ts:45-48`). On the APK the
     toggle does **nothing**.
   - The listener "Live Audio" badge (`listener-screen.component.ts:61-67`) can never
     light up on native.
5. **Misleading UI.** The listener sound-wave bars (`listener-screen.component.ts:71-79`)
   are a pure CSS animation, not driven by real audio. Combined with the pulsing rings and
   the recorder waveform, the UI implies live streaming that does not exist on mobile.
6. **Settings are not role-aware.** A *listener* sees "Auto-Start Microphone" and
   "Auto Submit on Stop" — both meaningless to someone who never records. A *speaker on
   native* sees a broadcast toggle that is dead.
7. **Settings panel fights the content.** It opens as a `min(40vh, 260px)` dropdown
   (`session-room.component.ts:102-104`) that pushes/overlaps the live turn.
8. **Scroll / primary-action risk.** In the speaker feedback phase, feedback cards +
   auto-submit countdown + Done + Try Again + Skip all stack inside the single scroll
   container. On short screens the primary "Done Speaking" can fall below the fold. Top
   bar + alert banner + settings each eat vertical space and squeeze the live area.
9. **Off-brand `confirm()` leave dialog** (`session-room.component.ts:542`) — already
   flagged generally in `MobileDesignAnalysis.md`.
10. **Listener cognitive load.** Progress + avatar + 2 pulse rings + fake wave + live
    badge + utterance card + re-read banner + tag flash + Good/Needs Work + "Your Turn Is
    Next" = ~8 simultaneous zones for a passive role. "Your Turn Is Next" shows always,
    even when it is not true.

---

## 3. First-time user journey analysis

- **Entry:** from lobby → "Synchronizing session..." spinner. No orientation about what
  the room is or what to do.
- **Speaker:** must infer that the big text is what to say, that the mic may auto-start
  (depends on a pref they have not seen), that a score will appear, and that they must tap
  Done. Five+ controls compete for attention. No first-run hint.
- **Listener:** sees another person's turn. Unclear what is expected of them; the
  Good/Needs Work buttons are unlabeled in purpose; "Your Turn Is Next" is shown
  unconditionally and is often wrong.

---

## 4. Features to keep / remove / hide

### Keep — essential during a live turn
- **Speaker:** utterance text (made dominant), grammar/context tag (small), mic control +
  recording state, live transcript + volume feedback, score feedback, **Done Speaking**
  (primary), **Try Again** (only when re-reads remain), **Skip** (tertiary), **Hint**
  (collapsed), turn progress, leave.
- **Listener:** who is speaking (name + avatar), what they are reading, turn progress,
  quick feedback (Good / Needs Work) on performance turns only, leave.
- **Both:** re-read notice, presence toasts, speaker-left alert, a minimal settings entry.

### Remove or hide
- **Native:** remove "Hear Speaker's Voice" toggle, the listener WebRTC request path, and
  the "Live Audio" badge entirely (the underlying feature is intentionally off — §7).
- Remove (or gate behind real audio) the **fake animated sound-wave bars** on the listener.
- Hide **Auto-Start Microphone** + **Auto Submit on Stop** for listeners (speaker-only).
- If a role/platform has **zero** applicable settings, hide the gear button entirely.
- De-emphasise the "Live" pill + timer into one compact status chip.
- Replace `confirm()` leave with an in-app confirmation sheet.

---

## 5. Layout architecture (all devices)

Replace the single capped column with a **3-zone CSS grid** owned by `session-room`:

```
┌───────────────────────────────┐  header  (auto height, never scrolls)
├───────────────────────────────┤
│            STAGE              │  stage   (1fr, the ONLY scroll region)
│   (speaker / listener body)   │
├───────────────────────────────┤
│           ACTION DOCK        │  dock    (auto, pinned, safe-area aware)
└───────────────────────────────┘
grid-template-rows: auto 1fr auto;  height: 100dvh;
```

The primary action (Done Speaking / mic / Good-Needs-Work) lives in the **dock**, so it is
**always visible regardless of stage scroll** — this structurally fixes problem #8.

### Mobile (< 768px)
- Single column, full-bleed (already a `fullBleedRoute`).
- **Speaker stage:** tag (tiny) → utterance (hero, largest readable) → hint (collapsed) →
  recorder. Dock: Done Speaking primary; Try Again / Skip secondary inline.
- **Listener stage:** speaker identity (one subtle "speaking" pulse only — drop the second
  ring + fake wave) → utterance card. Dock: Good / Needs Work (performance turns only).
- Settings: bottom sheet, role/platform filtered, 0–2 toggles.

### Tablet (768–1023px)
- Two panes: **left** stage (utterance + recorder, or speaker identity), **right** rail
  (turn progress detail, hint, presence/feedback). Larger type. Dock becomes a bottom bar
  spanning both panes.

### Desktop (≥ 1024px)
- Centered stage `max-w` ~720–860px with optional right rail; generous spacing.
- Cap utterance line length for readability (do not stretch full width).
- Web-only voice broadcast (if verified working) belongs **here**, never on mobile.

### Responsive strategy
- Breakpoints reuse documented tokens: `xxs 360`, `sm 640`, `md/tablet 768`, `lg/desktop 1024`.
- One source of truth for capability: a small **`SessionCapabilities`** service exposing
  `canBroadcastVoice`, `isSpeaker`, `isListener`, `platform`. UI and settings both read it,
  ending the scattered `Capacitor.isNativePlatform()` checks that exist today.

---

## 6. Settings simplification plan

Drive the panel from **role × platform**:

| Context | Settings shown |
|---|---|
| Speaker — native (APK) | Auto-Start Mic, Auto Submit |
| Speaker — web | Auto-Start Mic, Auto Submit (broadcast is automatic, no toggle) |
| Listener — native | **none → hide the gear** |
| Listener — web | Hear Speaker's Voice (only if web broadcast is verified working) |

Personal audio-archive consent stays out of the live room (it is a host/lobby concern —
`AudioArchiveService`, already correct).

---

## 7. Speaker / voice issue — root cause

**Not a WebRTC bug. It is a UI-gating + documentation failure.**

- `VoiceBroadcastService.startBroadcast()` returns early when
  `Capacitor.isNativePlatform()` is true (`voice-broadcast.service.ts:45-48`), and
  `handleBroadcastStarted()` additionally requires the `listenVoiceBroadcast` pref
  (`:96-100`).
- **Why it is disabled on native (by design):** on the APK the microphone is owned
  exclusively by the native Google `SpeechRecognizer` used for pronunciation scoring.
  Android cannot reliably share one mic between the WebView's `getUserMedia` (WebRTC
  capture) and the recognizer; they contend and the recognizer intermittently gets no
  audio → "No speech detected". Scoring is the core feature, so peer audio is sacrificed.
- **The defect:** the "Hear Speaker's Voice" toggle and the "Live Audio" badge are still
  rendered on native, so users enable a feature that can never function — exactly the
  reported complaint.
- **Fix:** gate the toggle and badge on `canBroadcastVoice` (false on native). Keep the
  recognizer's exclusive mic access. No change to scoring.

---

## 8. Implementation plan (phased, production-ready)

**Phase 0 — Capability source of truth** ✅ DONE
- Added `SessionCapabilitiesService` (`canBroadcastVoice = !Capacitor.isNativePlatform()`).
  `VoiceBroadcastService` + `session-room` settings now read it.

**Phase 1 — Hide non-functional / misleading UI (highest ROI, low risk)** ✅ DONE
- Gate "Hear Speaker's Voice" toggle + "Live Audio" badge on `canBroadcastVoice`.
- Bind fake listener sound-wave bars to real audio (`isReceivingAudio()`).
- (Adjusted) Auto-Start Mic / Auto Submit kept for all participants — turns rotate, so
  everyone speaks; only the platform-broken broadcast control was hidden.

**Phase 2 — 3-zone shell + sticky dock** ✅ DONE
- Shell already runs header (fixed) / stage (`flex-1 min-h-0 overflow-y-auto`, the only
  scroll region) / actions. Added a `.action-dock` (`position: sticky; bottom: 0` with a
  safe-area-aware bottom gradient) in both `speaker-screen` and `listener-screen`, so the
  primary action (Done Speaking / Skip / Done Reading / Good-Needs-Work) is always reachable
  while feedback content scrolls behind it. Structurally fixes the below-fold bug.

**Phase 3 — Hierarchy & typography** ✅ DONE
- Listener simplified: fake sound-wave gated to real audio (Phase 1), misleading "Your Turn
  Is Next" footer removed (no next-speaker data exists; redundant with "Speaking Now" +
  orientation hint). New copy (orientation hint, leave sheet) uses readable 13px, not the
  shouty 11px-black-uppercase. Speaker `questionFontSize` clamp left intact — it is listed
  as "do not regress" in the Mobile Design Standards and already makes the utterance the hero.

**Phase 4 — Responsive** ✅ DONE
- Stage container unlocked from the hard `max-w-[480px]`: now
  `max-w-[480px] md:max-w-[680px] lg:max-w-[760px]`, `px-4 md:px-6`, `pt-3 md:pt-5` — a
  centered, breathing stage on tablet/desktop instead of a thin column in a void. (A split
  two-pane rail was evaluated and deliberately skipped: the room is a single-focus task —
  read aloud / listen — so a wider centered stage reads better than splitting it.)

**Phase 5 — Polish** ✅ DONE
- `confirm()` leave → in-app confirmation sheet (`showLeaveConfirm`, Stay / Leave).
- Settings → bottom-sheet overlay (grabber + backdrop + close) that floats above content
  instead of pushing it down.
- First-run orientation hint (`showOrientation`, once per device via
  `gwf_session_room_seen`), role-aware copy for speaker vs listener.

---

## 9. Notes on known drift prevented
- Platform checks for voice broadcast are currently duplicated (service guard + per-pref
  guard + listener badge) and can drift from the settings UI that exposes the toggle.
  Centralising in `SessionCapabilities` makes "shown" and "works" derive from one value.
