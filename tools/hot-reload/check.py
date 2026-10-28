#!/usr/bin/env python3
"""End-to-end check of development hot reload.

Creates a game, starts `onegame run` (hot by default), drives it over live control, edits the game's C# on disk and shows that each
edit took effect in the running process with the world kept:

  1. a [View] method body      -> the label text and the captured frame change
  2. a sim OnTick body         -> Y now grows every tick; tick and X carry on
  3. a screen builder          -> the rebuilt screen's colour changes
  4. a new method + synced property -> the property shows in state_query and snapshots
  5. a rude edit (a synced member's type) -> hot_reload reports `rude`, the game restarts, the world (tick, X, Y) is loaded back
  6. an edit that does not compile -> reported as `error`, the game keeps running

Usage: tools/hot-reload/check.py --onegame "dotnet /path/onegame.dll" [--engine /path/to/checkout] [--work DIR]
Exit code 0 when every check passed. Everything it starts is stopped on every way out.
"""
import argparse, base64, hashlib, json, os, shlex, shutil, signal, socket, subprocess, sys, time

WINDOWS = os.name == 'nt'

failed = []


def check(name, ok, detail=''):
    print(('PASS ' if ok else 'FAIL ') + name + (' -- ' + str(detail) if detail != '' else ''), flush=True)
    if not ok:
        failed.append(name)


class Live:
    def __init__(self, path):
        # Python on Windows has no Unix sockets, so the game listens on a loopback TCP port (debug builds only) everywhere.
        host, port = path.split(':')[1:]
        self.s = socket.create_connection((host, int(port)), timeout=30)
        self.f = self.s.makefile('rw', newline='\n')
        self.n = 0
        self.rpc('initialize', {'protocolVersion': '2025-06-18', 'capabilities': {}, 'clientInfo': {'name': 'hot-check', 'version': '1'}})

    def rpc(self, method, params):
        self.n += 1
        self.f.write(json.dumps({'jsonrpc': '2.0', 'id': self.n, 'method': method, 'params': params}) + '\n')
        self.f.flush()
        line = self.f.readline()
        if not line:
            raise ConnectionError('closed')
        return json.loads(line)

    def call(self, op, **args):
        r = self.rpc('tools/call', {'name': op, 'arguments': args})['result']
        text = next((c['text'] for c in r['content'] if c['type'] == 'text'), '')
        png = next((base64.b64decode(c['data']) for c in r['content'] if c['type'] == 'image'), None)
        if r.get('isError'):
            raise RuntimeError(text)
        try:
            return json.loads(text), png
        except ValueError:
            return text, png

    def q(self, op, **args):
        return self.call(op, **args)[0]

    def close(self):
        try:
            self.s.close()
        except OSError:
            pass


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--onegame', required=True, help='the onegame command, e.g. "dotnet .../onegame.dll"')
    ap.add_argument('--engine', help='engine checkout to build the game against (omit to use the SDK package)')
    ap.add_argument('--work', default=os.path.join(os.environ.get('TMPDIR', '/tmp'), 'hot-reload-check'))
    a = ap.parse_args()
    # Windows paths keep their backslashes; quotes are still honoured.
    cli = [x.strip('"') for x in shlex.split(a.onegame, posix=not WINDOWS)]
    work = os.path.abspath(a.work)
    shutil.rmtree(work, ignore_errors=True)
    os.makedirs(work)
    new = cli + ['new', 'hotdemo'] + (['--engine', a.engine] if a.engine else [])
    subprocess.run(new, cwd=work, check=True, stdout=subprocess.DEVNULL)
    game = os.path.join(work, 'hotdemo')
    src = os.path.join(game, 'Game.cs')
    logf = open(os.path.join(work, 'run.log'), 'w', encoding='utf-8')
    env = dict(os.environ)
    if not WINDOWS:
        env.setdefault('VK_ICD_FILENAMES', '/usr/share/vulkan/icd.d/lvp_icd.json')
    # Its own process group, so everything the watcher starts can be stopped with it.
    group = {'creationflags': subprocess.CREATE_NEW_PROCESS_GROUP} if WINDOWS else {'start_new_session': True}
    with socket.socket() as probe:
        probe.bind(('127.0.0.1', 0))
        port = probe.getsockname()[1]
    proc = subprocess.Popen(cli + ['run', '--', '--headless', '--realtime', '--live', f'tcp:{port}'], cwd=game, stdout=logf, stderr=subprocess.STDOUT, env=env, **group)
    live = None
    try:
        sock = find_socket(os.path.join(work, 'run.log'), proc)
        live = connect(sock, proc)
        run(live, sock, proc, src, game, work, logf)
    finally:
        if live:
            live.close()
        stop(proc)
        logf.close()
    print('hot reload check:', 'FAILED ' + ', '.join(failed) if failed else 'all passed')
    return 1 if failed else 0


