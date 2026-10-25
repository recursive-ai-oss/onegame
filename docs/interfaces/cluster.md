# Dedicated servers and process clusters

Header: v1. Source: `src/OneGame.Server` (listening, remote clients), `src/OneGame.Server.Live` (`ServerApp`, server control), `src/OneGame.Host` (`RemoteSession`, `--connect`), `src/OneGame.Testing` (`ProcessCluster`, `ClusterSpec`, cluster scenarios), `src/OneGame.Cli` (`cluster`, `export --server`, `mcp --cluster`). Builds on `transport.md` (`INetListener`, `INetConnection`), `server-host.md` and `control.md`.

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

`ServerHost` accepts connections (`Node.Accept`) inside `StepOnce` and `PumpNetwork()` (the runner calls it between ticks). Each accepted connection gets the `Trust` returned by `ServerHost.Authenticate` (`Func<ClientRequest, Trust?>`; null refuses and closes it; the default applies the config's `trust`). The transport is unauthenticated today (see `transport.md`), so the hook sees only the remote address. A closed connection is unsubscribed from every scope. `ServerHost.Clients()` lists them (name, transport, address, state, RTT, loss, retransmits).

`ServerApp.Run(setup, args)`: `--config f.yaml` and/or `--name N --scope ID[:SEED] --listen udp:PORT|ws:PORT --tick-rate N --hash-history N`, `--live [path]`, `--manual` (tick only on `step`), `--ticks N`, `--parent PID` (exit when that process is gone). One JSON line goes to stdout when listening: `{"ready":true,"pid":123,"live":"/path.sock","listen":[{"transport":"udp","port":41000}]}`; `{"stopped":true,...}` follows at the end.

Control ops added to the server surface: `clients`, `hash_at {tick}` (the scope hash at a recorded tick, or null, and the view hash of each connected client at that tick: what its replica must hold), `state_query` accepts `entity` as `Type`, `Type#n` or an id and `property`.

## Client

`GameApp` options: `--connect udp://host:port|ws://host:port`, `--scope N` (default 1), `--name N`, `--net latency=100,jitter=10,loss=0.02,duplicate=0.01,reorder=0.05,seed=7` (the network conditioner on this client's outgoing traffic, so the server sees the latency and loss; UDP is conditioned per datagram, WebSocket per message), `--parent PID`. `GameOptions.Connect` is the API form. The game's world is then the **replica** (`GameInstance.World.Role == Replica`): `Step` pumps the connection instead of running a tick, commands (from views, `command_send`, scenarios) are forwarded to the server, and state writes (`state_patch`, `reset`, fixtures, snapshots) fail with `replica`. Each frame the network is pumped; so is every `DrainInput`, which keeps a paused live client connected.

Client control ops: `net_status` (state, address, transport, replica state, tick, hash, resyncs, drift reports with node/scope/tick/differences, transport stats), `net_wait {tick|serverTick, timeoutMs}` (pump until the replica reaches a tick), `net_diverge {entity?}` (corrupts replica state locally, to prove drift detection; development builds only like all live ops).

## Process clusters (`OneGame.Testing`)

`ClusterSpec` (YAML or code) names nodes:

```yaml
name: demo
nodes:
  - { name: master, role: server, exe: bin/Server, listen: udp+ws, scopes: [{ id: 1, seed: 7 }] }
  - { name: p1, role: client, exe: bin/Game, connect: master, transport: udp, net: { latencyMs: 100, lossRate: 0.02 } }
```

`ProcessCluster.Up(spec)` starts each node as its own OS process (servers first; clients when their `connect` target is ready), output to `<dir>/<node>.log`, and records `<dir>/cluster.json` (name, pid, live socket, endpoints, log per node). Nodes watch the starting process (`--parent`), so a crashed test leaves nothing behind within about a second; `Down()` stops them by PID (a live `shutdown`/`quit` first, then a kill) and verifies none is alive. `Call(node, op, args)` runs any control op on a node. `Hashes()` returns every node's (scope, role, tick, hash) and checks each client replica's view hash against its server's recorded view hashes **at the replica's tick**, plus each node's drift reports. `ClusterHashReport.Ok` is false on any mismatch or drift; mismatches name node, scope and tick. `TestCluster.Launch(spec)` is the test entry.

## Cluster scenarios

Scenario steps and expectations gain an optional `node:` key beside the verb; `onegame test` (single process) refuses them. New verbs for clusters:

```yaml
steps:
  - command: { entity: Arena, name: Hit, args: [3] }
    node: p1
  - wait: { node: p1, tick: 120, timeout: 20000 }
  - step: 30
    node: master          # a server runs N ticks (manual servers only); a client pumps N frames
expect:
  - state: { entity: Arena, property: Hits, min: 1 }
    node: p1
  - agree: { server: master, clients: [p1, p2] }   # replica hash equals the server's at the replica's tick
  - drift: { nodes: [p1, p2], equals: 0 }
```

Without `node:` a `step` goes to every node, `state`/`tick`/`hash` expectations are an error. Failures report `{kind, line, node, message, expected, actual}`.

## CLI

`onegame cluster up --spec f.yaml [--dir d]` (starts, prints the state JSON, leaves nodes running), `status`, `hashes` (exit 1 on mismatch or drift), `down`, `call <node> <op> [json]`, `test <scenario.yaml> --spec f.yaml` (up, run, down). The state file is found by `--dir` or `--cluster file`, else `./.onegame-cluster/cluster.json`. `onegame mcp --cluster file` is one MCP server for all nodes: every tool takes an extra `node` argument. `onegame export --server [--plugins]` publishes the server project NativeAOT (or `onegame-server` framework-dependent with `--plugins`) with the export report of `exporting.md`.

## Limits

One server connection per client process. Servers do not connect to each other yet. A snapshot larger than the transport's `MaxMessageBytes` (1 MiB default) closes that client's connection. Cluster launching uses `/bin/sh` and Unix sockets (Linux, macOS).
