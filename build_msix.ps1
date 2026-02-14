$ErrorActionPreference = "Stop"

# Paths
$RepoRoot = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native"
$ProjectDir = "$RepoRoot\CosmoWhisper-Windows\CosmoWhisper"
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
& "$RepoRoot\update_version.ps1"
if ($LASTEXITCODE -ne 0) { throw "Version sync failed!" }

# Ensure clean state
if (Test-Path $PayloadDir) { Remove-Item $PayloadDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $ImagesDir | Out-Null

Write-Host "1. Building WPF Application..." -ForegroundColor Cyan
# ENABLE SELF-CONTAINED to fix "Undisclosed Dependency" store rejection
dotnet publish "$ProjectDir\CosmoWhisper.csproj" -c Release -r win-x64 --self-contained true -o $PayloadDir

# Copy Manifest
Copy-Item "$PackageDir\Package.appxmanifest" "$PayloadDir\AppxManifest.xml"

Write-Host "2. Generating Assets from app.ico..." -ForegroundColor Cyan
Add-Type -AssemblyName System.Drawing

# Use master_logo.png as the source for high-quality assets
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
        # Fill background with App Theme Color (#000000) to match the master logo
        $Brush = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#000000"))
        $Graphics.FillRectangle($Brush, 0, 0, $Width, $Height)

        # Calculate best fit ratio
        $Ratio = [Math]::Min($Width / $InputImage.Width, $Height / $InputImage.Height)
        $Ratio = $Ratio * 0.8 # 80% scale to leave healthy padding

        $NewW = [int]($InputImage.Width * $Ratio)
        $NewH = [int]($InputImage.Height * $Ratio)
        $X = [int](($Width - $NewW) / 2)
        $Y = [int](($Height - $NewH) / 2)

        $Graphics.DrawImage($InputImage, $X, $Y, $NewW, $NewH)
        $Brush.Dispose()
    }
    else {
        # Standard stretch (fine for squares)
        $Graphics.DrawImage($InputImage, 0, 0, $Width, $Height)
    }

    $NewImage.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $Graphics.Dispose()
    $NewImage.Dispose()
}

# Generate required assets
# Sizes based on typical store requirements and manifest
Resize-Image $Bitmap 44 44 "$ImagesDir\Square44x44Logo.png"
Resize-Image $Bitmap 150 150 "$ImagesDir\Square150x150Logo.png"
Resize-Image $Bitmap 50 50 "$ImagesDir\StoreLogo.png"
# Use Fit=$true for non-square images to avoid distortion (Store Rejection 10.1.1.11)
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

if (Test-Path $PfxPath) {
    Write-Host "Reusing existing certificate: $PfxPath" -ForegroundColor Gray
}
else {
    Write-Host "Creating NEW Self-Signed Certificate..." -ForegroundColor Yellow
    
    # Create Self-Signed Certificate
    $Cert = New-SelfSignedCertificate -Type Custom -Subject $CertSubject -KeyUsage DigitalSignature -FriendlyName "CosmoWhisper Dev Cert" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    
    # Export PFX
    $Password = ConvertTo-SecureString -String $CertPass -Force -AsPlainText
    Export-PfxCertificate -Cert $Cert -FilePath $PfxPath -Password $Password
    Write-Host "NEW Certificate created and exported to PFX." -ForegroundColor Yellow
}


# Sign
& $SignTool sign /fd SHA256 /a /f $PfxPath /p $CertPass $MsixPath
if ($LASTEXITCODE -ne 0) { throw "SignTool failed!" }

Write-Host "SUCCESS! MSIX Package Created at: $MsixPath" -ForegroundColor Green

Write-Host "5. Installing Certificate (Trying auto-install)..." -ForegroundColor Cyan
try {
    # Install to CurrentUser (no admin needed, but not universally trusted like LocalMachine)
    # The trick for side-loading is installing into Trusted Root CA of the MACHINE or USER.
    # We will try installing to CurrentUser\Root which is allowed without admin elevation (sometimes),
    # but for true side-loading on Windows on many configs, LocalMachine\Root is required.
    
    # Try importing directly
    Write-Host "Importing to Trusted Root..."
    Import-Certificate -FilePath $PfxPath -CertStoreLocation Cert:\CurrentUser\Root -Verbose
    Import-Certificate -FilePath $PfxPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople -Verbose
    
    Write-Host "Certificate installed! You should be able to install the MSIX now." -ForegroundColor Green
}
catch {
    Write-Warning "Auto-install of certificate failed (likely needs Admin rights)."
    Write-Host "MANUAL STEP REQUIRED:" -ForegroundColor Yellow
    Write-Host "1. Double-click '$PfxPath'"
    Write-Host "2. Select 'Current User' or 'Local Machine'"
    Write-Host "3. Password is: $CertPass"
    Write-Host "4. Place in 'Trusted Root Certification Authorities'"
}

Start-Process explorer.exe "/select,$MsixPath"
