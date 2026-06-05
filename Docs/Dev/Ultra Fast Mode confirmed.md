Ultra Fast Mode confirmed. Module identified: Phase 3 — Live Session Audio Archive (primary), with dependencies on Phase 10 R2 Storage, Backend Live Session Module, and Backend Admin Module. I read only those sections. I did not read source files — the documented contracts are complete enough to analyze the flow, and tblSessionRecording does not exist anywhere in the brain (grep-confirmed), which itself confirms the core gap.

Here is the full review.

Session Recording Architecture — Review & Production Design
1. Current Architecture (as-built, end-to-end)
There is no session-level recorder today. What looks like "recording" is actually two independent per-turn capture mechanisms, both keyed to a single speaker's single turn:

Mechanism A — Voice-Analysis audio	Mechanism B — Audio Archive (Phase 3.5)
Trigger	Speech recognition turn save	MediaRecorder parallel to SpeechRecognition per turn
Endpoint	POST /api/sessions/voice-analysis (AudioBase64 optional)	POST /api/users/audio-archive (multipart)
R2 key	sessions/{sessionId}/turns/{turnIndex}/{userId}.ogg	sessions/{sessionId}/turns/{turnIndex}/{userId}_{ts}.webm
DB	tblVoiceAnalysis.AudioStorageKey	tblAudioArchive (1 row per session+user+turn)
Scope	One active speaker, one turn	One active speaker, one turn
Consent	implicit	host flag tblSession.IsRecordingEnabled + localStorage
The admin "recordings" view (GET /api/admin/sessions/{sessionId}/recordings) reads tblAudioArchive and returns every (user × turn) clip as a separate row. A 4-participant, 20-turn session = up to ~40–80 objects. That is exactly the reported symptom: fragmented clips per user, per turn; no clean duration/created-time/participant rollup.

Why fragmentation is structural, not a bug
The app is a distributed, turn-based system: SignalR orchestrates turns, only one speaker is active at a time, and each speaker records on their own device via the browser/Capacitor MediaRecorder. There is no media server, no WebRTC audio transport between peers, no shared audio stream. Recording is inherently sliced per turn because that's the only moment a mic is open. "One file per session" cannot emerge from this model without a server-side consolidation stage that does not exist yet.

A second, important reality: in this design the only audio that exists is the sequence of spoken turns. Listeners aren't streaming audio; facilitator "Read Aloud" turns have no recording phase at all (documented). So "capture all participant audio" effectively means "capture every spoken turn, in order" — unless you want to add continuous open-mic streaming, which is a much larger system.

