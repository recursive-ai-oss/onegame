#!/usr/bin/env python3
"""Checks what a running windowed dev build shows, over its live-control socket, and saves the presented frame as a PNG.

  capture-check.py ENDPOINT OUT.png [--command ENTITY:COMMAND] [--expect-overlay] [--expect-particles N] [--wait SECONDS]

--command sends a command first (for example `Torch:Hit`, which makes a particle burst) and takes the frame a moment later.
--expect-overlay requires the debug overlay to be showing (it reports its own text through the `overlay` operation).
--expect-particles N requires at least N live particles across the game's emitters (the `particles` operation).
Prints one JSON line; the exit code is 1 when an expectation failed."""
import json, os, sys, time
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mcp import Mcp

endpoint, out = sys.argv[1], sys.argv[2]
arg = lambda name: sys.argv[sys.argv.index(name) + 1] if name in sys.argv else None
m = Mcp(endpoint, timeout=120)
res, problems = {}, []
if arg('--command'):
    entity, command = arg('--command').split(':', 1)
    m.call('command_send', entity=entity, command=command)
time.sleep(float(arg('--wait') or 0.3))
if '--expect-overlay' in sys.argv:
    ov = m.call('overlay')
    res['overlay'] = {'visible': ov.get('visible'), 'lines': ov.get('lines')}
    if not ov.get('visible'):
        problems.append('the debug overlay is not showing')
if arg('--expect-particles'):
    p = m.call('particles')
    live = sum(i.get('live', 0) for i in p.get('items', []))
    res['particles'] = {'emitters': p.get('emitters'), 'live': live}
    if live < int(arg('--expect-particles')):
        problems.append(f'{live} live particles, wanted {arg("--expect-particles")}')
info, pngs = m.call2('capture_frame', presented=True)
if pngs:
    open(out, 'wb').write(pngs[0])
    res['capture'] = {'path': out, 'width': info.get('width'), 'height': info.get('height'), 'renderer': info.get('renderer'), 'sha256': info.get('sha256')}
else:
    problems.append('no presented frame returned')
res['ok'] = not problems
res['problems'] = problems
print(json.dumps(res))
sys.exit(0 if not problems else 1)
