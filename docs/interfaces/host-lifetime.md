# Host lifetime: what survives a run

A game that calls `GameApp.Run` more than once in a process (a menu loop, then a match, then the menu again) should not pay for a new window and graphics device each time. This file says what is kept, what is not, and how to change that. Code: `src/OneGame.Host` (`GameHost`, `GameInstance`, `GameApp`) and `PlatformApp.ResetForNewRun` in `src/OneGame.Platform`.

## The rule

`GameHost` owns the parts that are expensive and belong to no single game:

- the connection to the window system and the window (`PlatformApp`, `Window`),
- the graphics device,
- the swapchain.

Everything else belongs to the `GameInstance` and is destroyed with it: the world, the cluster or server session, the UI tree, input actions and rebinding state, the asset service, the art atlas, fonts, the render stage with its renderers, and every texture, model, mesh, environment and offscreen target the game made through its renderers, whether or not the game disposed them. A second run therefore starts from nothing a first run did; only the window, the device and the swapchain carry over. Scenario hashes of a game run twice in one process equal those of two separate processes (`HostReuseTests`).

## Who uses it

- `GameApp.Run` passes `GameHost.Shared` to every windowed run, unless `GameApp.ReuseWindow` is false (set it before the first run). Headless runs, browsers and phones never use a host. Reuse is on for macOS too (it was off until checked): AppKit needs the runs on the main thread, and a Mac game may call `GameHost.Release()` there before `Main` returns to close the window early; without it the process still leaves in about 10 ms, because the exit handler leaves macOS windows to the system. Windows and macOS reuse are covered by `HostReuseTests` (the Windows CI runner and a Mac).
- `GameInstance.Create(game, new GameOptions { Host = host })` does the same for code that makes its own instances. `host.Dispose()` closes the window and device; do it when no instance is running on the host.
- With no host, an instance makes its own window and device and closes them in `Dispose`, as before.

The shared host closes when the process exits (a `ProcessExit` handler; on macOS it leaves the window to the system), or earlier with `GameHost.Release()`. A game that leaves the loop for good and wants the GPU memory back calls `Release()`.

## How the stage releases GPU resources

`Renderer3D` and `Renderer2D` each keep a list of the resources they handed out and the game has not disposed yet (models, dynamic meshes, environment maps, billboard images, `Offscreen3D`, morphed instances; textures, render textures, materials). Disposing the renderer, which the stage does when the instance goes, disposes what is still listed after waiting for the GPU. Disposing a resource yourself removes it from the list, and disposing twice is harmless. Resources of other kinds (a `GpuTexture` you made straight on the device) are yours: the device outlives the instance, so free those yourself. `ResourceLifetimeTests` runs eight instances that make one of everything and dispose nothing: device memory is identical after each (21.6 MB and 100 allocations on lavapipe, of which 17 MB is the device's own staging pool).

## What a new run gets

- The window as the player left it (position, size, fullscreen). A run that asks for a different `Width`/`Height` than the previous run gets that size; one that asks for the same keeps the player's.
- The title of the new game.
- No queued input events, no held keys, buttons or touches, no pending quit request, text input off, cursor shown and unlocked (`PlatformApp.ResetForNewRun`).
- The same device. `ONEGPU_ADAPTER`, validation and `GameOptions.Gpu` are read when the device is made; a later run that asks for `GpuMode.Off` gets no device.
- The same swapchain when its sRGB-ness and present mode still fit what the new stage needs; it is rebuilt otherwise, and resized if the window changed in between.

A window the player closed is not reused: the host closes the device with it and the next run opens new ones. A different `Backend` or `DisplayName` replaces the window too.

## Rules and limits

- One running instance per host. A second `Create` on a busy host throws `InvalidOperationException`; a start that throws leaves the host free.
- Use a host from one thread, the one that runs the game. On macOS that is the main thread.
- While no run is active the window is not serviced. A game that works for a long time between runs calls `host.Pump()` now and then so the window manager does not mark it as not responding.
- The process-exit handler skips teardown when a run is still active (the process was told to exit from inside a game), and always on macOS.
- Teardown order is unchanged and lives in one place (`GameHost`): swapchain, window system connection, device. The window system connection closes while the driver is loaded, because the driver hooks display close.

## Switching connect options without a new run

`GameInstance.SwitchConnection(ConnectOptions? connect)` closes the current server connection (if any) and starts a fresh session on the same instance: a replica of the named scope, or with `null` a new local world. The window, device, swapchain, stage and renderers stay, so nothing is rebuilt. `WorldReplaced` is raised and `Options.Connect` shows the new value; the open screen is left as it is. Use it for sign-in, leaving a server and joining another.

## Measured

Three `GameApp.Run` calls in one process, a 3D scene with a planar reflection, 90 frames each, time from the call to the first presented frame (`RunLoopTests`, validation layers on, zero messages):

| Display, adapter | Run 1 | Run 2 | Run 3 | Teardown of a run | Process exit |
|---|---|---|---|---|---|
| Xvfb, RTX 4070 (driver 580) | 501 ms | 35 ms | 35 ms | 23-26 ms | under 50 ms |
| Xvfb, llvmpipe | 382 ms | 116 ms | 109 ms | 12-18 ms | 34 ms |
| Weston, llvmpipe | 412 ms | 115 ms | 109 ms | 0-8 ms | 29 ms |
| macOS Cocoa, Apple M2, Metal validation on | 308-1,845 ms | 11-18 ms | 31-33 ms | 10-25 ms | 10-29 ms (with or without `Release()`) |

Before this change run 2 took 121-147 ms on the same machines and each run's teardown 60-72 ms (RTX 4070) with the device destroyed each time.