2. Architectural / storage / sync / audio implications
Architecture: Need a new session-recording lifecycle decoupled from per-turn capture — start on session start, stop on session end, plus an async server-side merge stage. Today nothing owns "the session's recording."
Storage: R2 gwf-audio is fine, but needs a final-artifact key distinct from segments, and a tblSessionRecording row (one per session). Current per-turn objects become intermediate inputs, not the deliverable.
Synchronization: Turn-based ordering gives us a free canonical timeline (server knows turn order + server-side timestamps). True simultaneous mixing would require device-clock alignment (skew correction) — avoidable if we concatenate by turn order.
Audio processing: Segments are heterogeneous — web emits Opus/WebM, Android may emit different codec/sample-rate. A consolidated file requires re-encode + normalize to one codec/container. Raw byte-concatenation of WebM/Opus is not reliably seekable or duration-correct. This needs ffmpeg.
Format: Final should be AAC in .m4a/MP4 (universal: Safari, Chrome, Android, admin <audio>), or MP3. Opus/WebM has Safari/iOS playback gaps — avoid for the admin-facing master.
3. Design options & tradeoffs
Option	What it is	Fits current app?	Effort	"All participant audio" fidelity
A — Turn-clip concatenation (RECOMMENDED)	Keep per-turn capture; on session end a server job orders all turn segments by (turnIndex, server-ts) and ffmpeg-concats + transcodes into one .m4a; write one tblSessionRecording row	✅ Native fit — turns never overlap, so concat = faithful conversation	Low–Med	Full (every spoken turn, in order)
B — Continuous per-participant capture + server mix	Each device records its mic continuously start→end; upload one track per participant; ffmpeg amix with per-track session-offset	⚠ Needs clock sync, chunked resumable upload, more storage/CPU	High	True overlap + ambient, but mostly silence/echo
C — Real-time SFU mixing (LiveKit/mediasoup/Janus)	Add a WebRTC media server that mixes live	❌ New infra, new transport, biggest rewrite	Very High	True live mix
Recommendation: Option A. In a turn-based language-drill app, participants speak sequentially by design — ordered concatenation reconstructs the complete session conversation as a single coherent file, with near-zero added infrastructure and no clock-sync risk. Options B/C add large cost/complexity to capture overlapping audio that, by the app's own rules, doesn't occur. (One caveat for A: facilitator "Read Aloud" turns aren't captured today — see open questions.)

4. Recommended target architecture (production design — Option A)
4.1 Lifecycle / state machine

Host enables "Record Session" (tblSession.IsRecordingEnabled = 1)
        │
   Session START ──► RecordingState = CAPTURING   (create tblSessionRecording row, Status=CAPTURING)
        │
   per turn: existing MediaRecorder clip ─► uploaded as SEGMENT (intermediate)
        │
   Session END (EndSession) ──► Status = PENDING_MERGE  (enqueue merge job, idempotent guard)
        │
   Background worker: download segments ─► ffmpeg concat+transcode ─► upload final .m4a
        │
        ├─ success ─► Status = READY (set StorageKey, DurationSecs, SizeBytes, CompletedAt)  ► optionally delete segments
        └─ failure ─► Status = FAILED (retry w/ backoff; keep segments for re-merge)
4.2 New DB table tblSessionRecording (one row per session)

RecordingId      BIGINT PK
SessionId        FK UNIQUE          -- enforces one recording per session
StorageKey       NVARCHAR(512) NULL -- final R2 key, set when READY
Status           NVARCHAR(20)       -- CAPTURING|PENDING_MERGE|PROCESSING|READY|FAILED
Format           NVARCHAR(8)        -- 'm4a'
DurationSecs     INT NULL
SizeBytes        BIGINT NULL
SegmentCount     INT NULL
ParticipantsJson NVARCHAR(MAX)      -- [{userId,name,turns}] denormalized for admin display
CreatedAt        DATETIME2          -- recording start (session start)
CompletedAt      DATETIME2 NULL     -- merge finished
ExpiresAt        DATETIME2 NULL      -- retention cleanup
4.3 Storage keys (via StorageKeyBuilder — never inline, per Phase 10 rule)

Segments (intermediate):  sessions/{sessionId}/segments/{turnIndex:000}_{userId}.webm
Final (deliverable):      sessions/{sessionId}/recording/session_{sessionId}.m4a
Add StorageKeyBuilder.SessionRecordingSegment(...) and StorageKeyBuilder.SessionRecordingFinal(...). Bucket stays gwf-audio (private; presigned access only — Phase 10 Key Rules 1–3 preserved).

4.4 Merge pipeline (audio engineering)
Worker lists segment objects (from DB segment rows = reliable order; not R2 listing).
Stream each from R2 to worker temp dir.
ffmpeg: decode each → resample to a common rate (48 kHz mono) → loudness-normalize (loudnorm) → concat filter → encode AAC ~96 kbps → .m4a with correct moov/faststart for streaming playback.
Probe duration; upload final; update row to READY; (optionally) delete segments.
Idempotency: merge guarded by Status transition — re-running on READY is a no-op.
Gap tolerance: missing/failed segment is skipped + logged; ordering uses DB turn index so a hole doesn't corrupt the timeline.
Where it runs: in-process .NET IHostedService background queue with bounded concurrency + retry/back-off (fits the current single-API deploy). ffmpeg via Xabe.FFmpeg or a shelled bundled binary. Clean upgrade path to a dedicated container/worker for scale.

4.5 API changes
POST /api/sessions/{sessionId}/recording/segment — formalize segment upload (or reuse Mechanism B internally), tagging segments to the session recording.
GET /api/admin/sessions/{sessionId}/recordings → return a single SessionRecordingDto (was a list): { recordingId, status, durationSecs, createdAt, completedAt, participants[], audioUrl(presigned 120-min, null until READY), sizeBytes }. Old list shape deprecated.
Hook merge enqueue into the existing EndSession path in LiveSessionService.
4.6 Admin UX
One row per session: Duration · Created · Participants · Status badge (Processing/Ready/Failed) · single play/seek/download control bound to the presigned .m4a. No more N-clip lists.

5. Synchronization, scalability, failure, migration
Sync: Server-side turn order + server receive-timestamps are the timeline source of truth — no device-clock alignment needed (the key advantage of Option A).
Scalability: bounded merge concurrency + queue; download→merge→upload uses temp disk (not RAM); R2 free egress keeps merge cheap; one final object per session collapses object count and admin query cost.
Failure/idempotency: Status guards single-merge; failed merges retain segments and retry; late uploads handled by a short post-EndSession grace window before merge fires.
Migration: new table + migration (SQL Server + PostgreSQL, matching Phase 10/Phase 15 patterns); EF lowercase-name discipline (avoid the audiostoragkey 14-vs-15-char drift); historic sessions keep tblAudioArchive clips and can be lazily back-merged or left as-is.
6. Assumptions, risks, tradeoffs
Assumptions

Turn-based "one speaker at a time" remains the model (no overlapping live audio requirement).
R2 gwf-audio + presigned-URL pattern (Phase 10) is the storage standard; no public buckets.
Admin "recording per session" is the deliverable; per-turn personal clips are an internal detail.
Risks

Heterogeneous segment codecs (web Opus vs Android) → mandatory ffmpeg re-encode; raw concat will break duration/seek.
ffmpeg in-process can spike CPU/RAM on a small instance → must be off-request-thread + concurrency-capped.
Dropped/failed segment uploads create silent gaps → need gap-tolerant merge + segment-upload reliability (ideally upload-before-CompleteTurn).
Facilitator "Read Aloud" turns are not captured today → a literal "all participant audio" promise is incomplete until those are recorded too.
Tradeoffs

Option A trades true ambient/overlap fidelity for massive simplicity and zero clock-sync risk — correct for a turn-based drill app, wrong for a free-conversation product.
Deleting segments post-merge saves storage but removes re-merge/debug ability → favor short-TTL retention over immediate delete.
7. Open decisions that gate implementation
These genuinely fork the build, so I'll capture them before writing any code.

AskUserQuestion
OUT
Your questions have been answered: "Which capture/merge model should the single session recording use?"="Turn-clip concatenation (Recommended)", "How should the new session recording relate to the existing personal 'Audio Archive' (opt-in per-turn clips users review themselves)?"="Keep both, share segments (Recommended)", "Should facilitator 'Read Aloud' turns (currently NOT recorded) be captured so the file is truly 'all participant audio'?"="Yes, add facilitator capture (Recommended)". You can now continue with these answers in mind.
All three locked to the recommended path:

Turn-clip concatenation — server stitches ordered turn segments into one file.
Keep both, share segments — personal Audio Archive stays; session recording is a separate consolidated artifact from the same captures.
Add facilitator capture — Read-Aloud turns now recorded so the master is truly complete.
This design is forward-looking (not yet built), so per the strict rules it does not belong in ProjectOverview.md (validated system state only). It belongs in a Dev design doc, matching the existing FlowAudit_ProductionReady.md / MobileDesignAnalysis.md convention. Let me confirm that folder and persist the production-ready design there.

Globpattern: "Backend/Docs/Dev/*.md"
Found 5 files
Write c:\Live\GoWithFlow\Backend\Docs\Dev\SessionRecordingArchitecture.md
173 lines
# Session Recording Architecture — Production Design (2026-06-04)

Status: **DESIGN — approved decisions locked, pending implementation.**
This is a Dev design artifact, NOT system brain. `ProjectOverview.md` is updated only
when the flow is built and validated.

Authoring lenses: Audio Engineer · Senior Full-Stack · Solution Architect · Product Manager.

---

## 1. Problem statement

Current behavior: "session recordings" are actually per-turn clips. The admin sees up to
~N(users) × N(turns) fragmented files per session with no clean duration / created-time /
participant rollup.

Expected behavior:
- Host enables "Record Session" → recording spans **session start → session completion**.
- Capture all participant audio across the whole session.
- Produce **one consolidated audio file per session**.
- Store one final file in R2 and link it to the session.
- Admin sees **one recording per session** with duration, created time, participants, playback.

---

## 2. Why fragmentation is structural (not a bug)

The app is distributed and **turn-based**: SignalR orchestrates turns, only one speaker is
active at a time, and each speaker records on their own device via `MediaRecorder`. There is
no media server, no peer audio transport, no shared stream. The only audio that exists is the
ordered sequence of spoken turns. "One file per session" therefore requires a new
**server-side consolidation stage** that does not exist today.

Two current per-turn mechanisms (both keyed to one speaker + one turn):
- **A — Voice-analysis audio:** `POST /api/sessions/voice-analysis` (`AudioBase64` optional) →
  `gwf-audio` key `sessions/{sessionId}/turns/{turnIndex}/{userId}.ogg` → `tblVoiceAnalysis.AudioStorageKey`.
- **B — Audio Archive (Phase 3.5):** `MediaRecorder` parallel to recognition →
  `POST /api/users/audio-archive` → `tblAudioArchive` (1 row per session+user+turn). Admin endpoint
  `GET /api/admin/sessions/{sessionId}/recordings` returns every clip = the fragmentation.

---

## 3. Approved decisions (2026-06-04)

1. **Capture/merge model = Turn-clip concatenation.** Keep per-turn capture; on session end a
   server job orders the segments and ffmpeg-concats + transcodes into one `.m4a`. Turns never
   overlap, so concatenation reconstructs the full conversation with no device-clock sync.
2. **Relationship to personal Audio Archive = Keep both, share segments.** The personal opt-in
   archive (per-user private, 90-day) remains. The host/admin session recording is a separate
   consolidated artifact derived from the same captured segments. No feature loss.
3. **Facilitator audio = Capture it.** Add `MediaRecorder` to facilitator "Read Aloud" turns
   (currently not recorded) so the master file is truly "all participant audio."

---

## 4. Target architecture

### 4.1 Lifecycle / state machine
```
Host enables Record Session (tblSession.IsRecordingEnabled = 1)
   Session START ─► tblSessionRecording row created, Status = CAPTURING
   per turn (incl. facilitator): MediaRecorder clip ─► uploaded as SEGMENT (intermediate)
   Session END (EndSession) ─► Status = PENDING_MERGE (enqueue merge, idempotent guard)
   Worker: download segments ─► ffmpeg normalize+concat+encode ─► upload final .m4a
        success ─► Status = READY (StorageKey, DurationSecs, SizeBytes, CompletedAt)
        failure ─► Status = FAILED (retry w/ backoff; keep segments for re-merge)
```
A short grace window after `EndSession` precedes the merge to absorb late segment uploads.

### 4.2 New DB table `tblSessionRecording` (one row per session)
```
RecordingId      BIGINT PK
SessionId        FK UNIQUE            -- enforces exactly one recording per session
StorageKey       NVARCHAR(512) NULL   -- final R2 key, set when READY
Status           NVARCHAR(20)         -- CAPTURING|PENDING_MERGE|PROCESSING|READY|FAILED
Format           NVARCHAR(8)          -- 'm4a'
DurationSecs     INT NULL
SizeBytes        BIGINT NULL
SegmentCount     INT NULL
ParticipantsJson NVARCHAR(MAX)        -- [{userId,name,turns}] denormalized for admin display
CreatedAt        DATETIME2            -- recording start (= session start)
CompletedAt      DATETIME2 NULL       -- merge finished
ExpiresAt        DATETIME2 NULL       -- retention cleanup
```
Migrations for SQL Server + PostgreSQL. EF lowercase-name discipline (avoid the Phase 10
`audiostoragkey` 14-vs-15-char drift) — verify live column names before writing raw SQL.

Optional `tblSessionRecordingSegment` (or reuse `tblAudioArchive` rows tagged to the session)
to hold the ordered, gap-tolerant segment manifest the merge worker reads (DB order, not R2 list).

### 4.3 R2 storage keys (bucket `gwf-audio`, private; presigned-only — Phase 10 rules preserved)
Add to `StorageKeyBuilder` (never build inline):
```
SessionRecordingSegment: sessions/{sessionId}/segments/{turnIndex:000}_{userId}.webm
SessionRecordingFinal:   sessions/{sessionId}/recording/session_{sessionId}.m4a
```

### 4.4 Merge pipeline (audio engineering)
1. Read ordered segment manifest from DB (reliable order; tolerate missing turns).
2. Stream each segment from R2 to worker temp dir.
3. ffmpeg: decode each → resample 48 kHz mono → `loudnorm` → concat filter → AAC ~96 kbps →
   `.m4a` with faststart (`moov` at front) for seekable streaming playback.
4. Probe duration; upload final; set Status = READY; optionally short-TTL retain (not immediate
   delete) of segments for re-merge/debug.
5. Idempotency: Status transition guards a single merge; re-run on READY is a no-op.
6. Gap tolerance: missing/failed segment skipped + logged; DB turn index preserves timeline.

Output format = **AAC/.m4a** (universal: Safari/iOS, Chrome, Android, admin `<audio>`).
Do NOT raw-concat WebM/Opus — duration/seek break; transcode is mandatory because web (Opus/WebM)
and Android segments are heterogeneous.

Execution: in-process **.NET `IHostedService` background queue**, bounded concurrency,
retry/back-off. ffmpeg via `Xabe.FFmpeg` or bundled binary. Upgrade path: dedicated worker/container.

### 4.5 API changes
- Formalize segment upload (reuse Mechanism B internally), tagging each segment to the session
  recording manifest.
- `GET /api/admin/sessions/{sessionId}/recordings` → return a **single** `SessionRecordingDto`:
  `{ recordingId, status, durationSecs, createdAt, completedAt, participants[], sizeBytes,
  audioUrl (presigned 120-min, null until READY) }`. Old list shape deprecated.
- Enqueue merge from the existing `EndSession` path in `LiveSessionService`.

### 4.6 Client capture changes
- Drive capture from `tblSession.IsRecordingEnabled` (host flag) for the session recording,
  independent of the personal localStorage consent (which still governs personal archive).
- Add `MediaRecorder` to **facilitator "Read Aloud"** turns (new — they have no recording phase today).
- Continue uploading per-turn segments; the session recording is assembled server-side at end.

### 4.7 Admin UX
One row per session: Duration · Created · Participants · Status badge (Processing/Ready/Failed) ·
single play/seek/download bound to the presigned `.m4a`. No N-clip lists.

---

## 5. Synchronization / scalability / failure / migration

- Sync: server-side turn order + receive-timestamps are the timeline source of truth — no
  device-clock alignment needed (the key benefit of turn concatenation).
- Scalability: bounded merge concurrency + queue; temp-disk streaming (not RAM); R2 free egress;
  one final object per session collapses object count and admin query cost.
- Failure/idempotency: Status guards single merge; failed merges retain segments and retry; late
  uploads handled by post-`EndSession` grace window.
- Migration: new table(s) + SQL Server/PostgreSQL migrations (Phase 10/15 pattern). Historic
  sessions keep `tblAudioArchive` clips; can be lazily back-merged or left as-is.

---

## 6. Phased implementation roadmap

- **P1 — Data + lifecycle:** `tblSessionRecording` (+ segment manifest), migrations, `StorageKeyBuilder`
  additions, create-row on session start, Status enum.
- **P2 — Capture:** segment tagging to session recording; add facilitator-turn `MediaRecorder`;
  drive from host `IsRecordingEnabled`.
- **P3 — Merge worker:** `IHostedService` queue + ffmpeg concat/normalize/transcode; idempotency;
  retries; grace window; enqueue from `EndSession`.
- **P4 — Admin API + UX:** single `SessionRecordingDto`; status-aware player; deprecate clip list.
- **P5 — Retention/cleanup:** `ExpiresAt` job; segment TTL policy; back-merge tool for history.

---

## 7. Assumptions · Risks · Tradeoffs

Assumptions: turn-based one-speaker model persists; R2 `gwf-audio` + presigned pattern is the
storage standard; admin deliverable is one recording per session.

Risks: heterogeneous segment codecs require ffmpeg re-encode (raw concat breaks duration/seek);
in-process ffmpeg CPU/RAM spikes → must be off-request-thread + concurrency-capped; dropped
segment uploads create silent gaps → gap-tolerant merge + upload-before-CompleteTurn reliability.

Tradeoffs: concatenation trades true overlap/ambient fidelity for simplicity and zero clock-sync
(correct for a turn-based drill app); short-TTL segment retention over immediate delete preserves
re-merge/debug at a small storage cost.
Design persisted. Recording the approved-but-unbuilt decisions to project memory so a future session doesn't re-derive them (matches the existing MobileDesignAnalysis pending-design pattern).

Write C:\Users\mdfay\.claude\projects\c--Live-GoWithFlow\memory\project_session_recording_design.md
29 lines
---
name: project-session-recording-design
description: Approved (pending-impl) design to replace fragmented per-turn clips with one consolidated session recording
metadata:
  type: project
---

Session recording redesign approved 2026-06-04, **pending implementation**. Full design:
`Backend/Docs/Dev/SessionRecordingArchitecture.md`.

Problem: admin sees per-turn fragmented clips (`tblAudioArchive`, 1 row per session+user+turn);
`GET /api/admin/sessions/{sessionId}/recordings` returns them all. No consolidated file exists —
no `tblSessionRecording` table. App is turn-based (one speaker at a time, on-device MediaRecorder,
no media server), so fragmentation is structural.

Approved decisions:
1. **Turn-clip concatenation** — server orders per-turn segments + ffmpeg concat/normalize/transcode
   to one `.m4a` at session end. No device-clock sync (turns don't overlap).
2. **Keep both** — personal opt-in Audio Archive stays; new host/admin session recording is a
   separate consolidated artifact from the same segments.
3. **Capture facilitator "Read Aloud" turns** (currently unrecorded) so the master is complete.

Plan: new `tblSessionRecording` (one row/session, Status CAPTURING→PENDING_MERGE→READY/FAILED),
R2 keys `sessions/{id}/segments/...` + `sessions/{id}/recording/session_{id}.m4a`, in-process
IHostedService merge queue, enqueue from `EndSession`, admin returns single SessionRecordingDto.

Do NOT put this in ProjectOverview.md until built/validated (system brain = validated state only).
Related: [[project_native_speech_root_cause]] (same voice/MediaRecorder stack).
Read C:\Users\mdfay\.claude\projects\c--Live-GoWithFlow\memory\MEMORY.md
Edit C:\Users\mdfay\.claude\projects\c--Live-GoWithFlow\memory\MEMORY.md
Added 1 line
Summary
The fragmentation is structural, not a bug: the app is turn-based with on-device MediaRecorder and no media server, so the only audio that exists is a sequence of per-turn clips, and tblAudioArchive stores one row per (session × user × turn). There is no tblSessionRecording table and no server-side consolidation stage — that's the missing piece.

The approved production design (turn-clip concatenation → one .m4a via a server-side ffmpeg merge worker triggered at EndSession, with facilitator turns now captured, personal Audio Archive retained) is fully specified in SessionRecordingArchitecture.md, including DB schema, R2 keys, merge pipeline, API/admin changes, failure handling, and a 5-phase rollout. Nothing was implemented — this was the requested pre-implementation analysis and design. When you're ready, P1 (table + lifecycle) is the entry point.


TASK STATUS: COMPLETE
MODULE IDENTIFIED: Phase 3 — Live Session Audio Archive (deps: Phase 10 R2 Storage, Backend Live Session Module, Backend Admin Module)
PROJECTOVERVIEW UPDATED: NO
  → REASON: No code/logic was touched — this is a pre-implementation design. ProjectOverview.md is validated system state only; an unbuilt design must not be written as system brain. Design persisted to Backend/Docs/Dev/SessionRecordingArchitecture.md (Dev artifact convention, matching FlowAudit_ProductionReady.md / MobileDesignAnalysis.md). ProjectOverview + ModuleIndex get the tblSessionRecording table, new APIs, and stable flow contract once built and validated.
MODULEINDEX UPDATED: NO
  → REASON: No new module/API/table/section exists yet (design only). Will add `session recording` keyword mapping + table/API entries at implementation.
SOURCE FILES READ: NONE
  → Documented contracts in ProjectOverview (Phase 3.5 Audio Archive, Phase 10 R2, voice-analysis flow, admin recordings endpoint) were complete enough to analyze the end-to-end flow. Per source-read budget rule, no source reads were justified.
DRIFT DETECTED: NO
FLOW STABLE ENOUGH TO SKIP SOURCE READS NEXT TIME: YES (design doc + memory capture the approved decisions and current-state analysis)
PENDING ITEMS:
  - Implement P1–P5 per SessionRecordingArchitecture.md (table+lifecycle → capture → merge worker → admin API/UX → retention)
  - Confirm ffmpeg execution host (in-process IHostedService assumed; revisit if instance is CPU-constrained)
  - On build: update ProjectOverview.md to stable flow-contract level + ModuleIndex keyword/table/API entries
  