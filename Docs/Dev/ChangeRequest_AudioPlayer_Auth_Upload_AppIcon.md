# Change Request — Audio Player, Auth Persistence, Image Upload & App Icon

> **Status:** PLANNING ONLY — no code written. Awaiting user confirmation per CLAUDE.md §4a
> (Clarify-&-Confirm). Nothing in this file has been built or verified.
> **Raised:** 2026-06-22
> **Scope tiers:** mix of T1 (bug fixes) and T2 (new repeat feature, auth-persistence behaviour, upload
> validation/crop). Each item is tiered individually below.

---

## How to read this file

Each item has: **Owner / Reviewers → Module → Tier → Symptom (user words) → Senior analysis →
Options (recommended first) → What we need from you (the "good / temp / next / no" verdict)**.

Items are NOT started until you give a verdict per item. Voice (2,3,4) and any APK-visible change
(1,6,7,8,9,10) require **on-device verification on IV2201** before sign-off — a green build is not
acceptance (CLAUDE.md §5a / Voice §6).

Affected files of record (from ModuleIndex — to be confirmed at build time, marked `[VERIFY]`):
- Listen player: `ScriptPlaybackService`, `ListenScriptComponent`, `ListenVoicesSheetComponent`,
  `TtsService`, native `listenmedia/ListenMediaService.java` + `ListenMediaPlugin.java` + `listen-media.plugin.ts`
- Auth: `Backend Authentication Foundation` (JWT + refresh token), frontend auth/token storage `[VERIFY]`
- Image upload: `Backend User Module` avatar upload + frontend uploader `[VERIFY]`
- App icon: `Android Mobile Module` (Capacitor `res/` mipmaps, adaptive icon)

---

## Item 1 — Audio-player Settings panel renders with broken background / wrong UI
**Owner:** Kenji Tanaka (Frontend & Mobile) · **Reviewers:** Hannah Weiss (UX), Architect, QA
**Module:** Frontend Listen Script Module · **Tier:** T1 (UI bug)

**Symptom (user):** "in the audio player if i click the settings button then those below settings are
not showing properly, the background is not a correct ui."

**Senior analysis:** Most likely the settings sheet/panel (`ListenVoicesSheetComponent` or an inline
settings panel `[VERIFY]`) has a missing/transparent backdrop or a z-index / Material overlay theming
issue, so content shows over the player with no surface behind it. Must comply with `UIStandards.md`
(tokens `gw-*`/`gwf-*`, ≥44px touch targets, safe-area, max 14px body).

**Options:**
1. **(Recommended)** Re-style the settings surface as a proper bottom sheet / card with a solid
   tokenised background, correct elevation/z-index, safe-area padding, and standard spacing — fixes the
   root cause and aligns to `UIStandards.md`.
2. Quick patch: add a background colour only (temp) — leaves layout/elevation issues unaddressed.

**Need from you:** A screenshot of the broken settings panel would pin the exact defect fast. Confirm
Option 1 ("good") or accept Option 2 as temp.

---

## Item 2 — No audio output on **desktop** when listening from the Listen tab
**Owner:** Noor Haddad (Voice & Speech) · **Reviewers:** Kenji, Elena Petrova (Perf), QA
**Module:** Frontend Listen Script Module · **Tier:** T1 (bug; desktop/web path)

**Symptom (user):** "in the desktop while i am listening the audio from listening tab, the sound is not
getting."

**Senior analysis:** The Listen player uses on-device line-level TTS. On native it routes through the
foreground media service; on **web/desktop** it must use the Web Speech `speechSynthesis` path. Likely
causes (to confirm): (a) the player only wires the native media plugin and the web TTS branch is not
firing on desktop; (b) browser autoplay policy blocks `speechSynthesis` until a user gesture; (c)
voices list not loaded (`voiceschanged` race) so utterance is dropped silently. `[VERIFY]` against
`TtsService` / `ScriptPlaybackService`.

**Options:**
1. **(Recommended)** Diagnose the web TTS branch: ensure desktop uses `speechSynthesis`, wait for
   `voiceschanged`, and start playback from the user's click gesture (satisfies autoplay policy). Root-cause fix.
2. Defer if Listen is intended **mobile-only** — then we hide/disable the Listen player on desktop
   instead of fixing audio. (Need your product call.)

