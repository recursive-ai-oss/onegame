#!/usr/bin/env python3
"""A virtual gamepad made through /dev/uinput, playing a scripted timeline, so the real evdev path (device scan, ioctl probing,
event reads, the mapping database) can be exercised with no hardware.

  virtual-pad.py [start_seconds] [hold_seconds] [xbox|dualsense]

The models copy what the Linux drivers report: `xbox` is a wired 360-style pad (sticks -32768..32767, triggers 0..255 on Z/RZ);
`dualsense` is what hid-playstation reports for a PS5 pad (sticks and triggers 0..255, ids 054c:0ce6). Needs write access to /dev/uinput."""
import fcntl, os, struct, sys, time

UI_SET_EVBIT, UI_SET_KEYBIT, UI_SET_ABSBIT = 0x40045564, 0x40045565, 0x40045567
UI_DEV_CREATE, UI_DEV_DESTROY = 0x5501, 0x5502
EV_SYN, EV_KEY, EV_ABS = 0, 1, 3
BTN = dict(A=0x130, B=0x131, Y=0x133, X=0x134, TL=0x136, TR=0x137, TL2=0x138, TR2=0x139, SELECT=0x13a, START=0x13b, MODE=0x13c, THUMBL=0x13d, THUMBR=0x13e)
BTN_XBOX = dict(A=0x130, B=0x131, X=0x133, Y=0x134, TL=0x136, TR=0x137, SELECT=0x13a, START=0x13b, MODE=0x13c, THUMBL=0x13d, THUMBR=0x13e)
ABS_X, ABS_Y, ABS_Z, ABS_RX, ABS_RY, ABS_RZ, HX, HY = 0, 1, 2, 3, 4, 5, 0x10, 0x11

start = float(sys.argv[1]) if len(sys.argv) > 1 else 15
hold = float(sys.argv[2]) if len(sys.argv) > 2 else 10
model = sys.argv[3] if len(sys.argv) > 3 else 'xbox'
if model == 'dualsense':
    name, ids, buttons = b'Sony Interactive Entertainment DualSense Wireless Controller', (0x03, 0x054c, 0x0ce6, 0x8111), BTN
    sticks, trig, tmax, stick_lo, stick_hi = (ABS_X, ABS_Y, ABS_RX, ABS_RY), (ABS_Z, ABS_RZ), 255, 0, 255
    centre = 128
else:
    name, ids, buttons = b'Microsoft X-Box 360 pad', (0x03, 0x045e, 0x028e, 0x0114), BTN_XBOX
    sticks, trig, tmax, stick_lo, stick_hi = (ABS_X, ABS_Y, ABS_RX, ABS_RY), (ABS_Z, ABS_RZ), 255, -32768, 32767
    centre = 0

fd = os.open('/dev/uinput', os.O_WRONLY | os.O_NONBLOCK)
for ev in (EV_KEY, EV_ABS, EV_SYN):
    fcntl.ioctl(fd, UI_SET_EVBIT, ev)
for b in buttons.values():
    fcntl.ioctl(fd, UI_SET_KEYBIT, b)
absmax, absmin, fuzz, flat = [0] * 64, [0] * 64, [0] * 64, [0] * 64
for a in sticks:
    fcntl.ioctl(fd, UI_SET_ABSBIT, a); absmin[a] = stick_lo; absmax[a] = stick_hi
for a in trig:
    fcntl.ioctl(fd, UI_SET_ABSBIT, a); absmin[a] = 0; absmax[a] = tmax
for a in (HX, HY):
    fcntl.ioctl(fd, UI_SET_ABSBIT, a); absmin[a] = -1; absmax[a] = 1
dev = name.ljust(80, b'\0') + struct.pack('<HHHHI', *ids, 0) + struct.pack('<64i', *absmax) + struct.pack('<64i', *absmin) + struct.pack('<64i', *fuzz) + struct.pack('<64i', *flat)
os.write(fd, dev)
fcntl.ioctl(fd, UI_DEV_CREATE)

def emit(t, c, v):
    os.write(fd, struct.pack('<qqHHi', 0, 0, t, c, v))
def syn():
    emit(EV_SYN, 0, 0)
def log(m):
    print(f'{time.monotonic():.1f} pad {m}', flush=True)

for a in sticks:   # rest position
    emit(EV_ABS, a, centre)
syn()
log(f'created {model}'); time.sleep(start)
lo, hi = stick_lo, stick_hi
# evdev convention: ABS_Y toward the minimum = stick pushed up.
steps = [(f'{k} down', [(EV_KEY, c, 1)], 0.6) for k, c in buttons.items()]
steps = [s for k, c in buttons.items() for s in ((f'{k} down', [(EV_KEY, c, 1)], 0.8), (f'{k} up', [(EV_KEY, c, 0)], 0.4))]
steps += [
    ('left stick up', [(EV_ABS, ABS_Y, lo)], 1.5), ('left stick centre', [(EV_ABS, ABS_Y, centre)], 0.8),
    ('left stick right', [(EV_ABS, ABS_X, hi)], 1.5), ('left stick centre', [(EV_ABS, ABS_X, centre)], 0.8),
    ('right stick up', [(EV_ABS, ABS_RY, lo)], 1.5), ('right stick centre', [(EV_ABS, ABS_RY, centre)], 0.8),
    ('left trigger full', [(EV_ABS, ABS_Z, tmax)], 1.5), ('left trigger off', [(EV_ABS, ABS_Z, 0)], 0.8),
    ('right trigger full', [(EV_ABS, ABS_RZ, tmax)], 1.5), ('right trigger off', [(EV_ABS, ABS_RZ, 0)], 0.8),
    ('d-pad up', [(EV_ABS, HY, -1)], 0.8), ('d-pad centre', [(EV_ABS, HY, 0)], 0.5),
    ('d-pad right', [(EV_ABS, HX, 1)], 0.8), ('d-pad centre', [(EV_ABS, HX, 0)], 0.5),
]
for label, evs, wait in steps:
    log(label)
    for e in evs:
        emit(*e)
    syn(); time.sleep(wait)
log('done; holding'); time.sleep(hold)
fcntl.ioctl(fd, UI_DEV_DESTROY); os.close(fd); log('destroyed')
