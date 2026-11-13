# Documentation: Multi-Environment Protocol Resolution

## Overview
This document outlines the architectural changes implemented on 2026-05-16 to resolve Google Authentication failures in the MSIX (Store) version of CosmoWhisper and to prevent protocol collisions between development and production environments.

## The Problem
1.  **MSIX Manifest Isolation**: MSIX apps are sandboxed. Unlike standard Win32 apps, they cannot effectively use the Windows Registry to register custom protocols for deep-linking. They require explicit declaration in the `Package.appxmanifest`.
2.  **Protocol Collision**: Both Dev and Production versions previously targeted the same `cosmowhisper://` protocol. This caused the Dev build's registry registration to hijack the protocol from the Store build, or vice versa, leading to "Auth failed" or "Wrong app opened" scenarios.

## The Solution

### 1. Differentiated Protocol Names
We now use environment-aware protocol names:
- **Production / Store**: `cosmowhisper://`
- **Development**: `cosmowhisper-dev://`

This differentiation is handled automatically by the `ProtocolHandler` class based on the `IsDevMode` flag.

### 2. Manifest Declaration (MSIX Fix)
The `Package.appxmanifest` has been updated to include both protocols as extensions. This allows Windows to recognize and route the deep links to the MSIX package when it is installed.

```xml
<uap:Extension Category="windows.protocol">
  <uap:Protocol Name="cosmowhisper">
    <uap:DisplayName>CosmoWhisper Protocol</uap:DisplayName>
  </uap:Protocol>
</uap:Extension>
<uap:Extension Category="windows.protocol">
  <uap:Protocol Name="cosmowhisper-dev">
    <uap:DisplayName>CosmoWhisper Dev Protocol</uap:DisplayName>
  </uap:Protocol>
</uap:Extension>
```

### 3. Backend Integration
The backend auth route now checks the `platform` parameter sent by the app during the initial Google Auth request:
- If `platform=windows`, the backend redirects to `cosmowhisper://auth?token=...`
- If `platform=windows-dev`, the backend redirects to `cosmowhisper-dev://auth?token=...`

### 4. Instance Isolation (Mutex & Pipes)
To allow Louis to test Dev and Production builds side-by-side:
- **Mutex Name**: `CosmoWhisper_Dev_Mutex` vs `CosmoWhisper_Personal_Mutex`
- **Named Pipe**: `CosmoWhisper_Dev_Pipe` vs `CosmoWhisper_Personal_Pipe`
- **Auth Temp File**: `cosmowhisper_dev_auth.tmp` vs `cosmowhisper_personal_auth.tmp`

## Impact
- **Store Version**: Google Auth now functions correctly as Windows routes the `cosmowhisper://` link via the manifest declaration.
- **Developer Experience**: Dev builds no longer interfere with the stable installed version. Deep links from a dev session are correctly routed to the dev executable.
