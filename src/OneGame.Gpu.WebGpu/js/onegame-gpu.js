// The browser side of the WebGPU backend. The managed code records everything it wants done (creating resources, uploads,
// command encoding) into one byte stream and hands it to exec(); this file decodes the stream and calls the WebGPU API.
// Objects live in a table indexed by the handle numbers the managed side allocates.
// References: W3C WebGPU (https://www.w3.org/TR/webgpu/) and WGSL (https://www.w3.org/TR/WGSL/).

const O = [];            // handle -> WebGPU object (or a small record around one)
const M = [];            // handle -> metadata of textures
let device = null, queue = null, adapterInfo = null;
const messages = [];
let lost = null;
const td = new TextDecoder();
const viewCache = new Map();
const mipPipelines = new Map();

const FMT = [null, 'r8unorm', 'rg8unorm', 'rgba8unorm', 'rgba8unorm-srgb', 'bgra8unorm', 'bgra8unorm-srgb', 'r16float', 'rgba16float', 'r32float', 'rg32float', 'rgba32float', 'r32uint', 'depth32float', 'depth32float-stencil8', 'rg16float', 'rg11b10ufloat', 'rgb10a2unorm', 'depth16unorm'];
const BPT = [0, 1, 2, 4, 4, 4, 4, 2, 8, 4, 8, 16, 4, 4, 8, 4, 4, 4, 2];
const VFMT = ['float32', 'float32x2', 'float32x3', 'float32x4', 'float16x2', 'float16x4', 'uint32', 'uint32x2', 'uint32x4', 'sint32', 'sint32x2', 'unorm8x4', 'uint8x4', 'unorm16x2', 'unorm16x4'];
const TOPO = ['triangle-list', 'triangle-strip', 'line-list', 'point-list'];
const CULL = ['none', 'front', 'back'];
const CMP = ['never', 'less', 'equal', 'less-equal', 'greater', 'not-equal', 'greater-equal', 'always'];
const BF = ['zero', 'one', 'src', 'one-minus-src', 'src-alpha', 'one-minus-src-alpha', 'dst', 'one-minus-dst', 'dst-alpha', 'one-minus-dst-alpha'];
const BO = ['add', 'subtract', 'reverse-subtract', 'min', 'max'];
const ADDR = ['clamp-to-edge', 'repeat', 'mirror-repeat'];
const isDepth = f => f.startsWith('depth');
const hasStencil = f => f === 'depth32float-stencil8';

function note(m) { if (messages.length < 200) messages.push(m); }

// ---- stream reader ----
let dv, u8, p;
const u32 = () => { const v = dv.getUint32(p, true); p += 4; return v; };
const i32 = () => { const v = dv.getInt32(p, true); p += 4; return v; };
const f32 = () => { const v = dv.getFloat32(p, true); p += 4; return v; };
const u64 = () => { const lo = u32(), hi = u32(); return hi * 4294967296 + lo; };
const bytes = () => { const n = u32(); const s = u8.subarray(p, p + n); p += (n + 3) & ~3; return s; };
const str = () => { const b = bytes(); return b.length ? td.decode(b) : ''; };
const lbl = s => s ? { label: s } : {};

export async function init(requestJson) {
  const req = JSON.parse(requestJson || '{}');
  if (!navigator.gpu) throw new Error('This browser has no WebGPU (navigator.gpu is missing).');
  const adapter = await navigator.gpu.requestAdapter({ powerPreference: 'high-performance', forceFallbackAdapter: !!req.fallback });
  if (!adapter) throw new Error('No WebGPU adapter is available.');
  const want = ['depth32float-stencil8', 'rg11b10ufloat-renderable', 'bgra8unorm-storage', 'float32-filterable', 'float32-blendable', 'indirect-first-instance', 'depth-clip-control', 'timestamp-query', 'texture-compression-bc'];
  const features = want.filter(f => adapter.features.has(f));
  const limitNames = ['maxTextureDimension1D', 'maxTextureDimension2D', 'maxTextureDimension3D', 'maxTextureArrayLayers', 'maxBindGroups', 'maxBindingsPerBindGroup', 'maxDynamicUniformBuffersPerPipelineLayout', 'maxDynamicStorageBuffersPerPipelineLayout', 'maxSampledTexturesPerShaderStage', 'maxSamplersPerShaderStage', 'maxStorageBuffersPerShaderStage', 'maxStorageTexturesPerShaderStage', 'maxUniformBuffersPerShaderStage', 'maxUniformBufferBindingSize', 'maxStorageBufferBindingSize', 'maxVertexBuffers', 'maxBufferSize', 'maxVertexAttributes', 'maxVertexBufferArrayStride', 'maxInterStageShaderVariables', 'maxColorAttachments', 'maxColorAttachmentBytesPerSample', 'maxComputeWorkgroupStorageSize', 'maxComputeInvocationsPerWorkgroup', 'maxComputeWorkgroupSizeX', 'maxComputeWorkgroupSizeY', 'maxComputeWorkgroupSizeZ', 'maxComputeWorkgroupsPerDimension'];
  const requiredLimits = {};
  for (const k of limitNames) if (adapter.limits[k] !== undefined) requiredLimits[k] = adapter.limits[k];
  device = await adapter.requestDevice({ requiredFeatures: features, requiredLimits });
  queue = device.queue;
  device.addEventListener('uncapturederror', e => note('uncaptured error: ' + e.error.message));
  device.lost.then(i => { lost = i.reason + ': ' + i.message; });
  const info = adapter.info || {};
  adapterInfo = {
    name: info.description || info.device || ((info.vendor || 'webgpu') + ' ' + (info.architecture || '')).trim(),
    vendor: info.vendor || '', architecture: info.architecture || '', features,
    software: !!(adapter.isFallbackAdapter || info.isFallbackAdapter || /swiftshader|llvmpipe|software/i.test((info.description || '') + (info.vendor || '') + (info.architecture || ''))),
    maxTexture2D: device.limits.maxTextureDimension2D, maxTexture3D: device.limits.maxTextureDimension3D, maxLayers: device.limits.maxTextureArrayLayers,
  };
  return JSON.stringify(adapterInfo);
}

