#!/usr/bin/env python3
"""Checks the samples on a connected iPhone or iPad from a Mac and prints a one-line verdict.

  tools/devices/ios.py [--device UDID_OR_NAME] [--samples solitaire,lanterns,bricks] [--lan-host ADDRESS] [--out DIR]
                       [--no-cluster] [--keep] [--wait SECONDS] [--no-sign-device]

Per sample: find the device, make a development provisioning profile for it (the App Store Connect API registers the device and
builds the profile, so Xcode is never opened), export a device build, sign it, install it, run the scenario folder on the device
and compare every scenario's state hash with a run on this Mac, then (Solitaire) drive the running game over live control: a
presented frame, touch injected into the platform's queue, the audio device and background-and-back. Lanterns also joins a cluster
as a client of servers on this Mac. Everything the run starts is undone on every way out (finish, failure, Ctrl-C, SIGTERM): the
commands it is running, the game it launched, the keep-alive loop and the packages it installed (unless --keep). The signing keychain
of ios-signing.sh is a deliberate, reusable asset and stays.

Environment: ONEGAME (the onegame command), IOS_BUNDLE_PREFIX (default dev.onegame; use ids the account may register),
IOS_IDENTITY (a codesigning identity; default: the one the profile lists), IOS_PROFILE_DIR (ready-made <bundle id>.mobileprovision
files, used instead of the API), ASC_KEY_FILE / ASC_KEY_ID / ASC_ISSUER_ID (the API key; see asc.py), CAPTURE_TOLERANCE (default 8)
and CAPTURE_MAX_DIFF (default 0.005): a device GPU draws edges differently from the software renderer the references came from, so
captures get a per-channel tolerance and a budget of differing pixels; state hashes must match exactly regardless.
Written for the Python and bash that ship with macOS."""
import argparse, atexit, json, os, plistlib, re, shlex, signal, subprocess, sys, time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
ap = argparse.ArgumentParser()
ap.add_argument('--device'); ap.add_argument('--samples'); ap.add_argument('--lan-host'); ap.add_argument('--out')
ap.add_argument('--no-cluster', action='store_true'); ap.add_argument('--keep', action='store_true'); ap.add_argument('--wait', type=int, default=30)
args = ap.parse_args()
ONEGAME = shlex.split(os.environ.get('ONEGAME', 'onegame'))
OUT = args.out or os.path.join(os.environ.get('ONEGAME_ARTIFACTS', os.path.join(ROOT, 'artifacts')), 'device-ios')
PREFIX = os.environ.get('IOS_BUNDLE_PREFIX', 'dev.onegame')
TOL, MAXD = os.environ.get('CAPTURE_TOLERANCE', '8'), os.environ.get('CAPTURE_MAX_DIFF', '0.005')
os.makedirs(OUT, exist_ok=True)
fails, notes, summary = [], [], {}


def say(m):
    print(f'[ios] {m}', file=sys.stderr, flush=True)


def fail(m):
    fails.append(m); say('FAIL ' + m)


# --- teardown: runs once on every way out (normal end, sys.exit, Ctrl-C, SIGTERM) and touches only what this run started ---
D = None            # the device id, once found
children = []       # commands this run is waiting for
live_pids = []      # game processes this run launched on the device
bundles = []        # packages this run tried to install
keepalive = None    # the tunnel keep-alive loop
torn = False


def stop(p):
    """Stops a process this script started (by its Popen handle, never by name)."""
    if p is not None and p.poll() is None:
        p.terminate()
        try:
            p.wait(5)
        except subprocess.TimeoutExpired:
            p.kill()


def teardown(runner=None):
    global torn, keepalive
    if torn:
        return
    torn = True
    call = runner or (lambda cmd: subprocess.run(cmd, capture_output=True, text=True, timeout=60))
    for c in list(children):
        stop(c)
    stop(keepalive); keepalive = None
    if D:
        for pid in live_pids:
            call(['xcrun', 'devicectl', 'device', 'process', 'terminate', '--device', D, '--pid', str(pid)])
        if not args.keep:
            for b in bundles:
                call(['xcrun', 'devicectl', 'device', 'uninstall', 'app', '--device', D, b])


