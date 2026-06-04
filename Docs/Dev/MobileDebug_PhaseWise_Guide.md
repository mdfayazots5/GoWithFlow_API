# GoWithFlow — Mobile App Phase-Wise Debugging Guide

**Document Type:** Dev Debugging Playbook  
**Created:** 2026-06-04  
**Purpose:** Systematic phase-by-phase process to capture, isolate, and resolve app issues using WiFi + USB debugging  
**Setup:** Device and dev machine on same WiFi. USB debugging enabled on device.

---

## PRE-REQUISITES CHECKLIST

Before starting any phase, confirm all of the following:

| Check | Status |
|---|---|
| USB cable connected (data cable, not charge-only) | [ ] |
| USB Debugging ON → Settings → Developer Options → USB Debugging | [ ] |
| Developer Options visible (tap Build Number 7 times if not) | [ ] |
| ADB recognized: run `adb devices` in terminal — device must show | [ ] |
| Device and PC on same WiFi network | [ ] |
| Backend API server running on dev machine | [ ] |
| API base URL in app config points to dev machine IP (not localhost) | [ ] |

---

## PHASE 1 — CLEAN INSTALL CHECK

**Goal:** Rule out corrupt install, stale cache, or old data as the cause.

### Step 1.1 — Uninstall Existing App

```powershell
adb uninstall com.gowithflow.app
```

> If you don't know the package name exactly, run:
> ```powershell
> adb shell pm list packages | findstr flow
> ```

### Step 1.2 — Clear Any Residual Data

```powershell
adb shell rm -rf /sdcard/Android/data/com.gowithflow.app
```

### Step 1.3 — Install Fresh APK

```powershell
adb install -r "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"
```

Expected output: `Performing Streamed Install` → `Success`

If it says **INSTALL_FAILED_UPDATE_INCOMPATIBLE**:
```powershell
adb uninstall com.gowithflow.app
adb install "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"
```

### Step 1.4 — Verify Install

```powershell
adb shell pm list packages | findstr flow
```

Confirm the package appears. Then launch:

```powershell
adb shell monkey -p com.gowithflow.app -c android.intent.category.LAUNCHER 1
```

### Phase 1 Outcome

| Result | Next Step |
|---|---|
| App installs and launches → | Proceed to Phase 2 |
| App crashes on launch → | Go to Phase 3 (Logcat immediately) |
| Install fails repeatedly → | Check APK signing / Android version compatibility |

---

## PHASE 2 — DIRECT APP FLOW CHECK (NO TOOLS)

**Goal:** Manually walk through every screen to identify exactly where failure occurs.

### Step 2.1 — Login Flow

- [ ] Open app
- [ ] Enter valid credentials
- [ ] Tap Login
- **Expected:** Navigates to Home / Dashboard
- **If stuck:** Note exact screen where it freezes or shows error

### Step 2.2 — API Connectivity Check (same WiFi)

Before walking the full flow, confirm the app can reach the backend:

- Try logging in — if login succeeds, API is reachable
- If login fails with network error → API URL misconfigured or backend not running
- Check app config: API base URL must be `http://[YOUR-PC-IP]:PORT` not `http://localhost`

Find your PC IP:
```powershell
ipconfig | findstr IPv4
```

### Step 2.3 — Session Join Flow

- [ ] Navigate to Join Session screen
- [ ] Enter a valid Join Code
- [ ] Tap Join / Validate
- **Expected:** Lobby screen loads with session details
- **Note:** Which step fails — validation call, lobby load, or role assignment?

### Step 2.4 — Lobby → Session Start

- [ ] Confirm both users appear in lobby (admin + participant)
- [ ] Admin taps Start Session
- **Expected:** Both devices transition to Live Session screen
- **Note:** Does only one device transition? Or neither?

### Step 2.5 — Voice / Turn Flow

- [ ] First utterance displays on screen
- [ ] Tap microphone / start recording
- [ ] Speak the utterance
- **Expected:** Voice analysis result appears, next turn triggers
- **Note:** Does mic start? Does it submit? Does result come back?

### Step 2.6 — SignalR Real-Time Sync Check

- [ ] On admin device, start the session
- [ ] Watch participant device — does it update in real-time?
- [ ] On participant device, complete a turn
- [ ] Watch admin device — does it reflect the change?
- **Note:** If one side does not update, SignalR hub connection is the suspect

### Phase 2 Outcome — Fill this in

