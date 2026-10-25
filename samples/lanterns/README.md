# Lanterns

The networked sample. Two islands joined by a bridge, each simulated by its own server; a master server that keeps accounts, a leaderboard and the swap ledger. Players walk, pick up lanterns (red, blue, gold), cross the bridge, and swap one lantern for another with any player, on either island. It is drawn in 3D on the PBR renderer: low-poly islands, a wooden bridge, walkers, and lanterns that glow and light the ground around them, with the kit's HUD and swap dialog over it. The look is a placeholder, and the views can be replaced without touching the rules.

It is also the example for multiplayer: `docs/interfaces/sync.md` (views, prediction, transfer, relaying, escrow), `transport.md` and `cluster.md` are what it is built from.

## Layout

| Path | What |
|---|---|
| `Sim/` | The rules (library, shared by everything): `Island`, `Player`, `Lantern`, `Account`, `Market`, the swap handler, and `Worlds` (how each scope is built). Integer positions, named random streams, no floats. |
| `Server/` | `LanternsServer`: one program; the scope it is given decides if it is an island (1, 2) or the master (3). `Realm` is all of its logic. |
| `Views/`, `Game.cs` | The client: `Session` (what is mine, what is here), `IslandView` (HUD, names, swap dialog), `IslandScene` (the 3D scene and camera), `Motion` (view-only smoothing), `Autopilot`. |
| `models/`, `art/` | The glTF models (embedded in the program) and `art/Program.cs`, the small program that writes them: `dotnet run --project art -- models`. |
| `scenarios/` | Single-player scenarios, picks and captures (3D frames against lavapipe references, 4 of 255 per channel on at most 0.1% of pixels), run by `onegame test`. |
| `cluster.yaml`, `cluster/play.yaml`, `cluster/crash.yaml` | The five-process cluster, the scenario that plays 2000 ticks on it, and the one that kills the master (while a swap is open) and then island B and starts them again. |
| `cluster-3d.yaml` | The same cluster with graphics devices on the bots, for capturing what a bot sees. |
| `cluster/reconnect.yaml`, `cluster/leave.yaml` | Island A is killed and started again with bot1 playing on it (nobody restarts the bot); a bot is killed and its player leaves the game. |
| `cluster-android.yaml`, `cluster/play-android.yaml`, `cluster/android-client.sh` | The same cluster with bot1 on an Android device or emulator. |
| `cluster-release.yaml`, `cluster/play-release.yaml` | The same cluster made of exported builds, whose clients have no live control. |
| `tests/` | In-process network tests, privacy at the byte level, determinism, and the process cluster. |

## How the pieces work together

- **One connection per player.** A client connects to one island and follows three scopes over that link: its island, the other island and the master. The island serves the other two as relays, so what a player may see is decided by the owner's rules twice: at the authority and again at the island.
- **Walking is predicted.** `Player.Steer` is `[Command(OwnerOnly = true, Predicted = true)]`: the client runs it at once on a predicted copy of the island and the server runs it for real; the client corrects itself if they disagree.
- **Private data.** A player's colours, pending swap steps and the `Mine` marker are `Visibility.Owner`; everyone sees position and the total. The test `PrivacyTests` records every byte a client receives, directly and through a relay, and looks for another player's inventory.
- **The bridge is an authority transfer.** The sim moves a player to the arrival place and sets `Dest`; the island server (`Realm.HandOver`) then calls `BeginTransfer`. The entity gets a new id on the other island, its commands in flight are forwarded exactly once, and `Moves` (the count of applied steers) travels with it.
- **A swap is an escrow.** `Account.Accept` on the master opens a trade; the master's `EscrowDriver` takes one lantern from each player on their island (`Withdraw`), then credits the other (`Credit`), or gives it back. Servers save every other tick and answer peers only for what the save holds (`Durability.AfterSave`), so a server killed and started from its file keeps every lantern. A player who has a swap pending, or is already on the bridge, cannot take part in the other.
- **Servers find each other** with `--option peer.N=udp://host:port` and listen for each other on a private port, `--option peers=PORT` (the transport does not authenticate, so that port must not be reachable by players). A link that drops opens again by itself.

## On Android

