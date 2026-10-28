# Shared by the device scripts (source it; it defines functions only).
#
# A script keeps the jobs it starts in the background and waits for them with `waitjob`, so a signal reaches the script's own trap at
# once instead of after the child ends. Its teardown calls `killjobs`, which stops exactly those jobs and their children by process id
# (never by program name or port). Typical use:
#
#   trap TEARDOWN EXIT; trap 'exit 130' INT; trap 'exit 143' TERM
#   ( cd "$D" && long-command ) > log 2>&1 & waitjob $! || fail "long-command"
KIDS=()
killtree() {
  local c
  for c in $(pgrep -P "$1" 2>/dev/null); do killtree "$c"; done
  kill -TERM "$1" 2>/dev/null
  return 0
}
waitjob() { KIDS+=("$1"); wait "$1"; }
killjobs() {
  local p
  for p in ${KIDS[@]+"${KIDS[@]}"}; do killtree "$p"; done
  KIDS=()
}
