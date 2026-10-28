# Hot reload of game code

In a Debug build, `onegame run` applies your C# edits to the running game. Edit a view, a command handler or a screen, save, and the change shows in the next frame with the world untouched: same tick, same entities, same state.

```
onegame run                  # a window; edit Game.cs and save
onegame run -- --headless    # the same without a window, driven over live control
onegame run --no-hot         # run without it
```

There is nothing to set up. `onegame run` starts the game under `dotnet watch` (part of the .NET SDK you already have), which compiles each save and hands the runtime a metadata delta, the mechanism behind Visual Studio's Edit and Continue. The engine then drops what it cached from the old code. Batch runs (`--frames`, `--capture`, `--scenario`, `--replay`, `--json`) and `--release` builds do not use it; `--hot` forces it on.

The game listens for live control as with `--live` (it prints the socket path as `listening on ...`; pass `--live <path>` for your own) and runs in real time.

## What applies live

| Edit | Result |
|---|---|
| The body of a `[View]` method, a `Draw`, a binding lambda | Live on the next frame. |
| The body of `OnTick`, a command handler, a system | Live on the next tick: the simulation behaves differently from here on. |
| A screen builder (`DefineScreens`) | The open screen is rebuilt from the new code. Its own UI state (focus, scroll position, typed text) starts over. Screens you add or remove are picked up. |
| A new method, a new class, a new `[Synced]` property or `[Command]` | Live. A new synced property starts at its default on entities that already exist. |
| An asset or a shader file | Reloaded by the asset service and the shader reloader, as before. |

Code that ran once at start-up does not run again: `Setup`, `DefineActions`, `Style`, `DefineFonts`, `RegisterEntities`. Editing them is accepted but takes effect on the next start (`Ctrl+R` in the terminal restarts the game).

## Edits that cannot apply: the game restarts and keeps its world

Some edits cannot be applied to a running program: changing the type of a synced member, renaming or removing one, changing a base class or a method's signature on a type that is already loaded. `dotnet watch` calls these rude edits. When one happens `onegame run` says why and restarts the game:

```
hot reload: this edit cannot be applied to the running game: Updating the type of field requires restarting the application [ENC0009, generated code of Mygame_Ship, from a change to its synced members, commands or views].
hot reload: world saved; restarting and loading it.
```

The world was copied through live control when the file changed; the restarted game loads that copy through the generated snapshot migrations, so the tick and every entity carry over. Play between the copy and the restart (about a second) is lost. If the copy fails, the game restarts fresh and says so. Existing agent connections end at a restart; reconnect to the same socket.

An edit that does not compile is reported (`hot reload: ...Game.cs(29): ) expected [CS1026]`) and the game keeps running the last good code. Fix the file and the edit applies.

## Seeing it from an agent

The `hot_reload` operation reports `active`, `applied` (edits applied since this process started), `lastTypes`, `state` (`idle`, `applied`, `rude`, `error`, `restarted`), the message, `restarts`, `resumed` (this process loaded the world its predecessor saved) and `replaySafe`. A script that edits a file waits for `applied` to grow, then reads state or captures a frame. `tools/hot-reload/check.py` does this end to end: it creates a game, edits a view, a sim method, a screen, adds a synced property, makes a compile error and a rude edit, and checks the result of each.

## Determinism

Hot reload is for iterating, not for recording facts.

- A sim edit changes behaviour in the middle of a run. State before the edit came from the old code and state after it from the new.
- Hashes no longer match a clean run of the same inputs, and `replaySafe` turns false after the first edit.
- A session recording running across an edit is cut into a new segment at the edit, so each segment starts from a recorded state. Replaying the earlier segments with the new code will not reproduce their hashes. Record again from a clean start (`onegame run --no-hot`, or a scenario) for a recording you keep.
- Each process is edited alone. Nodes of a cluster started with `onegame cluster` are not hot; a server or client you run with `onegame run` is, and if it runs sim code its replicas will report drift against peers still on the old code. That drift is expected.

## What it does not do

- Exports and Release builds contain none of it: the runner, the update handler and the live-control library are Debug-only, and `onegame export` builds Release.
- It needs a writable `obj/` and the SDK's `dotnet watch`; the Android, iOS and web runs are not hot.
- Editing engine source in an engine checkout is hot too, with the same limits.
