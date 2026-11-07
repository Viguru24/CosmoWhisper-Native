# Task Plan - Resolving Account Blocking Sync Failure

Objective: Resolve the persistent "Account Blocked" state in the native client by fixing local state synchronization and ensuring immediate status refresh upon login or admin unblocking.

## Phase 1: Native Client State Hardening
- [/] **Fix Logout/Login Stale State**: Ensure `AppState.Shared.IsAccessBlocked` is reset to `false` when a new Auth token is received via the protocol handler.
- [ ] **Immediate Sync on Auth**: Force an immediate status check and heartbeat start in `ProtocolHandler.cs` after setting a new token.
- [ ] **Update `CheckAuthStatusAsync`**: Modify `AccountService.cs` so that manual or startup auth checks correctly update the `IsAccessBlocked` flag.
- [ ] **Heartbeat Optimization**: Ensure `StartHeartbeat` handles re-authentication gracefully without multiple loop instances.

## Phase 2: User Experience Improvements
- [ ] **Add "Force Sync" Button**: Implement a "Refresh Account" button in the Dashboard's Account view to allow manual clearing of stuck states.
- [ ] **Proactive Sync on Block Error**: If `AIService` hits a block, trigger a background `SyncStatus` call to see if the status has changed since the last heartbeat.

## Phase 3: Infrastructure & Verification
- [ ] **SSE Verification**: Confirm `status-stream` is receiving events on the local machine.
- [ ] **Log Audit**: Review `cosmo_debug.log` to ensure no persistent 403s are coming from the server for the unblocked account.

## Phase 4: Build & Deliver
- [ ] **Rebuild MSIX**: Create a new versioned build with the fixes.
- [ ] **Verification**: Confirm the "Blocked" overlay disappears immediately after login.

---
*Created by Antigravity*
