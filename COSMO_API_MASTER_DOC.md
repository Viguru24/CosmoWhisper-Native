# CosmoWhisper API — Master Technical Documentation

This document serves as the comprehensive technical reference for the CosmoWhisper Backend (`api.cosmowhisper.com`), documenting its architecture, the real-time synchronization engine, and the solutions to historical performance and connectivity bottlenecks.

---

## 1. Core Architecture
- **Environment:** Hetzner VPS (Ubuntu/Linux).
- **Runtime:** Node.js (TypeScript) managed by **PM2**.
- **Reverse Proxy:** **Nginx** (Handles SSL termination and surgical buffering).
- **Process Manager:** PM2 (Service Name: `cosmowhisper-api`).

---

## 2. Real-Time Sync Engine (The "Instant Push")
The system uses **Server-Sent Events (SSE)** to achieve sub-second synchronization between the Admin Dashboard and the Native Client.

### The Mechanism
1. **Client Connection:** The native app opens a persistent GET request to `/api/license/status-stream`.
2. **SSE Manager:** A singleton class in the backend (`SSEManager`) maintains an array of active response objects indexed by `userId`.
3. **Event Trigger:** When an admin toggles a user's status in the dashboard, the `notifyUser` method is called.
4. **Immediate Flush:** The server writes the event to the specific user's socket and calls `.flush()` to bypass standard Node.js internal buffers.

### Problems & Solutions
| Problem | Root Cause | Solution |
| :--- | :--- | :--- |
| **20-30s Sync Lag** | Nginx Proxy Buffering | Disabled buffering surgically for the SSE route using `proxy_buffering off;` in the Nginx config. |
| **"Unblock" Not Working** | Auth Middleware Blocking | Modified `auth.ts` to whitelist the `status-stream` route, allowing blocked users to maintain a "listening" connection. |
| **Silent Disconnects** | Network/Proxy Idle Timeouts | Implemented a 15-second high-frequency keep-alive (heartbeat) on the SSE channel to keep the pipe "hot." |
| **Type Mismatches** | `number` vs `string` IDs | Normalized all User IDs to `string` in the `SSEManager` to ensure reliable client lookups. |

---

## 3. Surgical Nginx Configuration
We implemented the "Best of Both Worlds" by splitting the API into two behaviors in `/etc/nginx/sites-enabled/cosmowhisper.com.conf`:

```nginx
# 1. Real-time route (No Buffering)
location /api/license/status-stream {
    proxy_buffering off;
    proxy_cache off;
    chunked_transfer_encoding off;
    proxy_read_timeout 86400s; # Stay open for 24h
}

# 2. Standard API (Buffered/Fast)
location / {
    proxy_buffering on;
    # Handles standard requests like transcriptions and dashboard lists
}
```

---

## 4. Hardened Security & Auth Flow
- **Middleware:** `authenticate` middleware extracts the JWT and validates the user.
- **Blocked State:** If a user is `is_active: false`, the heartbeat (`/api/license/status`) returns `403 Forbidden`. 
- **The Exception:** The `status-stream` is excluded from the `403` check, ensuring that once an admin clicks "Unblock," the signal can actually reach the blocked client.

---

## 5. Maintenance & Deployment
- **Local Dev:** `npm run dev` (Starts on `localhost:5000`).
- **Production Sync:** Files are synced via `scp` or `git push`, followed by `pm2 restart all`.
- **Logs:** 
  - Backend: `pm2 logs cosmowhisper-api`
  - Nginx: `/var/log/nginx/error.log`

---

## 6. Key Files
- `src/utils/sse.ts`: The heart of the Push system.
- `src/middleware/auth.ts`: Access control and whitelisting logic.
- `src/routes/license.ts`: Entry point for the real-time stream.
- `src/routes/admin.ts`: Trigger point for account status updates.


---

## 7. Local Development & Bypass Architecture

