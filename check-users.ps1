$json = (Invoke-WebRequest -Uri "https://api.cosmowhisper.com/api/admin/users" -Headers @{"x-admin-key"="cosmo-admin-2026"} -UseBasicParsing).Content
$obj = $json | ConvertFrom-Json
$badUsers = $obj | Where-Object { [string]::IsNullOrWhiteSpace($_.email) }
if ($badUsers) {
    Write-Host "Found $($badUsers.Count) users with no email!"
    foreach ($u in $badUsers) {
        Write-Host "ID: $($u.id)"
    }
} else {
    Write-Host "All users have emails."
}