```
Screen where failure occurs: _______________
Action that triggers failure: _______________
Error message shown (exact text): _______________
Does it affect both devices or only one: _______________
Is it consistent or intermittent: _______________
```

---

## PHASE 3 — LOG CAPTURE VIA USB (LOGCAT)

**Goal:** Capture full device logs during the failure to get exact errors.

### Step 3.1 — Clear Old Logs

```powershell
adb logcat -c
```

### Step 3.2 — Start Log Capture to File

Open a **new terminal window** and run:

```powershell
adb logcat -v time > "C:\Live\GoWithFlow\Backend\Docs\Dev\device_log.txt"
```

Leave this running in the background. It writes all logs continuously.

### Step 3.3 — Filter for App Logs Only (Cleaner)

For focused output, use the app tag filter in a second terminal:

```powershell
adb logcat -v time *:S ReactNativeJS:V ReactNative:V GoWithFlow:V > "C:\Live\GoWithFlow\Backend\Docs\Dev\app_log.txt"
```

> Adjust tag names based on your framework (React Native / Flutter / etc.)

### Step 3.4 — Reproduce the Issue

Now perform the exact steps that cause the failure:

1. Open the app
2. Walk through the flow up to the point of failure
3. Trigger the failure
4. Wait 5 seconds after the failure for logs to flush
5. Stop log capture: `Ctrl+C` in the logcat terminal

### Step 3.5 — Locate the Error in Log File

Open the log file and search for these patterns:

```powershell
Select-String -Path "C:\Live\GoWithFlow\Backend\Docs\Dev\device_log.txt" -Pattern "ERROR|EXCEPTION|FATAL|Failed|refused|timeout"
```

Or open in VS Code:
```powershell
code "C:\Live\GoWithFlow\Backend\Docs\Dev\device_log.txt"
```

Then `Ctrl+F` and search for: `error`, `exception`, `refused`, `timeout`, `signalr`, `hub`

### Phase 3 Key Log Patterns to Look For

| Pattern in Log | Likely Cause |
|---|---|
| `Connection refused` | Backend API not running or wrong IP/port |
| `Network request failed` | WiFi connectivity or CORS issue |
| `401 Unauthorized` | Token expired or not sent |
| `404 Not Found` | Wrong API endpoint path |
| `500 Internal Server Error` | Backend crash — check backend terminal |
| `SignalR connection failed` | Hub URL wrong or auth token missing |
| `WebSocket closed` | SignalR dropped — check hub keepalive |
| `Permission denied` (mic) | Microphone permission not granted |
| `NullPointerException` | App-level crash — check stack trace |
| `JSON parse error` | Response shape mismatch |

---

## PHASE 4 — ADD TARGETED LOGS TO APP CODE

**Goal:** If logcat output is unclear, add explicit log statements at key points in the code to trace the exact failure path.

### Step 4.1 — Identify the Suspect Area

Based on Phase 2 outcome, identify which module to instrument:

| Failure Point | File Area to Add Logs |
|---|---|
| Login fails | Auth service / login screen |
| Join code fails | Session validation service |
| Lobby does not load | Lobby screen / SignalR join handler |
| Session start fails | Session start service / hub event handler |
| Voice recording fails | VoiceRecorder component |
| Voice result not returned | Voice analysis service / API call |
| Turn not advancing | TurnState handler / SignalR subscriber |
| Real-time not syncing | SignalR hub connection / event subscriptions |

### Step 4.2 — Add Console Logs (React Native example)

At each key step in the suspect flow, add:

```javascript
console.log('[GWF-DEBUG] [ScreenName] Step: validate join code — payload:', JSON.stringify(payload));
console.log('[GWF-DEBUG] [ScreenName] API response:', JSON.stringify(response));
console.log('[GWF-DEBUG] [ScreenName] SignalR connected:', hubConnection.state);
console.log('[GWF-DEBUG] [ScreenName] Error caught:', error?.message, error?.stack);
```

**Always prefix with `[GWF-DEBUG]`** — this makes them easy to filter in logcat.

### Step 4.3 — Add Logs at These Specific Points

**API Call Entry:**
```javascript
console.log('[GWF-DEBUG] API CALL:', method, url, JSON.stringify(body));
```

**API Response:**
```javascript
console.log('[GWF-DEBUG] API RESPONSE:', status, JSON.stringify(data));
```

