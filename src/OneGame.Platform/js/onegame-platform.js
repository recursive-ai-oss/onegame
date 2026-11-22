// The browser side of the platform layer: one canvas, its size and pixel ratio, pointer, touch, keyboard, text input (including
// input methods) through a hidden text field, the Gamepad API, visibility, and the animation-frame loop.
// Events are queued here and handed to the managed side as one JSON text per frame.
// References: HTML Living Standard (UI events, the Gamepad API through the W3C Gamepad spec, requestAnimationFrame).

let canvas = null, input = null;
const queue = [];
let textActive = false;
let composing = false;
const pads = new Map();      // index -> { id, buttons: [bool], axes: [number] }
let injected = null;         // test hook: replaces navigator.getGamepads

const MODS = (e) => (e.shiftKey ? 1 : 0) | (e.ctrlKey ? 2 : 0) | (e.altKey ? 4 : 0) | (e.metaKey ? 8 : 0) | (e.getModifierState && e.getModifierState('CapsLock') ? 16 : 0) | (e.getModifierState && e.getModifierState('NumLock') ? 32 : 0);
const push = (o) => { if (queue.length < 20000) queue.push(o); };
// Pointer positions keep their fraction: a device pixel is the unit, not the granularity.
const pixels = (v) => v * (window.devicePixelRatio || 1);
let probe = null;
let lastSize = '', lastSafe = '';

// The canvas is as large as the visible part of the page. 100vw by 100vh is the largest the page can become, which on a phone is taller than
// what shows while the browser's toolbar is out, so the picture would stretch against the drawing surface and touches would land at the wrong height.
function measure() {
  const dpr = window.devicePixelRatio || 1;
  return { w: Math.max(1, Math.round(window.innerWidth * dpr)), h: Math.max(1, Math.round(window.innerHeight * dpr)), s: dpr, sa: safeArea() };
}

// The areas the display's cut-outs and system bars cover, in device pixels (CSS env(safe-area-inset-*), which a page gets with viewport-fit=cover).
function safeArea() {
  if (!probe) {
    probe = document.createElement('div');
    probe.setAttribute('aria-hidden', 'true');
    Object.assign(probe.style, { position: 'fixed', left: '0', top: '0', width: '0', height: '0', visibility: 'hidden', pointerEvents: 'none', boxSizing: 'content-box',
      paddingTop: 'env(safe-area-inset-top, 0px)', paddingRight: 'env(safe-area-inset-right, 0px)', paddingBottom: 'env(safe-area-inset-bottom, 0px)', paddingLeft: 'env(safe-area-inset-left, 0px)' });
    document.body.appendChild(probe);
  }
  const c = getComputedStyle(probe), d = window.devicePixelRatio || 1;
  const px = (v) => Math.max(0, Math.round((parseFloat(v) || 0) * d));
  return { l: px(c.paddingLeft), t: px(c.paddingTop), r: px(c.paddingRight), b: px(c.paddingBottom) };
}

// Fits the canvas to the visible area and reports a changed size or safe area once.
function relayout() {
  canvas.style.width = window.innerWidth + 'px';
  canvas.style.height = window.innerHeight + 'px';
  const n = measure(), size = n.w + 'x' + n.h + '@' + n.s, safe = n.sa.l + ',' + n.sa.t + ',' + n.sa.r + ',' + n.sa.b;
  if (size !== lastSize) { lastSize = size; push({ t: 'rs', w: n.w, h: n.h, s: n.s }); }
  if (safe !== lastSafe) { lastSafe = safe; push({ t: 'sa', l: n.sa.l, tp: n.sa.t, r: n.sa.r, b: n.sa.b }); }
}

