# Louis Personal Cosmo Whisper - Technical Documentation

> [!IMPORTANT]
> This is a specialized, private version of CosmoWhisper designed specifically for Louis. It features separate data storage and unique build identifiers to allow it to run side-by-side with the standard developer build.

## 📁 Key File Locations

| Item | Path |
| :--- | :--- |
| **Desktop Shortcut** | `C:\Users\louis\OneDrive\Desktop\GREEN_PERSONAL_COSMO.lnk` |
| **Executable Name** | `CosmoWhisperPersonal.exe` |
| **AppData Folder** | `%AppData%\CosmoWhisper_Personal` |
| **Settings File** | `%AppData%\CosmoWhisper_Personal\settings.json` |
| **Vocabulary File** | `%AppData%\CosmoWhisper_Personal\vocabulary.json` |
| **Source Branch** | `louis-personal-cosmo-whisper` |

## 🏗️ Build Configuration
The project is configured in `CosmoWhisper.csproj` with:
- `<AssemblyName>CosmoWhisperPersonal</AssemblyName>`

## 🔒 Key Differences & Features
1. **Parallel Execution:** Uses a custom assembly name and AppData folder to ensure no Mutex or file lock conflicts with `CosmoWhisperNative.exe`.
2. **Admin Anonymization:** The admin developer bypass email is set to `bruce.wayne@wayneenterprises.com` in `PreferenceManager.cs`.
3. **Personalized Environment:** All preferences, transcription hints, and custom vocabulary are stored separately from the main development environment.

## 🛠️ How to Build
To build this specific version, ensure you are on the `louis-personal-cosmo-whisper` branch and run:
```powershell
dotnet build CosmoWhisper-Windows\CosmoWhisper\CosmoWhisper.csproj -c Release
```
The output will be located in:
`CosmoWhisper-Windows\CosmoWhisper\bin\Release\net8.0-windows10.0.19041.0\win-x64\CosmoWhisperPersonal.exe`
