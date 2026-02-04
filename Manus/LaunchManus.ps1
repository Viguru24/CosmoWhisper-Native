Write-Host 'Starting MANUS for CosmoWhisper-Native...' -ForegroundColor Cyan
Set-Location 'c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native'
& 'c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native\Manus\PlanningTool.ps1' -Command list
Write-Host 'Manus Active. You can use ./Manus/PlanningTool.ps1 to manage plans.' -ForegroundColor Green
Read-Host 'Press Enter to close...'