export function init(canvasId) {
  canvas = document.getElementById(canvasId);
  if (!canvas) throw new Error('No canvas element with id "' + canvasId + '".');
  Object.assign(document.body.style, { margin: '0', overflow: 'hidden', background: '#000' });
  Object.assign(canvas.style, { position: 'fixed', left: '0', top: '0', width: window.innerWidth + 'px', height: window.innerHeight + 'px', display: 'block', touchAction: 'none', outline: 'none' });
  canvas.tabIndex = 0;
  const m = measure();
  canvas.width = m.w; canvas.height = m.h;
  lastSize = m.w + 'x' + m.h + '@' + m.s;
  lastSafe = m.sa.l + ',' + m.sa.t + ',' + m.sa.r + ',' + m.sa.b;

  input = document.createElement('input');
  input.type = 'text'; input.autocomplete = 'off'; input.autocapitalize = 'off'; input.spellcheck = false;
  input.setAttribute('aria-hidden', 'true');
  Object.assign(input.style, { position: 'fixed', left: '0', top: '0', width: '1px', height: '1px', opacity: '0', border: '0', padding: '0', pointerEvents: 'none' });
  document.body.appendChild(input);

  window.addEventListener('resize', relayout);
  window.addEventListener('orientationchange', relayout);
  if (window.visualViewport) window.visualViewport.addEventListener('resize', relayout);
  document.addEventListener('visibilitychange', () => push({ t: 'vis', hidden: document.hidden }));
  window.addEventListener('focus', () => push({ t: 'fc', f: true }));
  window.addEventListener('blur', () => push({ t: 'fc', f: false }));
  window.addEventListener('beforeunload', () => push({ t: 'close' }));

  // Position on the drawing surface: the canvas's own scale, so a surface that is a pixel off the CSS size still maps exactly.
  const rel = (e) => {
    const r = canvas.getBoundingClientRect();
    return { x: (e.clientX - r.left) * (canvas.width / (r.width || 1)), y: (e.clientY - r.top) * (canvas.height / (r.height || 1)) };
  };
  // A finger is a small integer (0 to 31) reused after it lifts, as the platform events define; the mouse and pen use the pointer events.
  const fingers = new Map();
  const fingerSlot = (e) => {
    let id = fingers.get(e.pointerId);
    if (id === undefined && e.type === 'pointerdown') {
      const used = new Set(fingers.values());
      id = 0;
      while (used.has(id)) id++;
      fingers.set(e.pointerId, id);
    }
    return id;
  };
  const touch = (kind, e, release) => {
    const id = fingerSlot(e);
    if (id === undefined) return;
    const p = rel(e);
    push({ t: kind, id, x: p.x, y: p.y, p: release ? 0 : (e.pressure > 0 ? e.pressure : 1) });
    if (release) fingers.delete(e.pointerId);
  };
  canvas.addEventListener('pointermove', (e) => {
    if (e.pointerType === 'touch') { touch('xm', e, false); return; }
    const p = rel(e);
    push({ t: 'pm', x: p.x, y: p.y, dx: pixels(e.movementX || 0), dy: pixels(e.movementY || 0), lock: document.pointerLockElement === canvas, m: MODS(e) });
  });
  canvas.addEventListener('pointerdown', (e) => {
    canvas.focus({ preventScroll: true });
    try { canvas.setPointerCapture(e.pointerId); } catch { }
    e.preventDefault();
    if (e.pointerType === 'touch') { touch('xd', e, false); return; }
    const p = rel(e);
    push({ t: 'pm', x: p.x, y: p.y, dx: 0, dy: 0, m: MODS(e) });
    push({ t: 'pd', b: e.button, x: p.x, y: p.y, m: MODS(e) });
  });
  canvas.addEventListener('pointerup', (e) => {
    try { canvas.releasePointerCapture(e.pointerId); } catch { }
    if (e.pointerType === 'touch') { touch('xu', e, true); return; }
    const p = rel(e);
    push({ t: 'pu', b: e.button, x: p.x, y: p.y, m: MODS(e) });
  });
  canvas.addEventListener('pointercancel', (e) => {
    if (e.pointerType === 'touch') { touch('xc', e, true); return; }
    const p = rel(e);
    push({ t: 'pu', b: 0, x: p.x, y: p.y, m: MODS(e) });
  });
  canvas.addEventListener('pointerenter', () => push({ t: 'pi', inside: true }));
  canvas.addEventListener('pointerleave', () => push({ t: 'pi', inside: false }));
  canvas.addEventListener('contextmenu', (e) => e.preventDefault());
  canvas.addEventListener('wheel', (e) => {
    const p = rel(e);
    const k = e.deltaMode === 0 ? 100 : e.deltaMode === 1 ? 1 : 1 / 3;
    push({ t: 'wh', x: p.x, y: p.y, dx: -e.deltaX / k, dy: -e.deltaY / k, m: MODS(e) });
    e.preventDefault();
  }, { passive: false });

  const keyHandler = (down) => (e) => {
    if (e.isComposing || e.key === 'Process' || e.keyCode === 229) return;
    push({ t: down ? 'kd' : 'ku', code: e.code, rep: e.repeat, m: MODS(e) });
    const shortcut = e.ctrlKey || e.metaKey || e.altKey || /^F\d+$/.test(e.code);
    if (down && !shortcut && !textActive && /^(Space|Arrow|Tab|Backspace|Page|Home|End|Enter|Slash|Quote)/.test(e.code)) e.preventDefault();
  };
  window.addEventListener('keydown', keyHandler(true));
  window.addEventListener('keyup', keyHandler(false));

  input.addEventListener('compositionstart', () => { composing = true; });
  input.addEventListener('compositionupdate', (e) => {
    const t = e.data || '';
    push({ t: 'tc', text: t, cs: t.length, ce: t.length });
  });
  input.addEventListener('compositionend', (e) => {
    composing = false;
    push({ t: 'tc', text: '', cs: 0, ce: 0 });
    if (e.data) push({ t: 'ti', text: e.data });
    input.value = '';
  });
  input.addEventListener('input', (e) => {
    if (composing || e.isComposing) return;
    if (e.data) push({ t: 'ti', text: e.data });
    input.value = '';
  });
  window.addEventListener('paste', (e) => { const t = e.clipboardData && e.clipboardData.getData('text'); if (t) push({ t: 'cb', text: t }); });

  document.addEventListener('pointerlockchange', () => push({ t: 'lock', on: document.pointerLockElement === canvas }));
  canvas.focus({ preventScroll: true });
  return JSON.stringify(m);
}

