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
*Last Updated: May 6, 2026*