export function takeMessages() {
  const out = messages.splice(0, messages.length);
  if (lost) { out.push('device lost: ' + lost); lost = null; }
  return out.join('\n');
}

export function configureCanvas(canvasId, width, height) {
  const canvas = document.getElementById(canvasId);
  if (!canvas) throw new Error('No canvas element with id "' + canvasId + '".');
  if (width > 0) canvas.width = width;
  if (height > 0) canvas.height = height;
  if (!canvas.__ctx) {
    const ctx = canvas.getContext('webgpu');
    ctx.configure({ device, format: 'bgra8unorm', viewFormats: ['bgra8unorm-srgb'], usage: 16 | 1, alphaMode: 'opaque' });
    canvas.__ctx = ctx;
  }
  return canvas.width + 'x' + canvas.height;
}

export function setCanvasSize(canvasId, width, height) {
  const canvas = document.getElementById(canvasId);
  canvas.width = width; canvas.height = height;
}

// ---- WGSL declaration scan: the types of the textures and samplers a shader declares ----
function scanDecls(wgsl) {
  const out = new Map();
  const re = /@group\((\d+)\)\s*@binding\((\d+)\)\s*var(?:<([^>]*)>)?\s+\w+\s*:\s*([^;]+);/g;
  let m;
  while ((m = re.exec(wgsl))) out.set(m[1] + ':' + m[2], { space: (m[3] || '').trim(), type: m[4].trim() });
  return out;
}

function textureEntry(d, fmtHint) {
  const t = d.type;
  let m;
  if ((m = /^texture_storage_(1d|2d|2d_array|3d)<(\w+),\s*(\w+)>/.exec(t))) {
    return { storageTexture: { access: m[3] === 'read' ? 'read-only' : m[3] === 'read_write' ? 'read-write' : 'write-only', format: m[2], viewDimension: m[1].replace('_', '-') } };
  }
  const dims = { '1d': '1d', '2d': '2d', '2d_array': '2d-array', 'cube': 'cube', 'cube_array': 'cube-array', '3d': '3d' };
  if ((m = /^texture_depth_(multisampled_)?(2d_array|2d|cube_array|cube)/.exec(t))) {
    return { texture: { sampleType: 'depth', viewDimension: dims[m[2]], multisampled: !!m[1] } };
  }
  if ((m = /^texture_multisampled_2d<(\w+)>/.exec(t))) {
    return { texture: { sampleType: m[1] === 'f32' ? 'unfilterable-float' : m[1] === 'u32' ? 'uint' : 'sint', viewDimension: '2d', multisampled: true } };
  }
  if ((m = /^texture_(2d_array|2d|cube_array|cube|3d|1d)<(\w+)>/.exec(t))) {
    const st = m[2] === 'f32' ? (fmtHint === 'unfilterable' ? 'unfilterable-float' : 'float') : m[2] === 'u32' ? 'uint' : 'sint';
    return { texture: { sampleType: st, viewDimension: dims[m[1]], multisampled: false } };
  }
  return null;
}

