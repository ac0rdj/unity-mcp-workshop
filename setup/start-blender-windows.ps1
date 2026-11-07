# Opens Blender with the MCP for Blender add-on on, Poly Haven ticked and the server running.
# Replaces the Preferences > Add-ons > tick > N panel > Start clicks.
#
#   Double-click setup\start-blender-windows.cmd
$ErrorActionPreference = 'Continue'
$env:PYTHONUTF8 = '1'
function Has($c) { [bool](Get-Command $c -ErrorAction SilentlyContinue) }
function RefreshPath {
    $env:Path = "$env:USERPROFILE\.local\bin;" + [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}
function Version-Of($dir) { try { return [version]($dir.Name -replace '[^0-9.]', '') } catch { return [version]'0.0' } }

Set-Location (Join-Path $PSScriptRoot '..')
RefreshPath
$boot = (Resolve-Path 'setup\blender_boot.py').Path

# Pick the newest Blender in Program Files.
$exe = Get-ChildItem "$env:ProgramFiles\Blender Foundation\Blender*\blender.exe" -ErrorAction SilentlyContinue |
    Sort-Object { Version-Of $_.Directory } -Descending | Select-Object -First 1
if (-not $exe) { Write-Host 'Blender not found. Install it from https://www.blender.org/download/ and run this again.'; exit 1 }

# The add-on must sit in THIS Blender's version folder (4.5 is not 5.2).
$line = (& $exe.FullName --version 2>$null | Select-Object -First 1)
if ($line -match 'Blender (\d+\.\d+)') { $ver = $Matches[1] } else { Write-Host "Couldn't read Blender's version."; exit 1 }
$dir = Join-Path $env:APPDATA "Blender Foundation\Blender\$ver\scripts\addons"
New-Item -ItemType Directory -Force $dir | Out-Null
if (-not (Has uvx)) { Write-Host 'uv is missing. Run: setup\setup-windows.cmd'; exit 1 }
uvx mcp-for-blender install-addon --addons-dir $dir 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Host "Couldn't copy the add-on into $dir"; exit 1 }
Write-Host "Add-on installed for Blender $ver."

Start-Process -FilePath $exe.FullName -ArgumentList @('--python', "`"$boot`"")

for ($i = 0; $i -lt 12; $i++) {
    Start-Sleep -Seconds 2
    if (Get-NetTCPConnection -LocalPort 9876 -State Listen -ErrorAction SilentlyContinue) {
        Write-Host "Blender $ver is ready: MCP server running on port 9876, Poly Haven on."
        exit 0
    }
}
Write-Host "Blender opened, but port 9876 isn't listening yet. In Blender press N > MCP for Blender > Start MCP Server."
exit 1