The client runs on Android as a NativeAOT APK, connected to servers over UDP or a WebSocket (the platform's crypto library is not needed: see `docs/interfaces/transport.md`). The project sets `AndroidInternet` so even a release APK may open sockets. With an emulator on the computer that runs the servers, the device sees that computer as `10.0.2.2`, and `cluster/android-client.sh` rewrites the address a cluster gives its clients:

```
onegame export --target android --dev --abi x86_64     # a dev build lets `onegame android run` read the app's output
onegame android install export/Lanterns-*-android-dev.apk
onegame cluster test cluster/play-android.yaml --spec cluster-android.yaml --json   # servers and one desktop bot here, bot1 on the device
```

`ONEGAME` (the command), `ANDROID_SERIAL`, `LANTERNS_HOST` and `LANTERNS_TIMEOUT` configure the script. The bot dithers around its target when the link has no latency, so `cluster-android.yaml` gives the device the same slow link as the other clusters.

## The 3D view

The rules live in a library that servers share and cannot reference the host, so no entity draws itself. `IslandScene` is the scene view's `Describer`: each frame it reads the island worlds (for the one I walk on, the predicted copy) and writes models, lights and the camera. The two islands sit side by side along X with the bridge between, so the camera follows my player over the hand-over without a cut; a walker waiting for the other server glides across at a fixed speed.

- **Motion is view state.** `Motion` moves the drawn position towards the state's position a little each tick, so a prediction correction bends the path instead of popping. It counts ticks, never the clock, so a capture after N ticks always looks the same.
- **Light and depth:** the scenery (trees, rocks, bushes) is drawn with `Instance`, and the moon casts cascaded shadows from walkers, lanterns, scenery and the bridge. The stage turns SSAO on; quality levels cap it (`quality` op).
- **Names** are UI labels pinned to a point above each walker (`PinToWorld`). Lanterns circling a walker are its count; only your own copy knows their colours.
- **Picking:** `scenarios/12-pick.yaml` clicks a lantern, a walker and the ground. Models of lanterns and walkers are pickable, scenery is not.
- **Looking at it:** `onegame view models/island-a.glb` renders one model; `onegame capture --out a.png --size 720p --fixture crowded --screen main --after 45` renders the game.

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

For a 3D capture from a running bot, use `cluster-3d.yaml` (bots with a graphics device) and ask a bot for a frame:

```
onegame cluster up --spec cluster-3d.yaml --dir run
onegame cluster call bot1 capture_frame '{"size":"720p","path":"bot1.png"}' --dir run     # written under run/artifacts
onegame cluster down --dir run
```

`bot1` reaches its island over UDP with 100 ms latency and 2% loss, `bot2` over a WebSocket. A client whose name starts with `bot` plays itself (`Autopilot`).

`cluster/crash.yaml` uses the `kill:`, `start:` and `restart:` steps (SIGKILL, then the same arguments, data folder and ports). After island B is back, `restart: bot2` joins a new player to it. `ClusterTests` then checks that lanterns held by players, plus those that left with disconnected players (`Island.Departed`), equal lanterns collected and that every client agrees with its server.

A client that loses its island connects again by itself (`ConnectOptions.Reconnect`) and, to the island, is a new player. A player whose connection is gone is removed (`Island.Leave`) from the island they are on, even when that is the other island; after a restart from a save, the players the island let in before are removed the same way.

Exports: `onegame export --project Lanterns.csproj --out export` and `onegame export --server --project Server --out export`. The exported clients are release builds with no live control, so the release cluster runs them with `release: true` and `--until-tick`: each plays headless in real time until its replica reaches that tick, prints one JSON line (tick, `viewHash`, followed scopes, drift count), and `cluster/play-release.yaml` waits for them to stop and compares what they printed with their servers:

```
onegame cluster test cluster/play-release.yaml --spec cluster-release.yaml --json     # export/ holds both exported folders
```

A windowed client on a machine with `WAYLAND_DISPLAY` set opens on that compositor even when `DISPLAY` names an Xvfb; pass `--backend x11 --display :N` there.

## Limits

- Player ids come from a counter on the island that was joined; nothing authenticates a player, so a returning client is a new player.
- A reconnecting client is a new player: the transport has no authentication, so nothing proves it is the same person.
- No Windows run, no real-network run.