// A pipeline layout in WebGPU is explicit about each texture's dimension, but the engine's layouts are shared by shaders that bind
// different views at the same slot (a 2D panorama and a cube map) and by entry points that each use only some slots. So the WebGPU layout
// is made per set of shaders (a variant), and a bind group is made against the variant of the pipeline it is used with.
function variantFor(L, shaders) {
  const key = shaders.map(x => x.sid).join(',');
  let V = L.variants.get(key);
  if (V) return V;
  V = { L, pl: null, bgls: null, sets: [], pushGroup: -1, pushBinding: -1, pushBG: null, pushBuf: null };
  L.variants.set(key, V);
  const decls = new Map();
  for (const sh of shaders) for (const [k, v] of sh.decls) if (!decls.has(k)) decls.set(k, v);
  let maxSet = -1;
  for (const b of L.bindings) maxSet = Math.max(maxSet, b.set);
  const entries = [];
  for (let s = 0; s <= maxSet; s++) entries.push([]);
  for (const b of L.bindings) {
    const vis = (b.stages & 1 ? 1 : 0) | (b.stages & 2 ? 2 : 0) | (b.stages & 4 ? 4 : 0);
    const e = { binding: b.binding, visibility: vis };
    const d = decls.get(b.set + ':' + b.binding);
    switch (b.kind) {
      case 0: e.buffer = { type: 'uniform', hasDynamicOffset: !!b.dynamic }; break;
      case 1: e.buffer = { type: 'storage', hasDynamicOffset: !!b.dynamic }; break;
      case 2: e.buffer = { type: 'read-only-storage', hasDynamicOffset: !!b.dynamic }; break;
      case 3: case 5: {
        const te = d && textureEntry(d, b.unfilterable ? 'unfilterable' : '');
        if (!te) continue;   // a slot this pipeline's shaders do not declare is left out of its layout
        Object.assign(e, te);
        break;
      }
      case 4:
        // A sampler the pipeline's shaders do not declare is left out, as textures are: a layout entry of the wrong kind
        // (filtering for the shadow map's comparison sampler) makes every bind group of the set invalid.
        if (!d) continue;
        e.sampler = { type: /sampler_comparison/.test(d.type) ? 'comparison' : 'filtering' };
        break;
    }
    entries[b.set].push(e);
    while (V.sets.length <= b.set) V.sets.push(new Set());
    V.sets[b.set].add(b.binding);
  }
  if (L.pushSize > 0) {
    const sh = shaders.find(x => x.pushGroup >= 0);
    if (!sh) throw new Error('push constants were declared but no shader of the pipeline has a push-constant block (WgslPushGroup/WgslPushBinding)');
    while (entries.length <= sh.pushGroup) entries.push([]);
    const vis = (L.pushStages & 1 ? 1 : 0) | (L.pushStages & 2 ? 2 : 0) | (L.pushStages & 4 ? 4 : 0);
    entries[sh.pushGroup].push({ binding: sh.pushBinding, visibility: vis, buffer: { type: 'uniform', hasDynamicOffset: true, minBindingSize: (L.pushSize + 15) & ~15 } });
    V.pushGroup = sh.pushGroup; V.pushBinding = sh.pushBinding;
    while (V.sets.length <= sh.pushGroup) V.sets.push(new Set());
  }
  V.bgls = entries.map(es => device.createBindGroupLayout({ entries: es }));
  V.pl = device.createPipelineLayout({ bindGroupLayouts: V.bgls, ...lbl(L.label) });
  return V;
}

// ---- push constants: a ring of uniform slots; each change writes a slot and binds it with a dynamic offset ----
const PUSH_SLOT = 256;   // minUniformBufferOffsetAlignment is at most 256
let pushRing = null, pushOff = 0;
function pushSlot(data) {
  const need = (data.length + PUSH_SLOT - 1) & ~(PUSH_SLOT - 1);
  if (!pushRing || pushOff + need > pushRing.size) {
    // A full ring is replaced, not reused: slots written for the encoder being recorded must stay intact until it is submitted.
    pushRing = device.createBuffer({ size: 1 << 20, usage: 0x40 | 8, label: 'push constants' });
    pushOff = 0;
  }
  const off = pushOff;
  queue.writeBuffer(pushRing, off, data);
  pushOff += need;
  return off;
}
function pushBlock(V, data) {
  const L = V.L;
  const off = pushSlot(data);
  if (V.pushBuf !== pushRing) {
    V.pushBuf = pushRing;
    V.pushBG = device.createBindGroup({ layout: V.bgls[V.pushGroup], entries: [{ binding: V.pushBinding, resource: { buffer: pushRing, offset: 0, size: (L.pushSize + 15) & ~15 } }] });
  }
  (rp || cp).setBindGroup(V.pushGroup, V.pushBG, [off]);
}

function defaultViewDim(m) {
  if (m.dim === 2) return '3d';
  if (m.dim === 1) return m.layers > 6 ? 'cube-array' : 'cube';
  return m.layers > 1 ? '2d-array' : '2d';
}

