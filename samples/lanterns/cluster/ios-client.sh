#!/bin/sh
# Runs the Lanterns client in the iOS simulator, so a cluster scenario can include a phone as one of its release-style clients. The
# simulator shares this Mac's network, so the addresses a cluster gives its clients work as they are. `onegame ios run` waits for the
# app to exit and returns the game's last JSON line as "result"; this script prints that line, which is what the cluster reads.
#
# Environment: ONEGAME (the onegame command, default onegame; may be several words, such as dotnet onegame.dll), ONEGAME_IOS_DEVICE (which simulator), LANTERNS_BUNDLE (default
# dev.onegame.lanterns), LANTERNS_TIMEOUT (seconds, default 300).
out=$(${ONEGAME:-onegame} ios run --bundle "${LANTERNS_BUNDLE:-dev.onegame.lanterns}" --timeout "${LANTERNS_TIMEOUT:-300}" --json -- "$@")
printf '%s' "$out" | python3 -c 'import json,sys; r=json.load(sys.stdin); print(json.dumps(r.get("result") or {"ok": False, "error": r}, separators=(",", ":")))' || printf '%s\n' "$out"
