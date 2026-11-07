# Unity MCP workshop setup for Windows. Safe to run as many times as you like.
#
#   Double-click setup\setup-windows.cmd       (normal)
#   setup\setup-windows.cmd -Port 8091          (if something else already uses port 8080)
#
# What it does: installs Git, uv and Claude Code if they're missing, then connects Claude Code (in this project only)
# to MCP for Unity, Unity's official MCP, Blender MCP and Context7. Optionally installs the Blender add-on.
param([int]$Port = 8080)

$ErrorActionPreference = 'Continue'
$env:PYTHONUTF8 = '1'          # uvx tools print Unicode; the default Windows console codepage chokes on it
$script:Failed = $false
function Ok($m)   { Write-Host "  [ok] $m" -ForegroundColor Green }
function Warn($m) { Write-Host "  [!]  $m" -ForegroundColor Yellow }
function Bad($m)  { Write-Host "  [x]  $m" -ForegroundColor Red; $script:Failed = $true }
function Step($m) { Write-Host ""; Write-Host $m -ForegroundColor White }
function Has($c)  { [bool](Get-Command $c -ErrorAction SilentlyContinue) }
function RefreshPath {
    $env:Path = "$env:USERPROFILE\.local\bin;" + [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}

Set-Location (Join-Path $PSScriptRoot '..')
if (-not (Test-Path 'Assets') -or -not (Test-Path 'ProjectSettings')) {
    Write-Host 'Run this from inside the downloaded project folder (it should contain Assets\ and setup\).'; exit 1
}
Write-Host "Project: $(Get-Location)"
RefreshPath

Step '1/6  Git (Claude Code on Windows needs it)'
if (Has git) { Ok "Git is installed ($(git --version))" }
elseif (Has winget) {
    Write-Host '  Installing Git...'
    winget install --id Git.Git -e --source winget --accept-package-agreements --accept-source-agreements | Out-Host
    RefreshPath
    if (Has git) { Ok 'Git installed' } else { Bad 'Git did not install. Get it from https://git-scm.com/download/win and run this again.' }
}
else { Bad 'Git is missing. Install it from https://git-scm.com/download/win and run this again.' }

Step '2/6  uv (runs the MCP servers)'
if (Has uvx) { Ok "uv is installed ($(uv --version))" }
else {
    Write-Host '  Installing uv...'
    powershell -NoProfile -ExecutionPolicy Bypass -Command "irm https://astral.sh/uv/install.ps1 | iex" | Out-Host
    RefreshPath
    if (Has uvx) { Ok 'uv installed' } else { Bad 'uv did not install. See https://docs.astral.sh/uv/getting-started/installation/' }
}

Step '3/6  Claude Code'
if (Has claude) { Ok "Claude Code is installed ($(claude --version))" }
else {
    Write-Host '  Installing Claude Code...'
    powershell -NoProfile -ExecutionPolicy Bypass -Command "irm https://claude.ai/install.ps1 | iex" | Out-Host
    RefreshPath
    if (Has claude) { Ok 'Claude Code installed' } else { Bad 'Claude Code did not install. See https://code.claude.com/docs/en/setup' }
}

Step "4/6  Unity's official MCP relay"
$relay = Join-Path $env:USERPROFILE '.unity\relay\relay_win.exe'
if (Test-Path $relay) { Ok "Found $relay" }
else { Warn 'Not installed yet. Open this project in Unity once (Unity installs the relay), then run this script again.'; $relay = $null }

Step '5/6  Connect Claude Code to the tools (this project only)'
if (Has claude) {
    function Add-Server($name, [string[]]$addArgs) {
        claude mcp remove $name -s local 2>&1 | Out-Null   # re-running is safe
        $out = claude mcp add -s local @addArgs 2>&1
        if ($LASTEXITCODE -eq 0) { Ok $name } else { Bad "${name}: $out" }
    }
    Add-Server 'UnityMCP' @('--transport', 'http', 'UnityMCP', "http://127.0.0.1:$Port/mcp")
    if ($relay) { Add-Server 'unity-official' @('unity-official', '--', $relay, '--mcp') }
    Add-Server 'blender' @('blender', '--', 'uvx', 'mcp-for-blender')
    Add-Server 'context7' @('--transport', 'http', 'context7', 'https://mcp.context7.com/mcp')
}
else { Bad 'Skipped (Claude Code missing)' }
$listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($listening) {
    $owner = (Get-Process -Id $listening[0].OwningProcess -ErrorAction SilentlyContinue).ProcessName
    Warn "Port $Port is in use by '$owner'. That's expected if Unity's MCP server is running. If Unity's MCP window"
    Warn "isn't green, pick a free port: run this again with e.g. -Port 8092 and set the same URL in Window > MCP for Unity."
}

Step '6/6  Blender add-on (Art track only, optional)'
$blender = (Has blender) -or (Test-Path "$env:ProgramFiles\Blender Foundation")
if ($blender) {
    $addonOk = $false
    if (Has uvx) { uvx mcp-for-blender install-addon 2>&1 | Out-Null; $addonOk = ($LASTEXITCODE -eq 0) }
    if ($addonOk) {
        Ok 'Add-on copied. In Blender: Edit > Preferences > Add-ons, search "MCP", tick "Interface: MCP for Blender" (or restart Blender).'
    }
    else { Warn "Couldn't copy the add-on. Open Blender once (so it creates its settings folder), then run this again." }
}
else { Warn 'Blender not found. Skip this unless you pick the Art track (get it free at blender.org, open it once, re-run).' }

Write-Host ''
if (-not $script:Failed) {
    Write-Host 'Done. ' -ForegroundColor Green -NoNewline
    Write-Host 'Next: open the project in Unity, then double-click setup\doctor-windows.cmd'
    Write-Host 'Then start Claude by double-clicking setup\start-windows.cmd (always from this project)'
    Write-Host '(Open a NEW terminal first so it picks up the newly installed tools.)'
    exit 0
}
Write-Host 'Some steps failed (see [x] above). Fix them and run this script again.' -ForegroundColor Red
exit 1