export function size() { return JSON.stringify(measure()); }

// The title the page set, so the game keeps it; the game's own name is used only for a page without one.
export function pageTitle() { return document.title || ''; }

// ---- gamepads (standard mapping: W3C Gamepad spec, section "Standard Gamepad") ----
const BUTTON_MAP = [0, 1, 2, 3, 4, 5, -1, -1, 6, 7, 9, 10, 11, 12, 13, 14, 8];   // standard button index -> GamepadButton ordinal; 6 and 7 are triggers
function pollGamepads() {
  const list = injected ? injected() : (navigator.getGamepads ? navigator.getGamepads() : []);
  const seen = new Set();
  for (const g of list) {
    if (!g || !g.connected) continue;
    seen.add(g.index);
    let st = pads.get(g.index);
    if (!st) {
      st = { buttons: [], axes: [0, 0, 0, 0, 0, 0] };
      pads.set(g.index, st);
      push({ t: 'gc', id: g.index, name: g.id });
    }
    g.buttons.forEach((b, i) => {
      const down = typeof b === 'object' ? b.pressed : !!b;
      const value = typeof b === 'object' ? b.value : (b ? 1 : 0);
      if (i === 6 || i === 7) {
        const axis = i === 6 ? 4 : 5;
        if (Math.abs(st.axes[axis] - value) > 0.01) { st.axes[axis] = value; push({ t: 'ga', id: g.index, axis, v: value }); }
      } else if (i < BUTTON_MAP.length && st.buttons[i] !== down) {
        st.buttons[i] = down;
        push({ t: 'gb', id: g.index, b: BUTTON_MAP[i], down });
      }
    });
    for (let a = 0; a < Math.min(4, g.axes.length); a++) {
      const v = g.axes[a];
      if (Math.abs(st.axes[a] - v) > 0.01) { st.axes[a] = v; push({ t: 'ga', id: g.index, axis: a, v }); }
    }
  }
  for (const idx of [...pads.keys()]) if (!seen.has(idx)) { pads.delete(idx); push({ t: 'gd', id: idx }); }
}

