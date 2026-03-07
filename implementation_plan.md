# CosmoWhisper — Fixing Transcription Proxy Routing

## Background

The transcription functionality is failing, often returning `[BLANK_AUDIO]` or an error. Investigation of the logs reveals that the Windows application is sending transcription requests to the wrong API endpoint (`/api/transcribe`) when the cloud proxy is used. The Node.js backend actually expects these requests at `/api/ai/transcribe`. Additionally, fallback URLs in the application are hardcoded to outdated domains, which prevents successful transcription when the primary backend URL fails.

**Root Causes:**
1.  **Incorrect Endpoint:** The app uses `/api/transcribe` instead of `/api/ai/transcribe`.
2.  **Outdated Fallbacks:** Fallback URLs in `AIService` point to `https://CosmoWhisper.com/api` and `https://api.cosmowhisper.com/api`, which may not be the correct active endpoints, especially during local development.
3.  **Mic Drop/Silent Audio:** The local model returns `[BLANK_AUDIO]`, indicating that early audio capture might be getting truncated or silenced before Whisper can process it.

## Proposed Changes

### 1. Correct the Proxy URL Endpoint
**File:** `CosmoWhisper-Windows\CosmoWhisper\Services\AIService.cs`
-   Update the `ProxyUrl` property to correctly append `/api/ai/transcribe` instead of `/api/transcribe`.
-   Update any hardcoded fallback URLs to reflect the current staging/production environments (e.g., ensuring they point to the correct `/api/ai/transcribe` route).

### 2. Investigate and Fix Audio Capture (Blank Audio)
**File:** `CosmoWhisper-Windows\CosmoWhisper\Managers\AudioRecorder.cs`
-   Review the microphone initialization and recording teardown sequence.
-   Ensure there's a sufficient buffer or delay before stopping the recording so that the final utterance isn't truncated.
-   Check if the `ProcessAudioFile` logic is inadvertently trimming necessary audio data.

## Verification Plan

### Manual Verification
1.  Launch the CosmoWhisper application.
2.  Ensure a valid `BackendUrl` is set (e.g., `http://localhost:5000` for local dev).
3.  Hold the hotkey (F9) and speak a clear, multi-second sentence.
4.  Monitor the `cosmo_debug.log` and `audio_debug.txt` to verify:
    -   The audio file size is reasonable (not near 0 bytes).
    -   The application sends the request to `<BackendUrl>/api/ai/transcribe`.
    -   The backend returns a 200 OK with the transcribed text.
5.  Verify the transcription appears in the active window.
