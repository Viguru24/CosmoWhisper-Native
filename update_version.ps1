# update_version.ps1
# AUTOMATED VERSION SYNCHRONIZATION SCRIPT
# This script ensures that the version number is consistent across all project files.
# It treats Package.appxmanifest as the "Source of Truth" and propagates the version to others.

$RepoRoot = $PSScriptRoot
$ManifestPath = "$RepoRoot\CosmoWhisper-Package\Package.appxmanifest"
$CsprojPath = "$RepoRoot\CosmoWhisper-Windows\CosmoWhisper\CosmoWhisper.csproj"
$DashboardPath = "$RepoRoot\CosmoWhisper-Windows\CosmoWhisper\DashboardWindow.xaml"
$SetupIssPath = "$RepoRoot\CosmoWhisper-Windows\CosmoWhisper\setup.iss"

# 1. READ CURRENT VERSION FROM MANIFEST
if (-not (Test-Path $ManifestPath)) {
    Write-Error "Cannot find Package.appxmanifest at $ManifestPath"
    exit 1
}

[xml]$xml = Get-Content $ManifestPath
$currentVersion = $xml.Package.Identity.Version
Write-Host "Current Version: $currentVersion" -ForegroundColor Cyan

# 2. CALCULATE NEW VERSION (Increment Revision)
# Format: Major.Minor.Build.Revision
$parts = $currentVersion.Split('.')
$newRevision = [int]$parts[2] + 1 
# Note: Usually the 3rd number is Build. MSIX uses Major.Minor.Build.Revision.
# Let's increment the 3rd number (Build) to match previous behavior (2.2.25 -> 2.2.26).
$newVersion = "$($parts[0]).$($parts[1]).$newRevision.0"

Write-Host "New Version: $newVersion" -ForegroundColor Green

# 3. UPDATE PACKAGE.APPXMANIFEST
$xml.Package.Identity.Version = $newVersion
$xml.Save($ManifestPath)
Write-Host "Updated Package.appxmanifest"

# 4. UPDATE CSPROJ (Assembly Version)
$csprojContent = Get-Content $CsprojPath
$csprojContent = $csprojContent -replace "<Version>.*?</Version>", "<Version>$newVersion</Version>"
Set-Content -Path $CsprojPath -Value $csprojContent
Write-Host "Updated CosmoWhisper.csproj"

# 5. UPDATE DASHBOARD (UI Display)
# Target: <TextBlock Text="Version 2.2.xx.x-PRO"
$dashboardContent = Get-Content $DashboardPath
$dashboardContent = $dashboardContent -replace 'Version [\d\.]+-PRO', "Version $newVersion-PRO"
Set-Content -Path $DashboardPath -Value $dashboardContent
Write-Host "Updated DashboardWindow.xaml"

# 6. UPDATE SETUP.ISS (Installer)
# Target: #define MyAppVersion "2.2.xx"
$shortVersion = "$($parts[0]).$($parts[1]).$newRevision" # 2.2.26
$setupContent = Get-Content $SetupIssPath
$setupContent = $setupContent -replace '#define MyAppVersion "[\d\.]+"', "#define MyAppVersion ""$shortVersion"""
Set-Content -Path $SetupIssPath -Value $setupContent
Write-Host "Updated setup.iss"

Write-Host "Version Synchronization Complete!" -ForegroundColor Yellow
exit 0
