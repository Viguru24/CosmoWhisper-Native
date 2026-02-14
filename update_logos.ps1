Add-Type -AssemblyName System.Drawing

$sourceFile = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native\master_logo.png"
$destRoot = "c:\Users\louis\OneDrive\Documents\GitHub"

# Helper for resizing with System.Drawing
function Resize-PNG {
    param($img, $width, $height, $outPath)
    $newImg = New-Object System.Drawing.Bitmap($width, $height)
    $g = [System.Drawing.Graphics]::FromImage($newImg)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($img, 0, 0, $width, $height)
    $newImg.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $newImg.Dispose()
}

# Ensure destination directories exist
function Ensure-Path {
    param($path)
    if (!(Test-Path $path)) { New-Item -ItemType Directory -Path $path -Force | Out-Null }
}

# Load Source
$srcImg = [System.Drawing.Image]::FromFile($sourceFile)
$masterPng = $sourceFile

Write-Host "Updating all logo assets..." -ForegroundColor Cyan

# --- Website Assets ---
$webPublic = "$destRoot\CosmoWhisper-App\website\public"
if (Test-Path $webPublic) {
    Resize-PNG $srcImg 32 32 "$webPublic\favicon.png"
    Resize-PNG $srcImg 192 192 "$webPublic\logo192.png"
    Resize-PNG $srcImg 512 512 "$webPublic\logo512.png"
    Resize-PNG $srcImg 180 180 "$webPublic\apple-touch-icon.png"
    Resize-PNG $srcImg 512 512 "$webPublic\logo.png"
}

# --- Windows App Assets ---
$winApp = "$destRoot\CosmoWhisper-Native\CosmoWhisper-Windows\CosmoWhisper"
if (Test-Path $winApp) {
    Resize-PNG $srcImg 256 256 "$winApp\sidebar_logo.png"
    # Use FFmpeg to create a valid Win32 ICO
    ffmpeg -i $masterPng -vf "scale=256:256" -y "$winApp\app.ico"
}

# --- Deploy & Package Assets ---
$deployFiles = "$destRoot\CosmoWhisper-Native\CosmoWhisper-Windows\Deploy\app_files"
if (Test-Path $deployFiles) {
    Copy-Item "$winApp\sidebar_logo.png" "$deployFiles\sidebar_logo.png" -Force
    Copy-Item "$winApp\app.ico" "$deployFiles\app.ico" -Force
}

$msixImages = "$destRoot\CosmoWhisper-Native\CosmoWhisper-Package\Payload\Images"
if (Test-Path $msixImages) {
    Resize-PNG $srcImg 44 44 "$msixImages\Square44x44Logo.png"
    Resize-PNG $srcImg 150 150 "$msixImages\Square150x150Logo.png"
    Resize-PNG $srcImg 50 50 "$msixImages\StoreLogo.png"
    Resize-PNG $srcImg 310 150 "$msixImages\Wide310x150Logo.png"
    Resize-PNG $srcImg 620 300 "$msixImages\SplashScreen.png"
}

$srcImg.Dispose()
Write-Host "SUCCESS: 11 assets updated with your CW Comet logo." -ForegroundColor Green
