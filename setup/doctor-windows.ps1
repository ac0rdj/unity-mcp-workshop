# Checks everything the workshop needs. Every [x] line says how to fix it.
#
#   Double-click setup\doctor-windows.cmd
#   setup\doctor-windows.cmd -Full      (also pings every MCP server, about 30 seconds)
#
# Safe to run any time. It changes nothing.
param([switch]$Full)

$ErrorActionPreference = 'Continue'
$script:Failed = $false
function Ok($m)         { Write-Host "  [ok] $m" -ForegroundColor Green }
function Warn($m, $fix) { Write-Host "  [!]  $m" -ForegroundColor Yellow; if ($fix) { Write-Host "       fix: $fix" } }
function Bad($m, $fix)  { Write-Host "  [x]  $m" -ForegroundColor Red; if ($fix) { Write-Host "       fix: $fix" }; $script:Failed = $true }
function Step($m)       { Write-Host ""; Write-Host $m -ForegroundColor White }
function Has($c)        { [bool](Get-Command $c -ErrorAction SilentlyContinue) }
function RefreshPath {
    $env:Path = "$env:USERPROFILE\.local\bin;" + [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}
function Reach($u) {   # any HTTP answer (even 404) means the network works
    try { Invoke-WebRequest -Uri $u -Method Head -UseBasicParsing -TimeoutSec 6 -ErrorAction Stop | Out-Null; return $true }
    catch { if ($_.Exception.Response) { return $true } else { return $false } }
}
function Registered($n) { claude mcp get $n 2>&1 | Out-Null; return ($LASTEXITCODE -eq 0) }
function Listening($p)  { [bool](Get-NetTCPConnection -LocalPort $p -State Listen -ErrorAction SilentlyContinue) }

try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch { }
Set-Location (Join-Path $PSScriptRoot '..')
RefreshPath

Step 'Project folder'
if ((Test-Path 'Assets') -and (Test-Path 'ProjectSettings')) { Ok "$(Get-Location)" }
else { Bad "Not a Unity project folder: $(Get-Location)" 'run this from the unzipped workshop folder (it contains Assets\ and setup\)' }
if ((Test-Path 'Packages\manifest.json') -and ((Get-Content 'Packages\manifest.json' -Raw) -match 'unity-mcp')) { Ok 'MCP for Unity is in Packages\manifest.json' }
else { Bad 'MCP for Unity package missing from Packages\manifest.json' 're-download the project (git pull, or a fresh ZIP)' }

Step 'Unity editor'
$want = ''
$pv = Get-Content 'ProjectSettings\ProjectVersion.txt' -ErrorAction SilentlyContinue | Where-Object { $_ -match '^m_EditorVersion:' } | Select-Object -First 1
if ($pv) { $want = ($pv -replace '^m_EditorVersion:\s*', '').Trim() }
$hubDirs = @("$env:ProgramFiles\Unity\Hub\Editor")
$sec = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'   # editors installed to a custom folder
if (Test-Path $sec) { $p = (Get-Content $sec -Raw).Trim().Trim('"'); if ($p) { $hubDirs += $p } }
$have = @()
foreach ($d in $hubDirs) { if (Test-Path $d) { $have += @(Get-ChildItem $d -Directory | Select-Object -ExpandProperty Name) } }
if ($have.Count -eq 0) { Bad "No Unity editor found in $($hubDirs[0])" 'Unity Hub > Installs > Install Editor > any Unity 6. Needs about 15 GB free.' }
elseif ($have -contains $want) { Ok "Unity $want installed (exact match)" }
elseif (@($have | Where-Object { $_ -like '6000.*' }).Count -gt 0) {
    Ok "Unity 6 installed ($($have -join ', ')). Project was made on $want. When Unity asks about the version, choose Continue / Upgrade."
}
else { Bad "No Unity 6 editor found (have: $($have -join ', '))" 'Unity Hub > Installs > Install Editor > any Unity 6' }

Step 'Disk space'
$freeGB = [math]::Floor((Get-Item .).PSDrive.Free / 1GB)
$need = 10; if (Test-Path 'Library') { $need = 3 }
if ($freeGB -ge $need) { Ok "$freeGB GB free" }
else { Bad "Only $freeGB GB free (need about $need GB for Unity's first import)" 'free space: empty the Recycle Bin, delete old downloads, or run Disk Cleanup' }

Step 'Internet'
foreach ($u in 'https://api.anthropic.com', 'https://github.com', 'https://mcp.context7.com/mcp') {
    if (Reach $u) { Ok "$u reachable" }
    else { Bad "Can't reach $u" 'try another network or a phone hotspot (some venue Wi-Fi blocks these)' }
}

Step 'Tools'
if (Has git) { Ok 'Git installed' } else { Bad 'Git missing' 'setup\setup-windows.cmd' }
if (Has uvx) { Ok 'uv installed' } else { Bad 'uv missing' 'setup\setup-windows.cmd' }
$haveClaude = Has claude
if ($haveClaude) {
    Ok "Claude Code installed ($(claude --version))"
    if (((claude auth status 2>&1) | Out-String) -match '"loggedIn":\s*true') { Ok 'Claude Code is logged in' }
    else { Bad 'Claude Code is not logged in' 'run:  claude   then type /login' }
}
else { Bad 'Claude Code missing' 'setup\setup-windows.cmd (then open a NEW terminal)' }

Step 'Claude is connected to the tools (for THIS folder)'
if ($haveClaude) {
    foreach ($s in 'UnityMCP', 'unity-official', 'blender', 'context7') {
        if (Registered $s) { Ok "$s registered" }
        elseif ($s -eq 'unity-official') { Warn "$s not registered yet" 'open the project in Unity once, then: setup\setup-windows.cmd' }
        else { Bad "$s not registered here" 'setup\setup-windows.cmd   (MCP servers are saved per folder, so rerun it in THIS folder)' }
    }
}

Step 'Unity is running and listening'
$port = 8080
if ($haveClaude) {
    $o = (claude mcp get UnityMCP 2>&1) | Out-String
    if ($o -match '127\.0\.0\.1:(\d+)/mcp') { $port = [int]$Matches[1] }
}
$conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $conn) { Warn "Nothing on port $port" 'open this project in Unity. Window > MCP for Unity should say Session Active (else click Start Server)' }
else {
    $owner = (Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue).ProcessName
    if ($owner -match 'python|uv|unity|mcp') { Ok "Unity's MCP server is listening on port $port ($owner)" }
    else { Warn "Port $port is used by '$owner', not Unity's MCP server" "pick a free port: setup\setup-windows.cmd -Port 8092, then type http://127.0.0.1:8092 in Window > MCP for Unity" }
}
if (Test-Path (Join-Path $env:USERPROFILE '.unity\relay\relay_win.exe')) { Ok "Unity's official relay is installed" }
else { Warn "Unity's official relay not installed yet" 'open the project in Unity once, then run setup again' }

