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
dotnet publish "$projectRoot\CosmoWhisper.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $publishDir
if ($LASTEXITCODE -ne 0) { Write-Error "Publish failed with exit code $LASTEXITCODE"; exit 1 }

Write-Host "Waiting for file handles to release..." -ForegroundColor DarkGray
Start-Sleep -Seconds 2

Write-Host "3. Creating Payload Zip..." -ForegroundColor Cyan
try {
    if (Test-Path $payloadPath) { Remove-Item $payloadPath -Force -ErrorAction Stop }
    
    # Use .NET ZipFile for better reliability and performance
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($publishDir, $payloadPath)
    
    if (-not (Test-Path $payloadPath)) { throw "Payload zip was not created." }
    $size = (Get-Item $payloadPath).Length
    if ($size -lt 1000) { throw "Payload zip is suspiciously small ($size bytes)." }
    
    Write-Host "Payload created successfully ($([math]::Round($size / 1MB, 2)) MB)." -ForegroundColor Green
}
catch {
    Write-Error "Failed to create payload.zip: $_"
    exit 1
}

Write-Host "4. Building Installer (Single File)..." -ForegroundColor Cyan
dotnet publish "$setupRoot\CosmoSetup.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$setupRoot\publish"

Write-Host "5. Done! Installer is at:" -ForegroundColor Green
$installerPath = Join-Path $setupRoot "publish\CosmoSetup.exe"
Write-Host $installerPath

# --- Deploy Archiving ---
$version = "2.2.16"
$deployDir = Join-Path $scriptPath "Deploy"
if (-not (Test-Path $deployDir)) { New-Item $deployDir -ItemType Directory }
$versionedInstaller = Join-Path $deployDir "CosmoWhisper_Installer_v$version.exe"
Copy-Item $installerPath $versionedInstaller -Force
Write-Host "6. Archived to Deploy: $versionedInstaller" -ForegroundColor Cyan

Write-Host "7. Launching Installer..." -ForegroundColor Green
Start-Process $versionedInstaller
