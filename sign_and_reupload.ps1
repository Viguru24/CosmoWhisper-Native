# sign_and_reupload.ps1
# Assemble, sign, and upload whisper-cuda-12.zip
$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
if (-not $RepoRoot) { $RepoRoot = Get-Location }

$tempDir = Join-Path $env:TEMP "cosmo_cuda_temp_signed"
if (Test-Path $tempDir) { Remove-Item -Recurse -Force $tempDir }
New-Item -ItemType Directory -Path $tempDir | Out-Null

Write-Host "Copying Whisper NuGet CUDA libraries..." -ForegroundColor Cyan
$nugetDir = "C:\Users\louis\.nuget\packages\whisper.net.runtime.cuda.windows\1.9.0\build\win-x64"
$whisperDlls = @("whisper.dll", "ggml-cuda-whisper.dll", "ggml-base-whisper.dll", "ggml-cpu-whisper.dll", "ggml-whisper.dll")
foreach ($dll in $whisperDlls) {
    Copy-Item (Join-Path $nugetDir $dll) (Join-Path $tempDir $dll)
}

Write-Host "Copying CUDA 13 system runtime libraries..." -ForegroundColor Cyan
$cudaSourceDir = "C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v13.1\bin\x64"
$cudaDlls = @("cublas64_13.dll", "cublasLt64_13.dll", "cudart64_13.dll", "nvJitLink_130_0.dll")
foreach ($dll in $cudaDlls) {
    Copy-Item (Join-Path $cudaSourceDir $dll) (Join-Path $tempDir $dll)
}

Write-Host "Signing all DLLs with dev certificate..." -ForegroundColor Yellow
$SignTool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
$PfxPath = "$RepoRoot\CosmoWhisper-Windows\CosmoWhisper-Package\CosmoWhisper_Key.pfx"
$CertPass = "CosmoDev123!"

if (-not (Test-Path $PfxPath)) {
    throw "PFX certificate not found at: $PfxPath. Run CosmoWhisper-Windows\build_msix.ps1 first to generate it!"
}

$dllsToSign = Get-ChildItem -Path $tempDir -Filter "*.dll"
foreach ($dll in $dllsToSign) {
    Write-Host "Signing $($dll.Name)..." -ForegroundColor Gray
    & $SignTool sign /fd SHA256 /a /f $PfxPath /p $CertPass $dll.FullName
    if ($LASTEXITCODE -ne 0) { throw "Failed to sign $($dll.Name)!" }
}

Write-Host "Creating zip archive..." -ForegroundColor Yellow
$zipPath = Join-Path $env:TEMP "whisper-cuda-13.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath }
Compress-Archive -Path (Join-Path $tempDir "*") -DestinationPath $zipPath

Write-Host "Moving to backend public folder..." -ForegroundColor Cyan
$backendPublicLibs = "$RepoRoot\..\CosmoWhisper-App\backend\public\static\libs"
if (-not (Test-Path $backendPublicLibs)) {
    # Fallback/alternative path
    $backendPublicLibs = "$RepoRoot\backend\public\static\libs"
}
New-Item -ItemType Directory -Path $backendPublicLibs -Force | Out-Null
Copy-Item $zipPath (Join-Path $backendPublicLibs "whisper-cuda-13.zip") -Force

Write-Host "Uploading to Ra Server..." -ForegroundColor Green
$VPS_IP = "192.168.1.28"
$VPS_PORT = "22"
$VPS_USER = "viguru"
$REMOTE_LIBS_PATH = "/opt/cosmowhisper/backend/public/static/libs"

# Ensure directory exists on VPS and copy zip
ssh -p $VPS_PORT "$VPS_USER@$VPS_IP" "mkdir -p $REMOTE_LIBS_PATH"
scp -P $VPS_PORT $zipPath "$VPS_USER@$VPS_IP`:$REMOTE_LIBS_PATH/whisper-cuda-13.zip"

Write-Host "Cleanup..." -ForegroundColor Gray
Remove-Item -Recurse -Force $tempDir
Remove-Item $zipPath

Write-Host "Done! Signed whisper-cuda-13.zip is now live on api.cosmowhisper.com." -ForegroundColor Green
