# CosmoWhisper Development Launcher
# Following Louis's rule: Use PowerShell for all scripts.

$ErrorActionPreference = "SilentlyContinue"

Write-Host "`n[INFO] Launching CosmoWhisper Development (Orange)..." -ForegroundColor Cyan
Write-Host "---------------------------------------------------"

# 1. Stopping clashing processes
Write-Host "[WAIT] Stopping clashing processes..." -ForegroundColor Yellow
$clashingProcs = @("CosmoWhisperNative", "CosmoWhisperNative2", "CosmoWhisper")
foreach ($procName in $clashingProcs) {
    $procs = Get-Process $procName -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "   -> Found running instance of $procName. Terminating..." -ForegroundColor Gray
        $procs | Stop-Process -Force
    }
}

# 2. Waiting for file locks
Write-Host "[WAIT] Waiting for processes to fully release file locks..." -ForegroundColor Gray
Start-Sleep -Seconds 2

# 3. Path setup
$rootDir = $PSScriptRoot
if (-not $rootDir) { $rootDir = Get-Location }
$projDir = Join-Path $rootDir "CosmoWhisper-Windows\CosmoWhisper"
$csproj = Join-Path $projDir "CosmoWhisper.csproj"

Write-Host "[INFO] Project Dir: $projDir" -ForegroundColor DarkCyan

if (-not (Test-Path $csproj)) {
    Write-Host "[FAIL] ERROR: Could not find project file at: $csproj" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# 4. Building
Write-Host "[BUILD] Building Development binary..." -ForegroundColor Cyan
# Removed /clp:ErrorsOnly to see full output
dotnet build "$csproj" -c Debug -r win-x64 --no-self-contained -nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "[WARN] Build failed with win-x64. Retrying generic build..." -ForegroundColor Yellow
    dotnet build "$csproj" -c Debug -nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] CRITICAL: Build Failure. Check the output above." -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
}

# 5. Launching
Write-Host "[LAUNCH] Starting Development Instance..." -ForegroundColor Green

# Check common build locations
$exeLocations = @(
    (Join-Path $projDir "bin\Debug\net8.0-windows10.0.19041.0\win-x64\CosmoWhisperNative2.exe"),
    (Join-Path $projDir "bin\Debug\net8.0-windows10.0.19041.0\CosmoWhisperNative2.exe"),
    (Join-Path $projDir "bin\win-x64\Debug\net8.0-windows10.0.19041.0\CosmoWhisperNative2.exe")
)

$exePath = $null
foreach ($loc in $exeLocations) {
    if (Test-Path $loc) {
        $exePath = $loc
        break
    }
}

if (-not $exePath) {
    Write-Host "[FAIL] ERROR: Could not find executable in any common build locations." -ForegroundColor Red
    Write-Host "Search list:" -ForegroundColor DarkGray
    $exeLocations | ForEach-Object { Write-Host "  - $_" -ForegroundColor DarkGray }
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "[INFO] Launching: $exePath" -ForegroundColor Magenta
try {
    # Using Start-Process with -WorkingDirectory and -PassThru
    $proc = Start-Process -FilePath "$exePath" -ArgumentList "--dev" -WorkingDirectory "$projDir" -PassThru
    if ($proc) {
        Write-Host "[OK] Process started successfully (PID: $($proc.Id))" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Process failed to start." -ForegroundColor Red
    }
} catch {
    Write-Host "[FAIL] Error launching process: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n[FINISH] Launcher complete." -ForegroundColor Cyan
Write-Host "Window will stay open for 10 seconds, or press any key to close..." -ForegroundColor Gray
if ($Host.UI.RawUI.KeyAvailable) { $Host.UI.RawUI.FlushInputBuffer() }
# Wait for 10 seconds or key press
$waitCount = 0
while ($waitCount -lt 100 -and -not $Host.UI.RawUI.KeyAvailable) {
    Start-Sleep -Milliseconds 100
    $waitCount++
}

exit 0
