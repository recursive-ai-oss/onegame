#!/usr/bin/env python3
"""Drives a running development build over its live port (one JSON-RPC message per line) and checks the common operations.

  live-probe.py <port> [--click <selector>] [--png <file>]

Runs: tools/list, info, state_query, an input_nav action, ui_click (when a selector is given or a button is found) and capture_frame,
and requires the capture to be a PNG. Prints one line per step; exits 1 on the first failure.
"""
import base64
import json
import socket
import sys
import time

port = int(sys.argv[1])
args = sys.argv[2:]
click = args[args.index("--click") + 1] if "--click" in args else None
png_out = args[args.index("--png") + 1] if "--png" in args else None

sock = None
for _ in range(200):
    try:
        sock = socket.create_connection(("127.0.0.1", port), timeout=60)
        break
    except OSError:
        time.sleep(0.25)
if sock is None:
    print("could not connect to the live port", port)
    sys.exit(1)
reader = sock.makefile("rb")
next_id = 0


def rpc(method, params=None):
    global next_id
    next_id += 1
    sock.sendall((json.dumps({"jsonrpc": "2.0", "id": next_id, "method": method, "params": params or {}}) + "\n").encode())
    while True:
        line = reader.readline()
        if not line:
            print("the game closed the connection during", method)
            sys.exit(1)
        message = json.loads(line)
        if message.get("id") == next_id:
            return message


def call(name, arguments=None):
    reply = rpc("tools/call", {"name": name, "arguments": arguments or {}})
    if "error" in reply:
        print(f"FAIL {name}: {reply['error']}")
        sys.exit(1)
    result = reply["result"]
    if result.get("isError"):
        print(f"FAIL {name}: {json.dumps(result)[:300]}")
        sys.exit(1)
    return result


def text(result):
    return "".join(c.get("text", "") for c in result["content"] if c["type"] == "text")


rpc("initialize", {"protocolVersion": "2025-06-18", "capabilities": {}, "clientInfo": {"name": "live-probe", "version": "1"}})
tools = [t["name"] for t in rpc("tools/list")["result"]["tools"]]
print("tools:", len(tools))
for needed in ("state_query", "ui_click", "capture_frame", "input_nav", "step"):
    if needed not in tools:
        print("missing operation", needed)
        sys.exit(1)

info = json.loads(text(call("info")))
print("info: tick", info.get("tick"), "hash", info.get("hash"))
print("state_query:", text(call("state_query"))[:120].replace("\n", " "))
print("input_nav:", text(call("input_nav", {"action": "next"}))[:100].replace("\n", " "))
call("step", {"frames": 2})

# A game's UI differs: click the given selector, or else the first of a few common ones that exists (the template shows a label).
clicked = False
for selector in ([click] if click else ["Button", "Label", "Text", "Panel"]):
    reply = rpc("tools/call", {"name": "ui_click", "arguments": {"selector": selector}})
    if "error" not in reply and not reply["result"].get("isError"):
        print("ui_click", selector + ":", text(reply["result"])[:100].replace("\n", " "))
        clicked = True
        break
if not clicked:
    print("FAIL ui_click found nothing to click")
    sys.exit(1)

# camera belongs to 3D games; a 2D game answers with a typed error, which is the expected answer here.
if "camera" in tools:
    reply = rpc("tools/call", {"name": "camera", "arguments": {}})
    print("camera:", text(reply["result"])[:90].replace("\n", " "))

capture = call("capture_frame", {"size": "320x180"})
images = [c for c in capture["content"] if c["type"] == "image"]
if not images:
    print("FAIL capture_frame returned no image")
    sys.exit(1)
data = base64.b64decode(images[0]["data"])
if data[:8] != b"\x89PNG\r\n\x1a\n":
    print("FAIL capture_frame is not a PNG")
    sys.exit(1)
print("capture_frame: PNG", len(data), "bytes")
if png_out:
    with open(png_out, "wb") as f:
        f.write(data)
print("live probe ok")
