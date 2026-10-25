# Lanterns

The networked sample. Two islands joined by a bridge, each simulated by its own server; a master server that keeps accounts, a leaderboard and the swap ledger. Players walk, pick up lanterns (red, blue, gold), cross the bridge, and swap one lantern for another with any player, on either island. Plain boxes and the kit's dialog: the look is a placeholder, and the views can be replaced without touching the rules.

It is also the example for multiplayer: `docs/interfaces/sync.md` (views, prediction, transfer, relaying, escrow), `transport.md` and `cluster.md` are what it is built from.

## Layout

| Path | What |
|---|---|
| `Sim/` | The rules (library, shared by everything): `Island`, `Player`, `Lantern`, `Account`, `Market`, the swap handler, and `Worlds` (how each scope is built). Integer positions, named random streams, no floats. |
| `Server/` | `LanternsServer`: one program; the scope it is given decides if it is an island (1, 2) or the master (3). `Realm` is all of its logic. |
| `Views/`, `Game.cs` | The client: `Session` (what is mine, what is here), `IslandView`, `Autopilot`. |
| `scenarios/` | Single-player scenarios and 720p captures, run by `onegame test`. |
| `cluster.yaml`, `cluster/play.yaml`, `cluster/crash.yaml` | The five-process cluster, the scenario that plays 2000 ticks on it, and the one that kills the master (while a swap is open) and then island B and starts them again. |
| `cluster-release.yaml`, `cluster/play-release.yaml` | The same cluster made of exported builds, whose clients have no live control. |
| `tests/` | In-process network tests, privacy at the byte level, determinism, and the process cluster. |

## How the pieces work together

- **One connection per player.** A client connects to one island and follows three scopes over that link: its island, the other island and the master. The island serves the other two as relays, so what a player may see is decided by the owner's rules twice: at the authority and again at the island.
- **Walking is predicted.** `Player.Steer` is `[Command(OwnerOnly = true, Predicted = true)]`: the client runs it at once on a predicted copy of the island and the server runs it for real; the client corrects itself if they disagree.
- **Private data.** A player's colours, pending swap steps and the `Mine` marker are `Visibility.Owner`; everyone sees position and the total. The test `PrivacyTests` records every byte a client receives, directly and through a relay, and looks for another player's inventory.
- **The bridge is an authority transfer.** The sim moves a player to the arrival place and sets `Dest`; the island server (`Realm.HandOver`) then calls `BeginTransfer`. The entity gets a new id on the other island, its commands in flight are forwarded exactly once, and `Moves` (the count of applied steers) travels with it.
- **A swap is an escrow.** `Account.Accept` on the master opens a trade; the master's `EscrowDriver` takes one lantern from each player on their island (`Withdraw`), then credits the other (`Credit`), or gives it back. Servers save every other tick and answer peers only for what the save holds (`Durability.AfterSave`), so a server killed and started from its file keeps every lantern. A player who has a swap pending, or is already on the bridge, cannot take part in the other.
- **Servers find each other** with `--option peer.N=udp://host:port` and listen for each other on a private port, `--option peers=PORT` (the transport does not authenticate, so that port must not be reachable by players). A link that drops opens again by itself.

## Run it

Single player (a walker called Ada to swap with; arrows or WASD to walk):

```
onegame run                       # in samples/lanterns
onegame test --json               # scenarios and captures, then the C# tests
```

The cluster. Build both programs into `bin/`, then start the five processes:

```
dotnet publish Server/Lanterns.Server.csproj -c Debug -o bin
dotnet publish Lanterns.csproj -c Debug -o bin
onegame cluster up --spec cluster.yaml --dir run
onegame cluster hashes --dir run --json      # exit 1 on a mismatch or drift
onegame cluster down --dir run
onegame cluster test cluster/play.yaml --spec cluster.yaml --json    # up, 2000 ticks, expectations, down
```

`bot1` reaches its island over UDP with 100 ms latency and 2% loss, `bot2` over a WebSocket. A client whose name starts with `bot` plays itself (`Autopilot`).

`cluster/crash.yaml` uses the `kill:`, `start:` and `restart:` steps (SIGKILL, then the same arguments, data folder and ports). After island B is back, `restart: bot2` joins a new player to it, because a client does not reconnect. `ClusterTests` then checks that lanterns held by players equal lanterns collected and that every client agrees with its server.

Exports: `onegame export --project Lanterns.csproj --out export` and `onegame export --server --project Server --out export`. The exported clients are release builds with no live control, so the release cluster runs them with `release: true` and `--until-tick`: each plays headless in real time until its replica reaches that tick, prints one JSON line (tick, `viewHash`, followed scopes, drift count), and `cluster/play-release.yaml` waits for them to stop and compares what they printed with their servers:

```
onegame cluster test cluster/play-release.yaml --spec cluster-release.yaml --json     # export/ holds both exported folders
```

A windowed client on a machine with `WAYLAND_DISPLAY` set opens on that compositor even when `DISPLAY` names an Xvfb; pass `--backend x11 --display :N` there.

## Limits

- Player ids come from a counter on the island that was joined; nothing authenticates a player, so a returning client is a new player.
- A player whose client leaves stays on the island.
- A client does not reconnect when its own island is killed; it has to start again (as a new player).
- No Windows run, no real-network run.
