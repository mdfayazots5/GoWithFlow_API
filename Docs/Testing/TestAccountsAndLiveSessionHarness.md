# Test Accounts & Live Session Harness

Purpose: reusable assets + procedure for testing flows that need a **second live participant**
(Session Room, listener screen, turn rotation, consolidated recording). Created 2026-06-08 during the
Session Room on-device verification.

---

## 1. Test accounts

| Role | Name | Mobile | Password | UserId | Notes |
|---|---|---|---|---|---|
| Host (primary) | Md Fayaz | 7075949956 | 123456 | 9 | The account normally on the test device. |
| **Test participant** | **Verify Bot** | **9911946608** | **123456** | **12** | Reusable 2nd participant for multi-party sessions. |

### How the bot was provisioned (for reference / re-creation)
- Registered via `POST /api/auth/register` (fields: `fullName, mobileNumber, email, password, ageGroup, preferredHintLanguage`).
- **Gotcha:** registration stores `passwordhash = null` and accounts are OTP-gated, so the new account
  **cannot log in** right after register. It was made loginable by copying user 9's password hash in
  Supabase Postgres:
  ```sql
  UPDATE tbluser
  SET passwordhash = (SELECT passwordhash FROM tbluser WHERE userid = 9)
  WHERE userid = 12;
  ```
  (Copying a known-good hash sets the bot's password to the same value, `123456`, without knowing the
  algorithm.) To disable the bot later: `UPDATE tbluser SET passwordhash = NULL WHERE userid = 12;`

### DB / API connection
- Prod API base (the production APK uses this): `https://gowithflow-api.onrender.com/api`
- Supabase Postgres connection string is in `Backend/GoWithFlow.API/appsettings.json` (committed in
  plaintext — flagged in the Secret Management review; rotate + move to env vars eventually).

---

## 2. Why a 2nd participant is required

Every script in the library has **2–3 speaker labels**, so a session always has ≥2 slots and
`canStart` stays false until all occupied members are ready. There is **no single-speaker script**, so
a solo host can never start a session. Hence the bot.

---

## 3. Spin up a live 2-participant session (API only)

All calls are JSON with `Authorization: Bearer <token>`. Host token = device `localStorage.gwf_token`;
bot token = login the bot.

1. **Login bot** → token: `POST /auth/login` `{ "mobileNumber":"9911946608", "password":"123456" }`
2. **Create session (host)**: `POST /sessions` `{ "sessionName":"...", "sessionDuration":15, "scriptId":<id>, "roomExpiryMinutes":60 }` → returns `sessionId`. Host auto-occupies slot 1.
3. **Invite bot to slot 2 (host)**: `POST /sessions/{id}/invitations` `{ "assignments":[{ "userId":12, "slotIndex":2 }] }` → response `data[0].invitationId`.
4. **Bot accepts**: `PATCH /sessions/{id}/invitations/{invitationId}` `{ "status":"ACCEPTED" }`
5. **Both ready**: `PATCH /sessions/ready` `{ "sessionId":<id>, "isReady":true }` (once with host token, once with bot token).
6. **Verify**: `GET /sessions/lobby/{id}` → `canStart` should be `true`.
7. **Start (host)**: `POST /sessions/{id}/start`
8. **Current turn**: `GET /turns/{id}/current` → `activeMemberId` is the first speaker (slot 1 = host),
   so the host device renders the **speaker** screen; the bot would be the listener.
9. **End when done (host)**: `POST /sessions/{id}/end`

Scripts available at time of writing (all multi-speaker): ids 3–10 (Hotel/Roleplay/Grammar Drill/
Bank/Doctor/Mock Interview). Use `GET /scripts?page=1&pageSize=10` to refresh.

---

## 4. Driving the device WebView (debug APK is debuggable)

The debug APK exposes a WebView devtools socket, so you can navigate routes and run JS via the
Chrome DevTools Protocol (CDP), and screenshot via adb.

```powershell
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
# 1. find + forward the webview devtools socket (pid changes per app launch)
$sock = (& $adb shell grep -a webview_devtools /proc/net/unix) -replace '.*@',''
& $adb forward tcp:9222 "localabstract:$sock"
# 2. get the page's webSocketDebuggerUrl
(Invoke-WebRequest -UseBasicParsing http://localhost:9222/json).Content   # -> ws://localhost:9222/devtools/page/<ID>
# 3. screenshot (capture on device then pull — PowerShell '>' corrupts binary)
& $adb shell screencap -p /sdcard/s.png
& $adb pull /sdcard/s.png .\shot.png
```

Run JS in the page (navigate, click, read DOM, call fetch with the app's token) by sending a CDP
`Runtime.evaluate` over the websocket (Node 22 has a global `WebSocket`). Example expression to open the
Session Room: `window.location.assign('https://localhost/live-session/room/<id>')`. App fetches inherit
the logged-in token, so you can run the whole setup in §3 from inside the WebView too.

Note: the app is served from `https://localhost` inside Capacitor; the Session Room route is
`/live-session/room/:sessionId` (guarded — the user must be a member of the session).

---

## 5. Related

- On-device **Speech & Capture Test** harness (no session needed): Settings → Diagnostics, route
  `/user/speech-debug`. Tests voice recognition + native audio capture + upload.
- Session Room redesign spec: `Backend/Docs/SessionRoomRedesign.md` (APK-verified 2026-06-08).