def _exit_on(signum, _frame):
    sys.exit(128 + signum)


atexit.register(teardown)   # Ctrl-C raises KeyboardInterrupt and leaves through here too
for _sig in (signal.SIGTERM, signal.SIGHUP):
    signal.signal(_sig, _exit_on)


def run(cmd, cwd=None, timeout=600, env=None):
    e = dict(os.environ, **(env or {}))
    p = subprocess.Popen(cmd, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, env=e)
    children.append(p)
    try:
        out, _ = p.communicate(timeout=timeout)
        return p.returncode, out or ''
    except subprocess.TimeoutExpired:
        p.kill()
        out, _ = p.communicate()
        return 124, (out or '') + '\n[timed out]'
    finally:
        if p.poll() is not None:   # still running means we were interrupted: the teardown stops it
            children.remove(p)


def last_json(text, start='{"ok"'):
    i = text.rfind('\n' + start)
    i = i + 1 if i >= 0 else text.find(start)
    if i < 0:
        return {}
    try:
        return json.JSONDecoder().raw_decode(text[i:])[0]
    except ValueError:
        return {}


def verdict(code, extra=''):
    status = 'PASS' if not fails else 'FAIL'
    line = f'IOS {status} {extra}'
    for s, v in summary.items():
        line += f' | {v}'
    if fails:
        line += ' | failures: ' + '; '.join(fails)
    if notes:
        line += ' | notes: ' + '; '.join(notes)
    print(line)
    open(os.path.join(OUT, 'verdict.txt'), 'w').write(line + '\n')
    sys.exit(0 if not fails else 1)


# --- 1. the device ---
def find_device():
    path = os.path.join(OUT, 'devices.json')
    run(['xcrun', 'devicectl', 'list', 'devices', '--json-output', path], timeout=60)
    try:
        devs = json.load(open(path))['result']['devices']
    except (OSError, ValueError, KeyError):
        return None, 'devicectl could not list devices'
    cands = []
    for d in devs:
        hw, cp, dp = d.get('hardwareProperties', {}), d.get('connectionProperties', {}), d.get('deviceProperties', {})
        if hw.get('reality', 'physical') == 'simulated' or hw.get('platform') not in (None, 'iOS', 'iPadOS'):
            continue
        name, udid = dp.get('name', ''), hw.get('udid', '')
        if args.device and args.device not in (name, udid, d.get('identifier')):
            continue
        cands.append({'name': name, 'udid': udid, 'id': d.get('identifier', udid), 'model': hw.get('marketingName') or hw.get('productType'),
                      'os': dp.get('osVersionNumber'), 'developerMode': dp.get('developerModeStatus'), 'tunnel': cp.get('tunnelState'),
                      'tunnelAddress': cp.get('tunnelIPAddress'), 'transport': cp.get('transportType')})
    ready = sorted([c for c in cands if c['tunnel'] != 'unavailable'], key=lambda c: c['tunnel'] != 'connected')   # a tunnel starts on first use
    if ready:
        return ready[0], ''
    if cands:
        return None, f"{cands[0]['name']} is paired but not connected (tunnel {cands[0]['tunnel']}): plug it in, unlock it and trust this Mac"
    return None, 'no iPhone or iPad known to this Mac'


dev, why = None, ''
for t in range(0, args.wait + 1, 3):
    dev, why = find_device()
    if dev:
        break
    time.sleep(3)
if not dev:
    print(f'IOS MISSING {why}')
    sys.exit(1)
say(f"device {dev['name']} ({dev['model']}) iOS {dev['os']} udid {dev['udid']} developerMode={dev['developerMode']} tunnel={dev['tunnel']}")
if dev['developerMode'] not in (None, 'enabled'):
    print(f"IOS WAITING {dev['name']}: Developer Mode is {dev['developerMode']} (Settings > Privacy & Security > Developer Mode)")
    sys.exit(1)
D = dev['id']

# --- 2. signing material ---
asc_env = all(os.environ.get(k) for k in ('ASC_KEY_FILE', 'ASC_KEY_ID', 'ASC_ISSUER_ID'))
PYTHON = sys.executable
if asc_env:
    rc, o = run([PYTHON, os.path.join(HERE, 'asc.py'), 'register', dev['udid'], dev['name'] or 'onegame test device'], timeout=90)
    say('register: ' + o.strip()[-160:])
    if rc != 0:
        fail('device registration: ' + o.strip()[-200:])
