"""Black-box peer for the WebSocket interop tests: a third-party implementation of RFC 6455.

  server PORT_FILE   listens on a free port, writes the port to PORT_FILE, echoes every binary message
  client PORT        connects, sends messages of many sizes, checks each echo, pings, closes with 1000
"""
import asyncio
import sys

from websockets.asyncio.client import connect
from websockets.asyncio.server import serve

SIZES = [0, 1, 125, 126, 127, 65535, 65536, 300000, 1000000]


def pattern(n, seed):
    x = (seed * 2654435761) & 0xFFFFFFFF
    out = bytearray(n)
    for i in range(n):
        x = (x * 1664525 + 1013904223) & 0xFFFFFFFF
        out[i] = x >> 24
    return bytes(out)


async def run_server(port_file):
    async def echo(ws):
        async for message in ws:
            await ws.send(message)

    async with serve(echo, "127.0.0.1", 0, max_size=None) as server:
        port = server.sockets[0].getsockname()[1]
        with open(port_file, "w") as f:
            f.write(str(port))
        await server.serve_forever()


async def run_client(port):
    async with connect(f"ws://127.0.0.1:{port}/", max_size=None) as ws:
        for i, n in enumerate(SIZES):
            data = pattern(n, i)
            await ws.send(data)
            echoed = await ws.recv()
            assert isinstance(echoed, bytes), "echo was not binary"
            assert echoed == data, f"echo of {n} bytes differs"
        pong = await ws.ping(b"hello")
        await asyncio.wait_for(pong, 5)
        await ws.close(1000, "done")
    print("OK")


if __name__ == "__main__":
    mode = sys.argv[1]
    if mode == "server":
        asyncio.run(run_server(sys.argv[2]))
    else:
        asyncio.run(run_client(int(sys.argv[2])))
