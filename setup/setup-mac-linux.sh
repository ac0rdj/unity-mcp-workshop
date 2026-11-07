#!/usr/bin/env bash
# Unity MCP workshop setup for macOS and Linux. Safe to run as many times as you like.
#
#   bash setup/setup-mac-linux.sh               # normal
#   bash setup/setup-mac-linux.sh --port 8091   # if something else already uses port 8080
#
# What it does: installs uv and Claude Code if they're missing, then connects Claude Code (in this project only)
# to MCP for Unity, Unity's official MCP, Blender MCP and Context7. Optionally installs the Blender add-on.
set -u

PORT=8080
while [ $# -gt 0 ]; do
  case "$1" in
    --port) PORT="${2:-}"; shift 2 ;;
    -h|--help) sed -n '2,9p' "$0"; exit 0 ;;
    *) echo "Unknown option: $1 (try --help)"; exit 1 ;;
  esac
done

cd "$(dirname "$0")/.." || exit 1
FAILED=0
ok()   { printf '  \033[32m✔\033[0m %s\n' "$1"; }
warn() { printf '  \033[33m!\033[0m %s\n' "$1"; }
bad()  { printf '  \033[31m✘\033[0m %s\n' "$1"; FAILED=1; }
step() { printf '\n\033[1m%s\033[0m\n' "$1"; }
export PATH="$HOME/.local/bin:$HOME/.cargo/bin:$PATH"
export PYTHONUTF8=1

if [ ! -d Assets ] || [ ! -d ProjectSettings ]; then
  echo "Run this from inside the downloaded project folder (it should contain Assets/ and setup/)."; exit 1
fi
echo "Project: $(pwd)"

step "1/5  uv (runs the MCP servers)"
if command -v uvx >/dev/null 2>&1; then
  ok "uv is installed ($(uv --version 2>/dev/null))"
else
  echo "  Installing uv..."
  curl -LsSf https://astral.sh/uv/install.sh | sh
  export PATH="$HOME/.local/bin:$PATH"
  if command -v uvx >/dev/null 2>&1; then ok "uv installed"; else bad "uv didn't install. See https://docs.astral.sh/uv/getting-started/installation/"; fi
fi

step "2/5  Claude Code"
if command -v claude >/dev/null 2>&1; then
  ok "Claude Code is installed ($(claude --version 2>/dev/null))"
else
  echo "  Installing Claude Code..."
  curl -fsSL https://claude.ai/install.sh | bash
  export PATH="$HOME/.local/bin:$PATH"
  if command -v claude >/dev/null 2>&1; then ok "Claude Code installed"; else bad "Claude Code didn't install. See https://code.claude.com/docs/en/setup"; fi
fi

step "3/5  Unity's official MCP relay"
case "$(uname -s)-$(uname -m)" in
  Darwin-arm64) RELAY="$HOME/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64" ;;
  Darwin-*)     RELAY="$HOME/.unity/relay/relay_mac_x64.app/Contents/MacOS/relay_mac_x64" ;;
  *)            RELAY="$HOME/.unity/relay/relay_linux" ;;
esac
if [ -x "$RELAY" ]; then
  ok "Found $RELAY"
else
  warn "Not installed yet. Open this project in Unity once (Unity installs the relay), then run this script again."
  RELAY=""
fi

step "4/5  Connect Claude Code to the tools (this project only)"
if command -v claude >/dev/null 2>&1; then
  add() {  # add <name> <claude mcp add args...>; removes an old entry first so re-running is safe
    local name="$1"; shift
    claude mcp remove "$name" -s local >/dev/null 2>&1
    if out=$(claude mcp add -s local "$@" 2>&1); then ok "$name"; else bad "$name: $out"; fi
  }
  add UnityMCP --transport http UnityMCP "http://127.0.0.1:$PORT/mcp"
  [ -n "$RELAY" ] && add unity-official unity-official -- "$RELAY" --mcp
  add blender blender -- uvx mcp-for-blender
  add context7 --transport http context7 https://mcp.context7.com/mcp
else
  bad "Skipped (Claude Code missing)"
fi
if command -v lsof >/dev/null 2>&1 && lsof -nP -iTCP:"$PORT" -sTCP:LISTEN >/dev/null 2>&1; then
  warn "Something is listening on port $PORT. That's expected if Unity is open. If Unity's MCP window isn't green,"
  warn "pick a free port: re-run with e.g. --port 8092 and set the same URL in Window > MCP for Unity."
fi

step "5/5  Blender add-on (Art track only, optional)"
if ls -d /Applications/Blender*.app >/dev/null 2>&1 || command -v blender >/dev/null 2>&1; then
  # The installer alone picks one Blender folder (often the newest, not the one you use).
  # Copy the add-on into every Blender version folder that exists.
  COPIED=0
  for d in "$HOME"/Library/Application\ Support/Blender/*/ "$HOME"/.config/blender/*/; do
    [ -d "$d" ] || continue
    if uvx mcp-for-blender install-addon --addons-dir "${d}scripts/addons" >/dev/null 2>&1; then
      ok "Add-on copied for Blender $(basename "$d")"; COPIED=1
    fi
  done
  if [ "$COPIED" -eq 0 ]; then
    warn "Couldn't copy the add-on. Open Blender once (so it creates its settings folder), then run this again."
  else
    ok "Blender is set up. To start it with the add-on on, run:  bash setup/start-blender-mac-linux.sh"
  fi
else
  warn "Blender not found. Skip this unless you pick the Art track (get it free at blender.org, open it once, re-run)."
fi

echo
if [ "$FAILED" -eq 0 ]; then
  printf '\033[32mDone.\033[0m Next: open the project in Unity, then run:  bash setup/doctor-mac-linux.sh\n'
  echo "Then start Claude with:  bash setup/start-mac-linux.sh   (always from this project, never from another folder)"
else
  printf '\033[31mSome steps failed (see ✘ above).\033[0m Fix them and run this script again.\n'
fi
exit "$FAILED"
