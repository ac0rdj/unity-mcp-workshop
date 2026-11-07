#!/usr/bin/env bash
# Starts Claude Code in the right folder. Use this instead of typing `claude` yourself.
#
#   bash setup/start-mac-linux.sh               # normal
#   bash setup/start-mac-linux.sh --port 8092   # if you moved Unity's MCP port
#
# Why: the Unity and Blender connections are saved per folder. `claude` started anywhere else
# can't see Unity, and it quietly falls back to reading scene files by hand.
set -u
PORT=8080
if [ "${1:-}" = "--port" ]; then PORT="${2:-8080}"; shift 2; fi
cd "$(dirname "$0")/.." || exit 1
export PATH="$HOME/.local/bin:$HOME/.cargo/bin:$PATH"
command -v claude >/dev/null 2>&1 || { echo "Claude Code isn't installed. Run: bash setup/setup-mac-linux.sh"; exit 1; }

# Unity can drop the UnityMCP entry after it opens the project. Put it back.
if ! claude mcp get UnityMCP >/dev/null 2>&1; then
  claude mcp add -s local --transport http UnityMCP "http://127.0.0.1:$PORT/mcp" >/dev/null 2>&1 \
    && echo "Reconnected UnityMCP (port $PORT)."
fi
if ! claude mcp get blender >/dev/null 2>&1 || ! claude mcp get context7 >/dev/null 2>&1; then
  echo "Some tools aren't connected for this folder. Run: bash setup/setup-mac-linux.sh"
fi

exec claude --append-system-prompt "Workshop context: this is a Unity project and the Unity editor is open. 'Scene', 'objects' and 'console' mean the open Unity scene and the Unity Console. Use the UnityMCP tools (mcp__UnityMCP__*) to inspect and change them. Never parse .unity files by hand. For Blender, use the blender MCP tools." "$@"
