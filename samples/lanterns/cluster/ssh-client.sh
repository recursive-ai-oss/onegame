#!/bin/sh
# Runs the Lanterns client on another Linux machine over ssh (a Steam Deck, say), so a cluster scenario can include it as a release-style
# client. The cluster gives its clients addresses on the loopback; the other machine reaches this one by its network address, so they are
# rewritten before they go over. The game's last output line is its JSON result, which is what the cluster reads.
#
# Environment: SSH_TARGET (user@host, required), SSH_OPTIONS (extra ssh options, optional), LANTERNS_REMOTE (the program's path on the
# other machine, required; $HOME is expanded there), LANTERNS_HOST (what that machine calls this one, required), LANTERNS_TIMEOUT
# (seconds, default 300).
host=${LANTERNS_HOST:?LANTERNS_HOST must name this computer as the other machine sees it}
target=${SSH_TARGET:?SSH_TARGET must be user@host}
remote=${LANTERNS_REMOTE:?LANTERNS_REMOTE must be the program path there}
words=
for a in "$@"; do
  a=$(printf '%s' "$a" | sed "s#://127\.0\.0\.1:#://$host:#")
  words="$words '$a'"
done
# shellcheck disable=SC2086
timeout "${LANTERNS_TIMEOUT:-300}" ssh ${SSH_OPTIONS:-} "$target" "cd \"\$(dirname $remote)\" && $remote $words" | grep '^{"ok"' | tail -n 1
