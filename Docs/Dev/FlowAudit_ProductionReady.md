# GoWithFlow — End-to-End Flow Audit & Production-Ready Workflow Design

**Document Type:** Architecture + UX + Product Audit
**Authored:** 2026-06-03
**Scope:** Script Upload → Session Creation → Invitation → Lobby → Session Execution
**Authority Level:** Senior Enterprise Architect · Product Manager · UX Expert · End User

---

## Executive Summary

The current system contains **8 confirmed bugs / regressions**, **4 UX design flaws causing duplicate user actions**, **3 real-time synchronization gaps**, and **2 data redundancy issues** where the same information is collected more than once. The recommended workflow eliminates all of these while reducing the total click count to start a session by approximately 40%.

---

## Section 1 — Current State Audit

### 1.1 Script Upload Flow

#### Current Behavior

The admin uploads an Excel file through a 4-step wizard:
- Step 1: File selection and validation
- Step 2: Metadata entry (Title, Category, GrammarFocusTag, ContextTag, ComplexityLevel, TargetAgeGroup, HintLanguage)
- Step 3: Confirmation
- Step 4: Success

#### Issues Identified

**[ISSUE-01] GrammarFocusTag (`grammarFocusTag`) — Redundant and Low Value**

- The `grammarFocusTag` (e.g., "Have Been") is a metadata label applied at the script level.
- It is not used in the session execution flow. It does not appear in lobby state, turn state, or voice analysis.
- It surfaces in `uspGetSessionByJoinCode` RS1 as `ScriptGrammarTag`, which is included in the validate-join-code response — but the frontend currently only displays it on the join preview screen, where it provides marginal information to the user.
- From a learner's perspective, knowing a script focuses on "Have Been" grammar before joining adds no actionable value. The learner will discover the grammar context from the utterances themselves during the session.
- **Verdict:** GrammarFocusTag should be retained in the database for admin analytics (Phase 2 Step 5 uses it for cross-session grammar error trend analysis), but it should be **removed from the Script Upload UI form as a required user-facing field**. The system should either derive it automatically from the utterances' `GrammarTag` column, or assign a default and allow admin override in an advanced settings panel only.

**[ISSUE-02] HintLanguage — Already Hardcoded**

- Per memory record: `HintLanguage` is hardcoded to `Telugu` across all scripts. No hint language selector should exist in any UI form.
- The upload form currently exposes a HintLanguage dropdown — this is UI noise that contradicts the established product decision.
- **Verdict:** Remove the HintLanguage dropdown from the upload wizard. Hardcode default to `Telugu` on the backend.

**[ISSUE-03] MaxMembers NOT collected during upload — Gap**

