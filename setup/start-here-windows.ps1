# One click for everything: sets up, checks, then opens Claude Code in this project.
# Run it again any time. If it shows a red line, fix that and run it again.
# Extra options: --port 8092 (if port 8080 is busy). Anything else goes to Claude.
$ErrorActionPreference = 'Continue'
$port = 8080
$rest = @()
for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -eq '--port' -and ($i + 1) -lt $args.Count) { $port = [int]$args[$i + 1]; $i++ }
    else { $rest += $args[$i] }
}
Set-Location (Join-Path $PSScriptRoot '..')
$env:START_HERE = '1'

Write-Host ''; Write-Host 'Step 1 of 3: setup' -ForegroundColor White
& "$PSScriptRoot\setup-windows.ps1" -Port $port
if ($LASTEXITCODE -ne 0) { Write-Host ''; Write-Host 'Fix the red lines above, then run this again.' -ForegroundColor Red; exit 1 }

Write-Host ''; Write-Host 'Step 2 of 3: check' -ForegroundColor White
& "$PSScriptRoot\doctor-windows.ps1"
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host ''; Write-Host 'Step 3 of 3: opening Claude' -ForegroundColor White
& "$PSScriptRoot\start-windows.ps1" --port $port @rest
