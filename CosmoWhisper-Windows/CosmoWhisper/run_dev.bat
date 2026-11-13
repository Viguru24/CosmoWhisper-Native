@echo off
setlocal enabledelayedexpansion

echo [INFO] Launching CosmoWhisper Development (Local Folder)...
echo [INFO] Stopping clashing processes...

taskkill /F /IM CosmoWhisperNative.exe /t 2>nul
taskkill /F /IM CosmoWhisperNative2.exe /t 2>nul
taskkill /F /IM CosmoWhisper.exe /t 2>nul

echo [WAIT] Waiting for processes to release file locks...
ping 127.0.0.1 -n 3 >nul

:: Set Project Path (Local to this folder)
set "PROJ_DIR=%~dp0"
set "CSPROJ=%PROJ_DIR%CosmoWhisper.csproj"

if not exist "%CSPROJ%" (
    echo [ERROR] Could not find project file at: "%CSPROJ%"
    pause
    exit /b 1
)

echo [BUILD] Building Development binary...
dotnet build "%CSPROJ%" -c Debug -r win-x64 --no-self-contained -nologo /clp:ErrorsOnly
if %ERRORLEVEL% neq 0 (
    echo [WARN] Build failed with win-x64. Retrying generic build...
    dotnet build "%CSPROJ%" -c Debug -nologo /clp:ErrorsOnly
    if !ERRORLEVEL! neq 0 (
        echo [ERROR] Critical Build Failure.
        pause
        exit /b 1
    )
)

echo [INFO] Starting Development Instance...

:: Try to find the exe
set "EXE_PATH=%PROJ_DIR%bin\Debug\net8.0-windows10.0.19041.0\win-x64\CosmoWhisperNative2.exe"
if not exist "!EXE_PATH!" set "EXE_PATH=%PROJ_DIR%bin\Debug\net8.0-windows10.0.19041.0\CosmoWhisperNative2.exe"

if not exist "!EXE_PATH!" (
    echo [ERROR] Could not find executable at: "!EXE_PATH!"
    pause
    exit /b 1
)

echo [OK] Launching: "!EXE_PATH!"
start "" "!EXE_PATH!" --dev

exit