profiles = os.path.join(OUT, 'profiles'); os.makedirs(profiles, exist_ok=True)


def profile_for(bundle):
    """Returns the path of a development profile for the bundle id that lists this device."""
    ready = os.path.join(os.environ.get('IOS_PROFILE_DIR', ''), bundle + '.mobileprovision') if os.environ.get('IOS_PROFILE_DIR') else ''
    if ready and os.path.exists(ready):
        return ready
    mine = os.path.join(profiles, bundle + '.mobileprovision')
    if os.path.exists(mine):
        info = decode_profile(mine)
        if info and dev['udid'] in info.get('ProvisionedDevices', []) and info['ExpirationDate'].timestamp() > time.time() + 86400:
            return mine
    if not asc_env:
        return None
    rc, o = run([PYTHON, os.path.join(HERE, 'asc.py'), 'profile', bundle, mine], timeout=120)
    if rc != 0:
        fail(f'profile for {bundle}: {o.strip()[-300:]}')
        return None
    return mine


def decode_profile(path):
    p = subprocess.run(['security', 'cms', '-D', '-i', path], capture_output=True)
    if p.returncode != 0:
        return None
    return plistlib.loads(p.stdout)


KEYCHAIN = None


def identity_for(info):
    """Returns the identity to sign with: the one asked for, else our own signing keychain's, else a login-keychain identity the profile lists."""
    global KEYCHAIN
    if os.environ.get('IOS_IDENTITY'):
        return os.environ['IOS_IDENTITY']
    import hashlib
    listed = {hashlib.sha1(c).hexdigest().upper() for c in info.get('DeveloperCertificates', [])}
    if asc_env and KEYCHAIN is None:
        rc, o = run(['bash', os.path.join(HERE, 'ios-signing.sh')], env={'PYTHON': PYTHON})
        if rc == 0 and o.strip():
            KEYCHAIN = o.strip().splitlines()[-1].split()[0]
        else:
            notes.append('signing keychain: ' + o.strip()[-200:])
            KEYCHAIN = ''
    chains = [KEYCHAIN] if KEYCHAIN else []
    for kc in chains + [None]:
        rc, o = run(['security', 'find-identity', '-v', '-p', 'codesigning'] + ([kc] if kc else []))
        for m in re.finditer(r'\)\s+([0-9A-F]{40})\s+"([^"]+)"', o):
            if m.group(1) in listed:
                return m.group(1)
    return None


def sign_and_install(app, bundle, entitlements):
    prof = profile_for(bundle)
    if not prof:
        return False
    info = decode_profile(prof)
    team = info['ApplicationIdentifierPrefix'][0]
    ident = identity_for(info)
    if not ident:
        fail(f'{bundle}: no keychain identity matches the profile\'s certificates')
        return False
    subprocess.run(['cp', prof, os.path.join(app, 'embedded.mobileprovision')], check=True)
    text = open(entitlements).read().replace('TEAMID', team)
    open(entitlements, 'w').write(text)
    rc, o = run(['codesign', '--force', '--sign', ident] + (['--keychain', KEYCHAIN] if KEYCHAIN else []) + ['--entitlements', entitlements, '--timestamp=none', app])
    if rc != 0:
        fail(f'{bundle}: codesign: {o.strip()[-300:]}')
        return False
    rc, o = run(['xcrun', 'devicectl', 'device', 'install', 'app', '--device', D, app], timeout=300)
    if rc != 0:
        fail(f'{bundle}: install: {o.strip()[-300:]}')
        return False
    return True


def launch(bundle, game_args, console=True, timeout=900, jsonout=None):
    cmd = ['xcrun', 'devicectl', 'device', 'process', 'launch', '--device', D, '--terminate-existing']
    if console:
        cmd.append('--console')
    if jsonout:
        cmd += ['--json-output', jsonout]
    return run(cmd + [bundle, '--'] + game_args, timeout=timeout)


