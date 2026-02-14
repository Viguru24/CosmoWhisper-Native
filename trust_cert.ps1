$ErrorActionPreference = "Stop"

$PfxPath = "$PSScriptRoot\CosmoWhisper-Package\CosmoWhisper_Key.pfx"
$CertPass = "CosmoDev123!"

try {
    Write-Host "Installing Dev Certificate..." -ForegroundColor Cyan
    $Password = ConvertTo-SecureString -String $CertPass -Force -AsPlainText
    
    # Needs Admin for LocalMachine\Root
    Import-PfxCertificate -FilePath $PfxPath -CertStoreLocation Cert:\LocalMachine\Root -Password $Password
    Import-PfxCertificate -FilePath $PfxPath -CertStoreLocation Cert:\LocalMachine\TrustedPeople -Password $Password
    
    Write-Host "SUCCESS! Certificate Installed." -ForegroundColor Green
    Start-Sleep -Seconds 2
}
catch {
    Write-Error "Failed to install certificate: $_"
    Read-Host "Press Enter to exit..."
}
