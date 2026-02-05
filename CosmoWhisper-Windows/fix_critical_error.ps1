$xamlPath = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native\CosmoWhisper-Windows\CosmoWhisper\DashboardWindow.xaml"
# Read as raw bytes to avoid encoding assumptions
$bytes = [System.IO.File]::ReadAllBytes($xamlPath)
$content = [System.Text.Encoding]::UTF8.GetString($bytes)

# Replace any multi-character PasswordChar mangling with a safe asterisk
# This regex targets PasswordChar="any characters"
$newContent = $content -replace 'PasswordChar="[^"]+"', 'PasswordChar="*"'

# Also check for the specific mangled string if it exists separately
$newContent = $newContent -replace 'â— ', '*'

# Write back as UTF8 with BOM to ensure WPF is happy
$utf8WithBom = New-Object System.Text.UTF8Encoding($true)
[System.IO.File]::WriteAllText($xamlPath, $newContent, $utf8WithBom)

Write-Output "Fixed PasswordChar encoding issues and set to safe asterisk."
