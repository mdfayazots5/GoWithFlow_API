# Session Recording Architecture — Production Design (2026-06-04)

Status: **BACKEND IMPLEMENTED & BUILDING (2026-06-05).** Frontend (Angular P2/P4) + ops
(run migrations, install ffmpeg) remain. Stable flow contract now lives in `ProjectOverview.md`
under "Backend Session Recording Module".

Backend delivered: `recordingenabled` flag + migrations (PG + SQL Server), host-only
`PATCH /api/sessions/{id}/recording`, `tblsessionrecording`, `SessionRecordingRepository/Service`,
ffmpeg merge worker (channel queue, idempotent claim, startup recovery/retry), daily retention
worker, admin single `SessionRecordingDto` at `GET /api/admin/sessions/{id}/recordings`,
`IStorageService.DownloadToAsync`. Verified via `dotnet build` (0 errors).

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
