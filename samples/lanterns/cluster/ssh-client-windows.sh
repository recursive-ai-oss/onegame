#!/bin/sh
# Runs the Lanterns client on a Windows machine over ssh (its default shell is cmd), so a cluster scenario can include it as a
# release-style client. Same idea as ssh-client.sh: the cluster gives its clients loopback addresses, the other machine reaches this
# one by its network address, so they are rewritten before they go over. The game's last output line is its JSON result.
#
# Environment: SSH_TARGET (user@host, required), SSH_OPTIONS (extra ssh options, optional), LANTERNS_REMOTE (the program's Windows path
# on the other machine, required, for example C:\Users\me\game\Lanterns.exe), LANTERNS_HOST (what that machine calls this one, required),
# LANTERNS_DOTNET (a .NET install folder to use, optional: needed for a framework-dependent build), LANTERNS_TIMEOUT (seconds, default 300).
host=${LANTERNS_HOST:?LANTERNS_HOST must name this computer as the other machine sees it}
target=${SSH_TARGET:?SSH_TARGET must be user@host}
remote=${LANTERNS_REMOTE:?LANTERNS_REMOTE must be the program path there}
words=
for a in "$@"; do
  a=$(printf '%s' "$a" | sed "s#://127\.0\.0\.1:#://$host:#")
  words="$words \"$a\""
done
pre=
if [ -n "${LANTERNS_DOTNET:-}" ]; then pre="set DOTNET_ROOT=$LANTERNS_DOTNET&& "; fi
# The folder holding the program is the working directory, as with the other clients.
dir=${remote%\\*}
# shellcheck disable=SC2086
timeout "${LANTERNS_TIMEOUT:-300}" ssh ${SSH_OPTIONS:-} "$target" "${pre}cd /d \"$dir\" && \"$remote\"$words" | tr -d '\r' | grep '^{"ok"' | tail -n 1