**Need from you:** Is Listen **meant to work on desktop**, or is it a mobile-first feature? Your answer
decides Option 1 vs 2.

---

## Item 3 — First word of the **first** voice line is clipped (audio starts from 2nd word)
**Owner:** Noor Haddad (Voice & Speech) · **Reviewers:** Kenji, Elena, QA
**Module:** Frontend Listen Script Module · **Tier:** T1 (bug)

**Symptom (user):** "while listening the voice from first voice, then it's first word voice not getting,
from second word it's getting the voice."

**Senior analysis:** Classic TTS cold-start: the engine begins speaking before the audio route /
synthesizer is fully warm, so the leading word is swallowed — only on the very first utterance.
Candidate fixes: warm up the TTS engine once on player open (speak a zero-volume/empty priming
utterance), or add a small lead-in delay / leading space before the first line, or wait for the engine
`ready`/`onstart` event before counting playback as started. `[VERIFY]` exact engine.

**Options:**
1. **(Recommended)** Warm-up priming on player open + start the first line only after `onstart`/ready —
   removes the clip without adding audible delay on every line.
2. Add a fixed lead-in delay before the first line only (simpler, slight delay; acceptable temp).

**Need from you:** Confirm Option 1 ("good") or accept Option 2 as temp.

---

## Item 4 — NEW idea: "Repeat" flag in Settings to give the user time to repeat each line
**Owner:** Priya Nair (Product) + Noor Haddad (Voice) · **Reviewers:** Architect, QA
**Module:** Frontend Listen Script Module · **Tier:** T2 (new feature / business rule)

**Symptom (user):** "while listening, at a time the full clarity will not get. my idea: in settings tab
give a flag for repeat — a feature to give time to user to repeat him. think and if you have any
clarifications or suggestions as a senior please tell me."

**Senior analysis (Priya + Noor):** This is a strong learning-UX idea — "listen, then shadow/repeat" is
a proven language-practice loop. As a senior I'd shape it as a **practice mode** rather than a single
boolean, because there are several independent knobs and we should not guess them:

- **Repeat-the-line vs pause-for-you-to-repeat** — two different behaviours:
  - *Auto-repeat:* play each line **N times** before advancing.
  - *Pause-for-repeat (shadowing):* play the line once, then **pause for a gap** so the user repeats out
    loud, then continue.
  - These can combine (play → pause → play again).
- **Gap length:** fixed seconds, or proportional to the line's spoken length (e.g. gap = 1.0× line
  duration). Proportional feels best because long lines need more time.
- **Repeat count:** 1–3 typically.
- **Scope of the flag:** a per-session Listen preference (client-side, like `showHardWords`), not a DB
  field — keeps it simple and reversible.

**Senior recommendation (the proper design):** Add a **"Practice / Repeat" group** in the Listen
settings sheet with: a toggle **Repeat mode** (off by default), a **count** (1–3, default 2) OR a
**"pause after each line"** toggle with a **gap = line length × factor** (default 1.0×). Persist as a
client-side Listen preference. No backend/DB change. Gate on the same settings sheet as Item 1.

**Options:**
1. **(Recommended — full)** Configurable practice group: Repeat count + Pause-for-repeat (proportional
   gap). Best learning value; ~T2 effort.
2. **(Minimal)** Single "Repeat each line ×2" toggle, no pause/gap controls — fastest, ships the core
   idea; can extend later.
3. **(Shadowing-only)** Just a "Pause after each line for me to repeat" toggle with proportional gap —
   matches your "give time to repeat" wording most literally.

**Need from you:** Pick the shape (1 / 2 / 3) and the defaults. My recommendation: **Option 1**, default
**off**, when on default **count 2** and **pause 1.0× line length**. Tell me "good / temp / change defaults".

---

## Item 5 — App asks for login again next day (session not persisted)
**Owner:** Omar Haddad (Security Architect — veto) · **Reviewers:** Architect, Daniel Okeke (Backend), QA
**Module:** Backend Authentication Foundation · **Tier:** T2 (auth/session behaviour) — **Security gate mandatory**

**Symptom (user):** "after login, if i open the app tomorrow then again asking the login page — check the
session once."