Step 'Blender (Art track only, optional)'
$bl = Get-ChildItem "$env:ProgramFiles\Blender Foundation\Blender*\blender.exe" -ErrorAction SilentlyContinue
if ($bl -or (Has blender)) {
    $addon = Get-ChildItem "$env:APPDATA\Blender Foundation\Blender" -Directory -ErrorAction SilentlyContinue |
        ForEach-Object { Join-Path $_.FullName 'scripts\addons\blender_mcp.py' } | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($addon) { Ok 'Add-on installed' } else { Warn 'Add-on not installed' 'setup\start-blender-windows.cmd' }
    if (Listening 9876) { Ok 'Blender is listening on port 9876' } else { Warn "Blender isn't listening on 9876" 'setup\start-blender-windows.cmd' }
}
else { Warn 'Blender not installed (only needed for the Art track)' 'https://www.blender.org/download/' }

if ($Full -and $haveClaude) {
    Step 'Live connection test (slow)'
    $out = (claude mcp list 2>&1) | Out-String
    foreach ($s in 'UnityMCP', 'unity-official', 'blender', 'context7') {
        $line = ($out -split "`n" | Where-Object { $_ -match "^$s`:" } | Select-Object -First 1)
        if ($line -and ($line -match 'Connected') -and ($line -notmatch 'fail')) { Ok "$s connected" }
        else { Bad "$s not connected: $line" 'see the Troubleshooting table in README.md' }
    }
}

Write-Host ''
if (-not $script:Failed) {
    Write-Host 'All good. ' -ForegroundColor Green -NoNewline
    Write-Host 'Start Claude with:  setup\start-windows.cmd'
    exit 0
}
Write-Host 'Fix the [x] lines above, then run this again.' -ForegroundColor Red
exit 1