def find_socket(log, proc, timeout=240):
    """The path the game announced on stdout ("listening on <path> (MCP ...")."""
    t0 = time.time()
    while time.time() - t0 < timeout:
        if proc.poll() is not None:
            raise SystemExit('onegame run exited early with ' + str(proc.returncode))
        for line in open(log, encoding='utf-8', errors='replace').read().splitlines():
            if line.startswith('listening on '):
                return line[len('listening on '):].split(' (MCP')[0]
        time.sleep(0.5)
    raise SystemExit('the game never announced its live-control socket')


def connect(sock, proc, timeout=240):
    t0 = time.time()
    while time.time() - t0 < timeout:
        if proc.poll() is not None:
            raise SystemExit('onegame run exited early with ' + str(proc.returncode))
        try:
            return Live(sock)
        except OSError:
            pass
        time.sleep(0.5)
    raise SystemExit('the game never listened on ' + sock)


def stop(proc):
    if WINDOWS:
        # The tree: dotnet watch, the game and the build servers it started.
        subprocess.run(['taskkill', '/T', '/F', '/PID', str(proc.pid)], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            proc.wait(20)
        except subprocess.TimeoutExpired:
            pass
        return
    if proc.poll() is None:
        try:
            os.killpg(proc.pid, signal.SIGTERM)
        except ProcessLookupError:
            return
        try:
            proc.wait(20)
        except subprocess.TimeoutExpired:
            os.killpg(proc.pid, signal.SIGKILL)
    try:
        os.killpg(proc.pid, signal.SIGKILL)  # anything left in the group (the watcher's children)
    except ProcessLookupError:
        pass


def edit(src, old, new):
    text = open(src, encoding='utf-8', newline='').read().replace('\r\n', '\n')
    assert old in text, 'scaffold changed: ' + old
    open(src, 'w', encoding='utf-8', newline='').write(text.replace(old, new, 1))


def wait_for(fn, what, timeout=90):
    t0 = time.time()
    last = None
    while time.time() - t0 < timeout:
        try:
            last = fn()
            if last:
                return last
        except (ConnectionError, OSError, RuntimeError, ValueError):
            pass
        time.sleep(0.4)
    check(what, False, 'timed out; last=' + str(last))
    return None


def ship(live):
    return live.q('state_query')['entities'][0]


def run(live, sock, proc, src, game, work, logf):
    hr = live.q('hot_reload')
    check('hot_reload reports an active runtime', hr['active'] is True and hr['applied'] == 0 and hr['replaySafe'] is True, hr)
    live.call('command_send', entity='Ship#1', command='Steer', args=[1, 0, False])
    time.sleep(0.5)
    s0 = ship(live)
    t0 = live.q('state_query')['tick']
    check('the game runs on its own and the ship moved', s0['X'] > 0 and t0 > 0, f"tick {t0} X {s0['X']}")
    _, png0 = live.call('capture_frame', size='360p')
    label0 = live.q('ui_query', selector='#pos')[0]['text']

    # 1. view method body
    edit(src, '$"x {X.ToInt()}', '$"HOT x {X.ToInt()}')
    edit(src, 'tint: Boost ? Color.Rgb(0xFFD080) : Color.White', 'tint: Color.Rgb(0x30FF30)')
    if wait_for(lambda: live.q('hot_reload')['applied'] >= 1, 'view edit applied'):
        label1 = live.q('ui_query', selector='#pos')[0]['text']
        _, png1 = live.call('capture_frame', size='360p')
        s1 = ship(live)
        check('view edit: label text changed', label1.startswith('HOT x'), f'{label0!r} -> {label1!r}')
        check('view edit: captured frame differs', hashlib.sha256(png0).digest() != hashlib.sha256(png1).digest())
        check('view edit: world kept (X unchanged, tick moved on)', s1['X'] == s0['X'] and live.q('state_query')['tick'] > t0, s1)

    # 2. sim OnTick body: Y grows by one each tick from now on
    y_before = ship(live)['Y']
    tick_before = live.q('state_query')['tick']
    edit(src, 'protected override void OnTick()\n    {', 'protected override void OnTick()\n    {\n        Y += Fixed.FromInt(1);')
    if wait_for(lambda: live.q('hot_reload')['applied'] >= 2, 'sim edit applied'):
        a1 = live.q('state_query')
        time.sleep(0.5)
        a2 = live.q('state_query')
        grew = a2['entities'][0]['Y'] - a1['entities'][0]['Y']
        ticks = a2['tick'] - a1['tick']
        check('sim edit: Y now changes every tick', ticks >= 10 and grew >= ticks, f'{grew} over {ticks} ticks')
        check('sim edit: tick continued, X kept', a1['tick'] > tick_before and a2['entities'][0]['X'] == s0['X'], f"tick {tick_before} -> {a2['tick']}")
        hr = live.q('hot_reload')
        check('hot_reload: applied edits make the run replay-unsafe', hr['replaySafe'] is False and hr['state'] == 'applied', hr)

    # 3. screen builder
    edit(src, 'TextColor(Color.White)', 'TextColor(Color.Rgb(0xFF0000))')
    if wait_for(lambda: live.q('hot_reload')['applied'] >= 3, 'screen edit applied'):
        color = live.q('ui_query', selector='#pos')[0]['style']['color']
        check('screen builder edit: the rebuilt screen has the new colour', color == '#ff0000', color)

    # 4. new method + new synced property (applies live)
    edit(src, '[Synced] public partial int Dy { get; private set; }', '[Synced] public partial int Dy { get; private set; }\n    [Synced] public partial int Fuel { get; private set; }')
    edit(src, 'protected override void OnTick()\n    {', 'protected override void OnTick()\n    {\n        Burn();')
    edit(src, '    [Command]', '    private void Burn() => Fuel += 1;\n\n    [Command]')
    if wait_for(lambda: live.q('hot_reload')['applied'] >= 4, 'new member edit applied'):
        time.sleep(0.3)
        st = ship(live)
        snap, _ = live.call('snapshot_save')
        check('new synced property and method: live, counting each tick', 'Fuel' in st and st['Fuel'] >= 5, st)
        check('new synced property: X and Y kept', st['X'] == s0['X'] and st['Y'] > y_before, st)

    # 6. an edit that does not compile: reported, the game keeps running the last good code
    edit(src, 'Burn();', 'Burn(; ')
    wait_for(lambda: live.q('hot_reload')['state'] == 'error', 'compile error reported')
    hr = live.q('hot_reload')
    check('compile error: reported with the compiler message', hr['state'] == 'error' and 'CS' in (hr.get('message') or ''), hr.get('message'))
    t = live.q('state_query')['tick']
    time.sleep(0.5)
    check('compile error: the game keeps running', live.q('state_query')['tick'] > t)
    edit(src, 'Burn(; ', 'Burn();')
    wait_for(lambda: live.q('hot_reload')['state'] == 'applied', 'fixed edit applied again')

    # 5. rude edit: the type of a synced member changes
    before = live.q('state_query')
    fuel = before['entities'][0]['Fuel']
    edit(src, 'public partial int Dy', 'public partial long Dy')
    edit(src, 'speed * Dy', 'speed * (int)Dy')
    edit(src, 'dy != ship.Dy', 'dy != (int)ship.Dy')

    def restarted():
        try:
            r = Live(sock)
        except OSError:
            return None
        try:
            h = r.q('hot_reload')
            return (r, h) if h['restarts'] >= 1 and h['state'] == 'restarted' else (r.close() or None)
        except (ConnectionError, RuntimeError, ValueError, OSError):
            r.close()
            return None

    time.sleep(2)
    got = wait_for(restarted, 'game restarted after the rude edit', 180)
    if got:
        live.close()
        live, h = got
        after = live.q('state_query')
        e = after['entities'][0]
        check('rude edit: message names the cause', 'ENC' in (h.get('message') or '') or 'restart' in (h.get('message') or '').lower(), h.get('message'))
        check('rude edit: the restarted game loaded the saved world', h['resumed'] is True and after['tick'] >= before['tick'], f"tick {before['tick']} -> {after['tick']}")
        check('rude edit: entity state carried over (X, Y, Fuel)', e['X'] == before['entities'][0]['X'] and e['Y'] >= before['entities'][0]['Y'] and e['Fuel'] >= fuel, e)
    log = open(os.path.join(work, 'run.log'), encoding='utf-8', errors='replace').read()
    check('output tells the user what could not be applied', 'cannot be applied to the running game' in log)


if __name__ == '__main__':
    sys.exit(main())
