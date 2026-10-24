#!/usr/bin/env python3
"""Regenerates testdata/ui-layout/expected.json from cases.html, using headless Chrome as a black-box layout oracle.

Every element in a case is a flex container with border-box sizing, as in the UI engine, and every element is positioned so
absolutely positioned children use their parent as the containing block. Boxes are reported relative to the case root.
Usage: generate.py [path-to-chrome]
"""
import html as htmllib
import json
import pathlib
import re
import subprocess
import sys
import tempfile

root = pathlib.Path(__file__).resolve().parents[2] / "testdata" / "ui-layout"
chrome = sys.argv[1] if len(sys.argv) > 1 else "google-chrome"
cases = []
for line in (root / "cases.html").read_text().splitlines():
    m = re.match(r"## (\S+) (\d+)x(\d+)$", line)
    if m:
        cases.append([m.group(1), int(m.group(2)), int(m.group(3)), []])
    elif line.strip():
        cases[-1][3].append(line)

page = ["""<!doctype html><html><head><style>
html,body{margin:0;display:block}
.case,.case *{box-sizing:border-box;display:flex;position:relative;margin:0;padding:0;border:0 solid}
.case [style*="display:none"]{display:none}
</style></head><body>"""]
for name, w, h, body in cases:
    page.append(f'<div class="case" data-name="{name}" style="width:{w}px;height:{h}px;flex-direction:column;overflow:visible">')
    page.append("\n".join(body))
    page.append("</div>")
page.append("""<script>
const out = {};
for (const c of document.querySelectorAll('.case')) {
  const base = c.getBoundingClientRect();
  const m = {};
  for (const e of c.querySelectorAll('[id]')) {
    const r = e.getBoundingClientRect();
    m[e.id] = [r.left - base.left, r.top - base.top, r.width, r.height];
  }
  out[c.dataset.name] = m;
}
const pre = document.createElement('pre'); pre.id = 'out'; pre.textContent = JSON.stringify(out);
document.body.appendChild(pre);
</script></body></html>""")

with tempfile.TemporaryDirectory() as d:
    f = pathlib.Path(d) / "cases.html"
    f.write_text("\n".join(page))
    r = subprocess.run([chrome, "--headless=new", "--no-sandbox", "--disable-gpu", "--hide-scrollbars", f"--user-data-dir={d}/profile",
                        "--dump-dom", f.as_uri()], capture_output=True, text=True, timeout=180)
m = re.search(r'<pre id="out">(.*?)</pre>', r.stdout, re.S)
if not m:
    sys.exit("no output from chrome: " + r.stderr[-500:])
data = json.loads(htmllib.unescape(m.group(1)))
rounded = {k: {i: [round(x, 4) for x in v] for i, v in sorted(b.items())} for k, b in data.items()}
(root / "expected.json").write_text(json.dumps(rounded, indent=1, sort_keys=True) + "\n")
print(f"{len(rounded)} cases, {sum(len(v) for v in rounded.values())} boxes")
