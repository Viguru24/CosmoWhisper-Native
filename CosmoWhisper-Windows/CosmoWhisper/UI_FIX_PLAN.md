# Consolidate Feedback & Implementation Plan

## 1. Interaction Sounds
**Issue:** User reports interaction sounds (clicks, etc.) are not working when enabled.
**Action:** 
- Investigate `SoundManager.cs`.
- Ensure it checks `PreferenceManager.Shared.Preferences.InteractionSounds` before playing.
- Verify where `SoundManager.PlaySound()` is called.

## 2. Test Playground Text Size
**Issue:** Text in the "Test Playground" box is too small.
**Action:**
- Locate `TxtPlayground` in `DashboardWindow.xaml`.
- Increase `FontSize` (currently likely 12 or 14, aim for 16-18).

## 3. Authentication UI (Account Section)
**Issue:** User wants to see Google Auth and Email/Password options. Current view is just "Cosmo User".
**Action:**
- Investigate `DashboardWindow.xaml` "Account" section.
- Identify the "Logged In" vs "Logged Out" states.
- If "Logged Out" UI is missing or simple, I need to add:
    - "Sign in with Google" button.
    - "Email/Password" input fields or button.
- Check `SubscriptionManager` or `AuthManager` for existing login logic.

## 4. Master Commands List UI
**Issue:** "Open Master Commands List" section looks "not filled out" and description text is "very, very small".
**Action:**
- Locate "Open Master Commands List" in `DashboardWindow.xaml`.
- Increase container width or button size (Fill width).
- Increase `FontSize` of the description text ("Includes system triggers...").

## 5. Usage/Plan Section UI (Squashed)
**Issue:** Plan/Usage section looks "squashed".
**Action:**
- Locate "Plan", "20.0 min remaining", "Usage This Month" in `DashboardWindow.xaml`.
- Add `Margin` / `Padding` to the StackPanels/Grids containing these elements to let them breathe.
- Inspect the progress bar layout.

---
**Execution Order:**
1.  **Explore**: Read `DashboardWindow.xaml` and `SoundManager.cs` to map out the changes.
2.  **Edit XAML**: Apply UI fixes (Playground, Master Commands, Usage, Auth buttons).
3.  **Edit C#**: Fix Sound logic if broken.
4.  **Run**: Restart and verify.
