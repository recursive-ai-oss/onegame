"""A small MCP client for a game's live-control socket (tcp:HOST:PORT, JSON lines), used by the device checks."""
import base64, json, socket


class Mcp:
    def __init__(self, hostport, timeout=60):
        host, port = hostport.replace('tcp:', '').rsplit(':', 1)
        host = host.strip('[]')   # an IPv6 literal may come bracketed
        self.sock = socket.create_connection((host, int(port)), timeout=timeout)
        self.f = self.sock.makefile('rw', encoding='utf-8', newline='\n')
        self.n = 0
        self.rpc('initialize', {'protocolVersion': '2025-06-18', 'capabilities': {}, 'clientInfo': {'name': 'device-check', 'version': '1'}})

    def rpc(self, method, params):
        self.n += 1
        self.f.write(json.dumps({'jsonrpc': '2.0', 'id': self.n, 'method': method, 'params': params}) + '\n')
        self.f.flush()
        line = self.f.readline()
        if not line:
            raise ConnectionError('the game closed the live-control socket')
        return json.loads(line)

    def call2(self, tool, **args):
        """Calls an operation; returns (its JSON result, the list of PNG byte strings it returned)."""
        r = self.rpc('tools/call', {'name': tool, 'arguments': args})
        if 'error' in r:
            raise RuntimeError(r['error'])
        res = r['result']
        text = next((c['text'] for c in res['content'] if c.get('type') == 'text'), '')
        if res.get('isError'):
            raise RuntimeError(text)
        pngs = [base64.b64decode(c['data']) for c in res['content'] if c.get('type') == 'image']
        try:
            return json.loads(text), pngs
        except ValueError:
            return text, pngs

    def call(self, tool, **args):
        return self.call2(tool, **args)[0]

    def close(self):
        self.sock.close()
