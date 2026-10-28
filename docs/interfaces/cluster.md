# Dedicated servers and process clusters

Source: `src/OneGame.Server` (listening, remote clients), `src/OneGame.Server.Live` (`ServerApp`, server control), `src/OneGame.Host` (`RemoteSession`, `--connect`), `src/OneGame.Testing` (`ProcessCluster`, `ClusterSpec`, cluster scenarios), `src/OneGame.Cli` (`cluster`, `export --server`, `mcp --cluster`). Builds on `transport.md` (`INetListener`, `INetConnection`), `server-host.md` and `control.md`.

## Roles

A **server** process is a `ServerHost` that listens; a **client** process is a game (`GameApp`) whose world is a replica of a server's scope. Both are ordinary onegame processes with their own live control socket (MCP, JSON lines). A game's server is its own small project (`onegame new --server`, built on `build/Server.targets`) that compiles the game's sim code and calls `ServerApp.Run(setup, args)`; `onegame-server` is the same entry with the plugin loader and no game types.

## Server

Config keys added to the server YAML (all optional):

```yaml
listen:                       # port 0 picks a free port; the bound ports are printed and listed by server_info
  - { transport: udp, port: 7777, address: 0.0.0.0 }
  - { transport: ws,  port: 7778 }
trust:                        # what every client may do; default: read and command every scope
  read: [1]
  command: [1]
hashHistory: 256              # ticks of hashes kept (the scope, and what each client holds) for cross-node comparison; default 0 = off, the cluster harness turns it on
```

