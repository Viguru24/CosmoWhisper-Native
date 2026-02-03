# Task Plan - CosmoWhisper Mouse & Click Fix

## Objective
Fix mouse side-button recording in Dashboard, ensure widget Red Orb click reliability, and successfully deploy the app without installation hangs.

## Success Criteria
- [ ] Widget Red Orb initiates recording immediately on Left Click.
- [ ] Dashboard "Mouse Button" recording properly captures side buttons (3, 4, etc.).
- [ ] "???" or "Listening..." state in Dashboard can be cancelled or completes successfully.
- [ ] App launches and has accessibility/automation permissions trusted.

## Phases
### Phase 1: Recovery & Planning
- [x] Analyze current stuck state (terminal hangs).
- [x] Kill stuck installation processes.
- [x] Initialize planning files (This file).

### Phase 2: Application Fixes
- [x] Implement `UnifiedClickDetector` for robust widget clicking.
- [x] Add local mouse monitor to `InputController` for Dashboard recording.
- [x] Verify Dashboard UI formatting for "Listening" state.

### Phase 3: Deployment & Verification
- [x] Build app locally (avoid sudo hangs).
- [x] Launch from build directory to verify fix.
- [x] Verify functionality (Orb Click, Side Button Record).

## Conclusion
- Mouse Logic: User remapped hardware mouse side-button to **F8**.
- App Config: App successfully bound to **Hotkey: 100** (F8).
- Permissions: TCC Access reset and re-granted.
- Install: App successfully running from `/Applications`.
