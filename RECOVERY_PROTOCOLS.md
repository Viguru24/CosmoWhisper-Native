# Preventing "Blocked" State Desynchronization

To ensure the native client never gets stuck in a "Blocked" state while the server reports an "Active" status, we have implemented a multi-layered defense. 

## Hardening Measures Implemented (v2.2.86.0)

1. **Atomic Login Reset**: `ProtocolHandler.cs` now forces `IsAccessBlocked = false` immediately upon receiving a new JWT token. No more "zombie blocks" after re-login.
2. **Proactive Re-sync**: `AIService.cs` now automatically triggers a background `CheckAuthStatusAsync` if an AI operation is attempted while the local state is "Blocked."
3. **Manual Override**: Added a **Refresh Status (🔄)** button in `AccountView.xaml` for users to force a server-side state pull.
4. **Script Integrity**: Updated `unblock.mjs` to target the correct `is_active` column instead of the non-existent `is_blocked` column.

## Future Guidelines

- **Stateless UI**: UI should default to "Unblocked" on startup until the first server handshake confirms otherwise.
- **Middleware Alignment**: Always check `backend/src/middleware/auth.ts` for the definitive "blocking" logic before writing maintenance scripts.
- **SSE Monitoring**: Ensure Nginx `X-Accel-Buffering: no` remains active to prevent SSE lag.
