# Research & Findings - Versioning & Fallback Issues

## 1. Version Identification Error
- **Observation:** User reported the app identifies as "Windows/Store" version.
- **Root Cause:** `WidgetWindow.xaml.cs` has a hardcoded `#if DEBUG` check. In non-debug builds (like the Personal build), it defaults to `Gray Gear` and logs `PRODUCTION`.
- **Root Cause 2:** `PreferenceManager.cs` relies on `package.Id.Name.Contains("MicroMeadow.CosmoWhisper")`. If the Personal build manifest wasn't updated, it reports as Store version.
- **Solution:** Rely on `IsStoreVersion` property which is saved in `settings.json` and already correctly set to `false` in the user's `CosmoWhisper_Personal` profile.

## 2. Groq API Fallback "Not Working"
- **Observation:** Groq API returns 200 OK in logs, but user perceives failure.
- **Root Cause:** "Race Mode" in `AIService.cs` picks the first finisher. If Local finishes first with `[BLANK_AUDIO]`, it returns that and discards the Cloud result.
- **Root Cause 2:** `WidgetWindow.xaml.cs` resets to idle if transcription is empty, giving no feedback that a fallback happened or failed.
- **Solution:** Filter out `[BLANK_AUDIO]` in the race logic. Force wait for Cloud if Local is empty.

## 3. Environment Isolation
- **Status:** Verified. `CosmoWhisper_Personal` is being used correctly for settings.
- **Discovery:** `LOUIS_PERSONAL_COSMO.md` confirms the build uses a unique assembly name `CosmoWhisperPersonal`.