To enable local developer productivity and separate dev/prod environments on Windows:
- **Dev Flag:** Running the application with `--dev` (or passing arguments matching `cosmowhisper-dev:`) flags the runtime via `COSMO_DEV` environment variable.
- **AppData Isolation:** Dev mode redirects all settings and logs to `%APPDATA%\CosmoWhisper_Dev` instead of the production folder.
- **Deep Linking Protocol:** Dev builds register the `cosmowhisper-dev://` custom URL protocol, which Windows forwards to the active dev executable.
- **Named Pipe IPC:** The dev instance listens on the named pipe `CosmoWhisper_Dev_Pipe` (with mutex `CosmoWhisper_Dev_Mutex`) to capture deep-link events from secondary launches.
- **Temp File Fallback:** If the named pipe connection fails (timeout, pipe busy), the secondary instance writes the protocol URL to a temp file (`cosmowhisper_dev_auth.tmp` in `%TEMP%`). The main instance monitors this file via `FileSystemWatcher` and processes it as a fallback.
- **Dev Auth Bypass:** If no token is set in dev mode, the app pings the local mock or simulates a successful check for `dev@cosmowhisper.local` with `unlimited` tier, allowing local test usage without needing a valid subscription token.

---

## 8. Developer Troubleshooting & Diagnostics

To prevent development friction, follow these guidelines when debugging authentication, deep links, or API connectivity:

### A. Lingering Zombie Processes (The "Double Instance" Bug)
Because the app registers a single-instance mutex (`CosmoWhisper_Dev_Mutex`) and named pipe (`CosmoWhisper_Dev_Pipe`) in dev mode, any background process running from a previous build will intercept new launch commands and deep-link redirects.
- **Symptoms:** You click a link or run the dev launcher, but nothing happens, or the old/prod app opens.
- **Diagnostic:** Check for running instances using PowerShell:
  ```powershell
  Get-Process | Where-Object { $_.Name -like "*CosmoWhisper*" }
  ```
- **Remediation:** Force-terminate all instances before building/launching:
  ```powershell
  Get-Process -Name "*CosmoWhisper*" -ErrorAction SilentlyContinue | Stop-Process -Force
  ```
  *(Note: The `run_dev.ps1` launcher script automatically runs this cleanup on startup.)*

### B. Custom Protocol Handler (Deep Link Redirects)
Google Auth and dashboard links redirect to `cosmowhisper-dev://`. Windows maps this to a specific executable path in the registry. If the link opens the production version or fails:
- **Registry Key:** `HKCU\Software\Classes\cosmowhisper-dev\shell\open\command`
- **Verification:** Run the following command in PowerShell to see which binary handles dev links:
  ```powershell
  Get-ItemProperty -Path "Registry::HKEY_CURRENT_USER\Software\Classes\cosmowhisper-dev\shell\open\command" -Name "(default)"
  ```
- **Solution:** Re-running the dev build with `--dev` will re-register the protocol handler to point to your latest `CosmoWhisperNative2.exe` path.

### C. ⚠️ CRITICAL: Backend OAuth Redirect Scheme (Recurring Issue)

**This is the #1 recurring authentication bug.** When a user clicks "Sign in with Google" from the dev app, the client opens a browser to:
```
https://api.cosmowhisper.com/api/auth/google?platform=windows-dev
```
The `platform=windows-dev` parameter is passed through Google OAuth via the `state` field. After authentication, the backend callback at `/api/auth/google/callback` returns an HTML page that redirects the browser to a deep link URL.

**THE BUG:** If the backend callback has a **hardcoded** protocol scheme (`cosmowhisper://`), then dev logins silently redirect the token to the **production** app instead of the dev app. The dev app never receives the token, and the user appears unauthenticated.

**THE FIX (already applied):** The callback in `src/routes/auth.ts` on the VPS must dynamically resolve the scheme:
```typescript
const scheme = platform === 'windows-dev' ? 'cosmowhisper-dev' : 'cosmowhisper';
```
This variable is then used in both the `<a href>` link and the `window.location.href` redirect:
```html
<a href="${scheme}://auth?token=${token}">Open CosmoWhisper</a>
<script>window.location.href = "${scheme}://auth?token=${token}";</script>
```

**Files involved on VPS:**
- `src/routes/auth.ts` — lines ~280 (scheme declaration) and ~370 (HTML template)

**Symptoms when broken:**
- User signs in via Google, browser shows "Authenticated!" page.
- The dev app does NOT show a welcome toast or update the Account tab.
- The production app (if running) captures the token instead.
- Dev logs show no `[Protocol] Protocol Action: auth` entry.
- The dev app falls back to `dev@cosmowhisper.local` bypass.