function getView(id, spec) {
  const key = id + '|' + spec;
  let v = viewCache.get(key);
  if (v) return v;
  const t = O[id], m = M[id];
  const a = spec.split(',');
  const dim = a[0] || defaultViewDim(m);
  const baseMip = +a[1] || 0;
  const mipCount = +a[2] || (m.mips - baseMip);
  const baseLayer = +a[3] || 0;
  let layerCount = +a[4];
  if (!layerCount) layerCount = (m.dim === 2) ? 1 : m.layers - baseLayer;
  const desc = { dimension: dim, baseMipLevel: baseMip, mipLevelCount: mipCount, baseArrayLayer: baseLayer, arrayLayerCount: layerCount };
  if (m.viewFormat) desc.format = m.viewFormat;
  if (hasStencil(m.fmt)) desc.aspect = 'depth-only';
  v = t.createView(desc);
  viewCache.set(key, v);
  return v;
}
const VIEWDIM = ['', '2d', '2d-array', 'cube', 'cube-array', '3d'];
function attView(id, mip, layer) {
  const m = M[id];
  const fmt = m.viewFormat;
  const k = id + '|att|' + mip + '|' + layer;
  let v = viewCache.get(k);
  if (!v) {
    v = O[id].createView({ dimension: '2d', baseMipLevel: mip, mipLevelCount: 1, baseArrayLayer: layer, arrayLayerCount: 1, ...(fmt ? { format: fmt } : {}) });
    viewCache.set(k, v);
  }
  return v;
}

function dropViews(id) {
  const pre = id + '|';
  for (const k of [...viewCache.keys()]) if (k.startsWith(pre)) viewCache.delete(k);
}

function realizeBindGroup(g, V) {
  if (!V) { note('bind group used before a pipeline was set'); return null; }
  const bgl = V.bgls[g.set];
  let bg = g.bgs.get(bgl);
  if (bg) return bg;
  const present = V.sets[g.set];
  const entries = [];
  for (const e of g.entries) {
    if (present && !present.has(e.binding)) continue;   // a slot this pipeline's shaders do not declare
    if (e.kind === 0) entries.push({ binding: e.binding, resource: { buffer: O[e.id], offset: e.offset, ...(e.size ? { size: e.size } : {}) } });
    else if (e.kind === 1) {
      if (!O[e.id]) note('bind group "' + (g.label || '') + '" set ' + g.set + ' binding ' + e.binding + ' uses a texture that was destroyed or never created (handle ' + e.id + ')');
      entries.push({ binding: e.binding, resource: getView(e.id, e.view) });
    }
    else entries.push({ binding: e.binding, resource: O[e.id] });
  }
  bg = device.createBindGroup({ layout: bgl, entries, ...lbl(g.label) });
  g.bgs.set(bgl, bg);
  return bg;
}

function blitPipeline(format, vol) {
  const key = format + (vol ? '|3d' : '');
  let e = mipPipelines.get(key);
  if (e) return e;
  const texType = vol ? 'texture_3d<f32>' : 'texture_2d<f32>';
  const sampleExpr = vol ? 'textureSample(t, s, vec3f(v.uv, u.z))' : 'textureSample(t, s, v.uv)';
  const module = device.createShaderModule({
    code: `@group(0) @binding(0) var s: sampler;
@group(0) @binding(1) var t: ${texType};
@group(0) @binding(2) var<uniform> u: vec4f;
struct VO { @builtin(position) p: vec4f, @location(0) uv: vec2f };
@vertex fn vs(@builtin(vertex_index) i: u32) -> VO {
  var pos = array<vec2f, 3>(vec2f(-1, -1), vec2f(3, -1), vec2f(-1, 3));
  var o: VO; let q = pos[i]; o.p = vec4f(q, 0, 1); o.uv = vec2f((q.x + 1) * 0.5, (1 - q.y) * 0.5); return o;
}
@fragment fn fs(v: VO) -> @location(0) vec4f { return ${sampleExpr}; }` });
  const pipeline = device.createRenderPipeline({ layout: 'auto', vertex: { module, entryPoint: 'vs' }, fragment: { module, entryPoint: 'fs', targets: [{ format }] }, primitive: { topology: 'triangle-list' } });
  const sampler = device.createSampler({ minFilter: 'linear', magFilter: 'linear' });
  e = { pipeline, sampler };
  mipPipelines.set(key, e);
  return e;
}

