#!/usr/bin/env python3
"""Drives a running dev build of Solitaire on a device over its live-control socket and prints one JSON result.

  live-check.py ENDPOINT OUTDIR [--serial ADB_SERIAL] [--expect-audio aaudio|audiounit]

Checks: the window and GPU the game reports, a presented frame (saved as PNG), real finger input when an adb serial is given
(`adb shell input tap` on the buttons' on-screen positions, so the platform's own touch path runs), a card drawn by finger,
the audio device (backend, started, frames played, peak after a draw), and that the state hash survives going to the home
screen and coming back."""
import json, os, subprocess, sys, time
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mcp import Mcp

endpoint, outdir = sys.argv[1], sys.argv[2]
serial = sys.argv[sys.argv.index('--serial') + 1] if '--serial' in sys.argv else None
expect_audio = sys.argv[sys.argv.index('--expect-audio') + 1] if '--expect-audio' in sys.argv else None
os.makedirs(outdir, exist_ok=True)
m = Mcp(endpoint, timeout=180)   # a window on a slow compositor can take seconds per frame
res, problems = {}, []


def adb(*a):
    return subprocess.run(['adb', '-s', serial, *a], capture_output=True, text=True, timeout=30).stdout


def centre(selector):
    q = m.call('ui_query', selector=selector)
    for _ in range(10):   # a screen change lays out over the next few frames
        if q:
            break
        time.sleep(0.5)
        m.call('step', frames=3)
        q = m.call('ui_query', selector=selector)
    if not q:
        raise RuntimeError(f'no element {selector}')
    s = q[0]['screen']          # pixels of the window
    return int(s['x'] + s['w'] / 2), int(s['y'] + s['h'] / 2)


def table():
    return m.call('state_query', entity='Table')


# A phone creates the window a moment after the process starts; wait until a frame has been presented.
for _ in range(40):
    info = m.call('info')
    if info.get('presented') or info.get('headless'):
        break
    time.sleep(0.5)
res['window'] = {k: info.get(k) for k in ('width', 'height', 'scale', 'backend', 'adapter', 'hash', 'drift')}

# Finger input: the menu button, then the stock. With an adb serial the taps are real (`adb shell input tap`, so the OS delivers them);
# without one they are injected into the platform's touch queue by `ui_tap`, which is the same path after the OS callback.
def tap(selector):
    if serial:
        x, y = centre(selector)
        adb('shell', 'input', 'tap', str(x), str(y))
    else:
        m.call('ui_tap', selector=selector)
    time.sleep(1)
    m.call('step', frames=3)


try:
    m.call('screen_open', name='main')
    m.call('step', frames=3)
    tap('#new-draw1')
    on_table = m.call('info').get('screen') == 'table'
    before = table()
    tap('#stock')
    after = table()
    drawn = len(after['Waste']) == len(before['Waste']) + 1 and after['Moves'] == before['Moves'] + 1
    res['finger'] = {'real_os_taps': bool(serial), 'menu_to_table': on_table, 'stock_drew_a_card': drawn, 'moves': after['Moves']}
    if not (on_table and drawn):
        problems.append('touch input did not play')
except Exception as e:
    res['finger'] = {'error': str(e)}
    problems.append(f'touch input: {e}')

# Sound: a few more draws, then the device section.
for _ in range(4):
    m.call('command_send', entity='Table', command='Draw')
    m.call('step', frames=30)
time.sleep(1.5)
m.call('step', frames=20)
dev = (m.call('audio') or {}).get('device', {})
dev = dict(dev, deviceState=dev.get('deviceState') or dev.get('state'))   # the Linux backend names it `state`
res['audio'] = {k: dev.get(k) for k in ('backend', 'deviceState', 'framesWritten', 'framesPlayed', 'peakPlayed', 'underruns', 'losses', 'audioThreadAllocatedBytes', 'detail')}
if expect_audio and dev.get('backend') != expect_audio:
    problems.append(f"audio backend {dev.get('backend')} (wanted {expect_audio})")
# A state of Running proves nothing on its own: the device must have been handed frames and must have taken them.
# The WASAPI sink has no counters to report (state, frames and peak stay empty), so for it only the backend name is checked.
if dev.get('backend') in ('wasapi',):
    res['audio']['note'] = 'this backend does not report device counters'
else:
    if dev.get('deviceState') not in ('started', 'Running', 'running'):
        problems.append(f"audio device not running (state {dev.get('deviceState')})")
    if not dev.get('framesWritten'):
        problems.append('no frames were written to the audio device (framesWritten 0)')
    if not dev.get('framesPlayed'):
        problems.append('the audio device took no frames (framesPlayed 0)')
    if not dev.get('peakPlayed'):
        problems.append('no sound reached the device (peakPlayed 0)')

# A frame as the window presents it.
png_info, pngs = m.call2('capture_frame', presented=True)
if pngs:
    path = os.path.join(outdir, 'presented.png')
    open(path, 'wb').write(pngs[0])
    res['capture'] = {'path': path, 'width': png_info.get('width'), 'height': png_info.get('height'), 'renderer': png_info.get('renderer'), 'sha256': png_info.get('sha256')}
else:
    problems.append('no presented frame returned')

# Going to the background and coming back keeps the state: the home button on a phone with adb, the lifecycle events elsewhere.
def board(c):
    t = c.call('state_query', entity='Table')
    return {k: v for k, v in t.items() if k != 'Elapsed'}   # the clock runs while the game is up


try:
    h0 = board(m)
    if serial:
        adb('shell', 'input', 'keyevent', 'KEYCODE_HOME')
        time.sleep(2)
        adb('shell', 'monkey', '-p', 'dev.onegame.solitaire', '-c', 'android.intent.category.LAUNCHER', '1')
        time.sleep(2.5)
        m = Mcp(endpoint)
    else:
        m.call('app_lifecycle', state='paused')
        time.sleep(1)
        m.call('app_lifecycle', state='resumed')
        time.sleep(1)
    h1 = board(m)
    res['background_and_back'] = {'real_home_button': bool(serial), 'same_cards_and_moves': h0 == h1, 'moves': h1.get('Moves')}
    if h0 != h1:
        problems.append('state changed across background and back')
except Exception as e:
    res['background_and_back'] = {'error': str(e)}
    problems.append(f'background and back: {e}')

res['ok'] = not problems
res['problems'] = problems
print(json.dumps(res))
sys.exit(0 if not problems else 1)
