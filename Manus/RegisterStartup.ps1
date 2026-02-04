Write-Host "Checking for existing Manus shortcuts..."
$ShortcutPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\LaunchManus_Cosmo.lnk"
$TargetScript = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native\Manus\LaunchManus.ps1"

# Create the launch script
$LaunchContent = @"
Write-Host 'Starting MANUS for CosmoWhisper-Native...' -ForegroundColor Cyan
Set-Location 'c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native'
& 'c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native\Manus\PlanningTool.ps1' -Command list
Write-Host 'Manus Active. You can use ./Manus/PlanningTool.ps1 to manage plans.' -ForegroundColor Green
Read-Host 'Press Enter to close...'
"@
Set-Content -Path $TargetScript -Value $LaunchContent

# Create Shortcut
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = "powershell.exe"
$Shortcut.Arguments = "-NoExit -ExecutionPolicy Bypass -File `"$TargetScript`""
$Shortcut.WorkingDirectory = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native"
$Shortcut.Save()

Write-Host "Manus Startup Shortcut created at: $ShortcutPath"