function generateMips(enc, id) {
  const m = M[id], t = O[id];
  const vol = m.dim === 2;
  const { pipeline, sampler } = blitPipeline(m.fmt, vol);
  const layers = vol ? 1 : m.layers;
  for (let layer = 0; layer < layers; layer++) {
    for (let mip = 1; mip < m.mips; mip++) {
      const depth = vol ? Math.max(m.depth >> mip, 1) : 1;
      const srcDepth = vol ? Math.max(m.depth >> (mip - 1), 1) : 1;
      const src = t.createView({ dimension: vol ? '3d' : '2d', baseMipLevel: mip - 1, mipLevelCount: 1, baseArrayLayer: vol ? 0 : layer, arrayLayerCount: 1 });
      const dst = t.createView({ dimension: vol ? '3d' : '2d', baseMipLevel: mip, mipLevelCount: 1, baseArrayLayer: vol ? 0 : layer, arrayLayerCount: 1 });
      for (let z = 0; z < depth; z++) {
        const entries = [{ binding: 0, resource: sampler }, { binding: 1, resource: src }];
        if (vol) {
          const ub = device.createBuffer({ size: 16, usage: 0x40 | 0x8, mappedAtCreation: true });
          new Float32Array(ub.getMappedRange()).set([0, 0, (z + 0.5) / depth, 0]);
          ub.unmap();
          entries.push({ binding: 2, resource: { buffer: ub } });
          garbage.push(ub);
        }
        const bg = device.createBindGroup({ layout: pipeline.getBindGroupLayout(0), entries });
        const att = { view: dst, loadOp: 'clear', storeOp: 'store', clearValue: [0, 0, 0, 0] };
        if (vol) att.depthSlice = z;
        const pass = enc.beginRenderPass({ colorAttachments: [att] });
        pass.setPipeline(pipeline); pass.setBindGroup(0, bg); pass.draw(3); pass.end();
      }
    }
  }
}

// ---- the interpreter ----
let enc = null, rp = null, cp = null, curPipeline = null, curV = null;
const pipeLayout = new WeakMap();
const pending = new Map();   // set index -> the bind group last set in this pass
function applyBind(set, gid, dyn) {
  const bg = realizeBindGroup(O[gid], curV);
  if (bg) (rp || cp).setBindGroup(set, bg, dyn);
}
const garbage = [];   // temporary buffers of the current encoder, destroyed once it is submitted

export function exec(view) {
  const copy = view.slice();   // the managed buffer is only valid during this call
  u8 = copy; dv = new DataView(copy.buffer, copy.byteOffset, copy.byteLength); p = 0;
  try {
    while (p < copy.byteLength) run(u32());
  } catch (e) {
    note('js: ' + (e && e.stack ? e.stack : e));
  }
}

