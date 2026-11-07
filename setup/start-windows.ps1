# Starts Claude Code in the right folder. Use this instead of typing `claude` yourself.
#
#   Double-click setup\start-windows.cmd
#   setup\start-windows.cmd --port 8092     (if you moved Unity's MCP port)
#
# Why: the Unity and Blender connections are saved per folder. `claude` started anywhere else
# can't see Unity, and it quietly falls back to reading scene files by hand.
$ErrorActionPreference = 'Continue'
function Has($c) { [bool](Get-Command $c -ErrorAction SilentlyContinue) }
function RefreshPath {
    $env:Path = "$env:USERPROFILE\.local\bin;" + [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}

$port = 8080
$rest = @()
for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -eq '--port' -and ($i + 1) -lt $args.Count) { $port = [int]$args[$i + 1]; $i++ }
    else { $rest += $args[$i] }
}

Set-Location (Join-Path $PSScriptRoot '..')
RefreshPath
if (-not (Has claude)) { Write-Host 'Claude Code is not installed. Run: setup\setup-windows.cmd (then open a NEW terminal)'; exit 1 }

# Unity can drop the UnityMCP entry after it opens the project. Put it back.
claude mcp get UnityMCP 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    claude mcp add -s local --transport http UnityMCP "http://127.0.0.1:$port/mcp" 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) { Write-Host "Reconnected UnityMCP (port $port)." }
}
foreach ($n in 'blender', 'context7') {
    claude mcp get $n 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host 'Some tools are not connected for this folder. Run: setup\setup-windows.cmd'; break }
}

$prompt = "Workshop context: this is a Unity project and the Unity editor is open. 'Scene', 'objects' and 'console' mean the open Unity scene and the Unity Console. Use the UnityMCP tools (mcp__UnityMCP__*) to inspect and change them. Never parse .unity files by hand. For Blender, use the blender MCP tools."
& claude --append-system-prompt $prompt @rest
