#!/bin/bash
# Makes (once) a keychain that only this tooling uses, holding a development signing identity whose certificate the App Store
# Connect API issued, so code signing works from a remote shell where the login keychain is locked and nobody is there to unlock it.
#
#   tools/devices/ios-signing.sh [DIR]      prints the keychain path and the identity's SHA-1 on success
#
# Needs the API key variables of asc.py in the environment. The keychain password and private key stay in DIR (default
# ~/onegame/signing), mode 700. Run it again and it reuses what is there. Written for the bash and tools that ship with macOS.
set -euo pipefail
HERE=$(cd "$(dirname "$0")" && pwd)
DIR=${1:-$HOME/onegame/signing}
PY=${PYTHON:-python3}
KC=$DIR/onegame-dev.keychain-db
umask 077; mkdir -p "$DIR"; chmod 700 "$DIR"
[ -f "$DIR/pass" ] || openssl rand -hex 16 > "$DIR/pass"
PASS=$(cat "$DIR/pass")
if [ ! -f "$KC" ]; then security create-keychain -p "$PASS" "$KC" >&2; fi
security unlock-keychain -p "$PASS" "$KC"
security set-keychain-settings "$KC"                       # never lock on a timer
find_id() { security find-identity -v -p codesigning "$KC" | awk '/"Apple Development/ {print $2; exit}'; }
ID=$(find_id || true)
if [ -z "$ID" ]; then
  if [ ! -f "$DIR/dev.cer" ]; then   # one certificate per key, even when a later step has to be repeated
    openssl genrsa -out "$DIR/key.pem" 2048 2>/dev/null
    openssl req -new -key "$DIR/key.pem" -subj "/CN=onegame development/O=onegame/C=US" -out "$DIR/csr.pem"
    $PY "$HERE/asc.py" certificate "$DIR/csr.pem" "$DIR/dev.cer" >&2
  fi
  openssl x509 -inform der -in "$DIR/dev.cer" -out "$DIR/dev.pem"
  openssl rsa -in "$DIR/key.pem" -out "$DIR/key-rsa.pem" 2>/dev/null   # the keychain reads the traditional RSA layout, not PKCS#8
  security import "$DIR/key-rsa.pem" -k "$KC" -t priv -f openssl -T /usr/bin/codesign >&2
  security import "$DIR/dev.pem" -k "$KC" -t cert -f openssl >&2
  security set-key-partition-list -S apple-tool:,apple: -s -k "$PASS" "$KC" >/dev/null 2>&1
  ID=$(find_id || true)
fi
[ -n "$ID" ] || { echo "no valid Apple Development identity in $KC (is the Apple intermediate certificate installed?)" >&2; security find-identity -p codesigning "$KC" >&2; exit 1; }
echo "$KC $ID"