function run(op) {
  switch (op) {
    case 1: { // buffer
      const id = u32(), size = u64(), usage = u32(), label = str();
      let u = 0;
      if (usage & 1) u |= 0x20; if (usage & 2) u |= 0x10; if (usage & 4) u |= 0x40; if (usage & 8) u |= 0x80;
      if (usage & 16) u |= 4; if (usage & 32) u |= 8; if (usage & 64) u |= 0x100;
      u |= 4 | 8;
      O[id] = device.createBuffer({ size: Math.max(4, (size + 3) & ~3), usage: u, ...lbl(label) });
      break;
    }
    case 2: { // texture
      const id = u32(), w = u32(), h = u32(), dl = u32(), fmt = u32(), dim = u32(), mips = u32(), samples = u32(), usage = u32(), label = str();
      let u = 0;
      if (usage & 1) u |= 4; if (usage & 2) u |= 8; if (usage & 4) u |= 16;
      const f = FMT[fmt];
      if (samples === 1) {
        u |= 1 | 2;
        if (mips > 1 && !isDepth(f)) u |= 16 | 4;
      }
      O[id] = device.createTexture({ size: [w, h, dl], format: f, dimension: dim === 2 ? '3d' : '2d', mipLevelCount: mips, sampleCount: samples, usage: u, ...lbl(label) });
      M[id] = { w, h, layers: dim === 2 ? 1 : dl, depth: dim === 2 ? dl : 1, fmt: f, bpt: BPT[fmt], dim, mips, samples };
      break;
    }
    case 3: { // sampler
      const id = u32(), mag = u32(), min = u32(), mip = u32(), au = u32(), av = u32(), aw = u32(), cmp = u32(), lo = f32(), hi = f32(), aniso = f32();
      const d = { magFilter: mag ? 'linear' : 'nearest', minFilter: min ? 'linear' : 'nearest', mipmapFilter: mip ? 'linear' : 'nearest', addressModeU: ADDR[au], addressModeV: ADDR[av], addressModeW: ADDR[aw], lodMinClamp: lo, lodMaxClamp: hi };
      if (cmp !== 255) d.compare = CMP[cmp];
      if (aniso > 1 && mag && min && mip) d.maxAnisotropy = Math.min(16, Math.floor(aniso));
      O[id] = device.createSampler(d);
      break;
    }
    case 4: { // shader
      const id = u32(), stage = u32(), entry = str(), code = str(), label = str();
      const pg = u32(), pb = u32();
      const module = device.createShaderModule({ code, ...lbl(label) });
      O[id] = { sid: id, module, entry, stage, decls: scanDecls(code), pushGroup: pg - 1, pushBinding: pb - 1 };
      break;
    }
    case 5: { // pipeline layout
      const id = u32(), n = u32(), label = str();
      const pushSize = u32(), pushStages = u32();
      const bindings = [];
      for (let i = 0; i < n; i++) bindings.push({ set: u32(), binding: u32(), kind: u32(), stages: u32(), dynamic: u32(), unfilterable: u32() });
      O[id] = { bindings, label, variants: new Map(), pushSize, pushStages };
      break;
    }
    case 6: { // bind group (created lazily, when a pipeline's layout is known)
      const id = u32(), layout = u32(), set = u32(), n = u32(), label = str();
      const entries = [];
      for (let i = 0; i < n; i++) {
        const binding = u32(), kind = u32(), rid = u32(), offset = u64(), size = u64();
        let view = '';
        if (kind === 1) { const k = u32(), bm = u32(), mc = u32(), bl = u32(), lc = u32(); view = VIEWDIM[k] + ',' + bm + ',' + mc + ',' + bl + ',' + lc; }
        entries.push({ binding, kind, id: rid, offset, size, view });
      }
      O[id] = { L: O[layout], set, entries, bgs: new Map(), label };
      break;
    }
    case 7: graphicsPipeline(); break;
    case 8: {
      const id = u32(), layout = u32(), cs = u32(), label = str();
      const L = O[layout], S = O[cs];
      const V = variantFor(L, [S]);
      O[id] = device.createComputePipeline({ layout: V.pl, compute: { module: S.module, entryPoint: S.entry }, ...lbl(label) });
      pipeLayout.set(O[id], V);
      break;
    }
    case 9: { // destroy
      const id = u32(), kind = u32();
      const o = O[id];
      if (kind === 1 || kind === 2) { try { o.destroy(); } catch { } }
      if (kind === 2) dropViews(id);
      O[id] = undefined; M[id] = undefined;
      break;
    }
    case 10: {
      const id = u32(), off = u64(), data = bytes();
      // WebGPU writes whole 4-byte words; buffers are allocated rounded up, so a short tail is padded with zeros.
      if (data.length % 4) { const padded = new Uint8Array((data.length + 3) & ~3); padded.set(data); queue.writeBuffer(O[id], off, padded); } else queue.writeBuffer(O[id], off, data);
      break;
    }
    case 11: { // write texture
      const id = u32(), mip = u32(), layer = u32(), x = u32(), y = u32(), z = u32(), w = u32(), h = u32(), d = u32(), data = bytes();
      const m = M[id];
      const origin = m.dim === 2 ? [x, y, z] : [x, y, layer];
      queue.writeTexture({ texture: O[id], mipLevel: mip, origin }, data, { bytesPerRow: w * m.bpt, rowsPerImage: h }, [w, h, d]);
      break;
    }
    case 20: enc = device.createCommandEncoder(); pushOff = 0; break;
    case 21: { queue.submit([enc.finish()]); enc = null; for (const g of garbage.splice(0)) g.destroy(); break; }
    case 22: {
      curV = null; pending.clear();
      const n = u32();
      const colors = [];
      for (let i = 0; i < n; i++) {
        const t = u32(), mip = u32(), layer = u32(), res = u32(), load = u32(), store = u32(), r = f32(), g = f32(), b = f32(), a = f32();
        const c = { view: attView(t, mip, layer), loadOp: load === 1 ? 'clear' : 'load', storeOp: store === 1 ? 'discard' : 'store', clearValue: [r, g, b, a] };
        if (load === 2) c.loadOp = 'clear';
        if (res) c.resolveTarget = attView(res, 0, 0);
        colors.push(c);
      }
      const desc = { colorAttachments: colors };
      if (u32()) {
        const t = u32(), mip = u32(), layer = u32(), load = u32(), store = u32(), clear = f32();
        const m = M[t];
        const d = { view: attView(t, mip, layer), depthLoadOp: load === 1 || load === 2 ? 'clear' : 'load', depthStoreOp: store === 1 ? 'discard' : 'store', depthClearValue: clear };
        if (hasStencil(m.fmt)) { d.stencilLoadOp = 'clear'; d.stencilStoreOp = 'discard'; d.stencilClearValue = 0; }
        desc.depthStencilAttachment = d;
      }
      rp = enc.beginRenderPass(desc);
      break;
    }
    case 23: rp.end(); rp = null; break;
    case 24: cp = enc.beginComputePass(); curV = null; pending.clear(); break;
    case 25: cp.end(); cp = null; break;
    case 26: {
      const id = u32();
      curV = pipeLayout.get(O[id]) || null;
      (rp || cp).setPipeline(O[id]);
      // Bind groups may be set before the pipeline; each is made against the layout of the pipeline it ends up used with.
      // A group made for another layout is not carried over: the application binds again after changing to an incompatible pipeline.
      for (const [set, b] of pending) if (curV && O[b.gid].L === curV.L) applyBind(set, b.gid, b.dyn);
      break;
    }
    case 46: { const data = bytes(); pushBlock(curV, data); break; }
    case 27: {
      const set = u32(), gid = u32(), n = u32();
      const dyn = []; for (let i = 0; i < n; i++) dyn.push(u32());
      pending.set(set, { gid, dyn });
      if (curV) applyBind(set, gid, dyn);
      break;
    }
    case 28: { const slot = u32(), id = u32(), off = u64(); rp.setVertexBuffer(slot, O[id], off); break; }
    case 29: { const id = u32(), fmt = u32(), off = u64(); rp.setIndexBuffer(O[id], fmt ? 'uint32' : 'uint16', off); break; }
    case 30: { const x = f32(), y = f32(), w = f32(), h = f32(), a = f32(), b = f32(); rp.setViewport(x, y, w, h, a, b); break; }
    case 31: { const x = i32(), y = i32(), w = u32(), h = u32(); rp.setScissorRect(Math.max(0, x), Math.max(0, y), w, h); break; }
    case 32: { const vc = u32(), ic = u32(), fv = u32(), fi = u32(); rp.draw(vc, ic, fv, fi); break; }
    case 33: { const ic = u32(), inst = u32(), first = u32(), base = i32(), fi = u32(); rp.drawIndexed(ic, inst, first, base, fi); break; }
    case 34: { const id = u32(), off = u64(), n = u32(); for (let i = 0; i < n; i++) rp.drawIndirect(O[id], off + i * 16); break; }
    case 35: { const id = u32(), off = u64(), n = u32(); for (let i = 0; i < n; i++) rp.drawIndexedIndirect(O[id], off + i * 20); break; }
    case 36: { const x = u32(), y = u32(), z = u32(); cp.dispatchWorkgroups(x, y, z); break; }
    case 37: { const id = u32(), off = u64(); cp.dispatchWorkgroupsIndirect(O[id], off); break; }
    case 40: { const s = u32(), so = u64(), d = u32(), dof = u64(), size = u64(); enc.copyBufferToBuffer(O[s], so, O[d], dof, size); break; }
    case 41: { // texture -> buffer, tightly packed rows
      const t = u32(), mip = u32(), layer = u32(), b = u32(), off = u64();
      const m = M[t];
      const w = Math.max(m.w >> mip, 1), h = Math.max(m.h >> mip, 1), d = m.dim === 2 ? Math.max(m.depth >> mip, 1) : 1;
      const row = w * m.bpt;
      const origin = m.dim === 2 ? [0, 0, 0] : [0, 0, layer];
      if (row % 256 === 0 || h * d === 1) {
        enc.copyTextureToBuffer({ texture: O[t], mipLevel: mip, origin }, { buffer: O[b], offset: off, bytesPerRow: row, rowsPerImage: h }, [w, h, d]);
      } else {
        for (let z = 0; z < d; z++) for (let y = 0; y < h; y++)
          enc.copyTextureToBuffer({ texture: O[t], mipLevel: mip, origin: [0, y, m.dim === 2 ? z : layer] }, { buffer: O[b], offset: off + (z * h + y) * row }, [w, 1, 1]);
      }
      break;
    }
    case 42: {
      const b = u32(), off = u64(), t = u32(), mip = u32(), layer = u32();
      const m = M[t];
      const w = Math.max(m.w >> mip, 1), h = Math.max(m.h >> mip, 1), d = m.dim === 2 ? Math.max(m.depth >> mip, 1) : 1;
      const row = w * m.bpt;
      if (row % 256 === 0 || h * d === 1) {
        enc.copyBufferToTexture({ buffer: O[b], offset: off, bytesPerRow: row, rowsPerImage: h }, { texture: O[t], mipLevel: mip, origin: m.dim === 2 ? [0, 0, 0] : [0, 0, layer] }, [w, h, d]);
      } else {
        for (let z = 0; z < d; z++) for (let y = 0; y < h; y++)
          enc.copyBufferToTexture({ buffer: O[b], offset: off + (z * h + y) * row }, { texture: O[t], mipLevel: mip, origin: [0, y, m.dim === 2 ? z : layer] }, [w, 1, 1]);
      }
      break;
    }
    case 43: {
      const s = u32(), d = u32();
      const m = M[s];
      for (let mip = 0; mip < m.mips; mip++) {
        const w = Math.max(m.w >> mip, 1), h = Math.max(m.h >> mip, 1);
        const depthOrLayers = m.dim === 2 ? Math.max(m.depth >> mip, 1) : m.layers;
        enc.copyTextureToTexture({ texture: O[s], mipLevel: mip }, { texture: O[d], mipLevel: mip }, [w, h, depthOrLayers]);
      }
      break;
    }
    case 44: generateMips(enc, u32()); break;
    case 45: { // backbuffer
      const id = u32(), canvasId = str(), srgb = u32();
      const canvas = document.getElementById(canvasId);
      O[id] = canvas.__ctx.getCurrentTexture();
      dropViews(id);
      M[id] = { w: canvas.width, h: canvas.height, layers: 1, depth: 1, fmt: 'bgra8unorm', viewFormat: srgb ? 'bgra8unorm-srgb' : 'bgra8unorm', bpt: 4, dim: 0, mips: 1, samples: 1 };
      break;
    }
    default: throw new Error('unknown op ' + op);
  }
}

