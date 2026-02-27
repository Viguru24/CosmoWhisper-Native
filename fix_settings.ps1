$pathNative = "$env:APPDATA\CosmoWhisper_Native\settings.json"
$pathDev = "$env:APPDATA\CosmoWhisper_Dev\settings.json"

function Revert-Manus-Settings($path) {
    if (Test-Path $path) {
        Write-Host "Reverting Manus application settings in $path..."
        $content = Get-Content $path -Raw
        if ([string]::IsNullOrWhiteSpace($content)) { return }
        try {
            $json = $content | ConvertFrom-Json
            
            # Disable Manus Integration in the App (User: "Nothing to do with the application")
            $json.EnableManusAgent = $false
            $json.ManusNarrationEnabled = $false
            
            # Ensure US Settings are still applied ("Enable US")
            $json.EnableRegionalSpelling = $true
            $json.InterfaceLanguage = "en-US"
            $json.SelectedDateFormat = "MM/dd/yyyy"
            
            $json | ConvertTo-Json -Depth 10 | Set-Content $path
            Write-Host "Updated settings in $path"
        }
        catch {
            Write-Host "Error parsing JSON: $_"
        }
    }
}

Revert-Manus-Settings $pathNative
Revert-Manus-Settings $pathDev