// Tests replace the gamepad source with a function returning Gamepad-shaped objects.
export function injectGamepads(fn) { injected = fn; }

let drains = 0;
export function drain() {
  pollGamepads();
  // Browsers announce no change of the safe area by itself; it follows rotation and resizing, which relayout already sees, and this catches the rest.
  if (++drains % 30 === 0) relayout();
  if (queue.length === 0) return '';
  const out = JSON.stringify(queue);
  queue.length = 0;
  return out;
}

export function setTitle(t) { document.title = t; }
export function setCursorHidden(h) { canvas.style.cursor = h ? 'none' : 'default'; }
export function setCursorLocked(l) { if (l) canvas.requestPointerLock && canvas.requestPointerLock(); else if (document.pointerLockElement) document.exitPointerLock(); }
export function setFullscreen(f) { if (f) (canvas.requestFullscreen || (() => { })).call(canvas); else if (document.fullscreenElement) document.exitFullscreen(); }
export function setTextInput(active, x, y, w, h) {
  textActive = active;
  if (active) {
    const dpr = window.devicePixelRatio || 1;
    Object.assign(input.style, { left: (x / dpr) + 'px', top: (y / dpr) + 'px', width: Math.max(1, w / dpr) + 'px', height: Math.max(1, h / dpr) + 'px' });
    input.focus({ preventScroll: true });
  } else {
    input.blur(); input.value = '';
    canvas.focus({ preventScroll: true });
  }
}
export function clipboardWrite(text) { try { navigator.clipboard.writeText(text); } catch { } }

