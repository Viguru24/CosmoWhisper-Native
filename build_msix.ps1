$ErrorActionPreference = "Stop"

# Paths
$RepoRoot = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native"
$ProjectDir = "$RepoRoot\CosmoWhisper-Windows\CosmoWhisper"
$PackageDir = "$RepoRoot\CosmoWhisper-Package"
$PayloadDir = "$PackageDir\Payload"
$ImagesDir = "$PayloadDir\Images"
$MakeAppx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"
$SignTool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
$CertName = "CosmoWhisper_SelfSigned"
$CertPass = "CosmoDev123!" # Password for signing cert
$CertPublisher = "CN=E79B034B-3211-490A-96BE-648E426FE339"
$CertSubject = $CertPublisher

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

# Find source icon (we know it's in Deploy/app_files/app.ico based on earlier checks)
$IconSource = Get-ChildItem -Path "$RepoRoot\CosmoWhisper-Windows\Deploy\app_files" -Filter "app.ico" -Recurse | Select-Object -First 1
if (-not $IconSource) {
    Write-Error "Could not find app.ico!"
}

$Icon = [System.Drawing.Icon]::ExtractAssociatedIcon($IconSource.FullName)
$Bitmap = $Icon.ToBitmap()

function Resize-Image {
    param($InputImage, $Width, $Height, $OutputPath, $Fit = $false)
    $NewImage = New-Object System.Drawing.Bitmap($Width, $Height)
    $Graphics = [System.Drawing.Graphics]::FromImage($NewImage)
    $Graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

    if ($Fit) {
        # Fill background with App Theme Color (#333333) to prevent transparency issues on tiles
        $Brush = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#333333"))
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
Write-Host "NOTE: To install, you MUST trust the certificate first. Right-click .msix -> Properties -> Digital Signatures -> Details -> View Certificate -> Install Certificate -> Local Machine -> "Trusted Root Certification Authorities"." -ForegroundColor Yellow
