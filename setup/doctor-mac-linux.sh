#!/usr/bin/env bash
# Checks everything the workshop needs. Every red line says how to fix it.
#
#   bash setup/doctor-mac-linux.sh          # about 15 seconds
#   bash setup/doctor-mac-linux.sh --full   # also pings every MCP server (about 30 seconds)
#
# Safe to run any time. It changes nothing.
set -u
cd "$(dirname "$0")/.." || exit 1
export PATH="$HOME/.local/bin:$HOME/.cargo/bin:/opt/homebrew/bin:/usr/local/bin:$PATH"
FULL=0; [ "${1:-}" = "--full" ] && FULL=1
UA='unity-mcp-workshop-doctor/1.0 (curl)'
FAILED=0
ok()   { printf '  \033[32m✔\033[0m %s\n' "$1"; }
warn() { printf '  \033[33m!\033[0m %s\n' "$1"; [ -n "${2:-}" ] && printf '      fix: %s\n' "$2"; }
bad()  { printf '  \033[31m✘\033[0m %s\n' "$1"; [ -n "${2:-}" ] && printf '      fix: %s\n' "$2"; FAILED=1; }
step() { printf '\n\033[1m%s\033[0m\n' "$1"; }

step "Project folder"
if [ -d Assets ] && [ -d ProjectSettings ]; then ok "$(pwd)"
else bad "Not a Unity project folder: $(pwd)" "run this from the unzipped workshop folder (it contains Assets/ and setup/)"; fi
if [ -f Packages/manifest.json ] && grep -q "unity-mcp" Packages/manifest.json; then ok "MCP for Unity is in Packages/manifest.json"
else bad "MCP for Unity package missing from Packages/manifest.json" "re-download the project (git pull, or a fresh ZIP)"; fi

step "Unity editor"
WANT=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt 2>/dev/null | head -1)
case "$(uname -s)" in
  Darwin) HUB="/Applications/Unity/Hub/Editor"; HUB2F="$HOME/Library/Application Support/UnityHub/secondaryInstallPath.json" ;;
  *)      HUB="$HOME/Unity/Hub/Editor";         HUB2F="$HOME/.config/UnityHub/secondaryInstallPath.json" ;;
esac
HUB2=""; [ -f "$HUB2F" ] && HUB2=$(tr -d '"' < "$HUB2F")   # editors installed to a custom folder
HAVE=$( { ls "$HUB" 2>/dev/null; [ -n "$HUB2" ] && ls "$HUB2" 2>/dev/null; } | tr '\n' ' ')
if [ -z "$HAVE" ]; then
  bad "No Unity editor found in $HUB" "Unity Hub > Installs > Install Editor > any Unity 6. Needs about 15 GB free."
elif echo " $HAVE" | grep -q " $WANT "; then ok "Unity $WANT installed (exact match)"
elif echo " $HAVE" | grep -q " 6000\."; then
  ok "Unity 6 installed (${HAVE% }). Project was made on $WANT. When Unity asks about the version, choose Continue / Upgrade."
else bad "No Unity 6 editor found (have: ${HAVE% })" "Unity Hub > Installs > Install Editor > any Unity 6"; fi

step "Disk space"
if [ -d /System/Volumes/Data ]; then TARGET=/System/Volumes/Data; else TARGET="$HOME"; fi
FREE=$(df -k "$TARGET" 2>/dev/null | awk 'NR==2{printf "%d", $4/1048576}')
if [ -d Library ]; then NEED=3; else NEED=10; fi
if [ -z "$FREE" ]; then warn "Couldn't read free disk space"
elif [ "$FREE" -ge "$NEED" ]; then ok "${FREE} GB free"
else bad "Only ${FREE} GB free (need about ${NEED} GB for Unity's first import)" "free space: empty the Trash, delete old downloads, or clear caches"; fi

step "Internet"
for u in https://api.anthropic.com https://github.com https://mcp.context7.com/mcp; do
  code=$(curl -s -o /dev/null -w '%{http_code}' -m 6 -A "$UA" "$u" 2>/dev/null)
  if [ -n "$code" ] && [ "$code" != 000 ]; then ok "$u reachable"
  else bad "Can't reach $u" "try another network or a phone hotspot (some venue Wi-Fi blocks these)"; fi