**Senior analysis (Omar + Daniel):** Likely the **access (JWT) token has a short lifetime and the
refresh-token flow isn't being used on app cold-start**, so by the next day the access token is expired
and the app routes to login instead of silently refreshing. Need to confirm `[VERIFY]`: (a) is a
refresh token issued & stored persistently (secure storage on native, not just in-memory/session)?
(b) does the app, on launch, attempt a silent refresh before deciding "logged out"? (c) refresh-token
lifetime (e.g. 7–30 days) and rotation policy.

**Security note (veto holder):** "Stay logged in" must be done **safely** — refresh token in secure
storage (Capacitor Secure Storage / Keychain-equivalent), refresh-token **rotation**, server-side
revocation on logout, and a sane absolute expiry. We will NOT solve this by extending the access-token
lifetime to days (that's the insecure shortcut).

**Options:**
1. **(Recommended)** Implement/repair silent refresh on app launch using a persisted, rotating refresh
   token in secure storage; access token stays short-lived. Proper, secure "stay logged in."
2. **(Temp / insecure — flagged)** Just lengthen the access-token lifetime. Quick but weakens security
   (no revocation, long-lived bearer). Only as an explicit temp with risk accepted.

**Need from you:** Confirm Option 1 ("good"), and tell me the desired "stay logged in" window
(e.g. 14 or 30 days). I'll `[VERIFY]` current token lifetimes against the Auth source before building.

---

## Item 6 — Image upload accepts any size; need a fixed target size + crop tool
**Owner:** Kenji Tanaka (Frontend) · **Reviewers:** Omar (Security), Daniel (Backend), QA
**Module:** Backend User Module (avatar upload) `[VERIFY] — confirm this is the profile-avatar upload`
**Tier:** T2 (adds crop UI + client validation)

**Symptom (user):** "while uploading an image it's taking all sizes — set a specific size and give an
option to crop images while uploading."

**Senior analysis:** Two parts: (a) **constrain dimensions/aspect** (e.g. square avatar, output
512×512), and (b) **add an interactive crop step** before upload. Recommend a client-side cropper that
outputs a normalised square image, then upload the cropped result; backend keeps its own size/type
guard as defence-in-depth. Must obey `UIStandards.md` for the crop modal (touch targets, safe-area).

**Open decisions (need your call):**
- **Which upload(s)?** Profile avatar only, or other image uploads too? `[VERIFY]`
- **Output spec:** aspect ratio (square recommended for avatar) and output size (512×512 recommended).
- **Cropper library** vs hand-rolled — recommend a small, maintained cropper compatible with Angular 19
  + Capacitor `[VERIFY]` (no name invented here).

**Options:**
1. **(Recommended)** Add crop modal (square, output 512×512, downscale client-side) + keep backend
   size/type validation. Proper UX + smaller uploads.
2. **(Minimal)** Just enforce max size/dimensions and reject oversize, no crop UI — less work, but
   doesn't satisfy your "give an option to crop."

**Need from you:** Confirm scope (which uploads), aspect/size, and Option 1 vs 2.

---

## Item 7 — Error message for >2MB image upload not shown properly
**Owner:** Kenji Tanaka (Frontend) · **Reviewers:** Daniel (Backend), QA
**Module:** Backend User Module (avatar upload) `[VERIFY]` · **Tier:** T1 (bug; pairs with Item 6)

**Symptom (user):** "if i upload an image more than 2 mb, then the error msg not showing properly."

**Senior analysis:** Either the client validates size but the toast/inline error isn't rendered
correctly, or the backend rejects it (413 / validation error) and the client swallows the message
instead of surfacing the standard error envelope. `[VERIFY]` whether the 2MB limit is client-side,
server-side, or both, and that the client reads the standard error envelope. Best handled **together
with Item 6** since the crop/downscale step changes when/whether the 2MB limit is even hit.

**Options:**
1. **(Recommended)** Show a clear inline/toast message ("Image must be under 2 MB") on the client
   *before* upload, and also surface the backend's standard error envelope if the server rejects.
   Bundle with Item 6.

**Need from you:** Confirm we fix this alongside Item 6 ("good").

---

## Item 8 — Player prev/next should jump by **question**, not line-by-line
**Owner:** Kenji Tanaka + Priya Nair · **Reviewers:** Architect, QA
**Module:** Frontend Listen Script Module · **Tier:** T2 (playback navigation flow change)

**Symptom (user):** "in the audio player, if we click front or back buttons it goes one by one. I want:
back = start of the back question, front = start from the next question."

**Senior analysis:** Today prev/next step one **line/utterance** at a time. You want them to step one
**question** (Q&A item) at a time:
- **Next** → jump to the **start of the next question**.
- **Back** → jump to the **start of the previous question** (standard media behaviour: if you're partway
  into the current question, "back" first restarts the *current* question; a second "back" goes to the
  previous one — `[VERIFY]` if you want that nuance or always jump to the previous question).

This depends on the script being Q&A-structured (it is, per the Q&A module). For non-Q&A scripts we'd
fall back to current line-stepping. `[VERIFY]` how questions are delimited in the script model.

**Options:**
1. **(Recommended)** Question-level jump with the "back restarts current question first" nuance (matches
   how music/podcast apps behave) for Q&A scripts; line-stepping fallback for non-Q&A scripts.
2. **(Literal)** Back always goes to previous question start, Next always to next question start (no
   "restart current" nuance).

**Need from you:** Confirm Option 1 vs 2, and confirm this applies only to Q&A-structured scripts.

---

## Item 9 — "Jump back to playing line" button when the user scrolls away
**Owner:** Kenji Tanaka (Frontend) · **Reviewers:** Hannah Weiss (UX), QA
**Module:** Frontend Listen Script Module · **Tier:** T2 (new UI behaviour)

**Symptom (user):** "while playing, if i scroll the question/answer list, it auto-scrolls back to the
playing line on role switch without my click. Instead: when i scroll, show a small floating up/down
icon; if i click it, then go to the playing line."

**Senior analysis (Kenji + Hannah):** Two coupled changes:
1. **Stop forced auto-scroll while the user is manually scrolling.** Detect user scroll → suspend
   auto-follow until they opt back in. (This is the real annoyance.)
2. **Add a floating "return to current line" pill/FAB** that appears only when the playing line is
   off-screen, with an **up or down chevron** depending on whether the playing line is above or below
   the viewport; tapping it smooth-scrolls to the playing line and re-enables auto-follow.

This is a well-known "chat scroll-to-latest" pattern; applies cleanly here. Must follow `UIStandards.md`
(≥44px target, safe-area, tokens).

**Options:**
1. **(Recommended)** Suspend auto-follow on manual scroll + floating directional "go to current line"
   button that re-arms auto-follow on tap. Full fix of both halves.
2. **(Minimal)** Only suspend auto-follow on manual scroll (no button) — quieter but no quick way back.

**Need from you:** Confirm Option 1 ("good"). Confirm button style: small **pill with chevron + "Now
playing"** vs **icon-only FAB** (recommend the pill for clarity).

---

## Item 10 — Change the app icon (senior's choice)
**Owner:** Marcus Bauer (DevOps/SRE) · **Reviewers:** Hannah Weiss (UX), Architect
**Module:** Android Mobile Module (Capacitor) · **Tier:** T2 (APK packaging / branding)

**Symptom (user):** "as a senior change the app icon."

**Senior analysis (Marcus + Hannah):** Android needs a full adaptive-icon set (foreground + background
layers, all mipmap densities, monochrome layer for themed icons) generated and placed in the Android
res folders, then rebuilt into the APK. I should **not invent a logo**; I need the source artwork or
your direction.

**Open decisions (need your call):**
- **Source art:** do you have a logo/SVG/PNG (high-res, ideally square + transparent) to use? If not,
  do you want me to propose a simple wordmark/glyph concept first for approval before generating the set?
- **Adaptive icon:** confirm a background colour + foreground (recommended for modern Android).

**Options:**
1. **(Recommended)** You provide source art → I generate the full adaptive mipmap set + monochrome
   layer, wire it into the Android project, rebuild APK, verify on IV2201.
2. I propose 1–2 icon concepts (described/mocked) for your approval first, then generate the set.

**Need from you:** Provide the logo asset, or pick Option 2 so I propose a concept first. (Cannot
proceed without art direction — won't guess your brand.)

---

## Cross-cutting notes & verification plan

- **No code is written until you give a per-item verdict** (good / temp / next / no). Items 6+7 and
  potentially 1+4+8+9 (all Listen settings/player) are best batched.
- **Verification (CLAUDE.md §5a / Voice §6):** items 1,2,3,4,8,9,10 must be **rendered/on-device verified
  on IV2201** (test creds 7075949956 / 123456) before any "COMPLETE" sign-off. Item 5 needs an auth
  round-trip test (login → reopen after token expiry → confirm silent refresh).
- **`[VERIFY]` markers** above are the exact points where I'll read the minimal source file at build
  time (per the Source-Read Budget rule) — I have not read those source files yet, so I'm not asserting
  current behaviour as fact.
- **ProjectOverview.md** entries (Listen player flow, Auth refresh flow, Avatar upload flow) will be
  updated in Detailed Flow Capture format as each approved item ships.

---

## Decision log — CONFIRMED 2026-06-22

| Item | Verdict | Decision |
|---|---|---|
| 1 Settings UI | PENDING SCREENSHOT | Option 1 (proper re-style). Awaiting screenshot of broken panel to pin exact defect. |
| 2 Desktop sound | ✅ CONFIRMED | Option 1 — **fix desktop audio**. Listen works on desktop + mobile; repair web speechSynthesis path. |
| 3 First-word clip | ✅ CONFIRMED | Option 1 — TTS warm-up on player open + start first line after engine ready. |
| 4 Repeat feature | ✅ CONFIRMED | Option 1 — **Full practice group**: Repeat toggle + count (1–3, default 2) + pause-to-repeat (gap = line length × factor). Off by default. |
| 5 Stay logged in | ✅ CONFIRMED | Option 1 — **secure silent refresh** (rotating refresh token in secure storage, short access token). Window: **30 days**. |
| 6 Image crop/size | ✅ CONFIRMED | Option 1 — **profile avatar only**, crop modal, output **square 512×512**. |
| 7 >2MB error | ✅ CONFIRMED | Clear inline/toast "<2 MB" error before upload + surface backend error. **Bundled with Item 6.** |
| 8 Prev/next = question | ✅ CONFIRMED | Option 1 — **music-app style** (Back restarts current question first, 2nd Back = previous; Next = next question). Line-stepping fallback for non-Q&A scripts. |
| 9 Scroll-follow button | ✅ CONFIRMED | Option 1 — **pill with up/down chevron + "Now playing"**; suspend auto-follow on manual scroll, re-arm on tap. |
| 10 App icon | ✅ CONFIRMED | Option 2 — **propose 1–2 concepts first** for approval, then generate full adaptive mipmap set + rebuild APK. |

> All confirmed items remain **PLANNING** until built; voice/UI/APK items still require on-device
> verification on IV2201 before sign-off. Build order suggestion: batch the Listen items (1,2,3,4,8,9),
> then auth (5), then upload (6+7), then app-icon concept (10).

## Build status — 2026-06-22

| Item | Code status | Verification |
|---|---|---|
| 1 Settings UI | ✅ Built (global `.preview-bottom-sheet` surface + sheet bg) | ⏳ UNVERIFIED — needs rendered/APK check |
| 2 Desktop sound | ✅ Built (drop hard-coded en-IN; retry web voices; no empty-list cache) | ⏳ UNVERIFIED — needs desktop check |
| 3 First-word clip | ✅ Built (`TtsService.warmUp()` on player open) | ⏳ UNVERIFIED — needs device check |
| 4 Practice/Repeat | ✅ Built (engine state + Settings group; JS loop on all platforms when ON) | ⏳ UNVERIFIED — needs device check |
| 5 Stay logged in | ✅ Built (30-day refresh; centralized single-flight refresh; startup hydration) | ⏳ UNVERIFIED — needs login→next-day check |
| 6 Avatar crop | ✅ Built (`AvatarCropperComponent`, 512×512 JPEG) | ⏳ UNVERIFIED — needs rendered/APK check |
| 7 >2MB error | ✅ Built (type+2MB toast before upload; upload error toast) | ⏳ UNVERIFIED — needs check |
| 8 Prev/next = question | ✅ Built (`questionStarts`, music-app prev/next) | ⏳ UNVERIFIED — needs check |
| 9 Scroll-follow pill | ✅ Built (suspend auto-follow + "Now playing" pill) | ⏳ UNVERIFIED — needs check |
| 10 App icon | ⏸ Concepts proposed — awaiting your pick before asset generation | n/a |

Frontend `vite build` green. Backend change was config-only (refresh-token 7→30 days in all 3 appsettings).
**Per Voice §6 / §5a a green build is NOT acceptance** for the voice/UI items — they need IV2201 verification.
