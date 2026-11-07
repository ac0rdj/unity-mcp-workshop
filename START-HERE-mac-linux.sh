#!/usr/bin/env bash
# One command for everything: sets up, checks, then opens Claude Code in this project.
#
#   bash START-HERE-mac-linux.sh
#
# Run it again any time. If it shows a red line, fix that and run it again.
# Extra options: --port 8092 (if port 8080 is busy). Anything else goes to Claude.
cd "$(dirname "$0")" || exit 1
export START_HERE=1

PORT_ARGS=()
REST=()
while [ $# -gt 0 ]; do
  case "$1" in
    --port) PORT_ARGS=(--port "${2:-8080}"); shift 2 ;;
    *) REST+=("$1"); shift ;;
  esac
done

printf '\n\033[1mStep 1 of 3: setup\033[0m\n'
bash setup/setup-mac-linux.sh ${PORT_ARGS[@]+"${PORT_ARGS[@]}"} || { printf '\n\033[31mFix the red lines above, then run this again.\033[0m\n'; exit 1; }

printf '\n\033[1mStep 2 of 3: check\033[0m\n'
bash setup/doctor-mac-linux.sh || exit 1

printf '\n\033[1mStep 3 of 3: opening Claude\033[0m\n'
exec bash setup/start-mac-linux.sh ${PORT_ARGS[@]+"${PORT_ARGS[@]}"} ${REST[@]+"${REST[@]}"}
