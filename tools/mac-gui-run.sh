#!/bin/sh
# Runs a command-line program as a macOS application through LaunchServices (`open`), so the program is a normal GUI app that can
# become the frontmost application and own key focus. Started straight from a shell or an SSH session, a program is not activated, and
# tests that need key focus or fullscreen cannot pass.
#
#   tools/mac-gui-run.sh <log-file> <program> [arguments...]
#
# The program's standard output and error go to <log-file>. LaunchServices does not report the program's exit status, so read the log.
# The wrapper bundle lives in a temporary folder and is removed afterwards.
set -u
if [ "$#" -lt 2 ]; then
    echo "usage: $0 <log-file> <program> [arguments...]" >&2
    exit 64
fi
LOG=$1
shift
case $LOG in /*) ;; *) LOG=$PWD/$LOG ;; esac
WORK=$(mktemp -d "${TMPDIR:-/tmp}/onegame-gui.XXXXXX")
APP=$WORK/Run.app
mkdir -p "$APP/Contents/MacOS"
cat > "$APP/Contents/Info.plist" <<'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>run</string>
<key>CFBundleIdentifier</key><string>dev.onegame.guirun</string>
<key>CFBundleName</key><string>Run</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
EOF
{
    echo '#!/bin/sh'
    printf 'cd %s || exit 1\n' "$(printf '%s' "$PWD" | sed "s/'/'\\\\''/g; s/^/'/; s/\$/'/")"
    printf 'exec >%s 2>&1\n' "'$LOG'"
    # LaunchServices starts the app with its own environment; carry over what the program needs.
    env | grep -E '^(DOTNET_|MTL_|ONEGAME_|PATH=|HOME=|TMPDIR=)' | while IFS= read -r line; do
        name=${line%%=*}
        value=${line#*=}
        printf "export %s='%s'\n" "$name" "$(printf '%s' "$value" | sed "s/'/'\\\\''/g")"
    done
    # exec, so the program itself is the process LaunchServices started and the system treats it as the app.
    printf 'exec'
    for a in "$@"; do printf " '%s'" "$(printf '%s' "$a" | sed "s/'/'\\\\''/g")"; done
    printf '\n'
} > "$APP/Contents/MacOS/run"
chmod +x "$APP/Contents/MacOS/run"
open -W -n "$APP"
rm -rf "$WORK"
