# CosmoWhisper Installer
$appName = "CosmoWhisper"
$installPath = "$env:LOCALAPPDATA\$appName"

Write-Host "Installing $appName..." -ForegroundColor Cyan

# Stop any running instances
Get-Process | Where-Object { $_.ProcessName -like "*CosmoWhisper*" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Create install directory
if (!(Test-Path $installPath)) {
    New-Item -ItemType Directory -Path $installPath -Force | Out-Null
}

# Copy files
Write-Host "Copying files to $installPath..."
Copy-Item "FinalRelease\*" -Destination $installPath -Recurse -Force

# Create desktop shortcut
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\$appName.lnk")
$Shortcut.TargetPath = "$installPath\CosmoWhisperNative.exe"
$Shortcut.WorkingDirectory = $installPath
$Shortcut.IconLocation = "$installPath\CosmoWhisperNative.exe,0"
$Shortcut.Save()

# Create start menu shortcut
$startMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
$Shortcut2 = $WshShell.CreateShortcut("$startMenuPath\$appName.lnk")
$Shortcut2.TargetPath = "$installPath\CosmoWhisperNative.exe"
$Shortcut2.WorkingDirectory = $installPath
$Shortcut2.IconLocation = "$installPath\CosmoWhisperNative.exe,0"
$Shortcut2.Save()

Write-Host "`n Installation complete!" -ForegroundColor Green
Write-Host "Location: $installPath"
Write-Host "`nLaunching $appName..."

Start-Process "$installPath\CosmoWhisperNative.exe"
