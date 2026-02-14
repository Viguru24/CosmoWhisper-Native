# VPN & Connectivity Architecture Guide

This document outlines the "Sticky Proxy" system implemented to ensure CosmoWhisper remains functional behind restrictive corporate VPNs, firewalls, and ISP blocks.

## The Problem
Many VPNs and corporate networks block direct traffic to AI API endpoints (like `api.groq.com` or `api.openai.com`) or interfere with local loopback (`localhost`). This causes the app to hang, time out, or fail during transcription and chat processing.

## The Solution: Multi-Tier Sticky Proxy
The application uses a dynamic, fail-over routing system that "learns" the best path for your current network environment.

### 1. Connection Hierarchy
The app attempts connections in the following order:
1.  **Direct (Primary):** `https://api.groq.com`
    *   Lowest latency. Used by default on open networks.
2.  **Firebase Proxy (VPN Bypass):** `https://cosmowhisper-app.web.app/api`
    *   Routes through Google/Firebase edge nodes (rarely blocked).
    *   Compatible with standard OpenAI SDK paths.
3.  **Render Backend (Reliability Fallback):** `https://cosmowhisper-app.onrender.com`
    *   Final fallback if the CDN/Proxy layer is unavailable.

### 2. "Sticky" Proxy Logic
To prevent the app from feeling "laggy" while waiting for repeated timeouts, we use **Sticky Proxy Mode**:
*   **Trigger:** The first time a "Direct" request fails (e.g., due to a timeout or DNS error), the app logs the failure.
*   **Switch:** The internal `_useProxy` flag is set to `true` for the remainder of the application session.
*   **Behavior:** All subsequent requests skip the "Direct" attempt entirely and go straight to the **Firebase Proxy**.
*   **Result:** A smooth, responsive experience even on restricted networks.

## Implementation Details

### Native (C#)
*   **File:** `CosmoWhisper-Windows/CosmoWhisper/Services/AIService.cs`
*   **Key Constant:** `FirebaseProxyUrl = "https://cosmowhisper-app.web.app/api"`
*   **Behavior:** Tries Direct -> Firebase -> Render.

### Backend (Node.js/Firebase)
*   **File:** `website/functions/src/index.ts`
*   **Routes:**
    *   `/api/chat/completions` -> Proxies to Groq/Gemini chat.
    *   `/api/audio/transcriptions` -> Handles multipart file uploads and proxies to Groq Whisper.
*   **Key Forwarding:** Supports `Authorization: Bearer <key>` forwarding, allowing users to use their own keys even through the proxy.

### Electron (Frontend)
*   **File:** `src/main/ai.ts`
*   **Setting:** `useProxy` (Optional manual toggle, though automatic fallback is being standardized).

## Troubleshooting
If you encounter connectivity issues:
1.  Check the logs in `%AppData%\CosmoWhisper_Native\logs\groq_errors.log`.
2.  Ensure your VPN allows traffic to `*.web.app`.
3.  Restart the app to reset "Sticky Mode" if you move to a cleaner network. 