// The animation-frame loop: calls back with the time in milliseconds until stop() is called.
let running = false;
export function startFrames(callback) {
  running = true;
  const tick = (t) => {
    if (!running) return;
    try {
      callback(t);
    } catch (e) {
      // A frame that threw is that frame's problem: the loop goes on, so the page keeps drawing and a tool can still capture it.
      const o = (window.__onegame = window.__onegame || {});
      o.frameErrors = (o.frameErrors || 0) + 1;
      o.lastFrameError = String((e && e.stack) || e);
      if (o.frameErrors <= 3 || o.frameErrors % 600 === 0) console.error('onegame: a frame failed (' + o.frameErrors + '): ' + o.lastFrameError);
    }
    // Page-level counters for tools: how many frames ran and when the first one finished (milliseconds since navigation start).
    const o = (window.__onegame = window.__onegame || {});
    o.frames = (o.frames || 0) + 1;
    if (o.frames === 1) o.firstFrameMs = performance.now();
    requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
}
export function stopFrames() { running = false; }

// ---- WebSocket (binary messages), polled by the managed side ----
const sockets = [];
export function wsOpen(url) {
  const s = { ws: null, inbox: [], state: 0, code: 0 };
  try {
    const ws = new WebSocket(url);
    ws.binaryType = 'arraybuffer';
    ws.onopen = () => { s.state = 1; };
    ws.onmessage = (e) => { if (e.data instanceof ArrayBuffer) s.inbox.push(new Uint8Array(e.data)); };
    ws.onerror = () => { };
    ws.onclose = (e) => { s.state = 3; s.code = e.code; };
    s.ws = ws;
  } catch { s.state = 3; s.code = 1006; }
  sockets.push(s);
  return sockets.length - 1;
}
export function wsState(id) { return sockets[id].state; }
export function wsCode(id) { return sockets[id].code; }
export function wsPending(id) { const q = sockets[id].inbox; return q.length ? q[0].length : -1; }
export function wsReceive(id, dest) { const m = sockets[id].inbox.shift(); dest.set(m); return m.length; }
export function wsSend(id, data) { const s = sockets[id]; if (s.state === 1) s.ws.send(data.slice()); }
export function wsClose(id) { const s = sockets[id]; if (s.ws && s.state < 2) { s.state = 2; s.ws.close(1000); } }

// ---- Files that survive a reload (IndexedDB), read and written synchronously by the managed side ----
// The page loads every file into memory with storageOpen before the program starts (boot script), so reads never wait. A write updates
// memory at once and queues one IndexedDB transaction per file; a transaction either stores the whole file or nothing, so a reload
// during a write finds the old file or the new one. Reference: W3C Indexed Database API 3.0 (transactions are atomic).
const kv = new Map();        // key -> Uint8Array
let kvDb = null, kvKind = 'memory', kvPending = 0, kvFailed = 0, kvWhy = '';
export function storageOpen(name) {
  return new Promise((resolve) => {
    if (!globalThis.indexedDB) { kvWhy = 'indexedDB is not available'; resolve(kvKind); return; }
    let rq;
    try { rq = indexedDB.open(name || 'onegame-files', 1); } catch (e) { kvWhy = String(e); resolve(kvKind); return; }
    rq.onupgradeneeded = () => { rq.result.createObjectStore('files'); };
    rq.onerror = () => { kvWhy = String(rq.error); resolve(kvKind); };
    rq.onblocked = () => { kvWhy = 'blocked'; resolve(kvKind); };
    rq.onsuccess = () => {
      const db = rq.result;
      try {
        const tx = db.transaction('files', 'readonly');
        const cur = tx.objectStore('files').openCursor();
        cur.onsuccess = () => {
          const c = cur.result;
          if (c) { if (c.value instanceof Uint8Array || c.value instanceof ArrayBuffer) kv.set(String(c.key), new Uint8Array(c.value)); c.continue(); }
        };
        tx.oncomplete = () => { kvDb = db; kvKind = 'indexeddb'; resolve(kvKind); };
        tx.onerror = tx.onabort = () => { kvWhy = String(tx.error); resolve(kvKind); };
      } catch (e) { kvWhy = String(e); resolve(kvKind); }
    };
  });
}
function kvPersist(work) {
  if (!kvDb) return;
  kvPending++;
  try {
    const tx = kvDb.transaction('files', 'readwrite');
    work(tx.objectStore('files'));
    tx.oncomplete = () => { kvPending--; };
    tx.onerror = tx.onabort = () => { kvPending--; kvFailed++; kvWhy = String(tx.error); };
  } catch (e) { kvPending--; kvFailed++; kvWhy = String(e); }
}
export function kvSize(key) { const v = kv.get(key); return v ? v.length : -1; }
export function kvRead(key, dest) { const v = kv.get(key); if (!v) return -1; dest.set(v); return v.length; }
export function kvWrite(key, data) { const v = data.slice(); kv.set(key, v); kvPersist((s) => s.put(v, key)); }
export function kvDelete(key) { if (kv.delete(key)) kvPersist((s) => s.delete(key)); }
export function kvList(prefix) { return [...kv.keys()].filter((k) => k.startsWith(prefix)).sort().join('\n'); }
// kind: "indexeddb" or "memory"; pending: transactions not yet committed; failed: transactions the browser refused (quota, closed database).
export function kvStatus() { return JSON.stringify({ kind: kvKind, pending: kvPending, failed: kvFailed, why: kvWhy, files: kv.size }); }
