#!/usr/bin/env python3
"""Compares the per-scenario state hashes of two scenario-run JSON outputs (desktop `onegame test --json`, `onegame android test --json`,
`onegame ios test --json`, or a run on another host) and prints one JSON line.

  compare-hashes.py REFERENCE.json OTHER.json

`hashEqual` counts scenarios with the same hash; `failed` lists scenarios that did not pass (usually a capture past its tolerance,
which `captureNotes` quotes); `different` lists scenarios whose hash differs. Exit 0 when every scenario is hash-equal and passed."""
import json, sys


def scenarios(path):
    text = open(path, errors='replace').read()
    dec = json.JSONDecoder()
    i = text.find('{"ok"')
    if i < 0:
        raise SystemExit(f'{path}: no JSON result found')
    doc, _ = dec.raw_decode(text[i:])

    def find(o):
        if isinstance(o, dict):
            if isinstance(o.get('scenarios'), list):
                return o['scenarios']
            for v in o.values():
                r = find(v)
                if r is not None:
                    return r
        return None

    found = find(doc)
    if found is None:
        raise SystemExit(f'{path}: no scenarios in the result')
    return {s['name']: s for s in found}


ref, other = scenarios(sys.argv[1]), scenarios(sys.argv[2])
equal, different, failed, notes = 0, [], [], {}
for name, r in ref.items():
    o = other.get(name)
    if o is None:
        different.append(name + ' (missing)')
        continue
    if o.get('hash') == r.get('hash'):
        equal += 1
    else:
        different.append(name)
    if not o.get('passed'):
        failed.append(name)
        notes[name] = [e.get('actual') or e.get('message') for e in o.get('expectations', []) if not e.get('passed', True)][:3] or o.get('failure')
print(json.dumps({'scenarios': len(ref), 'passed': sum(1 for s in other.values() if s.get('passed')), 'hashEqual': equal, 'different': different, 'failed': failed, 'captureNotes': notes}))
sys.exit(0 if equal == len(ref) and not failed else 1)
