$pathNative = "$env:APPDATA\CosmoWhisper_Native\settings.json"
$pathDev = "$env:APPDATA\CosmoWhisper_Dev\settings.json"

function Revert-Manus-Settings($path) {
    if (Test-Path $path) {
        Write-Host "Reverting Manus settings in $path..."
        $content = Get-Content $path -Raw
        if ([string]::IsNullOrWhiteSpace($content)) { return }
        try {
            $json = $content | ConvertFrom-Json
            
            # Disable Manus Integration in the App
            $json.EnableManusAgent = $false
            $json.ManusNarrationEnabled = $false
            
            # Keep US Settings (User asked for US previously)
            # $json.EnableRegionalSpelling = $true
            # $json.InterfaceLanguage = "en-US"
            
            $json | ConvertTo-Json -Depth 10 | Set-Content $path
            Write-Host "Reverted Manus settings in $path"
        } catch {
            Write-Host "Error parsing JSON: $_"
        }
    }
}

Revert-Manus-Settings $pathNative
Revert-Manus-Settings $pathDev