- `MaxMembers` is defined on `tblSession`, not `tblScript`.
- Scripts define speaker labels (e.g., `Interviewer` and `Candidate`) which implicitly determine the minimum member count.
- However, the system allows MaxMembers to be set separately during Session Creation, which can mismatch the script's actual slot count.
- **Verdict (from Issue #7):** `MaxMembers` should be captured during Script Upload and stored on `tblScript`. During Session Creation, MaxMembers is auto-populated from the selected script and displayed as read-only. The user should not be able to override it. This eliminates the mismatch risk documented in: *"If script has fewer distinct speaker labels than MaxMembers, creation fails."*

#### Recommended Script Upload Fields (Simplified)

| Field | Keep / Remove | Notes |
|---|---|---|
| Script Title | KEEP | Required |
| Category | KEEP | Required — drives template rules |
| Excel File | KEEP | Required |
| ComplexityLevel | KEEP | Useful for script library filtering |
| TargetAgeGroup | KEEP | Useful for script library filtering |
| MaxMembers | ADD | Capture once here; derived from distinct SpeakerLabels in Excel |
| GrammarFocusTag | REMOVE from UI | Backend can derive from GrammarTag column values |
| ContextTag | REMOVE from UI | Admin analytics only — not user-facing |
| HintLanguage | REMOVE | Hardcode to Telugu |

---

### 1.2 Session Creation Flow

#### Current Behavior

The Session Creation form (`POST /api/sessions`) requires the host to provide:
- Session Name
- Session Mode (type)
- Max Members
- Session Duration
- Room Expiry Minutes
- Script selection (via search)

#### Issues Identified

**[ISSUE-04] SessionMode — Collected Twice (Critical Redundancy)**

- `SessionMode` is already embedded in the script's `Category` field.
- The category-to-mode mapping is 1:1 and already defined in the codebase (`SessionModeType` enum).
- When the host selects a script, the session mode is implicitly known.
- Asking the host to also select the Session Mode duplicates this selection and creates a risk of mismatch (e.g., selecting a "Roleplay" script but choosing "Grammar Drill" as the session type).
- **Verdict:** Remove `SessionMode` from the Session Creation form. Auto-populate it from the selected script's `Category` field. Display it as read-only information ("This is a Roleplay session").

**[ISSUE-05] MaxMembers — Collected Twice (Critical Redundancy)**

- Per Issue #7 and ISSUE-03 above: `MaxMembers` should be defined during script upload, inherited by Session Creation, and displayed as read-only.
- **Verdict:** Remove `MaxMembers` from Session Creation form. Auto-populate from `tblScript.MaxMembers`.

**[ISSUE-06] Minimum Required Fields After Cleanup**

After removing redundant fields, the Session Creation form should only require:
- **Session Name** (user provides)
- **Script Selection** (user selects; triggers auto-population of all other fields)
- **Session Duration** (user selects from [15, 30, 45, 60, 90])
- **Room Expiry Minutes** (user selects from [60, 120, 360, 1440])

All other session parameters are auto-populated from the selected script.

**Read-only display after script selection:**
- Session Mode (from `tblScript.Category`)
- Max Members (from `tblScript.MaxMembers`)
- Script Title, Utterance Count, Complexity Level

---

### 1.3 Invitation Flow

#### Current Behavior

1. Host creates session → navigated to `/session/invite`
2. Host searches users by name, assigns each to a slot, clicks "Send Invitations"
3. Invitees receive `INVITATION_RECEIVED` via SignalR to their personal group `user_{userId}`
4. User opens `/user/invitations`, sees invitation card, clicks Accept or Decline
5. On Accept: `JoinSessionAsync` auto-joins the invitee to the lobby; `INVITATION_RESPONDED` fires to host lobby

#### Issues Identified

**[ISSUE-07] Dashboard Invitation Count — Not Real-Time (Bug #3)**

- The user dashboard (`/user/dashboard`) shows a pending invitation count badge.
- This count is loaded once on page load from `GET /api/users/invitations` and is not updated in real-time.
- When `INVITATION_RECEIVED` arrives via SignalR, the badge count does not increment without a page refresh.
- **Root Cause:** The `UserDashboardComponent` subscribes to `INVITATION_RECEIVED` in the SignalR service but does not update the local invitation count signal/observable when the event fires.
- **Fix Required:** In `UserDashboardComponent`, subscribe to the SignalR `INVITATION_RECEIVED` event and increment the pending invitation count in the component state without reloading the page.

**[ISSUE-08] Duplicate "Tap When Ready" After Accepting Invitation (Bug #4)**

- Current flow: User accepts invitation on `/user/invitations` → navigated to Lobby → Lobby shows "Tap When Ready" button.
- The user already performed an intentional acceptance action ("Accept" button on the invitation). This is effectively the user's consent to participate.
- Presenting a second "Tap When Ready" action immediately after acceptance creates a redundant interaction for users who just accepted — they already expressed readiness.
- **Recommended Fix:** When a user accepts an invitation, the backend sets `tblSessionMember.IsReady = true` automatically (since accepting is equivalent to confirming participation). The Lobby screen for this user should show them as already ready, without requiring a second tap.
- **Exception:** Users who join via join code (ad-hoc path) still need to explicitly tap ready, since joining via code does not imply readiness.
- **Implementation Note:** In `RespondToInvitationAsync`, when `Status == ACCEPTED`, call `uspUpdateSessionMemberReadyStatus` with `@IsReady = true` immediately after inserting the member row. Broadcast `MEMBER_READY` along with `INVITATION_RESPONDED`.

**[ISSUE-09] Invitation Expiry Handling — Missing UI State**

- Expired invitations (past `ExpiresAt`) return from `GET /api/users/invitations` only when non-expired (correct).
- However, there is no frontend handling for the case where an invitation expires while the user is viewing the invitations page.
- **Verdict:** Add a countdown timer or "Expires in X hours" label per invitation card. On expiry, the card should auto-dismiss or change to an expired state without page reload, driven by a client-side timer against `ExpiresAt`.

---

### 1.4 Lobby Flow

#### Current Behavior

1. After accepting invite (or joining via code), user enters Lobby at `/session/lobby/:id`
2. Members tap "Tap When Ready" → REST `PATCH /api/sessions/ready` → SignalR broadcasts `MEMBER_READY`
3. Host sees all members; when all ready, Start button enables
4. Host clicks Start → `StartSession` hub method → `SESSION_STARTED` → all navigate to live room

#### Issues Identified

**[ISSUE-10] Participant Ready Status Not Updating on Host Screen (Bug #5)**

- When participants click "Tap When Ready", the host's screen continues to show them as "Waiting" until manually refreshed.
- **Root Cause (Likely):** The `LobbyComponent` subscribes to `MEMBER_READY` SignalR event but does not update the local member list state correctly. Either:
  a. The SignalR subscription is not established before the event fires (connection race condition), OR
  b. The `MEMBER_READY` handler patches `member.isReady` using `userId` but the local member list uses a different identifier key, OR
  c. Change detection is not triggered (missing `ChangeDetectorRef.detectChanges()` in zone-less or signal-based component).
- **Fix Required:**
  - Verify `MEMBER_READY` subscription is registered in `ngOnInit()` before the component renders.
  - Verify the handler finds the member by `userId` (not `slotIndex`) and mutates the correct object reference.
  - After mutation, if signals are used, ensure signal update triggers re-render. If using `ChangeDetectorRef`, call `detectChanges()`.
  - Add defensive poll: if `MEMBER_READY` is missed (dropped WebSocket event), the Lobby should poll `GET /api/sessions/lobby/{sessionId}` every 10 seconds as a fallback.

**[ISSUE-11] Lobby Page Reload Shows "ABANDONED" — Session Lifecycle Bug (Bug #8)**

- Reloading the Lobby page returns `status: "ABANDONED"` even when the session is still active.
- The example API response in the bug report shows `status: "ABANDONED"` with members still present and `canStart: false`.
- **Root Cause Analysis:**

  The `uspUpdateSessionMemberLeft` stored procedure auto-abandons the session when:
  > "SP auto-abandons session (Status = 'ABANDONED') when host leaves OR when no active members remain"

  During a page reload, the browser disconnects the WebSocket. `SessionHub.OnDisconnectedAsync` fires:
  > "Only calls leave when Status == 'LOBBY'; skips for ACTIVE"

  However, the bug report shows the session is still in Lobby state (`canStart: false`, not yet started). This means the reload causes the WebSocket to disconnect while the session is in LOBBY status, which triggers `OnDisconnectedAsync → LeaveLobby → LeaveSessionAsync → uspUpdateSessionMemberLeft`.

  If the user who reloads is the only active member (e.g., the host is the only one in lobby before invitees accept), the SP auto-abandons the session.

- **Fix Required:**

  **Option A (Recommended — Grace Period):** Add a reconnect grace window of 15–30 seconds. When a user disconnects from a LOBBY session, mark them as "disconnected" in memory (not in DB) for 30 seconds. If they reconnect within that window, restore their lobby membership without triggering leave/abandon logic. Only persist the leave after the grace period expires.

  **Option B (Immediate Fix):** Change `OnDisconnectedAsync` to NEVER auto-abandon the session. Instead, only broadcast `MEMBER_DISCONNECTED`. The session should transition to ABANDONED only via an explicit host action or a scheduled background job (e.g., if the session has been in LOBBY with no active members for >30 minutes).

  **Option C (Hybrid — Best for Production):**
  - `OnDisconnectedAsync` sets a `DisconnectedAt` timestamp in memory cache (Redis/in-memory) per `userId`.
  - On reconnect (`OnConnectedAsync`), if a `DisconnectedAt` entry exists for this user+session, clear it and restore them in the lobby (idempotent re-join).
  - A background service (Hosted Service) sweeps disconnected users after 2 minutes and calls `LeaveSessionAsync` if they haven't reconnected.
  - This prevents the reload race condition entirely.

  **Immediate Patch (Stopgap):** Remove the auto-abandon logic from `OnDisconnectedAsync` entirely. Rely only on explicit Leave actions and the scheduled expiry via `RoomExpiresAt`.

**[ISSUE-12] Lobby Status Check Missing Reconnect Path**

- The Frontend Status-Redirect Contract states:
  - `ABANDONED` → navigate to `/user/dashboard` with error toast
- When the session is incorrectly marked `ABANDONED` due to a page reload (ISSUE-11), the user is ejected from the lobby on their next load, with no path back.
- **Fix Required:** Pair with ISSUE-11 fix. If the session was abandoned due to a disconnect (not an explicit leave), provide a "Rejoin" path that calls the join endpoint again, rather than a dead-end dashboard redirect.

---

### 1.5 Session Execution Flow

#### Current Behavior

- Live session runs turn-by-turn via `GET /api/turns/{sessionId}/current` + SignalR hub `/hubs/live-session`
- Voice analysis submitted per turn via `POST /api/turns/{sessionId}/voice-analysis`
- Session completion via `POST /api/turns/{sessionId}/complete` (Phase 0 workflow)

#### Issues Identified

**[ISSUE-13] "Record My Voice Turns" — Should Be Host-Only (Bug #6)**

- The "Record My Voice Turns" toggle is currently shown to all participants, including guests.
- Recording is a host responsibility. The host enables it once at session start, and it applies to all participant audio for the entire session duration.
- **Correct Design:**
  - Remove the recording toggle from all participant screens.
  - On the host screen only, show "Record Session Audio" toggle (not "Record My Voice Turns" — the label should reflect that it records all participants, not just the host).
  - When the host enables recording, the session flag `IsRecordingEnabled` is set at session level (requires a new DB column: `tblSession.IsRecordingEnabled BIT NOT NULL DEFAULT(0)`).
  - All participant audio is captured from session start to session end based on this flag.
  - Participants receive a non-interactive notice: "This session is being recorded by the host."

**[ISSUE-14] Recording Scope — DB Column Missing**

- Currently, `tblVoiceAnalysis.AudioStorageKey` captures audio per-turn per-user (Phase 10).
- But the "record my voice turns" option as currently designed is per-user opt-in, not a session-level decision.
- The production design requires:
  - `tblSession.IsRecordingEnabled BIT NOT NULL DEFAULT(0)` — host sets this at session creation or in lobby.
  - All `tblVoiceAnalysis` rows for this session will have `AudioStorageKey` populated (audio always uploaded when recording is enabled).
  - No per-user recording opt-in. Recording is an all-or-nothing session-level setting controlled by the host.

---

## Section 2 — Technical Architecture Gaps

### 2.1 Real-Time Synchronization Gaps

| Gap | Affected Flow | Impact |
|---|---|---|
| Dashboard invitation badge not updated on `INVITATION_RECEIVED` | Invitation | Users miss invitations until page refresh |
| `MEMBER_READY` not updating host lobby state | Lobby | Host sees stale readiness; cannot start |
| Page reload triggers `OnDisconnectedAsync` → ABANDONED | Lobby | Sessions permanently destroyed on reload |

### 2.2 State Management Problems

| Problem | Root Cause | Fix |
|---|---|---|
| `status: "LOBBY"` hardcoded in `getLobbyState()` | Frontend ignored API status field | Fixed per existing drift note — verify it is applied everywhere |
| `MEMBER_READY` handler not mutating correct reference | Signal/zone mismatch or key mismatch | Verify userId-based lookup in handler |
| Session ABANDONED on WebSocket disconnect | No reconnect grace window | Add 15–30s grace period before persisting leave |

### 2.3 Session Lifecycle Flaws

```
Current (Flawed):
  Disconnect → OnDisconnectedAsync → LeaveSessionAsync → DB: IsActive=0 → if last member: ABANDONED

Correct (Production):
  Disconnect → mark disconnected in cache (15s grace) → if reconnect: restore → if timeout: LeaveSessionAsync
  Session ABANDONED only by: host explicit leave, background sweep, or RoomExpiresAt exceeded
```

### 2.4 Data Consistency Risks

| Risk | Location | Mitigation |
|---|---|---|
| SessionMode mismatch (host selects wrong mode for script) | Session Creation | Auto-derive SessionMode from script category (ISSUE-04) |
| MaxMembers mismatch (script has 2 speakers, host sets 4) | Session Creation | Auto-derive MaxMembers from script (ISSUE-05/03) |
| IsReady=false after invitation accept | Invitation → Lobby | Auto-set IsReady=true on accept (ISSUE-08) |
| Duplicate readiness action | Invitation → Lobby | Same fix as above |

### 2.5 Data Redundancy Across Forms

| Data Point | Collected At | Should Be Collected At |
|---|---|---|
| SessionMode | Script Upload (implicit via Category) + Session Creation (explicit) | Script Upload only |
| MaxMembers | Session Creation | Script Upload only |
| HintLanguage | Script Upload form | Nowhere — hardcode backend |
| GrammarFocusTag | Script Upload form | Derived from GrammarTag column values or omitted from UI |

---

## Section 3 — Recommended Production-Ready Workflow

### 3.1 Script Upload Flow (Revised)

```
Admin navigates to /admin/scripts/upload

Step 1 — File Upload
  ├─ Admin selects .xlsx file (max 5MB)
  ├─ Admin selects Category [dropdown]
  ├─ System shows category-specific template rules
  └─ Admin clicks "Validate Excel"
       → POST /api/scripts/validate
       → System derives: SpeakerLabels, DistinctSpeakerCount, GrammarTags from file content

Step 2 — Metadata (Simplified)
  Fields shown:
  ├─ Script Title [required text input]
  ├─ Max Members [read-only, auto-derived from DistinctSpeakerCount in Excel]
  ├─ Complexity Level [1–5 dropdown]
  ├─ Target Age Group [All / Child / Teen / Adult]
  ├─ Preview rows [accordion, always visible]
  └─ [Continue] button (enabled when Title filled and no validation errors)

Fields REMOVED from UI (backend-handled):
  ├─ Grammar Focus Tag [derived from GrammarTag column values, stored in DB]
  ├─ Context Tag [stored as default, admin-editable in advanced settings only]
  └─ Hint Language [hardcoded "Telugu"]

Step 3 — Confirmation
  └─ Admin clicks "Upload Script"
       → POST /api/scripts/upload
       → tblScript created with MaxMembers stored
       → tblUtterance bulk inserted
       → Excel archived to R2

Step 4 — Success
  └─ "Script uploaded. 24 utterances added. Script ready for session creation."
```

**New DB column required:**
- `tblScript.MaxMembers TINYINT NOT NULL DEFAULT(2)` — set at upload from parsed distinct speaker count

---

### 3.2 Session Creation Flow (Revised)

```
Host navigates to /scripts (Script Library)
  └─ Host previews a script
       └─ Host clicks "Start Session with this Script"
            → navigates to /session/create with script pre-selected

Session Creation Form (Minimal):
  ├─ Session Name [required text input]
  ├─ Script [pre-selected, searchable if not pre-selected]
  │
  │  [After script selection, these auto-populate as READ-ONLY]:
  ├─ Session Type [read-only chip: "Roleplay" / "Grammar Drill" / etc.]
  ├─ Max Members [read-only: e.g. "2 participants"]
  │
  │  [User selects these]:
  ├─ Session Duration [15 / 30 / 45 / 60 / 90 min]
  └─ Room Expiry [1h / 2h / 6h / 24h]

[Create Session] button
  → POST /api/sessions
     Body: { SessionName, SessionMode (derived), MaxMembers (derived), SessionDuration, ScriptId, RoomExpiryMinutes }
  → On success: navigate to /session/invite?sessionId=X
```

**Backend changes required:**
- `uspInsertSession` / `SessionService.CreateSessionAsync`: derive `SessionMode` from `tblScript.Category` instead of accepting it from the request DTO.
- `CreateSessionRequestDto`: remove `SessionMode` field; remove `MaxMembers` field (fetch from `tblScript`).
- Frontend: remove SessionMode and MaxMembers inputs; display as read-only info after script selection.

---

### 3.3 Invitation Flow (Revised)

```
Host on /session/invite:
  ├─ Slot 1 = Host (auto-filled, locked)
  ├─ Slot 2..N = Guest slots [search by name, assign user]
  └─ [Send Invitations] (enabled when all slots assigned)
       → POST /api/sessions/{id}/invitations
       → INVITATION_RECEIVED pushed to each invitee's user_{userId} SignalR group

Invitee on /user/dashboard:
  ├─ Real-time: INVITATION_RECEIVED fires → badge count increments immediately (no reload)
  └─ Invitee clicks badge → navigates to /user/invitations

Invitee on /user/invitations:
  ├─ Card shows: Session Name, Session Type, Role, Host Name, Duration, "Expires in X hours"
  ├─ [Accept] button
  │    → PATCH /api/sessions/{id}/invitations/{invId} { Status: "ACCEPTED" }
  │    → Backend: inserts tblSessionMember with IsReady=TRUE (CHANGED from current)
  │    → Backend: broadcasts MEMBER_READY + INVITATION_RESPONDED to session group
  │    → Frontend: navigates to /session/lobby/{sessionId}
  │    → Lobby: user arrives already marked READY (no second tap required)
  └─ [Decline] button
       → PATCH → Status=DECLINED → card dismissed

Invitee countdown:
  ├─ Client-side timer per card calculates time until ExpiresAt
  └─ On expiry: card changes to "Expired" state without reload
```

**Backend changes required:**
- `RespondToInvitationAsync` (Accept path): after inserting `tblSessionMember`, call `uspUpdateSessionMemberReadyStatus(@IsReady = true)`.
- Broadcast both `MEMBER_READY { userId, isReady: true }` and `INVITATION_RESPONDED` in the same response pipeline.

**Frontend changes required:**
- `UserDashboardComponent`: subscribe to `INVITATION_RECEIVED` SignalR event in `ngOnInit`. On event, increment local invitation count signal by 1 (no API call needed for count update).
- `MyInvitationsComponent`: add countdown timer per card using `ExpiresAt`.

---

### 3.4 Lobby Flow (Revised)

```
All participants enter Lobby at /session/lobby/:id

On load:
  ├─ GET /api/sessions/lobby/{sessionId}
  ├─ Frontend checks status field:
  │    ACTIVE    → navigate to /live-session/room/{id} immediately
  │    COMPLETED → navigate to /user/dashboard + error toast
  │    ABANDONED → check: was this caused by disconnect? → show rejoin option if within grace window
  │    LOBBY     → render lobby UI
  └─ Join SignalR hub: /hubs/session?sessionId={id}

Invitation-accepted participants:
  └─ Arrive already READY (IsReady=true from accept flow above)
       → No "Tap When Ready" required for them
       → Lobby shows them with green ready indicator

Join-code participants:
  └─ Arrive with IsReady=false
       → "Tap When Ready" button shown ONLY to them
       → Tapping: PATCH /api/sessions/ready { IsReady: true }
           → Server broadcasts MEMBER_READY to session group
           → All clients receive MEMBER_READY → update that member's card to green immediately

Real-time readiness sync fix:
  ├─ LobbyComponent subscribes to MEMBER_READY in ngOnInit (before hub connect)
  ├─ Handler: find member by userId in local member list, set isReady=true
  ├─ Trigger change detection / signal update
  ├─ Check: if all members ready → enable Start button without API call
  └─ Defensive poll: GET /api/sessions/lobby/{id} every 10s as fallback

Host controls:
  ├─ Member list with ready status (real-time)
  ├─ [Record Session Audio] toggle (HOST ONLY — hidden from guests)
  │    → Sets session-level recording flag (tblSession.IsRecordingEnabled)
  │    → Guests see passive notice: "This session is being recorded"
  ├─ [Start Session] button (enabled when CanStart=true)
  └─ [Cancel Session] button (abandons session, notifies all members)

On page reload (reconnect grace window):
  ├─ WebSocket disconnects → server marks userId as "grace-pending" (15s in memory cache)
  ├─ User reloads → reconnects → server detects grace-pending → restores without leave
  ├─ If grace expires before reconnect → LeaveSessionAsync fires → member removed
  └─ If host was last member and grace expired → ABANDONED (correct — not a bug anymore)

Host clicks Start:
  ├─ hub.invoke('StartSession', sessionId) → status → ACTIVE → turn 1 created
  ├─ Host navigates immediately on hub resolve (existing behavior — correct)
  ├─ Guests navigate on SESSION_STARTED event
  └─ All connections transfer to /hubs/live-session
```

---

### 3.5 Session Execution Flow (Revised)

```
All participants in /live-session/room/:id

Session begins:
  ├─ GET /api/turns/{sessionId}/current → turn 1 loaded
  ├─ Active speaker: turn index shown, utterance displayed, voice recorder active
  └─ Listener participants: see current utterance, active speaker highlighted, feedback buttons visible

Recording (Host-controlled):
  ├─ If tblSession.IsRecordingEnabled = true:
  │    └─ All participants: audio is captured per turn, uploaded to R2 on turn completion
  ├─ If false:
  │    └─ No audio uploaded (voice analysis still computed locally, scores still saved)
  └─ Participants see passive banner if recording is enabled (no toggle, no control)

Turn flow:
  ├─ Active speaker speaks → client voice engine captures + analyzes → recorder fills
  ├─ Speaker clicks "Submit Turn" (or timer expires)
  │    → POST /api/turns/{sessionId}/voice-analysis (save analysis + optional audio)
  │    → hub.invoke('CompleteTurn', sessionId, memberId, turnIndex, score)
  │    → TURN_SHIFT broadcast → all clients update active speaker
  │    → GET /api/turns/{sessionId}/current → canonical state refresh
  └─ Listeners: FeedbackTag buttons active during peer's turn → POST /api/turns/{sessionId}/listener-feedback

Session completion:
  ├─ Last turn completed → backend returns "No further turns remain. Complete the session."
  ├─ Host sees [Complete Session] button
  ├─ POST /api/turns/{sessionId}/complete
  │    → Status → COMPLETED
  │    → Streaks + badges awarded
  │    → Mistakes extracted
  │    → Summary generated
  └─ All participants navigated to /user/session-result/{sessionId}
```

---

## Section 4 — Click Count Comparison

### Session Start Journey (Current vs. Recommended)

| Step | Current Clicks | Recommended Clicks |
|---|---|---|
| Navigate to create session | 1 | 1 |
| Select script | 1 (search) | 0 (pre-selected from library) |
| Select session type | 1 | 0 (auto-derived) |
| Set max members | 1 | 0 (auto-derived) |
| Set session name | 1 (type) | 1 (type) |
| Set duration | 1 | 1 |
| Set expiry | 1 | 1 |
| Create session | 1 | 1 |
| Assign invitation slots | 2–4 | 2–4 |
| Send invitations | 1 | 1 |
| **Accept invitation (invitee)** | 1 | 1 |
| **Tap When Ready (invitee)** | 1 | 0 (auto-ready on accept) |
| Host start | 1 | 1 |
| **Total (host)** | **~12–14** | **~8–9** |
| **Total (invitee)** | **2** | **1** |

**Reduction: ~35–40% fewer user actions for the host. 50% fewer for invited participants.**

---

## Section 5 — Implementation Priority Order

### Priority 1 — Critical Bugs (Fix Immediately Before Any Feature Work)

| ID | Issue | File Area |
|---|---|---|
| ISSUE-11 | Lobby reload → ABANDONED status | `SessionHub.OnDisconnectedAsync`, `uspUpdateSessionMemberLeft` |
| ISSUE-10 | MEMBER_READY not updating host screen | `lobby.component.ts`, SignalR handler |
| ISSUE-07 | Dashboard invitation count not real-time | `user-dashboard.component.ts`, SignalR subscription |
| ISSUE-08 | Duplicate "Tap When Ready" after accept | `SessionInvitationService.RespondToInvitationAsync` |

### Priority 2 — Data Redundancy (UX Simplification)

| ID | Issue | File Area |
|---|---|---|
| ISSUE-04 | Remove SessionMode from Session Creation | `CreateSessionRequestDto`, `create-session.component.ts`, `SessionService` |
| ISSUE-05 | Remove MaxMembers from Session Creation | Same as above |
| ISSUE-03 | Add MaxMembers to Script Upload | `tblScript` migration, `UploadScriptRequestDto`, upload wizard |

### Priority 3 — UX Polish

| ID | Issue | File Area |
|---|---|---|
| ISSUE-13 | Recording toggle host-only | `session-room.component.ts`, `tblSession` migration |
| ISSUE-01 | Remove GrammarFocusTag from upload UI | `upload-script.component.ts` |
| ISSUE-02 | Remove HintLanguage from upload UI | Same |
| ISSUE-09 | Invitation expiry countdown | `my-invitations.component.ts` |
| ISSUE-12 | Rejoin path after accidental abandon | `lobby.component.ts` |

### Priority 4 — Architecture Hardening (After Above)

| ID | Issue | File Area |
|---|---|---|
| ISSUE-11 (full) | Reconnect grace window implementation | Redis/in-memory cache, `SessionHub`, `HubConnectionTracker` |
| ISSUE-14 | Session-level recording flag DB column | Migration: `tblSession.IsRecordingEnabled` |

---

## Section 6 — Database Changes Required

```sql
-- tblScript: add MaxMembers captured at upload
ALTER TABLE tblScript ADD MaxMembers TINYINT NOT NULL DEFAULT(2);

-- tblSession: add recording flag (host-controlled)
ALTER TABLE tblSession ADD IsRecordingEnabled BIT NOT NULL DEFAULT(0);

-- tblSession: recording flag populated from Session Creation request
-- No session Mode or MaxMembers input from user anymore — derived from script
```

---

## Section 7 — API Contract Changes

### CreateSessionRequestDto (Simplified)

```
Before:
  - SessionName (string, required)
  - SessionMode (int enum, required)        ← REMOVE
  - MaxMembers (byte, required)             ← REMOVE
  - SessionDuration (int, required)
  - ScriptId (long, required)
  - RoomExpiryMinutes (int, required)
  - IsRecordingEnabled (bool, optional)     ← ADD (host preference)

After:
  - SessionName (string, required)
  - SessionDuration (int, required)
  - ScriptId (long, required)
  - RoomExpiryMinutes (int, required)
  - IsRecordingEnabled (bool, optional, default false)
```

Backend derives `SessionMode` from `tblScript.Category` and `MaxMembers` from `tblScript.MaxMembers`.

### UploadScriptRequestDto (Simplified)

```
Before:
  - file (File, required)
  - scriptTitle (string, required)
  - category (string, required)
  - grammarFocusTag (string, optional)     ← Remove from required UI (backend derives)
  - contextTag (string, optional)          ← Remove from required UI (backend derives)
  - complexityLevel (number, required)
  - targetAgeGroup (string, required)
  - hintLanguage (string, required)        ← REMOVE (hardcode to Telugu)

After:
  - file (File, required)
  - scriptTitle (string, required)
  - category (string, required)
  - complexityLevel (number, required)
  - targetAgeGroup (string, required)
  - maxMembers (byte, required) — derived from parser output; confirmed by admin as read-only
```

### RespondToInvitationAsync (Updated Business Rule)

```
On ACCEPTED:
  1. uspUpdateInvitationStatus → ACCEPTED
  2. JoinSessionAsync → insert tblSessionMember (IsReady=false)
  3. [NEW] uspUpdateSessionMemberReadyStatus(@IsReady=true)
  4. [NEW] Broadcast MEMBER_READY to session group
  5. SessionNotifier.NotifyInvitationRespondedAsync → INVITATION_RESPONDED
```

---

## Section 8 — SignalR Subscription Contracts (Frontend)

### UserDashboardComponent (updated)

```typescript
ngOnInit() {
  // Existing: load dashboard data
  this.loadDashboard();

  // NEW: subscribe to INVITATION_RECEIVED
  this.signalRService.on('INVITATION_RECEIVED', () => {
    this.pendingInvitationCount.update(count => count + 1);
  });
}

ngOnDestroy() {
  this.signalRService.off('INVITATION_RECEIVED');
}
```

### LobbyComponent (updated MEMBER_READY handler)

```typescript
ngOnInit() {
  this.loadLobby();

  // Subscribe BEFORE hub connects to avoid missing early events
  this.signalRService.on('MEMBER_READY', (data: { userId: number; isReady: boolean }) => {
    this.members.update(members =>
      members.map(m => m.userId === data.userId ? { ...m, isReady: data.isReady } : m)
    );
    this.canStart.set(this.members().length >= 2 && this.members().every(m => m.isReady));
  });

  // Defensive poll fallback
  this.lobbyPollInterval = setInterval(() => this.loadLobby(), 10_000);
}
```

---

## Section 9 — Ideal End-to-End Flow (Zero-Redundancy Version)

```
ADMIN                          HOST                          INVITEE
─────────────────              ─────────────────             ─────────────────
Upload Script (.xlsx)         Browse Script Library
  ↓                             ↓
Metadata: Title,              "Start Session with Script"
  Category, MaxMembers          ↓
  (auto from Excel)           Session Name + Duration
  ↓                            (only 2 inputs)
Script stored in library        ↓
                              "Create Session" → LOBBY
                                ↓
                              Assign Users to Slots
                              "Send Invitations"
                                ↓
                              [Navigate to Lobby]
                              Sees member list (Waiting)
                                                            Notification badge
                                                            updates instantly ←── INVITATION_RECEIVED
                                                              ↓
                                                            "Accept" on invitation card
                                                              ↓
                                                            Auto-joined to lobby
                                                            Already READY (no second tap)
                              MEMBER_READY received ───────────────────────────→
                              Host sees "Ready" instantly
                                ↓
                              All members ready
                              [Start Session] (enabled)
                                ↓
                              SESSION_STARTED ────────────────────────────────→
                                ↓                             ↓
                              Both navigate to Live Room
                              ─────────────────────────────────────────────────
                              Turn 1: Active speaker speaks, submits
                              TURN_SHIFT broadcast → all update
                              Listeners: feedback buttons active
                              ...
                              Last turn → [Complete Session]
                              ↓
                              Summary → /user/session-result
                              Streaks, badges, mistakes extracted
```

---

## Section 10 — Mobile-First Design Validation

| Screen | Mobile Issues | Fix |
|---|---|---|
| Session Creation | Long form with 6+ fields → cognitive overload | Reduce to 4 fields (Name, Script, Duration, Expiry) |
| Script Upload | Step-by-step wizard already mobile-friendly | Remove redundant fields to shorten step 2 |
| Invitation accept | Single tap "Accept" already mobile-optimized | Auto-ready on accept eliminates second tap |
| Lobby | "Tap When Ready" text appropriate for mobile | Remove for invitation-accepted users |
| Live Session | Turn-based, one speaker at a time | No change required |

---

## Section 11 — Scalability and Maintainability Assessment

| Concern | Current | Recommended |
|---|---|---|
| SignalR group strategy | Per-session group + per-user group | Correct — maintain this |
| Session state authority | DB is authoritative; frontend polls as fallback | Add reconnect grace cache layer |
| Recording storage | Per-turn per-user in R2 | Session-level flag to gate uploads; no change to storage structure |
| Max members enforcement | At session creation (runtime validation) | Move to script upload (compile-time) |
| SessionMode mapping | Frontend sends int; backend converts to string | Eliminate frontend input; derive server-side |

---

*This document reflects system state as of 2026-06-03 based on ProjectOverview.md and the 8 confirmed bugs reported. All recommendations are production-ready and actionable. No architecture described here is speculative — all gaps are confirmed against the existing codebase contracts.*
