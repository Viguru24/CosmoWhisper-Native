param(
    [string]$Version = "",
    [switch]$Install,
    [switch]$Run
)

$ErrorActionPreference = "Stop"

# Paths
$RepoRoot = $PSScriptRoot
$ProjectDir = "$RepoRoot\CosmoWhisper"
$PackageDir = "$RepoRoot\CosmoWhisper-Package"
$PayloadDir = "$PackageDir\Payload"
$ImagesDir = "$PayloadDir\Images"
$MakeAppx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"
$SignTool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"

$CertPass = "CosmoDev123!" # Password for signing cert
$CertPublisher = "CN=E79B034B-3211-490A-96BE-648E426FE339"
$CertSubject = $CertPublisher

# 0. AUTOMATED VERSION SYNC (The "Rule")
Write-Host "0. Synchronizing Versions..." -ForegroundColor Cyan
if ($Version -ne "") {
    & "$RepoRoot\update_version.ps1" -Version $Version
} else {
    & "$RepoRoot\update_version.ps1"
}
if ($LASTEXITCODE -ne 0) { throw "Version sync failed!" }

# Ensure clean state
if (Test-Path $PayloadDir) { Remove-Item $PayloadDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $ImagesDir | Out-Null

Write-Host "1. Building WPF Application..." -ForegroundColor Cyan
dotnet publish "$ProjectDir\CosmoWhisper.csproj" -c Release -r win-x64 --self-contained true -o $PayloadDir

# Copy Manifest
Copy-Item "$PackageDir\Package.appxmanifest" "$PayloadDir\AppxManifest.xml"

Write-Host "2. Generating Assets from app.ico..." -ForegroundColor Cyan
Add-Type -AssemblyName System.Drawing

$SourceImagePath = "$RepoRoot\master_logo.png"

if (-not (Test-Path $SourceImagePath)) {
    Write-Error "Could not find master_logo.png at $SourceImagePath!"
}

Write-Host "Using high-quality source: $SourceImagePath" -ForegroundColor Green
$Bitmap = [System.Drawing.Bitmap]::FromFile($SourceImagePath)

function Resize-Image {
    param($InputImage, $Width, $Height, $OutputPath, $Fit = $false)
    $NewImage = New-Object System.Drawing.Bitmap($Width, $Height)
    $Graphics = [System.Drawing.Graphics]::FromImage($NewImage)
    $Graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

    if ($Fit) {
        $Brush = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#000000"))
        $Graphics.FillRectangle($Brush, 0, 0, $Width, $Height)
        $Ratio = [Math]::Min($Width / $InputImage.Width, $Height / $InputImage.Height)
        $Ratio = $Ratio * 0.8
        $NewW = [int]($InputImage.Width * $Ratio)
        $NewH = [int]($InputImage.Height * $Ratio)
        $X = [int](($Width - $NewW) / 2)
        $Y = [int](($Height - $NewH) / 2)
        $Graphics.DrawImage($InputImage, $X, $Y, $NewW, $NewH)
        $Brush.Dispose()
    }
    else {
        $Graphics.DrawImage($InputImage, 0, 0, $Width, $Height)
    }

    $NewImage.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $Graphics.Dispose()
    $NewImage.Dispose()
}

Resize-Image $Bitmap 44 44 "$ImagesDir\Square44x44Logo.png"
Resize-Image $Bitmap 150 150 "$ImagesDir\Square150x150Logo.png"
Resize-Image $Bitmap 50 50 "$ImagesDir\StoreLogo.png"
Resize-Image $Bitmap 310 150 "$ImagesDir\Wide310x150Logo.png" $true 
Resize-Image $Bitmap 620 300 "$ImagesDir\SplashScreen.png" $true
$Bitmap.Dispose()

Write-Host "3. Packaging MSIX..." -ForegroundColor Cyan
$MsixPath = "$PackageDir\CosmoWhisper.msix"
if (Test-Path $MsixPath) { Remove-Item $MsixPath }
& $MakeAppx pack /d $PayloadDir /p $MsixPath
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed!" }

Write-Host "4. Signing Package..." -ForegroundColor Cyan
$PfxPath = "$PackageDir\CosmoWhisper_Key.pfx"
if (Test-Path $PfxPath) { Remove-Item $PfxPath -Force }
$Cert = New-SelfSignedCertificate -Type Custom -Subject $CertSubject -KeyUsage DigitalSignature -FriendlyName "CosmoWhisper Dev Cert" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
$Password = ConvertTo-SecureString -String $CertPass -Force -AsPlainText
Export-PfxCertificate -Cert $Cert -FilePath $PfxPath -Password $Password

& $SignTool sign /fd SHA256 /a /f $PfxPath /p $CertPass $MsixPath
if ($LASTEXITCODE -ne 0) { throw "SignTool failed!" }

Write-Host "SUCCESS! MSIX Package Created at: $MsixPath" -ForegroundColor Green
Write-Host "5. Installing Certificate..." -ForegroundColor Cyan
try {
    Import-Certificate -FilePath $PfxPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
    Import-Certificate -FilePath $PfxPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
    Write-Host "Certificate installed!" -ForegroundColor Green
}
catch {
    Write-Warning "Auto-install of certificate failed."
}

if ($Install -or $Run) {
    Write-Host "6. Installing MSIX package locally..." -ForegroundColor Cyan
    Add-AppxPackage -Path $MsixPath -ForceApplicationShutdown
    if ($Run) {
        $pkg = Get-AppxPackage -Name "MicroMeadow.CosmoWhisper"
        if ($pkg) {
            Start-Process "shell:AppsFolder\$($pkg.PackageFamilyName)!App"
        }
    }
} else {
    Start-Process explorer.exe "/select,$MsixPath"
}
