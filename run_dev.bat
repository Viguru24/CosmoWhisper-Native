@echo off
setlocal
title CosmoWhisper Dev Launcher (Publish Mode)

set APP_EXE=CosmoWhisperNative2.exe
set APP_PATH=c:\CosmoWhisper-Dev-Restore\CosmoWhisper-Windows\CosmoWhisper\bin\Debug\net8.0-windows10.0.19041.0\win-x64

echo [1/3] Terminating any existing CosmoWhisper processes...
taskkill /F /IM %APP_EXE% /T 2>nul
taskkill /F /IM CosmoWhisper.exe /T 2>nul

echo [2/3] Changing directory to Publish folder...
cd /d "%APP_PATH%"

echo [3/3] Launching CosmoWhisper in --dev mode...
start "" "%APP_EXE%" --dev

echo.
echo ==================================================
echo   CosmoWhisper is now running from DEV OUTPUT
echo   Location: %APP_PATH%
echo ==================================================
timeout /t 3
exit
