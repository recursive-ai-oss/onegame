#!/bin/sh
# Runs the Lanterns client on a connected iPhone or iPad, so a cluster scenario can include one as a release-style client. The cluster
# gives its clients addresses on the loopback; the device reaches this Mac by its network address, so they are rewritten before they go
# over. `devicectl device process launch --console` waits for the app to exit and shows what it printed; the game's last line is its
# JSON result, which is what the cluster reads.
#
# Environment: IOS_DEVICE (the device's UDID or name; default: the only connected one), LANTERNS_HOST (what the device calls this Mac; required),
# LANTERNS_BUNDLE (default dev.onegame.lanterns), LANTERNS_TIMEOUT (seconds, default 300).
host=${LANTERNS_HOST:?LANTERNS_HOST must name this Mac as the device sees it}
device=${IOS_DEVICE:-$(xcrun devicectl list devices 2>/dev/null | awk '/connected/ && !/simulated/ {print $3; exit}')}
for a in "$@"; do
  shift
  set -- "$@" "$(printf '%s' "$a" | sed "s#://127\.0\.0\.1:#://$host:#")"
done
log=$(mktemp)
perl -e "alarm shift; exec @ARGV" "${LANTERNS_TIMEOUT:-300}" xcrun devicectl device process launch --device "$device" --terminate-existing --console "${LANTERNS_BUNDLE:-dev.onegame.lanterns}" -- "$@" > "$log" 2>&1
# the console prefixes each line with a date and the process name; the result is the JSON from the first brace
tr -d '\r' < "$log" | sed -n 's/^.*\({"ok".*}\)$/\1/p' | tail -n 1 | grep . || { printf '{"ok":false,"error":%s}\n' "$(tail -c 300 "$log" | python3 -c 'import json,sys; print(json.dumps(sys.stdin.read()))')"; }
rm -f "$log"
