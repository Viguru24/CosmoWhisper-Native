# How to Build the MSIX Package for CosmoWhisper

You are now MSIX-ready! Follow these steps to generate the package.

## 1. Prerequisites
- **Visual Studio 2022** with the following workloads installed:
  - *.NET Desktop Development*
  - *Universal Windows Platform development* (specifically the **Windows Application Packaging Project** template).

## 2. Create the Packaging Project
Since packaging projects require specific GUIDs and solution integration that cannot be fully automated via script, please do this once:

1.  Open `CosmoWhisper.sln` in Visual Studio.
2.  Right-click the **Solution 'CosmoWhisper'** > **Add** > **New Project**.
3.  Search for **"Windows Application Packaging Project"** (C#).
4.  Name it: `CosmoWhisper.Package`.
5.  Location: `c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native`.
6.  **Target Version**: Windows 11 (10.0.22621.0).
7.  **Min Version**: Windows 10 (1809 - 10.0.17763.0).

## 3. Configure the Package
1.  In the new `CosmoWhisper.Package` project, right-click **Applications**.
2.  Select **Add Reference**.
3.  Check **CosmoWhisper** (your main project). This links the EXE to the package.
4.  Right-click `CosmoWhisper` under Applications and select **Set as Entry Point**.

## 4. Updates Applied
I have already:
- **Modified `TrayManager.cs`**: Calls to `StartupManager` now check if the app is packaged. If packaged, it directs the user to Windows Settings instead of trying to write to the Registry (which fails or is virtualized in MSIX).
- **Prepared Manifest**: A `Package.appxmanifest` file is located in `CosmoWhisper-Package/Package.appxmanifest`. You can copy its contents (especially the `<Capabilities>` and `<Extensions>` sections) into your new project's manifest to enable Microphone access and Startup tasks.

## 5. Build & Publish
1.  Right-click `CosmoWhisper.Package` -> **Publish** -> **Create App Packages**.
2.  Select **Sideloading** (or Store if you have a dev account ready).
3.  Follow the wizard to create a signing certificate (Visual Studio will generate a test certificate for you).
4.  Build!

## 6. Testing
- Locate the output logic (usually inside `AppPackages` folder).
- Double-click the `.msixbundle` to install.
- Verify that "Start with Windows" in the Tray Menu shows the new message.
