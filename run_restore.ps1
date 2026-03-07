# Restore script for CosmoWhisper-Native
# This script restores the latest backup to the current directory.

$BackupRoot = "C:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native-Backups"
$TargetDir = $PSScriptRoot

if (-not (Test-Path $BackupRoot)) {
    Write-Host "ERROR: Backup directory not found at $BackupRoot" -ForegroundColor Red
    exit
}

# Find the latest backup (sorted by name/date)
$LatestBackup = Get-ChildItem -Path $BackupRoot -Directory | Sort-Object Name -Descending | Select-Object -First 1

if ($null -eq $LatestBackup) {
    Write-Host "ERROR: No backups found in $BackupRoot" -ForegroundColor Red
    exit
}

Write-Host "RESTORE: Found latest backup at $($LatestBackup.FullName)" -ForegroundColor Cyan
$Confirmation = Read-Host "Are you sure you want to overwrite the current folder with this backup? (Y/N)"

if ($Confirmation -ne "Y") {
    Write-Host "RESTORE: Aborted by user." -ForegroundColor Yellow
    exit
}

Write-Host "RESTORE: Initiating restore..." -ForegroundColor Green
# Use robocopy for fast, reliable restore
robocopy $LatestBackup.FullName $TargetDir /E /MT:16 /V /R:1 /W:1 /NP /XD .git .vscode bin obj .gemini

Write-Host "`nRESTORE: COMPLETED SUCCESSFULLY! ✅" -ForegroundColor Green
Write-Host "Source: $($LatestBackup.FullName)" -ForegroundColor Gray
Write-Host "Target: $TargetDir" -ForegroundColor Gray