`ServerHost` accepts connections (`Node.Accept`) inside `StepOnce` and `PumpNetwork()` (the runner calls it between ticks). Each accepted connection gets the `Trust` returned by `ServerHost.Authenticate` (`Func<ClientRequest, Trust?>`; null refuses and closes it; the default applies the config's `trust`). The transport authenticates the client (see `transport-security.md`): `ClientRequest.Key` is the public key the client proved (null only on an insecure development link), alongside the remote address. A closed connection is detached from the node (`Node.Detach`) after `ServerHost.ClientLeft` is raised for it. `ServerHost.Clients()` lists them (name, transport, address, state, RTT, loss, retransmits).

`ServerApp.Run(setup, args)`: `--config f.yaml` and/or `--name N --scope ID[:SEED] --listen udp:PORT|ws:PORT --tick-rate N --hash-history N`, `--live [path]`, `--manual` (tick only on `step`), `--ticks N`, `--parent PID` (exit when that process is gone), `--key FILE` (repeatable), `--game-id TEXT` and `--insecure` (the server's security; a listening server needs a key or a development run, see `transport-security.md` section 9). One JSON line goes to stdout when listening: `{"ready":true,"pid":123,"live":"/path.sock","listen":[{"transport":"udp","port":41000}]}`; `{"stopped":true,...}` follows at the end.

Control ops added to the server surface: `clients`, `hash_at {tick}` (the scope hash at a recorded tick, or null, and the view hash of each connected client at that tick: what its replica must hold), `state_query` accepts `entity` as `Type`, `Type#n` or an id and `property`.

## Client

`GameApp` options: `--connect udp://host:port|ws://host:port`, `--scope N` (default 1), `--name N`, `--net latency=100,jitter=10,loss=0.02,duplicate=0.01,reorder=0.05,seed=7` (the network conditioner on this client's outgoing traffic, so the server sees the latency and loss; UDP is conditioned per datagram, WebSocket per message), `--parent PID`, and the link's security: `--server-key <public key or .pub file>[,...]`, `--identity <file>` (the player's key, made if missing), `--game-id`, `--insecure` (development only). `onegame run --connect` fills `--server-key` and `--identity` from the per-user development keys when it is given none. `GameOptions.Connect` is the API form (`ConnectOptions.Security`). The game's world is then the **replica** (`GameInstance.World.Role == Replica`): `Step` pumps the connection instead of running a tick, commands (from views, `command_send`, scenarios) are forwarded to the server, and state writes (`state_patch`, `reset`, fixtures, snapshots) fail with `replica`. If the connection closes (the server died, restarted or timed out) the client keeps trying to connect again for `ReconnectWindowMs` (60 s) and resubscribes to every scope it followed, rebuilding their replicas from fresh snapshots; to the game that is the same server with a new connection, and with the same identity key it is the same player (Lanterns maps the key to its player id and saves the table). `IsClosed` is true only for a refusal, a version mismatch, a security failure (`SecurityRequired`, `HandshakeFailed`), a kick or an expired window. Each frame the network is pumped; so is every `DrainInput`, which keeps a paused live client connected.

Client control ops: `net_status` (state, `reconnects`, `reconnecting`, address, transport, replica state, tick, hash, resyncs, drift reports with node/scope/tick/differences, transport stats), `net_wait {tick|serverTick, timeoutMs}` (pump until the replica reaches a tick), `net_diverge {entity?}` (corrupts replica state locally, to prove drift detection; development builds only like all live ops).

## Process clusters (`OneGame.Testing`)

`ClusterSpec` (YAML or code) names nodes:

```yaml
name: demo
nodes:
  - { name: master, role: server, exe: bin/Server, listen: udp+ws, scopes: [{ id: 1, seed: 7 }] }
  - { name: p1, role: client, exe: bin/Game, connect: master, transport: udp, net: { latencyMs: 100, lossRate: 0.02 } }
```

`ProcessCluster.Up(spec)` starts each node as its own OS process (servers first; clients when their `connect` target is ready), output to `<dir>/<node>.log`, and records `<dir>/cluster.json` (name, pid, live socket, endpoints, log per node). Nodes watch the starting process (`--parent`), so a crashed test leaves nothing behind within about a second; `Down()` stops them by PID (a live `shutdown`/`quit` first, then a kill) and verifies none is alive. `Call(node, op, args)` runs any control op on a node. `Hashes()` returns every node's (scope, role, tick, hash) and checks each client replica's view hash against its server's recorded view hashes **at the replica's tick**, plus each node's drift reports. `ClusterHashReport.Ok` is false on any mismatch or drift; mismatches name node, scope and tick. `TestCluster.Launch(spec)` is the test entry.

### Nodes on other machines

Servers bind the loopback address, so clients on the same computer reach them. For a client on a phone or another computer, start the cluster with `ONEGAME_CLUSTER_BIND=0.0.0.0` (or one interface's address): every server then listens there, on the same ports, and a client node whose `exe` is a script (`samples/lanterns/cluster/android-client.sh`, `ios-device-client.sh`, `ssh-client.sh`) rewrites `127.0.0.1` to this computer's network address before it starts the game on the device. The links are encrypted, but the development keys are on this computer only: a phone is given the public key as a flag (the cluster passes `--server-key`).

### Killing and starting nodes

`Kill(node)` ends a node's process with SIGKILL (nothing saved, nothing announced), `Start(node)` runs it again with the same arguments, data folder and ports and waits until it is up, `Restart(node)` does both. Servers load what they saved; a restarted client is a new connection and, to the game, a new player. Only the process that started the cluster can do this (`not_owner` otherwise). A node that is down stays in `cluster.json` with its old pid until it starts again.

### Release clients

An exported release build has no live control and takes no `--parent`. A client node with `release: true` (and `args: [--until-tick, N]`, required) runs `--headless --realtime --json`, plays until its replica reaches tick N or its connection closes, and prints one JSON line with its tick, `viewHash`, `scope`, connection and replica state, drift count and the followed scopes. The cluster reads that line from the node's log once the process has exited: `wait: { node, tick }` waits for the exit, `Hashes()` and `agree` compare the printed view hash with the server's recorded hash at that tick (so check within the server's `hashHistory`, 512 ticks), and `Call` on such a node is `no_live`. Game options for headless clients: `--realtime` (follow the clock; without it `--headless --frames N` runs its frames as fast as it can) and `--until-tick N` (a connected client only).

`Hashes()` also checks the scopes a client follows besides its main one (`net_status.followed`, served by its server's relay): each must be live and within 240 ticks of the tick its authority is at, so a relay that stopped passing frames on is reported as `(followed)`; `agree` fails on it.

A windowed client started on a machine that has `WAYLAND_DISPLAY` set opens on that compositor, not on `DISPLAY`: under Xvfb pass `--backend x11 --display :N` (or unset `WAYLAND_DISPLAY`). A window the compositor never presents leaves the swapchain acquire waiting, which looks like a client idle at 0% CPU.

## Cluster scenarios

Scenario steps and expectations gain an optional `node:` key beside the verb; `onegame test` (single process) refuses them. New verbs for clusters:

```yaml
steps:
  - command: { entity: Arena, name: Hit, args: [3] }
    node: p1
  - wait: { node: p1, tick: 120, timeout: 20000 }
  - step: 30
    node: master          # a server runs N ticks (manual servers only); a client pumps N frames
  - kill: master          # SIGKILL the process
  - start: master         # again, same args, data and ports (restart: master does both)
expect:
  - state: { entity: Arena, property: Hits, min: 1 }
    node: p1
  - agree: { server: master, clients: [p1, p2] }   # replica hash equals the server's at the replica's tick
  - drift: { nodes: [p1, p2], equals: 0 }
```

Without `node:` a `step` goes to every node, `state`/`tick`/`hash` expectations are an error. Failures report `{kind, line, node, message, expected, actual}`.

## CLI

`onegame cluster up --spec f.yaml [--dir d]` (starts, prints the state JSON, leaves nodes running), `status`, `hashes` (exit 1 on mismatch or drift), `down`, `kill <node>...` (SIGKILL the named nodes of a running cluster, including one started by another process; JSON `{ok, result:{nodes:[{name,pid,alive}]}}`; starting one again needs the spec and stays with the process that started the cluster), `call <node> <op> [json]`, `test <scenario.yaml> --spec f.yaml` (up, run, down). The state file is found by `--dir` or `--cluster file`, else `./.onegame-cluster/cluster.json`. `onegame mcp --cluster file` is one MCP server for all nodes: every tool takes an extra `node` argument. `onegame export --server [--plugins]` publishes the server project NativeAOT (or `onegame-server` framework-dependent with `--plugins`) with the export report of `exporting.md`.

## Servers that talk to each other

Every server of a cluster is given its port before any node starts. A server's `args` may use `{port:NAME}` (the port server NAME listens on, udp and ws alike), `{key:NAME}` (the public key file of server NAME) and `{dir}` (the cluster folder), so a server that starts first can be told where a later one will listen. The game's server code opens its own outgoing connections from the `ServerSetup` hooks `Started`, `BeforeTick`, `AfterTick` and `Pump`, and reads `--option KEY=VALUE` settings from `ServerConfig.Options`. A client may follow more scopes than one over its connection: `--also-scopes 2,3` (`ConnectOptions.AlsoScopes`, `GameInstance.Replicas`), and `GameDefinition.OnFrame` runs each frame after the network was pumped.

## Limits

One server connection per client process. A snapshot or frame over the transport's `MaxMessageBytes` is split by the sync layer (`sync.md`, "Links that go away"), up to 32 MiB; a bundle in an entity transfer is still capped at 48 KiB. A client that reconnects is a new connection with the same identity key, so the same player. Every node gets a development key in `<cluster folder>/keys` (made on first use, deleted with the cluster) and `ONEGAME_DEV=1`. Cluster launching uses `/bin/sh` and Unix sockets (Linux, macOS).
