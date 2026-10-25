#!/bin/sh
# Runs the Lanterns client on an Android device or emulator, so a cluster scenario can include a phone as one of its release-style
# clients. The cluster starts a client with the arguments of a desktop one, among them --connect udp://127.0.0.1:PORT; the servers run
# on this computer, and the emulator reaches this computer's loopback as 10.0.2.2, so the address is rewritten before it goes over.
# The game's last output line is its JSON result, which is what the cluster reads when this script ends.
#
# Environment: ONEGAME (the onegame command, default onegame), ANDROID_SERIAL (which device), LANTERNS_PACKAGE (default dev.onegame.lanterns),
# LANTERNS_HOST (what the device calls this computer, default 10.0.2.2), LANTERNS_TIMEOUT (seconds, default 300).
host=${LANTERNS_HOST:-10.0.2.2}
for a in "$@"; do
  shift
  set -- "$@" "$(printf '%s' "$a" | sed "s#://127\.0\.0\.1:#://$host:#")"
done
exec "${ONEGAME:-onegame}" android run --package "${LANTERNS_PACKAGE:-dev.onegame.lanterns}" --timeout "${LANTERNS_TIMEOUT:-300}" -- "$@"
