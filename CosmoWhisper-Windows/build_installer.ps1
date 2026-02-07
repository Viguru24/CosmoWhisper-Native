$ErrorActionPreference = "Stop"
$scriptPath = $PSScriptRoot
$projectRoot = Join-Path $scriptPath "CosmoWhisper"
$setupRoot = Join-Path $scriptPath "CosmoSetup"
$publishDir = Join-Path $projectRoot "publish_output\release"
$payloadPath = Join-Path $setupRoot "payload.zip"

Write-Host "0. Killing all possible 'zombie' instances..." -ForegroundColor Cyan
$processes = @("CosmoWhisperNative", "CosmoWhisper", "CosmoSetup", "CosmoWhisper-Windows")
foreach ($proc in $processes) {
    Stop-Process -Name $proc -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 2

Write-Host "1. Cleaning previous builds..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $payloadPath) { Remove-Item $payloadPath -Force }

Write-Host "2. Publishing CosmoWhisper (Main App)..." -ForegroundColor Cyan
# Using SingleFile to keep it clean, but ensuring we include resources
dotnet publish "$projectRoot\CosmoWhisper.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDir
if ($LASTEXITCODE -ne 0) { Write-Error "Publish failed with exit code $LASTEXITCODE"; exit 1 }

Write-Host "Waiting for file handles to release..." -ForegroundColor DarkGray
Start-Sleep -Seconds 2

Write-Host "3. Creating Payload Zip..." -ForegroundColor Cyan
$maxRetries = 5
$retryCount = 0
$success = $false

while (-not $success -and $retryCount -lt $maxRetries) {
    try {
        if (Test-Path $payloadPath) { Remove-Item $payloadPath -Force -ErrorAction Stop }
        Compress-Archive -Path "$publishDir\*" -DestinationPath $payloadPath -Force -ErrorAction Stop
        $success = $true
    }
    catch {
        Write-Warning "Compression failed (Assessment: File might be locked). Retrying in 2 seconds... ($($retryCount + 1)/$maxRetries)"
        Start-Sleep -Seconds 2
        $retryCount++
    }
}

if (-not $success) {
    Write-Error "Failed to create payload.zip after multiple attempts."
}

Write-Host "4. Building Installer (Single File)..." -ForegroundColor Cyan
dotnet publish "$setupRoot\CosmoSetup.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$setupRoot\publish"

Write-Host "5. Done! Installer is at:" -ForegroundColor Green
$installerPath = Join-Path $setupRoot "publish\CosmoSetup.exe"
Write-Host $installerPath

# --- Deploy Archiving ---
$version = "2.2.11"
$deployDir = Join-Path $scriptPath "Deploy"
if (-not (Test-Path $deployDir)) { New-Item $deployDir -ItemType Directory }
$versionedInstaller = Join-Path $deployDir "CosmoWhisper_Installer_v$version.exe"
Copy-Item $installerPath $versionedInstaller -Force
Write-Host "6. Archived to Deploy: $versionedInstaller" -ForegroundColor Cyan

Write-Host "7. Launching Installer..." -ForegroundColor Green
Start-Process $versionedInstaller