def copy_to(bundle, src, dest):
    return run(['xcrun', 'devicectl', 'device', 'copy', 'to', '--device', D, '--source', src, '--destination', dest,
                '--domain-type', 'appDataContainer', '--domain-identifier', bundle], timeout=120)


def copy_from(bundle, src, dest):
    return run(['xcrun', 'devicectl', 'device', 'copy', 'from', '--device', D, '--source', src, '--destination', dest,
                '--domain-type', 'appDataContainer', '--domain-identifier', bundle], timeout=120)


# --- 3. samples ---
samples = (args.samples or ','.join(['solitaire', 'lanterns'] + (['bricks'] if os.path.isdir(os.path.join(ROOT, 'samples', 'bricks')) else []))).split(',')
for s in samples:
    sd = os.path.join(ROOT, 'samples', s)
    if not os.path.isdir(sd):
        notes.append(f'{s} not in this tree'); continue
    W = os.path.join(OUT, s); os.makedirs(W, exist_ok=True)
    bundle = f'{PREFIX}.{s}'
    say(f'{s}: export device build as {bundle}')
    rc, o = run(ONEGAME + ['export', '--target', 'ios', '--dev', '--bundle-id', bundle, '--json', '--out', os.path.join(W, 'export')], cwd=sd, timeout=1200)
    open(os.path.join(W, 'export.log'), 'w').write(o)
    ex = last_json(o)
    if rc != 0 or not ex.get('app'):
        fail(f'{s} export: {o.strip()[-300:]}'); continue
    say(f'{s}: sign and install')
    bundles.append(bundle)   # from here on the teardown removes it
    if not sign_and_install(ex['app'], bundle, ex['entitlements']):
        continue

    say(f'{s}: desktop reference run on this Mac')
    rc, o = run(ONEGAME + ['test', '--no-unit', '--json'], cwd=sd, timeout=900)
    open(os.path.join(W, 'desktop.json'), 'w').write(o)
    if rc != 0:
        notes.append(f'{s}: reference run failed')

    say(f'{s}: scenarios on the device')
    rc, o = copy_to(bundle, os.path.join(sd, 'scenarios'), 'Documents/scenarios')
    if rc != 0:
        fail(f'{s} copy scenarios: {o.strip()[-200:]}'); continue
    res = {}
    for folder in ('scenarios', 'scenarios/scenarios'):   # copying a folder may or may not nest it
        rc, o = launch(bundle, ['--headless', '--scenario', folder, '--artifacts', 'artifacts', '--json', '--capture-tolerance', TOL, '--capture-max-diff', MAXD])
        open(os.path.join(W, 'device.log'), 'w').write(o)
        res = last_json(o)
        if not res:
            copy_from(bundle, 'Documents/onegame-stdout.txt', os.path.join(W, 'device-stdout.txt'))
            try:
                res = last_json(open(os.path.join(W, 'device-stdout.txt')).read())
            except OSError:
                res = {}
        if res.get('passed') is not None or res.get('scenarios'):
            break
        if 'locked' in o.lower():
            fail(f'{s}: the device is locked: unlock it and keep it awake (Settings > Display > Auto-Lock > Never)'); break
    if not res:
        fail(f'{s}: no scenario result from the device: {o.strip()[-300:]}'); continue
    json.dump({'result': res}, open(os.path.join(W, 'device.json'), 'w'))
    rc, o = run([PYTHON, os.path.join(HERE, 'compare-hashes.py'), os.path.join(W, 'desktop.json'), os.path.join(W, 'device.json')])
    cmp_ = last_json(o, '{"scenarios"')
    n, eq, ok = cmp_.get('scenarios'), cmp_.get('hashEqual'), cmp_.get('passed')
    summary[s] = f'{s} scenarios {ok}/{n} hash-equal {eq}/{n}'
    if not n or eq != n:
        fail(f'{s}: state hashes differ from this Mac: {cmp_.get("different")}')
    if ok != n:
        notes.append(f'{s}: {n - (ok or 0)} scenario(s) failed (captures past tolerance {TOL}/{MAXD}?) {json.dumps(cmp_.get("captureNotes"))[:200]}')

    if s == 'solitaire':
        say('solitaire: live control')
        port = 7878
        rc, o = launch(bundle, ['--live', f'tcp:*:{port}', '--realtime', '--seed', '1'], console=False, jsonout=os.path.join(W, 'launch.json'))
        try:
            pid = json.load(open(os.path.join(W, 'launch.json')))['result']['process']['processIdentifier']
            live_pids.append(pid)
        except (OSError, ValueError, KeyError):
            pid = None
        addr = dev.get('tunnelAddress')
        if not addr:
            rc2, _ = run(['xcrun', 'devicectl', 'device', 'info', 'details', '--device', D, '--json-output', os.path.join(W, 'details.json')])
            try:
                addr = json.load(open(os.path.join(W, 'details.json')))['result']['connectionProperties']['tunnelIPAddress']
            except (OSError, ValueError, KeyError):
                addr = None
        if not addr or not pid:
            fail(f'solitaire live: could not start or address the game (pid {pid}, address {addr}): {o.strip()[-200:]}')
        else:
            time.sleep(4)
            # CoreDevice closes the device tunnel when nothing uses it, which cuts the game's live-control connection; a cheap call
            # every few seconds keeps it open for the length of the check.
            keepalive = subprocess.Popen(['bash', '-c', f'while :; do xcrun devicectl device info lockState --device {D} >/dev/null 2>&1; sleep 3; done'])
            rc, o = run([PYTHON, os.path.join(HERE, 'live-check.py'), f'[{addr}]:{port}', os.path.join(W, 'live'), '--expect-audio', 'audiounit'], timeout=180)
            stop(keepalive); keepalive = None
            open(os.path.join(W, 'live.json'), 'w').write(o)
            try:
                d = json.loads(o.strip().splitlines()[-1])
                summary[s] += f" touch={'ok' if d['finger'].get('stock_drew_a_card') else 'FAIL'} audio={d['audio']['backend']}/{d['audio']['deviceState']} peak={d['audio']['peakPlayed']:.3f} capture={d['capture']['width']}x{d['capture']['height']} gpu={d['window']['adapter']!r}"
                if rc != 0:
                    fail('solitaire live: ' + '; '.join(d.get('problems', [])))
            except (ValueError, KeyError, IndexError):
                fail('solitaire live: ' + o.strip()[-300:])
            run(['xcrun', 'devicectl', 'device', 'process', 'terminate', '--device', D, '--pid', str(pid)], timeout=30)
            live_pids.remove(pid)

    if s == 'lanterns' and not args.no_cluster:
        host = args.lan_host
        if not host:
            rc, o = run(['ipconfig', 'getifaddr', 'en0'])
            host = o.strip() if rc == 0 else ''
        if not host:
            notes.append('lanterns cluster skipped: this Mac has no en0 address; pass --lan-host')
        else:
            say(f'lanterns: servers on {host}, bot on the device')
            run(['dotnet', 'publish', 'Server/Lanterns.Server.csproj', '-c', 'Debug', '-o', 'bin', '-v', 'quiet', '-clp:NoSummary'], cwd=sd, timeout=900)
            run(['dotnet', 'publish', 'Lanterns.csproj', '-c', 'Debug', '-o', 'bin', '-v', 'quiet', '-clp:NoSummary'], cwd=sd, timeout=900)
            env = {'ONEGAME_CLUSTER_BIND': '0.0.0.0', 'LANTERNS_HOST': host, 'LANTERNS_BUNDLE': bundle, 'IOS_DEVICE': D, 'LANTERNS_TIMEOUT': os.environ.get('LANTERNS_TIMEOUT', '400'), 'ONEGAME': ' '.join(ONEGAME)}
            rc, o = run(ONEGAME + ['cluster', 'test', 'cluster/play-ios.yaml', '--spec', 'cluster-ios-device.yaml', '--json'], cwd=sd, timeout=1500, env=env)
            open(os.path.join(W, 'cluster.log'), 'w').write(o)
            if last_json(o).get('ok') is True:
                summary[s] += f' cluster=agree+0drift via {host}'
            else:
                fail('lanterns cluster: ' + o.strip()[-300:].replace('\n', ' '))

# --- 4. clean up ---
teardown()
verdict(0, f"{dev['model']} iOS {dev['os']}")