function graphicsPipeline() {
  const id = u32(), layoutId = u32(), vsId = u32(), psId = u32();
  const nvb = u32();
  const buffers = [];
  for (let i = 0; i < nvb; i++) {
    const stride = u32(), step = u32(), na = u32();
    const attributes = [];
    for (let a = 0; a < na; a++) attributes.push({ shaderLocation: u32(), format: VFMT[u32()], offset: u32() });
    buffers.push({ arrayStride: stride, stepMode: step ? 'instance' : 'vertex', attributes });
  }
  const topology = TOPO[u32()], cull = CULL[u32()], front = u32() ? 'cw' : 'ccw';
  const nt = u32();
  const targets = [];
  for (let i = 0; i < nt; i++) {
    const fmt = FMT[u32()], hasBlend = u32();
    const t = { format: fmt };
    if (hasBlend) {
      const c = { srcFactor: BF[u32()], dstFactor: BF[u32()], operation: BO[u32()] };
      const a = { srcFactor: BF[u32()], dstFactor: BF[u32()], operation: BO[u32()] };
      t.blend = { color: c, alpha: a };
    }
    t.writeMask = u32();
    targets.push(t);
  }
  let depthStencil;
  if (u32()) {
    const format = FMT[u32()], write = u32(), cmp = CMP[u32()], bias = i32(), slope = f32(), clamp = f32();
    depthStencil = { format, depthWriteEnabled: !!write, depthCompare: cmp, depthBias: bias, depthBiasSlopeScale: slope, depthBiasClamp: clamp };
  }
  const samples = u32(), depthClamp = u32(), label = str();
  const L = O[layoutId], VS = O[vsId], PS = psId ? O[psId] : null;
  const V = variantFor(L, PS ? [VS, PS] : [VS]);
  const desc = {
    layout: V.pl,
    vertex: { module: VS.module, entryPoint: VS.entry, buffers },
    primitive: { topology, cullMode: cull, frontFace: front, ...(depthClamp ? { unclippedDepth: true } : {}) },
    multisample: { count: samples },
    ...lbl(label),
  };
  if (PS) desc.fragment = { module: PS.module, entryPoint: PS.entry, targets };
  if (depthStencil) desc.depthStencil = depthStencil;
  O[id] = device.createRenderPipeline(desc);
  pipeLayout.set(O[id], V);
}