done

step "Tools"
if command -v uvx >/dev/null 2>&1; then ok "uv installed"; else bad "uv missing" "bash setup/setup-mac-linux.sh"; fi
if command -v claude >/dev/null 2>&1; then
  ok "Claude Code installed ($(claude --version 2>/dev/null))"
  if claude auth status 2>/dev/null | grep -q '"loggedIn": true'; then ok "Claude Code is logged in"
  else bad "Claude Code is not logged in" "run:  claude   then type /login"; fi
else bad "Claude Code missing" "bash setup/setup-mac-linux.sh (then open a NEW terminal)"; fi

step "Claude is connected to the tools (for THIS folder)"
if command -v claude >/dev/null 2>&1; then
  for s in UnityMCP unity-official blender context7; do
    if claude mcp get "$s" >/dev/null 2>&1; then ok "$s registered"
    elif [ "$s" = "unity-official" ]; then warn "$s not registered yet" "open the project in Unity once, then: bash setup/setup-mac-linux.sh"
    else bad "$s not registered here" "bash setup/setup-mac-linux.sh   (MCP servers are saved per folder, so rerun it in THIS folder)"; fi
  done
fi

step "Unity is running and listening"
PORT=$(claude mcp get UnityMCP 2>/dev/null | sed -n 's|.*127\.0\.0\.1:\([0-9]*\)/mcp.*|\1|p' | head -1); PORT="${PORT:-8080}"
if lsof -nP -iTCP:"$PORT" -sTCP:LISTEN >/dev/null 2>&1; then ok "Something is listening on port $PORT (Unity's MCP server)"
else warn "Nothing on port $PORT" "open this project in Unity. Window > MCP for Unity should say Session Active (else click Start Server)"; fi
case "$(uname -s)-$(uname -m)" in
  Darwin-arm64) RELAY="$HOME/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64" ;;
  Darwin-*)     RELAY="$HOME/.unity/relay/relay_mac_x64.app/Contents/MacOS/relay_mac_x64" ;;
  *)            RELAY="$HOME/.unity/relay/relay_linux" ;;
esac
if [ -x "$RELAY" ]; then ok "Unity's official relay is installed"
else warn "Unity's official relay not installed yet" "open the project in Unity once, then run setup again"; fi

step "Blender (Art track only, optional)"
if ls -d /Applications/Blender*.app >/dev/null 2>&1 || command -v blender >/dev/null 2>&1; then
  ADD=$(ls "$HOME"/Library/Application\ Support/Blender/*/scripts/addons/blender_mcp.py "$HOME"/.config/blender/*/scripts/addons/blender_mcp.py 2>/dev/null | head -1)
  if [ -n "$ADD" ]; then ok "Add-on installed"; else warn "Add-on not installed" "bash setup/setup-mac-linux.sh"; fi
  if lsof -nP -iTCP:9876 -sTCP:LISTEN >/dev/null 2>&1; then ok "Blender is listening on port 9876"
  else warn "Blender isn't listening on 9876" "bash setup/start-blender-mac-linux.sh"; fi
else warn "Blender not installed (only needed for the Art track)" "https://www.blender.org/download/"; fi

if [ "$FULL" -eq 1 ] && command -v claude >/dev/null 2>&1; then
  step "Live connection test (slow)"
  OUT=$(timeout 60 claude mcp list 2>&1)
  for s in UnityMCP unity-official blender context7; do
    line=$(printf '%s\n' "$OUT" | grep -E "^$s:" | head -1)
    if printf '%s' "$line" | grep -q '✔ Connected'; then ok "$s connected"
    else bad "$s not connected: $(printf '%s' "$line" | sed -E 's/^.* - //')" "see the Troubleshooting table in README.md"; fi
  done
fi

echo
if [ "$FAILED" -eq 0 ]; then printf '\033[32mAll good.\033[0m Start Claude with:  bash setup/start-mac-linux.sh\n'
else printf '\033[31mFix the ✘ lines above, then run this again.\033[0m\n'; fi
exit "$FAILED"
