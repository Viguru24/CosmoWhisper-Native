# Uninstall.ps1 - CosmoWhisper Uninstaller

# 1. Close Application if running
Get-Process "CosmoWhisperNative" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process "CosmoWhisper" -ErrorAction SilentlyContinue | Stop-Process -Force

Start-Sleep -Seconds 2

# 2. Remove Registry Keys
Remove-Item -Path "HKCU:\Software\Classes\cosmowhisper" -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\CosmoWhisper" -Recurse -ErrorAction SilentlyContinue
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "CosmoWhisper" -ErrorAction SilentlyContinue

# 3. Remove Shortcuts
$desktop = [Environment]::GetFolderPath("Desktop")
$startMenu = [Environment]::GetFolderPath("ApplicationData") + "\Microsoft\Windows\Start Menu\Programs"

Remove-Item "$desktop\CosmoWhisper.lnk" -ErrorAction SilentlyContinue
Remove-Item "$startMenu\CosmoWhisper.lnk" -ErrorAction SilentlyContinue

# 4. Remove Files (The script itself is running from here, so we schedule a self-delete cmd)
$installPath = $PSScriptRoot

Write-Host "Uninstallation Complete. Removing files..."

# Launch a background process to delete the folder after this script exits
Start-Process cmd.exe -ArgumentList "/C timeout /t 3 & rmdir /s /q `"$installPath`"" -WindowStyle Hidden

Exit