// ---- reads ----
export async function readTexture(id, mip, layer, w, h, d, bpt) {
  const m = M[id];
  const row = w * bpt, padded = (row + 255) & ~255;
  const buf = device.createBuffer({ size: padded * h * d, usage: 8 | 1 });
  const e = device.createCommandEncoder();
  e.copyTextureToBuffer({ texture: O[id], mipLevel: mip, origin: m.dim === 2 ? [0, 0, 0] : [0, 0, layer] }, { buffer: buf, bytesPerRow: padded, rowsPerImage: h }, [w, h, d]);
  queue.submit([e.finish()]);
  await buf.mapAsync(1);
  const src = new Uint8Array(buf.getMappedRange());
  const out = new Uint8Array(row * h * d);
  for (let r = 0; r < h * d; r++) out.set(src.subarray(r * padded, r * padded + row), r * row);
  buf.unmap(); buf.destroy();
  return stash(out);
}

const reads = new Map();
let ticketCounter = 0;
function stash(bytes) { const t = ++ticketCounter; reads.set(t, bytes); return t; }
export function takeRead(ticket, dest) { dest.set(reads.get(ticket)); reads.delete(ticket); }

export async function readBuffer(id, offset, size) {
  const padded = (size + 3) & ~3;
  const buf = device.createBuffer({ size: Math.max(4, padded), usage: 8 | 1 });
  const e = device.createCommandEncoder();
  e.copyBufferToBuffer(O[id], offset, buf, 0, Math.max(4, padded));
  queue.submit([e.finish()]);
  await buf.mapAsync(1);
  const out = new Uint8Array(buf.getMappedRange()).slice(0, size);
  buf.unmap(); buf.destroy();
  return stash(out);
}

export function whenIdle() { return queue.onSubmittedWorkDone(); }