**SignalR Connection State:**
```javascript
hubConnection.onreconnecting(err => console.log('[GWF-DEBUG] SignalR reconnecting:', err));
hubConnection.onclose(err => console.log('[GWF-DEBUG] SignalR closed:', err));
hubConnection.onreconnected(id => console.log('[GWF-DEBUG] SignalR reconnected:', id));
```

**Event Received:**
```javascript
hubConnection.on('TurnAdvanced', (data) => {
  console.log('[GWF-DEBUG] TurnAdvanced received:', JSON.stringify(data));
});
```

**Error Boundary / Catch Block:**
```javascript
catch (err) {
  console.log('[GWF-DEBUG] CAUGHT ERROR:', err?.message, err?.stack);
}
```

### Step 4.4 — Rebuild and Reinstall

After adding logs, rebuild the app and push to device:

```powershell
# React Native — Metro bundler (dev build, instant logs)
npx react-native run-android

# OR install a new APK build
adb install -r "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"
```

### Step 4.5 — Filter Logs to Only Your Tags

```powershell
adb logcat -v time | findstr "GWF-DEBUG"
```

Or write to file:
```powershell
adb logcat -v time | findstr "GWF-DEBUG" > "C:\Live\GoWithFlow\Backend\Docs\Dev\debug_log.txt"
```

---

## PHASE 5 — BACKEND LOG CROSS-REFERENCE

**Goal:** Match app-side log timestamps with backend API logs to confirm whether the request reached the server.

### Step 5.1 — Watch Backend Console

Keep the backend terminal visible. When you trigger the failure in the app, watch for:

- Incoming request printed to console
- Any 400/500 response
- Any unhandled exception

### Step 5.2 — Check Backend Log Timestamps

If the backend uses file logging, check:

```
C:\Live\GoWithFlow\Backend\logs\
```

Match the timestamp from the app log against the backend log to confirm:

- Did the request arrive?
- What did the backend return?
- Did the backend crash silently?

### Step 5.3 — Cross-Reference Table (Fill During Debugging)

```
Timestamp (app log):    _______________
Request sent to:        _______________
Backend received it:    YES / NO
Backend response code:  _______________
Backend error (if any): _______________
App received response:  YES / NO
App parsed correctly:   YES / NO
Conclusion:             _______________
```

---

## PHASE 6 — WIRELESS ADB (WIFI DEBUGGING — NO USB NEEDED)

**Goal:** Once USB is used once for pairing, switch to wireless debugging for cleaner setup.

### Step 6.1 — Connect via USB first (one time)

```powershell
adb tcpip 5555
```

### Step 6.2 — Get device IP

On device: Settings → About Phone → Status → IP Address  
OR:
```powershell
adb shell ip route | findstr wlan
```

### Step 6.3 — Connect Wirelessly

```powershell
adb connect [DEVICE-IP]:5555
```

### Step 6.4 — Verify

```powershell
adb devices
```

Device should show as `[IP]:5555 device`. Now you can remove the USB cable and still run logcat.

---

## QUICK REFERENCE — COMMON COMMANDS

```powershell
# Check connected devices
adb devices

# Install APK
adb install -r "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"

# Uninstall app
adb uninstall com.gowithflow.app

# Clear logs
adb logcat -c

# Full log capture
adb logcat -v time > "C:\Live\GoWithFlow\Backend\Docs\Dev\device_log.txt"

# Filtered log (app debug only)
adb logcat -v time | findstr "GWF-DEBUG"

# Launch app
adb shell monkey -p com.gowithflow.app -c android.intent.category.LAUNCHER 1

# Get device IP
adb shell ip route

# Switch to wireless ADB
adb tcpip 5555
adb connect [DEVICE-IP]:5555

# Screenshot
adb exec-out screencap -p > "C:\Live\GoWithFlow\Backend\Docs\Dev\screenshot.png"

# Screen record (30 seconds)
adb shell screenrecord /sdcard/screen.mp4
adb pull /sdcard/screen.mp4 "C:\Live\GoWithFlow\Backend\Docs\Dev\screen.mp4"
```

---

## DEBUG SESSION LOG TEMPLATE

Fill this out each time you run a debug session:

```
Date:                   _______________
Phase Reached:          _______________
Exact failure point:    _______________
Error message (exact):  _______________
Log file saved:         YES / NO → path: _______________
Backend logs checked:   YES / NO
Root cause identified:  YES / NO
Root cause:             _______________
Fix applied:            _______________
Verified fixed:         YES / NO
Remaining unknown:      _______________
```

---

*Last Updated: 2026-06-04*
