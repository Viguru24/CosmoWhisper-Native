# Switch CosmoWhisper Vocabulary to Batman Theme
Write-Host " Switching to Batman Vocabulary..." -ForegroundColor DarkCyan

$ProjectRoot = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native"
$BatmanSource = "$ProjectRoot\CosmoWhisper-Windows\CosmoWhisper\Managers\VocabularyExamples_Batman.json"
$ActiveVocab = "$env:APPDATA\CosmoWhisper\vocabulary.json"
$BackupVocab = "$env:APPDATA\CosmoWhisper\vocabulary.backup.json"

if (Test-Path $BatmanSource) {
    if (Test-Path $ActiveVocab) {
        Copy-Item $ActiveVocab $BackupVocab -Force
        Write-Host " Backed up current vocabulary to vocabulary.backup.json" -ForegroundColor Gray
    }
    
    Copy-Item $BatmanSource $ActiveVocab -Force
    Write-Host " Batman Vocabulary successfully loaded!" -ForegroundColor Green
    Write-Host "Please restart CosmoWhisper or click 'Reload Examples' to see changes." -ForegroundColor Cyan
}
else {
    Write-Error "Batman vocabulary file not found at $BatmanSource"
}
