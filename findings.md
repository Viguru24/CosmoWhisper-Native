# Findings - Account Sync Issues

## 1. Stale Local State in Native App
- **Discovery**: `AccountService.SignOut()` explicitly sets `AppState.Shared.IsAccessBlocked = true` to prevent unauthenticated access. However, the login flow via `ProtocolHandler.cs` sets the `AuthToken` but **never resets** `IsAccessBlocked` to `false`.
- **Impact**: After logging back in, the app remains in a "Blocked" UI state until the next Heartbeat pulse (30-second interval) or until a `SyncStatus` call is manually triggered.
- **Root Cause**: Lack of state reset in the `auth` protocol handler.

## 2. Heartbeat/Sync Delay
- **Discovery**: The Heartbeat loop runs every 30 seconds. If a user unblocks themselves in the dashboard and returns to the app, they may have to wait up to 30 seconds for the app to realize the block is gone.
- **SSE Status**: The app has an SSE listener (`status-stream`), but it depends on the Heartbeat being active. If Heartbeat was stopped during logout, the SSE listener won't restart until the heartbeat does.

## 3. SSE Buffering (Potential)
- **Discovery**: The Nginx configuration for `api.cosmowhisper.com` lacks `proxy_buffering off`. This can cause SSE events (like "unblock") to be held in the Nginx buffer, preventing real-time updates.

## 4. Duplicate Check Logic
- **Discovery**: `AIService` checks `AppState.Shared.IsAccessBlocked` before every transcription. If this flag is stuck at `true`, the user is permanently locked out locally regardless of server status.

---
*Last Updated: 2026-05-07*
