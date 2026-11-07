#!/usr/bin/env bash
# Opens Blender with the MCP for Blender add-on on, Poly Haven ticked and the server running.
# Replaces the Preferences > Add-ons > tick > N panel > Start clicks.
#
#   bash setup/start-blender-mac-linux.sh
set -u
cd "$(dirname "$0")/.." || exit 1
export PATH="$HOME/.local/bin:$HOME/.cargo/bin:/opt/homebrew/bin:/usr/local/bin:$PATH"
HERE="$(pwd)"

# Pick Blender: the plain /Applications/Blender.app first, then any Blender*.app, then `blender`.
APP=""
if [ -d /Applications/Blender.app ]; then APP=/Applications/Blender.app
else APP=$(ls -d /Applications/Blender*.app 2>/dev/null | head -1); fi
if [ -n "$APP" ]; then BIN="$APP/Contents/MacOS/Blender"; else BIN=$(command -v blender || true); fi
[ -n "$BIN" ] && [ -x "$BIN" ] || { echo "Blender not found. Install it from https://www.blender.org/download/ and run this again."; exit 1; }

# The add-on must sit in THIS Blender's version folder (4.5 is not 5.2).
VER=$("$BIN" --version 2>/dev/null | sed -n 's/^Blender \([0-9]*\.[0-9]*\).*/\1/p' | head -1)
[ -n "$VER" ] || { echo "Couldn't read Blender's version."; exit 1; }
if [ "$(uname -s)" = Darwin ]; then DIR="$HOME/Library/Application Support/Blender/$VER/scripts/addons"
else DIR="$HOME/.config/blender/$VER/scripts/addons"; fi
mkdir -p "$DIR"
command -v uvx >/dev/null 2>&1 || { echo "uv is missing. Run: bash setup/setup-mac-linux.sh"; exit 1; }
uvx mcp-for-blender install-addon --addons-dir "$DIR" >/dev/null 2>&1 || { echo "Couldn't copy the add-on into $DIR"; exit 1; }
echo "Add-on installed for Blender $VER."

if [ -n "$APP" ]; then open -n -a "$APP" --args --python "$HERE/setup/blender_boot.py"
else nohup "$BIN" --python "$HERE/setup/blender_boot.py" >/dev/null 2>&1 & fi

for i in 1 2 3 4 5 6 7 8 9 10 11 12; do
  sleep 2
  if lsof -nP -iTCP:9876 -sTCP:LISTEN >/dev/null 2>&1; then echo "Blender $VER is ready: MCP server running on port 9876, Poly Haven on."; exit 0; fi
done
echo "Blender opened, but port 9876 isn't listening yet. In Blender press N > MCP for Blender > Start MCP Server."
exit 1
