$source = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native"
$target = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native\Backups\Manual_Backup_$(Get-Date -Format 'yyyyMMdd_HHmm')"

Write-Host "Starting optimized backup: $source -> $target"
New-Item -ItemType Directory -Path $target -Force

# Copy files excluding bin, obj, git and backup folders
Get-ChildItem -Path $source -Recurse -Exclude "bin", "obj", ".git", ".cursor", ".gemini", "Backups", "_ProjectBackups" | ForEach-Object {
    $destPath = $_.FullName.Replace($source, $target)
    if ($_.PSIsContainer) {
        New-Item -ItemType Directory -Path $destPath -Force | Out-Null
    }
    else {
        $parent = Split-Path -Parent $destPath
        if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
        Copy-Item -Path $_.FullName -Destination $destPath -Force
    }
}

Write-Host "Backup completed successfully!"