**How to verify the fix is live:**
```bash
# SSH into VPS and check the compiled output
ssh -p 8522 root@49.12.79.244 "grep 'windows-dev' /var/www/cosmowhisper/backend/dist/routes/auth.js"
```
If this returns a match containing `cosmowhisper-dev`, the fix is live. If it returns nothing, the backend needs to be recompiled.

### D. Token Handoff Architecture (Client-Side)

When the browser redirects to `cosmowhisper-dev://auth?token=...`, Windows launches a secondary instance of `CosmoWhisperNative2.exe`. The secondary instance must forward the token to the already-running main instance. There are **two** delivery mechanisms:

| Channel | File | Timeout | Reliability |
|---|---|---|---|
| **Named Pipe** (primary) | `SingleInstanceProvider.cs` | 5000ms | Fast but can timeout if pipe is busy |
| **Temp File** (fallback) | `Program.cs` writes → `SingleInstanceProvider.cs` watches | Instant via `FileSystemWatcher` | Bulletproof fallback |

**Flow:**
1. `Program.cs` — The secondary instance writes the URL to `%TEMP%\cosmowhisper_dev_auth.tmp` immediately.
2. `Program.cs` — The secondary instance attempts to connect to the named pipe `CosmoWhisper_Dev_Pipe` with a 5000ms timeout.
3. If the pipe connects, the URL is forwarded instantly and the secondary instance exits.
4. If the pipe times out, the secondary instance exits anyway — the temp file is already written.
5. `SingleInstanceProvider.cs` — The main instance's `FileSystemWatcher` detects the temp file, reads and deletes it, then dispatches to `ProtocolHandler.Handle()`.

**Symptoms when broken:**
- User signs in, browser redirects correctly to `cosmowhisper-dev://`, but the app doesn't react.
- `cosmo_debug.log` shows NO `[Protocol] Protocol Action: auth` entry.
- A stale `cosmowhisper_dev_auth.tmp` file lingers in `%TEMP%`.

### E. Direct API & Key Setup
To transcribe directly through Groq (bypassing the server proxy and local execution):
1. Open settings in **Preferences** and enable the **"Force Direct API (No Proxy/Fallback)"** toggle.
2. Verify that your Groq API key is present in your dev settings profile:
   - **Dev Settings Path:** `%APPDATA%\CosmoWhisper_Dev\settings.json`
   - **JSON Format:**
     ```json
     {
       "GroqApiKey": "gsk_your_key_here",
       "ForceDirectApi": true
     }
     ```
3. To test transcription without any cloud auth token, ensure the app shows the user logged in as `dev@cosmowhisper.local` with "Unlimited Access".

### F. Log Files
Always watch the debug logs for runtime information:
- **Dev Log Path:** `%APPDATA%\CosmoWhisper_Dev\logs\cosmo_debug.log`
- **Protocol Log Path:** `%APPDATA%\CosmoWhisper\logs\cosmo_debug.log` (ProtocolHandler logs here due to hardcoded path)
- **Tail Logs Command:**
  ```powershell
  Get-Content -Path "$env:APPDATA\CosmoWhisper_Dev\logs\cosmo_debug.log" -Wait -Tail 50
  ```
- **Search for auth events:**
  ```powershell
  Select-String -Path "$env:APPDATA\CosmoWhisper\logs\cosmo_debug.log" -Pattern "Protocol"
  ```

---

## 9. Changelog of Recurring Issues

| Date | Issue | Root Cause | Fix |
|---|---|---|---|
| 2026-06-17 | Dev app login via Google doesn't register; user appears offline in back office | Backend `auth.ts` callback had hardcoded `cosmowhisper://` scheme, sending dev tokens to the production app | Made scheme dynamic: `const scheme = platform === 'windows-dev' ? 'cosmowhisper-dev' : 'cosmowhisper';` in VPS `src/routes/auth.ts` |
| 2026-06-17 | Named pipe timeout causing token loss during secondary instance handoff | Pipe client timeout was 1000ms (too short); no fallback if pipe failed | Increased timeout to 5000ms; added `FileSystemWatcher` temp file fallback in `SingleInstanceProvider.cs` |

---
*Last Updated: June 17, 2026*
