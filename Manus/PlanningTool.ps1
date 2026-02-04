<#
.SYNOPSIS
    Manus Planning Tool (PowerShell Edition) for Antigravity
    Usage: ./PlanningTool.ps1 -Command [create|update|list|get|mark] -Params...
#>

param(
    [string]$Command,
    [string]$Id,
    [string]$Title,
    [string[]]$Steps,
    [int]$StepIndex = -1,
    [string]$Status,
    [string]$Notes
)

$DataFile = "$PSScriptRoot\plans.json"

function Get-Plans {
    if (Test-Path $DataFile) {
        $json = Get-Content $DataFile -Raw -ErrorAction SilentlyContinue
        if ($json) { return $json | ConvertFrom-Json }
    }
    return @{}
}

function Save-Plans($plans) {
    $plans | ConvertTo-Json -Depth 5 | Set-Content $DataFile
}

$plans = Get-Plans

# Initialize if empty/object vs hashtable issues
if (-not $plans) { $plans = @{} }
if ($plans -is [PSCustomObject]) { 
    $newPlans = @{}
    $plans.PSObject.Properties | ForEach-Object { $newPlans[$_.Name] = $_.Value }
    $plans = $newPlans
}

switch ($Command.ToLower()) {
    "create" {
        if (-not $Id -or -not $Title) { Write-Error "Missing ID or Title"; exit }
        $stepList = @()
        if ($Steps) { foreach ($s in $Steps) { $stepList += @{ title = $s; status = "not_started"; notes = "" } } }
        
        $plans.$Id = @{
            id    = $Id
            title = $Title
            steps = $stepList
        }
        Save-Plans $plans
        Write-Output "Plan '$Id' created."
    }
    
    "list" {
        Write-Output "--- MANUS PLANS ---"
        if ($plans.Count -eq 0) { Write-Output "No plans found." }
        foreach ($key in $plans.Keys) {
            $p = $plans.$key
            $completed = ($p.steps | Where-Object { $_.status -eq 'completed' }).Count
            $total = $p.steps.Count
            Write-Output "[$key] $($p.title) ($completed/$total)"
        }
        Write-Output "-------------------"
    }
    
    "get" {
        if (-not $plans.$Id) { Write-Error "Plan not found"; exit }
        $p = $plans.$Id
        Write-Output "PLAN: $($p.title) ($($p.id))"
        $i = 0
        foreach ($s in $p.steps) {
            $mark = "[ ]"
            if ($s.status -eq "completed") { $mark = "[x]" }
            if ($s.status -eq "in_progress") { $mark = "[>]" }
            Write-Output "$i. $mark $($s.title)"
            $i++
        }
    }
    
    "mark" {
        if (-not $plans.$Id) { Write-Error "Plan not found"; exit }
        if ($StepIndex -lt 0) { Write-Error "Invalid Index"; exit }
        
        $p = $plans.$Id
        if ($StepIndex -ge $p.steps.Count) { Write-Error "Index out of range"; exit }
        
        $p.steps[$StepIndex].status = if ($Status) { $Status } else { "completed" }
        if ($Notes) { $p.steps[$StepIndex].notes = $Notes }
        
        Save-Plans $plans
        Write-Output "Step $StepIndex updated in '$Id'."
    }
    
    default {
        Write-Output "Manus Planning Tool v1.0"
        Write-Output "Commands: create, list, get, mark"
    }
}
