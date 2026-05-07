@echo off
setlocal enabledelayedexpansion

echo 🚀 Launching CosmoWhisper Development (Orange)...
echo 🛑 Stopping clashing processes...

:: Kill any running instances (Native and legacy)
taskkill /F /IM CosmoWhisperNative.exe /t 2>nul
taskkill /F /IM CosmoWhisperNative2.exe /t 2>nul
taskkill /F /IM CosmoWhisper.exe /t 2>nul

:: Wait for processes to fully release file locks
echo ⏳ Waiting for processes to terminate...
timeout /t 2 /nobreak >nul

:: Set Project Path
set PROJ_DIR=%~dp0CosmoWhisper-Windows\CosmoWhisper
set CSPROJ=%PROJ_DIR%\CosmoWhisper.csproj

:: Build the dev binary
echo 🔨 Building Development binary...
dotnet build "%CSPROJ%" -c Debug -r win-x64 --no-self-contained -nologo /clp:ErrorsOnly
if %ERRORLEVEL% neq 0 (
    echo ❌ Build failed! Retrying without specific Runtime Identifier...
    dotnet build "%CSPROJ%" -c Debug -nologo /clp:ErrorsOnly
    if !ERRORLEVEL! neq 0 (
        echo ❌ Critical Build Failure.
        pause
        exit /b 1
    )
)

echo ✨ Starting Development Instance (Orange Mode)...

:: Try to find the exe in common build locations
set EXE_PATH=%PROJ_DIR%\bin\Debug\net8.0-windows10.0.19041.0\win-x64\CosmoWhisperNative2.exe
if not exist "!EXE_PATH!" set EXE_PATH=%PROJ_DIR%\bin\Debug\net8.0-windows10.0.19041.0\CosmoWhisperNative2.exe

if not exist "!EXE_PATH!" (
    echo ❌ Could not find executable at: !EXE_PATH!
    pause
    exit /b 1
)

:: Start Development App with --dev flag
start "" "!EXE_PATH!" --dev

exit
