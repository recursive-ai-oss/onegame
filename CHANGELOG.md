# Changelog

All notable changes to onegame are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/) from 1.0.0.

## [Unreleased]

### Added

- Fake-store prices: `FakeStore.SetPrice(product, micros, currency, display?)` sets the price text, amount in millionths and ISO 4217 code a product reports on its next load, so store screenshots and scenarios can show regional prices. `FakePrice.Format` writes the text the same way on every machine (`$4.99`, `€4.99`, `¥1,200` for JPY with no minor units, `CHF 4.50`; no culture involved) and `FakePrice.TryParseAmount` reads `4.99`. The `store` control op and scenario step gain `action: price` (`product`, `amount`, `currency`, optional `display`; products reload) and a `products` action that lists them; scenario expectations check `store: {price: "coins=€4.99", currency: "coins=EUR", micros: "coins=4990000"}`. Products keep the old $0.99-and-up prices until one is set.

## [2.5.0] - 2026-11-25

### Added

- `onegame export --target web --aot` (also `onegame web test --aot`, or `OneGameWebAot=true` in the project) compiles the game to WebAssembly ahead of time for compute-heavy games, at the cost of build time and size; the interpreter stays the default. A missing `wasm-tools` workload stops the export first with the install command, and the export JSON has `aot`. Solitaire: export 50 s -> 162 s, `_framework` 17.7 MB -> 48.9 MB (3.4 -> 7.2 MB Brotli), scenario hashes equal; a 475-particle fluid step 11.1 ms -> 2.4 ms in Chrome (indicative). `docs/exporting.md`.
- Game-defined command-line options. `GameDefinition.OptionNames` declares the names a game takes itself; they work in release and development builds beside the engine's and the development options (and on Android through the `onegame.args` extra), and the game reads them from `GameInstance.Args` (also `GameOptions.Args`, `GameArgs.Empty`). Undeclared options are refused as before, so a misspelt `--scenaro` still fails, and the error lists the game's own options. A non-development run's `--json` summary has a `gameOptions` object. `onegame android live` now forwards the game arguments after `--`. `docs/interfaces/game-options.md`.
- Haptics. `Haptics.Tick()`, `Haptics.Buzz()` and `Haptics.Play(HapticKind)` give touch feedback on phones: Android through `View.performHapticFeedback` (no permission needed), iOS through the UIKit feedback generators; desktop and the web do nothing. Every request is recorded on every platform (`Haptics.GetLog`), read by the new `haptics` control operation and the scenario expectation `haptic: {kind, count, min, max, frames}`. `Haptics.Enabled` is the game's own vibration switch. `docs/interfaces/platform.md`.
- Android release packaging. `onegame export --target android --aab --keystore <file> --key-alias <name>` writes a signed Android App Bundle for Google Play (base module, per-ABI splits, uncompressed 16 KiB-aligned libraries, built with Google's bundletool, fetched once with a pinned SHA-256 or named with `--bundletool`) and an APK signed with the same key. Keystores may be PKCS12 or JKS; the passwords come from `ONEGAME_ANDROID_STORE_PASSWORD` / `ONEGAME_ANDROID_KEY_PASSWORD` and are passed to the signing tools through the environment, never on a command line. `--version-code`, `--version-name` (or `AndroidVersionCode`, `AndroidVersionName`) set the version; `--symbols` with `--aab` also writes `native-debug-symbols.zip`. The export result gains `versionCode`, `versionName`, `targetSdk`, `signingCertSha256`, `bundle`, `bundleSize`, `bundleSha256`, `symbolsArchive`; `signing` is `release-key` for the game's own key. `docs/interfaces/android.md`.
- The export warns `page_size_4k` when a native library has segments aligned below 16 KiB, and zip-aligns with 16 KiB pages.
- `GameDefinition.OnBack(game)`: a game sees the system back button or gesture first and returns true when it handled it. Unhandled, the UI gets a `Cancel` and the app quits as before. Android activities opt out of the platform's own back callback (`enableOnBackInvokedCallback=false`), because at target SDK 36 it finished the activity next to the key.
- `Window.ClaimSystemGestures(regions)` with `GestureRegion`, `ScreenEdges`, `ClaimedGestureRegions`, `ClaimedEdges` and `GestureClaimLimit`: a game keeps drags that start inside its regions from becoming the system's back swipe (Android `setSystemGestureExclusionRects`, capped near 200 dp per edge). iOS already defers all edges; desktop and web record only.
- Control ops `system_gestures` (read and set the claim) and `input_back`; `InputInjector.Back`.
- 2D particle fluids through `PhysicsSpace2D` on OnePhysics 1.0.0: `AddFluid(FluidOptions)` (presets `FluidPreset.Water/Oil/Honey/Sand/Gravel/Snow/Mud/Clay/Dough` with optional viscosity, density, friction, gravity scale, speed limit and layer), `FluidWorldSettings` (`SetFluidSettings`/`FluidSettings`), `FillFluidBox`, `FillFluidCircle`, `AddFluidParticle`, `AddEmitter`, `AddDrain` (`SetEnabled`, `Remove`, `Emitted`, `Drained`), count and shape queries (`ParticleCount`, `OverlapFluidCircle`, `OverlapFluidBox`, `FluidDensityAt`), `FluidState` for a body, wet and dry events (`FluidEvents`), and a span-filling `GetFluidParticles` for views that allocates nothing. Inputs and outputs are `Fixed`/`FVec2`; `FluidRef`, `FluidEmitterRef` and `FluidDrainRef` are checked synced values. Fluids are in the space's saves, restores, replays and hash with no format change. `PhysicsDebugShapes` draws particles, emitters and drains (`IncludeFluids`); the `physics` table in `state_query` reports `fluidParticles` and a `fluids` array. `examples/physics/fluid` and `docs/physics.md`.

### Changed

- **A release Android export (no `--dev`) now needs a signing key**: it exits 2 `keystore_required` instead of signing with the debug key. Pass `--keystore` and `--key-alias`, or `--debug-key` for a build that only has to install on a test device. `--keystore` no longer assumes the password `android`; set `ONEGAME_ANDROID_STORE_PASSWORD`. `--dev` exports keep the debug key.
- Play Billing exports declare the Billing Library's `com.google.android.play.billingclient.version` meta-data, taken from the library's own manifest, so Play can read the bundled version.
- Android exports target SDK 36 by default (Google Play requires it) and link against platform 36: install `platforms;android-36`. `--target-sdk <level>` or `<AndroidTargetSdk>` sets another level; below 36 the export warns (`target_sdk_low`). Behaviour changes at 36 are predictive back and enforced edge-to-edge.

### Fixed

- Android `Window.SafeArea` was zero under the enforced edge-to-edge window of target SDK 36; it now reads `WindowInsets.getInsets` (and includes the display cut-out and the system-gesture bands from API 30).
- `--live` works in an Android development build. The control server's request-state protector used the platform random and key-derivation provider when it was built, and phones have no OpenSSL, so the app aborted at startup; OneModelContext 1.0.2 builds it on first use instead, and live control answers on the emulator (`onegame android live`, then `onegame mcp --socket`).

## [2.4.0] - 2026-11-25

### Added

- Skinned instancing. `Scene3D.AddSkinnedInstanced(model, capacity)` and `InstanceBatch3D.Add/Set(transform, pose, tint)`, `SetPose`, `Posed` and `BoundsMargin` draw many copies of one skinned model, each in its own `NodePose`, with one instanced draw per primitive per view; copies are culled, cast and receive shadows and take fog like the plain batch. In view code, `SceneWriter.Instance(name, transform-or-matrix, pose, tint)`. 500 animated Fox models are 2 draws (the ground and the crowd) plus one per shadow tile, 0 B per frame, 0.70 ms GPU at 1280x720 with shadows on an RTX 4070 (indicative; 2,000 copies 1.66 ms). The instance vertex record grows from 64 to 80 bytes (the palette start); frames of plain batches are unchanged. `docs/interfaces/render3d.md`.
- Fog modes. `FogSettings.Mode` (`FogMode.Linear` default and unchanged, `Exponential`, `ExponentialSquared`, `Height`) with `Density`, `HeightBase` and `HeightFalloff`; `Material3D.FogExempt` leaves a material out of fog (a sky dome or other backdrop layer). The `environment` op reads and sets `fogMode`, `fogDensity`, `fogHeightBase`, `fogHeightFalloff`.
- `ModelInstance3D.CastShadows` and `ModelOptions.CastShadows`: one instance can be drawn without casting shadows.
- `image-rendering: pixelated` (`StyleBuilder.ImageRendering`, `ImageRendering`, `Ui.PixelImage`, `DrawCommand.Pixelated`): nearest-neighbour sampling for images, tiles and nine-slices.
- `backdrop-blur` (`StyleBuilder.BackdropBlur`, `DrawKind.BackdropBlur`, `UiCanvas.BackdropBlur`): a UI box blurs what is drawn behind it, including a 3D scene, as in CSS `backdrop-filter: blur()`. No extra pass is recorded in frames that use none.
- Light shafts. `Scene3D.Volumetric` (`VolumetricSettings`: `Enabled`, `Density`, `Intensity`, `Anisotropy`, `MaxDistance`, `LinkToFog`) and `Light3D.Volumetric` / `VolumetricIntensity` make directional, spot and point lights scatter through the air, occluded by their shadow maps (a ray march at a quarter or half of the frame, upsampled by surface distance, run before bloom). The quality ladder bounds it (`QualitySettings.VolumetricSteps` and `VolumetricScale`: none at Low, 16 steps at a quarter size on Medium, 24 at half on High, 48 at half on Ultra). Nothing changes while it is off: no pass, the same pixels. `FrameStats3D.Volumetric` and a `volumetric` control operation report and set it. The passes are `light_shafts` and `light_shafts_composite`; the depth prepass now also runs for shafts and is shared with ambient occlusion.
- 3D render scale. `Scene3D.RenderScale` and `SceneView3D.RenderScale` (0.25 to 1, default 1) draw the 3D frame at a fraction of the window and stretch it over the target before the 2D and UI passes, so the UI stays sharp and cost follows the pixel count. Full size adds no pass and draws the same pixels; picking, projection, anchors and captures keep using window pixels. New control op `render_scale`, `FrameStats3D.RenderWidth/RenderHeight`, and `Renderer3D.SceneColor` (with `SceneColorWidth/Height`) exposing the tone-mapped scene image to later passes of the same graph frame. `docs/interfaces/render3d.md`.
- Environments without a hitch. `Renderer3D.CreateEnvironmentAsync(HdrImage, EnvironmentLoadOptions?)` returns an `EnvironmentLoad` (`State`, `Progress`, `IsReady`, `Error`, `Take()`, `Cancel()`, `Dispose()`): the panorama is converted on a worker thread (in slices on the render thread in a browser), copied to the GPU in bands and filtered a few passes per frame under `EnvironmentLoadOptions.GpuBudget`, while the game keeps drawing its current map. The finished map is pixel-identical to `CreateEnvironment` plus `PrepareEnvironment`; the synchronous calls are unchanged. A face size of 0 follows quality, and a quality change mid-load restarts the filtering at the new size. `Renderer3D.RetireEnvironment` frees a replaced map once frames in flight are done. On the RTX 4070 the worst frame of a 2048x1024 panorama at 256 is 1.3 ms against 50 ms for the synchronous pair.
- `Render3DStage.AdoptEnvironment(load, EnvironmentSlot)` swaps a ready load into `Environment.Map` or `NextMap` at the start of a frame and frees what it replaced. The `environment` control operation starts a load (`load` file or `sky` width, `size`, `into`), cancels it (`cancel`) and reports its state and progress (`EnvironmentResult.Load`).
- `GameHost` and `GameOptions.Host`: the window, graphics device and swapchain of a windowed game can outlive one `GameInstance`, so a process that runs several games in a row starts each later one drawing at once. `GameApp.Run` uses `GameHost.Shared` (`GameApp.ReuseWindow = false` opts out; `GameHost.Release()` closes it early; it also closes at process exit). On macOS the runs are on the main thread, as AppKit requires; at process exit the window is left to the system, and a game may call `GameHost.Release()` on the main thread to close it earlier (on an Apple M2 runs 2 and 3 draw their first frame in 11-33 ms and the process leaves in 10-29 ms either way). A window the player closed is replaced, and nothing but the window, device and swapchain carries over. See `docs/interfaces/host-lifetime.md`.
- `GameInstance.SwitchConnection(ConnectOptions?)`: closes the server connection and starts a fresh session (a replica of another scope or server, or a new local world) on the running instance, keeping the window, device and renderers.
- `PlatformApp.ResetForNewRun()`: drops queued events, held input and a pending quit request, keeping the windows.
- Layered materials for blocky worlds: `Renderer3D.CreateLayeredMaterial(name, width, height, layers, ...)` builds a `Material3D` whose base colour is a texture array (nearest, repeating inside each layer, no blending between layers), and each vertex chooses its layer with `MeshVertex3D.Layer` (a new init property and a five-argument constructor; for glTF meshes, the first component of `TEXCOORD_1`). One material and one draw per chunk replace one per texture per chunk: a 36-chunk, 32-texture test scene goes from 1,151 draws to 36. `Material3D.LayerCount`. Every material's bind group gains two bindings (set 2, bindings 11 and 12: a one-texel placeholder array and a sampler); the 3D pixel shaders read the base colour through one function, so existing materials draw as before.

### Changed

- `Renderer3D.CreateMaterial` now starts as the rough non-metal its documentation describes (metallic 0). It started fully metallic, the glTF default, which has no diffuse light, so lit meshes read dark and desaturated; set `Material3D` textures and colours as before. Layered materials start the same way.
- Every view of one `Renderer3D` (the main scene and each `Offscreen3D`) now shares a single shadow atlas instead of pooling one per view, so sixteen sampled views with shadows reserve one 192 MiB atlas (High, directional plus point light) rather than sixteen. `Offscreen3D.Dispose` also drops the renderer's cached bind groups for its targets.
- A second `GameApp.Run` in one process no longer rebuilds the window and device: with a 3D scene the first frame of later runs arrives in 35 ms on an RTX 4070 (121-147 ms before) and in about 110 ms on llvmpipe, and a run's teardown no longer destroys the device. `GameInstance.Options` can now change after creation (only `Connect`, through `SwitchConnection`).

### Fixed

- A render graph no longer throws when the device refuses its GPU timestamp queries (Metal runs out of counter sample buffers after a few graphs, for example with many `Offscreen3D` views): that graph runs untimed (`TimingEnabled` false, `GpuFrameMs` null, no pass times).
- Disposing a renderer, and so a `GameInstance`, now frees every GPU resource it handed out that the game left undisposed: `Renderer3D` models, dynamic meshes, environment maps (also finished async loads), billboard images, `Offscreen3D` targets and morphed-instance buffers, and `Renderer2D` textures, render textures and materials (post chains and tile sets release through them). With a `GameHost` the device outlives the instance, so a game that dropped these without disposing leaked about 300 MB per run. Disposing something yourself first, or twice, is fine.
- `Renderer3D.ReadGpuPassTimes` and the graph's pass times could return a wrapped value of about 1.6e13 ms when a pass's end timestamp read below its start; such an interval is now zero.
- `FrameStats3D.GpuFrameMs` was null on about one frame in a hundred on a busy GPU because the timing ring slot of the only finished frame was recycled before it was read; the graph now keeps the newest resolved frame, so it is a value from the first finished frame on.
- The shader hot-reload test in the Testing tests skips on Metal, like the Render2D one (reloading recompiles to SPIR-V only), instead of failing there.

## [2.3.0] - 2026-11-22

### Changed

- Live control speaks MCP through RecursiveAI.OneModelContext (1.0.1; it has no dependencies of its own). New `ControlServer` (in `OneGame.Control`) serves a control endpoint's operations as tools: `Server` runs over a stream, `OpenSession()` handles one message at a time. `LiveServer` and the page channel (`?control=`) use it; the tool list (names, descriptions, schemas, read-only hints) and every operation's result are unchanged, and a transcript against a live game matches. `McpProtocol` is `[Obsolete]` and now an adapter over `ControlServer`; it stays for binary compatibility.
- What a client may notice: send `initialize` first (all MCP clients do; the harness's own clients now do), because an uninitialized connection is read as the stateless 2026-07-28 revision; an unknown requested revision is answered with the library's newest legacy one (2025-11-25) instead of 2025-06-18, while 2025-06-18, 2025-03-26 and 2024-11-05 negotiate as before; `isError` is left out of a successful tool result (the spec's default); `resources/list` and the other empty capabilities answer instead of "method not found"; an exception inside an operation is an `isError` result with code `internal_error` and the exception's text, where the socket used to answer a JSON-RPC internal error. `ServerVerb` and `LiveClient` initialize before their first call.

### Fixed

- `onegame export --target web` restores the game before it reads the project, so the first export of a game that was never built has its shaders (it failed with "namespace Shaders does not exist"). `tools/web/run-web-tests.sh` works from a clean checkout.
- Cluster hash check: a client's main-scope replica is compared with the view the server holds for that client's own connection, not with any of the server's views, so a client holding another audience's valid view is reported. `NetStatus.LocalPort` (an init property) and `INetConnection.LocalPort` (default 0) let a client name its connection to the server; release clients report `localPort` in their result.
- A 3D stage (`Render3DStage`) whose scene update or 2D drawing throws leaves later frames and captures working, as the 2D stage already did; a test covers it.

## [2.2.0] - 2026-11-22

### Added

- `hash_at {tick, audience}` and `audience_watch` on a server, `ServerHost.WatchAudience` and `HashesAt(tick, client, audience)`: the authority records the view hash of a named audience (player, team, relay, interest) at every tick, within `hashHistory`, so a replica that follows through relays or under an identity the server has no connection for can be compared with it. Hashes only.
- `ProcessCluster.Audience`, the scenario step `audience:`, `agree: { scope }` and a client node's `audiences:` in the cluster spec: declare who a followed replica is, and every `Hashes()` compares it with the authority's record of that audience; a wrong audience or an unrecorded tick is reported.
- Render textures and post effects for the 2D layer. `Renderer2D.CreateRenderTexture`, `RenderTexture2D` (`Texture` is a sprite texture), `Renderer2D.BeginTarget`/`EndTarget`, and `Layer2D.Target`/`TargetClear`: draw sprites, shapes and text into an image of any size and show it as a sprite (minimaps, reflections, cached layers). A render texture has the renderer's own format. `PostChain.AddBloom(...)` (bright pass, blur at reduced size, added back) and `PostChain.AddMaterial(material)` (a full-screen pixel shader of the game's own), run over the whole frame (`Draw2D.Post`, `DrawScene`) or over one layer (`Layer2D.Post`, so a HUD layer above stays sharp). `Draw2D` gets `RenderTexture(name, w, h)`, `RenderLayer(name, texture)`, `Post` and `CreatePostChain()`. A frame without a target or a chain has no extra pass and draws as before; steady frames allocate nothing. `examples/glow`, `docs/2d.md`, `docs/interfaces/render2d.md`; the web export is checked in headless Chrome (`tools/web/chain-test.mjs`).
- One-shot sim events. `[SimEvent]` on a partial struct declares an event type; sim code emits with `Entity.Emit` or `World.Emit`; views read each event once through `World.Events.NewReader()` (`EventLog`, `EventReader`, `EventRecord`, `EventInfo`, `ISimEvent<T>`). Events reach exactly the replicas that may see the emitting entity (and the type's own `Owner`/`Team`/`Hidden` visibility), never appear in saves, snapshots or state hashes, are empty after a restore, load or join, and replay to the same stream. The log keeps 512 events with a per-tick limit (`EventsPerTickLimit`, default 128). New message type 24 after a tick's frame, sent only when an audience has events (traffic of scopes without events is unchanged). A predicted world records none, and relays do not pass events on. Harness: the `event:` scenario expectation, the `sim_events` live-control operation, and `examples/events`. Rules: emitting from a view is OG0008, a float payload is OG0001. See `docs/events.md`.
- Scopes a client follows at run time (subscribed by game code after start) are addressable (`RemoteSession.Replicas` and `GameInstance.Replicas` now include them; they were fixed at start): `command_send {scope}` and `state_query {scope}` on a client, `ProcessCluster.Send(node, entity, command, args, scope)` and `ProcessCluster.Get(...)`, and a `state:` expectation with `scope:`. A scope the client does not follow answers `not_found`. `net_status.followed` lists every replica the node holds, each with its `authority`.

### Changed

- Wire: a new message (type 24, events) follows a tick's frame, sent only when a game emits events that an audience may see; traffic of scopes without events is byte-identical. An older build that receives it reports an unknown message type and carries on.
- A client link's peer name in `net_status` and in drift reports is now the server's address instead of `remote`.

## [2.1.0] - 2026-11-22

### Added

- `Layer2D.Camera`, `Layer2D.Viewport`, `Layer2D.ScreenSpace` and `Layer2D.View(...)`, `Scene2D.GetLayer(name)`, and on `Draw2D` `ScreenLayer`, `GetLayer(name)` and layer overloads of `WorldToScreen`/`ScreenToWorld`: a layer can draw through a camera of its own, inside a screen rectangle, or in plain pixels, so a HUD sits over a zoomed and panned world in one frame and a minimap shares the frame with the main view. A scene whose layers have none of these draws as before.
- `Draw2D.Text` and `ScreenText` take a `z`, and text now takes part in the draw order (layer, `z`, call order) with sprites and shapes: a sprite on a later layer, or drawn later at the same `z`, covers a text. `Scene2D.Mark`, `ISceneMarkerSink` and the `markers` parameter of `Renderer2D.DrawScene` are the low-level seam. Sprites and text that do not interleave batch exactly as before.
- `Draw2D.WarmUpText(characters, size, family, weight)` and `TextQuadBuilder.Warm`: rasterize the glyphs of some characters at a size ahead of time.
- `MusicOptions.StartAtFrame`: start a music track at a given frame of the audio timeline, the same clock and units as `PlayOptions.StartAtFrame`, so beat-synced music lands exactly.
- `InputState.PointerEdges`: every pointer and touch event of the frame in order, each with its own sub-pixel position (`PointerEdge`, `PointerEdgeKind`), with `EndFrame()` (the game host calls it after each frame), `DroppedPointerEdges` and `PointerEmulatedFromTouch`. A press that is followed by a long move in the same frame, and a finger that moves and lifts in the same frame, now report where they happened; reading `PointerX` and `TryGetTouch` at frame time gives only where the pointer ended up. See `docs/input.md`.
- `Renderer2D.CancelFrame()`: drops a frame that was begun but not ended, so a view that threw does not leave the renderer mid-frame.
- `tests/WebGame` and `tools/web/run-web-tests.sh`: a small game and the headless-Chrome checks of the web host (pointer positions, phone canvas, safe area, titles, fingerprints, capture after a failing frame, a game's own material).
- `AudioService.Reset()`: stops every voice and music at once and waits until the mixer has taken it. `onegame test` calls it at the start of every scenario.
- Scenario steps `pointer: {action: down|move|up|click, x, y, button}` (`StepKind.PointerAction`) and a `button: left|right|middle` option on `click: {x, y}` (`ScenarioStep.Button`), so drags and right-clicks can be tested from a scenario. Cluster scenarios take them too.

### Changed

- Behaviour change: `Draw2D.Text` and `ScreenText` were always drawn over every sprite of the frame. They are now ordered like sprites (see Added): a text drawn before a sprite at the same layer and `z` is under it. Give the text a later layer or a higher `z` to keep it on top.
- Text cost: `Draw2D.ScreenText` and `Text` no longer upload the glyph atlas once per call that rasterizes something new (the pages go up once, when the frame ends), a text drawn at a new size reuses the earlier shaping with new advances (identical output, no new shaping), repeated labels hit a small cache keyed by the string object, and the glyph atlas is keyed by one 64-bit value. In the browser build at 4x CPU throttle, 100 `ScreenText` calls at sizes that are new every frame went from 94 ms to 8 ms of main-thread time per frame.

- `onegame new` writes a `global.json` that selects the Microsoft.Testing.Platform runner, and `Game.targets` (so the SDK package too) leaves `tests/**` out of the game's own compile, so a xunit.v3 project added under `tests/` passes `onegame test` (the .NET 10 SDK refused `dotnet test --project` without it).
- Text: the default family (an empty `font-family`, `Draw2D.Text` without `family`) now honours weight and slant: the nearest face of the first family added is used. Games that load a bold face get bold text; with only a regular face nothing changes.
- Sync: a computed interest key (an `Entity.InterestKey` override) is read once per entity per tick during publish, not once per entity per view; with 100 views over 200 entities that is 200 reads a tick instead of about 20,000. Frames, view hashes and digests are byte-identical.
- Sync: a `Subscribe` that arrives while the connection's trust is empty (`Trust.None`) is held until the game sets `Connection.Trust`, instead of being refused with `NotTrustedToRead` (a fresh link no longer needs a second ask). It is refused if the trust then lacks the scope, when the connection detaches, or after `ClusterOptions.HeldSubscribeTimeoutMs` (5 s). The wire format is unchanged.
- `InputInjector.Wheel` (and the live-control `input_pointer action=wheel`) is stamped with the current pointer position, so an injected wheel scrolls the scroll view under the pointer. It used 0,0.
- Web: pointer positions keep their fraction (they were rounded to whole device pixels), and the canvas is sized to the visible area, `innerWidth` by `innerHeight` in CSS pixels, and follows `resize`, `orientationchange` and the visual viewport. It was 100vw by 100vh, which on a phone with its toolbar out is taller than the area on show, so the picture stretched against the touch positions.
- Web: `SafeAreaChanged` now fires from `env(safe-area-inset-*)` (the page needs `viewport-fit=cover`, which the stock page has), and `Window.SafeArea` holds the value from the start.
- Web: the page keeps a title it sets itself. The stock page now has no `<title>`, so the game's `Name` is used as written (it was written to the page in every case).
- Web export: the game's own project is built as its csproj says. The generated wrapper project carries `AllowUnsafeBlocks`, `NoWarn`, `DefineConstants`, `InternalsVisibleTo`, the `OneShader*` properties, the game's `OneShader` and `OneShaderVariant` items with their metadata, and its package references (`RecursiveAI.OneShader.Build` among them), so a custom `Sprite.Material` compiled from the game's HLSL exists in the browser. WGSL is added to `OneShaderTargets` when the game left it out.
- Web export: the imports in `boot.mjs`, and the page's `boot.mjs` script, carry `?v=<hash of the file>`, so a CDN or cache that kept an old loader cannot pair it with a new program. The `.br` and `.gz` copies are rewritten to match.
- In a browser, `AudioServiceOptions` defaults are lean: metering off, no bus meters, no reverbs, 32 voices and a clamping limiter (other platforms are unchanged; set the properties to opt back in). An idle page spends 2% of the main thread on audio instead of 21% (81% to 5.6% at 4x CPU throttle in headless Chrome on the solitaire sample).
- Web audio creates its `AudioContext` on the first user gesture instead of at load, so Chrome no longer warns that it was not allowed to start. The output reads `awaiting_gesture` until then, as before.
- `onegame test` clears the `ui-scale:` override at the start of each scenario, as it does the audio voices.

### Fixed

- X11: a window asked to maximize right after a move could remember (0, 0) as the position to restore to. The configure handler read the position, then the size, and a maximize landing between them paired the maximized position with the old size. It now reads the size after the position, so such an event counts as stale.
- `onegame test` counted 0 unit tests when the test platform coloured its summary (a CI log, `CI=true`): the colour codes before `total:` and `succeeded:` hid them. The counts now ignore ANSI escapes.
- Web, WebGPU: a custom material as the first draw of a frame failed with "No bind group set at group index 0" and drew nothing. `Renderer2D` now binds the frame constants after the pass's first pipeline.
- A view that threw while a frame was being built left the render graph and the 2D renderer mid-frame, so every later frame and capture failed with "the previous frame was never executed". The stage now cancels the frame and the game's own error is what shows. In a browser the animation-frame loop also survives a frame that threw (`window.__onegame.frameErrors` and `lastFrameError` say what happened), and a page frame no longer runs between an offscreen capture's start and its readback.
- `PlayOptions.Pitch` no longer throws. The mixer's converter now runs down to a quarter of the rate, so a 44.1 kHz clip plays at up to 4.35x in a 48 kHz mixer (it stopped at about 2.17 before), and a pitch beyond what the clip allows, zero, negative or infinite plays at the nearest allowed speed (NaN plays at 1). `SetPitch` clamps the same way. A fault in the web audio pump is counted in `AudioOutput.RenderFaults`, kept in `LastError`, and no longer escapes into the page's timer, which ended the game loop.
- Late sounds in a long `onegame test` run reported `started: false` and `sound:` expectations saw 0 plays: voices and queued commands from earlier scenarios filled the mixer. Each scenario now starts with the audio reset.

## [2.0.0] - 2026-11-13

The GPU device layer moves to the standalone `RecursiveAI.OneGpu` 1.0.0 packages. This is a major release; [docs/migrating-to-2.0.md](docs/migrating-to-2.0.md) lists what a 1.x game changes.

### Changed (breaking)

- The device layer (`GpuDevice`, buffers, textures, pipelines, encoders, `ShaderSource`, `GpuBackends` and the Vulkan, Metal and WebGPU backends) is in `RecursiveAI.OneGpu`, `RecursiveAI.OneGpu.Backends`, `.Vulkan`, `.Metal` and `.WebGpu`; namespaces are `RecursiveAI.OneGpu` and `RecursiveAI.OneGpu.Vulkan` and so on. Type names are unchanged. `RecursiveAI.OneGame.Gpu` keeps only the render graph (`OneGame.Gpu.Graph`).
- The packages `RecursiveAI.OneGame.Gpu.Backends`, `.Vulkan`, `.Metal` and `.WebGpu` are no longer produced (32 packages become 28).
- `GpuModule.Name` and `VulkanModule.Name` are removed; `GpuDeviceOptions.ApplicationName` defaults to `"onegpu"`.
- Web: the WebGPU script is `onegpu.js` (module `onegpu`), published by the `RecursiveAI.OneGpu.WebGpu` package at `_content/RecursiveAI.OneGpu.WebGpu/onegpu.js` instead of being copied by the export. Custom pages register it as `onegpu`.

### Changed

- GPU environment variables are `ONEGPU_VALIDATION`, `ONEGPU_ADAPTER`, `ONEGPU_BACKEND`, `ONEGPU_VALIDATION_OPTIONAL` and `ONEGPU_VULKAN_LIBRARY`. The old `ONEGAME_*` names are still honoured by games and `onegame view`: they are copied to the new names before the first device is created.
- Vulkan devices share one instance per process, and validation messages are collected per device.

### Added

- `onegame export --target linux-x64|win-x64|osx --dev`: a desktop export with the development tools (live control, `--live`), as the Android and iOS exports have. The folder gets a `-dev` suffix and the result says `devTools: true`. Release exports are unchanged.

### Fixed

- Linux games no longer crash (exit 139) when they quit on NVIDIA drivers under X11: the X display now closes before the graphics device and its driver are released, because the driver hooks display close on the connection.
- Closing a game's window while it runs no longer fails with a lost surface: the swapchain is released before the window, and nothing presents to a closed window.

## [1.27.0] - 2026-11-12

Render cost you can read: CPU and GPU frame time in the 3D frame statistics, measured without stalling the frame.

### Added

- CPU and GPU frame time in `FrameStats3D`: `CpuFrameMs` (build, record and submit), `GpuFrameMs` (from GPU timestamp queries, `null` where the device has none or timing is off) and `GpuFrameIndex`, the graph frame the GPU time belongs to. GPU results are read without waiting and are one or two frames late. `Renderer3D.ReadGpuPassTimes` gives the per-pass breakdown of the same frame. `RenderGraph.TryReadFrameTime` and `RenderGraph.ExecuteCpuMicroseconds` are the underlying calls.

### Fixed

- Lanterns sample: a player who reconnects while their leave is still being applied, or while their character is crossing the bridge to the other island, keeps their character; a character that arrives late after such a rejoin is not duplicated.

## [1.26.0] - 2026-11-12

Water that mirrors the world: planar reflection and scrolling textures for 3D materials, on OneShader 1.7.0.

### Added

- Planar reflection for water and polished floors in `OneGame.Render3D`. A material opts in with `Material3D.PlanarReflection` (and `ReflectionStrength`), and the game sets the one horizontal mirror plane with `Scene3D.Reflection.Height`. While an opted-in material is drawn, a `planar_reflection` pass renders the scene mirrored about the plane at a size set by the quality level (off at Low, then 0.5x, 0.75x, 1x), and the material's specular light comes from it, weighted by smoothness; a frame with no opted-in material adds no pass, image or binding work. Materials of a loaded glTF file are reached by name with `Model3D.FindMaterial`. `FrameStats3D.PlanarReflection` reports whether the pass ran, and `QualitySettings` gains `ReflectionScale`.
- `Material3D.UvScroll`: a constant texture-space velocity added to the material's texture coordinates, driven by the new `Scene3D.Time` clock (the renderer keeps no clock of its own).

### Changed

- The generated frame-constant bindings of the built-in 3D shaders (`OneGame.Render3D.Shaders.*.Frame`) gain the reflection fields, and their `Size` grows from 512 to 1024 bytes.
- OneShader and OneShader.Build 1.3.0 to 1.7.0. Shader bytes can differ (the Metal output no longer drops a guarded `continue`; WGSL writes the largest float exactly), renders do not.

## [1.25.1] - 2026-11-11

A fix for games that add or remove entities during a tick and then index `World.Entities`.

### Fixed

- `World.Entities[i]` and `Count` were O(n) per access after an entity was added or removed during a tick (the view scanned past removed entities on every call), so an indexed loop over thousands of entities took tens of milliseconds. They now read a compacted copy built once per change. Which entities are listed, and their order, are unchanged.

## [1.25.0] - 2026-11-11

Custom render stages can draw above the UI next to the debug panel, and a 3D view rendered offscreen can be drawn as a 2D texture without copying it back to the CPU.

### Added

- Custom render stages can draw text and rectangles above the UI: `Render2DStage.AddOverlay`/`RemoveOverlay` take an `IFrameOverlay`, and `OverlayCanvas` gains `Outline` and `Image`. Overlays share the stage's 2D batch and glyph atlas, and draw under the development panel (`Render2DStage.Overlay`), which keeps its own slot, so the two no longer replace each other.
- `Renderer2D.WrapTexture` and `RebindTexture`: a `Texture2D` over a GPU image the caller owns, such as a sampled `Offscreen3D` target, drawn by sprites, `Ui.Image` and overlays with no readback. `examples/stage` shows both.

### Fixed

- Vulkan adapter choice is deterministic: adapters are kept in one total order (kind, vendor id, device id, name, API version, UUID) instead of the driver enumeration order, so a multi-GPU machine picks the same adapter every time and `ONEGAME_GPU_ADAPTER` / `GpuDeviceOptions.Adapter` match the same one. The rule is in `docs/interfaces/gpu.md`.
- A presented capture (`capture_frame` with `presented`, `GameInstance.CapturePresented`) no longer returns nothing when the window could not be drawn: it throws `PresentedCaptureException` with a `Reason` (minimized, window not drawn, no backbuffer in time, surface lost), which the control op reports in its message. Games with no window or device still return null.

## [1.24.0] - 2026-11-11

Physics is stable and comes to 3D: `RecursiveAI.OneGame.Physics` is built on OnePhysics 1.0.0 and ships at the engine version, so every onegame package is now stable.

### Added

- 3D physics spaces saved, stepped and hashed with the world: rigid bodies, characters, vehicles, queries, prediction forks, explicit origin rebasing and debug geometry through the 3D debug renderer. `examples/physics/3d` shows a falling pile, a walking character and a driven car.

### Changed

- `RecursiveAI.OneGame.Physics` is stable on OnePhysics 1.0.0 and ships at the engine version, without a preview suffix. 2D physics results and hashes are unchanged from 1.23.0.

### Fixed

- Two games in one process (or a game whose screen was rebuilt after another game started) no longer paste from the other game's clipboard: each text field uses the clipboard of its own window.
- Vulkan: a submission that does not draw into the acquired swapchain image (such as an upload at startup) no longer consumes that image's acquire and render-done semaphores, which could make the first presented frame wait on the wrong work.

## [1.23.0] - 2026-11-10

Drift checks get cheap for large scopes: a drift-check tick with 5,000 entities and 16 audiences went from about a second to a few milliseconds on the server. Hashes are unchanged. `RecursiveAI.OneGame.Physics` ships as 1.23.0-preview.1.

### Added

- `driftCheckInterval:` in the server configuration (`ServerConfig.DriftCheckInterval`, default 30, 1 to 10,000 ticks), `ServerHost.DriftCheckInterval` (code wins over the file; set before `Start`), `Node.DriftCheckInterval`, and `driftCheckInterval` in `server_info`. A longer interval costs the server and every client less per tick and finds drift later. It cannot be 0 on a server; out-of-range values are errors. `hashHistory`, `hash_at` and cluster hash comparisons do not depend on it.
- `Entity.StateVersion` and `SyncList.Version`: counters that change whenever an entity's synced state (or a list) changes, so caches can be keyed on them.

### Changed

- Drift-check ticks no longer re-serialize and re-hash every entity for every audience: each entity caches its full and per-visibility-mask hashes, invalidated by any change (`Entity.StateVersion`, `SyncList.Version`), and the digest folds the world hash in the same pass. Hashes, recordings and the wire are unchanged. A check tick with 5,000 idle 4.8 KB entities and 16 audiences went from about 1,000 ms to about 3 ms on the authority and from about 33 ms to about 0.5 ms on a replica.

### Fixed

- 1.22.0: `Connection.Channel` returned an internal send-pacing wrapper instead of the transport for UDP connections, so code that read the client's proven key through `connection.Channel is IAuthenticatedChannel` or closed the client through `connection.Channel is INetConnection` silently failed: such a server trusted no one, and clients joined but never received entities. `Connection.Channel` is the transport again, as before 1.22; pacing is kept internally.

## [1.22.0] - 2026-11-10

Per-connection spatial interest: a connection receives only the part of a scope it is looking at, so a scope with many players no longer costs every client the whole world. `RecursiveAI.OneGame.Physics` ships as 1.22.0-preview.1.

### Added

- `[InterestKey]` on a synced `uint`/`int` property (0 = global), `AuthorityScope.SetInterest`/`ClearInterest`/`InterestOf`, `Connection.SetInterest`, `AuthorityScope.RequireInterest`, `InterestSet` and `InterestGrid2D` (cells with enter/leave radii, sticky entity keys). Entities entering and leaving a connection's interest are sent as additions and removals; joins carry only in-interest entities. Works for clients, peer nodes and relays, needs no wire change (1.21 clients and servers interoperate), and keeps view hashes and drift checks exact. Hot tables and random streams are not filtered (see `docs/interfaces/sync.md`).
- `SubmitResult.OutsideInterest`; a command to an entity outside the connection's view is answered as an unknown entity.
- `AuthorityScope.Traffic` (bytes per entity type), `EntitiesSent`, `EntitiesRemoved`, `FrameBytes`, `SnapshotBytes`, listed per scope by the server's `server_info`.
- `ServerHost.MaxSendQueueBytes` and `ConnectOptions.MaxSendQueueBytes`; the default (8 MiB) and how to raise it are documented in `transport.md`.
- OG0017 also covers `[InterestKey]` declarations.
- A `listen:` entry takes `maxSendQueueBytes`; `ListenConfig.MaxSendQueueBytes` is its code form.
- `ServerHost.ClientJoined`: raised when a client is admitted, before its first message, so a game can set its interest before the first snapshot.
- `IBoundedSendChannel`: transports with a bounded send queue report how full it is.
- `InterestRequiredException` and `World.HiddenTablesOnly`: once a scope requires interest, making its random streams visible or adding a visible table later is refused.

### Changed

- A node paces what it sends over a bounded transport: a snapshot (or any burst) larger than the send queue is fed in as acknowledgements free room instead of overflowing at once. What waits is capped, and overflow still closes the link.
- A server logs a send-queue overflow with the setting to raise instead of a bare "left".
- A `ClientJoined` handler that throws refuses that one client instead of stopping the server.
- A transfer's `Moved` notice names only the entities each watcher could see, and commands from such a watcher to an entity in transit are held or forwarded as before.

## [1.21.0] - 2026-11-10

Creating thousands of meshes at once no longer leaves the GPU driver holding gigabytes of upload memory. `RecursiveAI.OneGame.Physics` ships as 1.21.0-preview.1.

### Fixed

- Creating thousands of meshes (or any burst of buffer and texture uploads) in one frame no longer leaves gigabytes of driver memory mapped. The Vulkan backend stages uploads through a few fixed 4 MiB chunks, at most `GpuDevice.StagingBudgetBytes` (32 MiB) in all: when they are all in use, the writing call waits for the GPU to finish the oldest copies instead of making more buffers. Idle chunks and empty device-memory blocks are returned after a while. A burst of 2,500 chunk-sized meshes used to hold about 1.5 GB of staging for the life of the device; it now peaks at 32 MiB and settles at 16 MiB.

### Added

- `GpuMemoryStats.StagingBytes` and `GpuDevice.StagingBudgetBytes`.

## [1.20.0] - 2026-11-09

3D scenes get distance fog and a crossfade between two environment maps for day and night cycles, and steady frames on fast GPUs allocate nothing. `RecursiveAI.OneGame.Physics` ships as 1.20.0-preview.1.

### Added

- Distance fog in the 3D renderer. `Scene3D.Fog` (`FogSettings`: `Enabled`, `Color`, `Start`, `End`, `MaxDensity`) fades every surface (PBR, toon, flat, unlit, outlines, masked and blended materials, instanced draws, dynamic meshes, billboards) towards a colour with its distance from the camera. It is a per-frame constant: changing it uploads nothing and rebuilds nothing, and steady frames still allocate nothing. Off by default; frames with fog off are unchanged.
- Environment crossfade. `Environment3D.NextMap` and `Blend` mix the backdrop, the diffuse light and the specular light of two environment maps, so a day cycle can step through keyframes without a pop: at `Blend` 1, making the next map the first and the following one the next at `Blend` 0 draws the same picture. Changing `Blend` refilters nothing.
- Live control `environment` operation: read and set the fog, the blend weight, the environment intensity and its yaw.

### Fixed

- The Vulkan backend no longer allocates managed memory on a random frame of a steady run. Command encoders are created up front, and upload staging pools are sized from what a submission uses and then kept fixed (a miss waits for the GPU instead of growing the pool). On a fast GPU the pools used to grow whenever the GPU first lagged further behind, which showed up as sporadic small allocations in frames that should allocate nothing.

## [1.19.0] - 2026-11-09

Meshes a game builds can be added, replaced and freed while it runs, windows remember where they were and how they were shown, Windows games open without a console window, and captures of windowed games show all their art. `RecursiveAI.OneGame.Physics` ships as 1.19.0-preview.1.

### Added

- Meshes built, replaced and freed at run time. `SceneView3D.Models.SetMesh(id, material, vertices, indices)` adds or replaces a mesh by name (entities draw it with `SceneWriter.Model` like any model), `Remove` and `Replace` free or swap models and meshes, and `Contains` checks a name. `Renderer3D.CreateMaterial` makes a shared material without a glTF file, and `Renderer3D.CreateMesh` returns a `DynamicMesh3D` (`Update`, `Dispose`) for a hand-filled `Scene3D`. Buffers are sized in classes and reused, updates copy through the device's staging memory, freed buffers are released once the GPU has finished with them, and nothing waits for the GPU; a soak of thousands of swaps holds GPU memory flat and steady frames allocate nothing.
- `SceneWriter.Model(..., ModelOptions)` sets a per-draw opacity and label (`ModelOptions.Opacity`, `Label`, `Pickable`).
- `scene_models` live-control operation lists what the 3D view draws: model, label, opacity, entity, triangle count, position, and whether the mesh was built at run time.
- `GpuDevice.MemoryStats` reports the backend's device-memory tally (Vulkan).
- Window placement. `Window.Placement` reads where a window is and how it is shown as a plain `WindowPlacement` value (normal rectangle in screen pixels plus `Normal`/`Maximized`/`Fullscreen`) that a game can save with `Format()` and read back with `TryParse`; `WindowOptions.Placement` creates a window from it (a maximized window is created maximized, remembering the rectangle to restore to), and `Window.ApplyPlacement`, `Maximize`, `Restore` and `MoveTo` change a live one. A placement saved on a monitor that is gone is moved onto a connected screen (`PlatformApp.GetWorkAreas`, `WindowPlacement.ClampTo`). Win32 and X11 (EWMH) place position and state; Wayland and Cocoa place size and state only (xdg-shell has no position request); phones and browsers do nothing. `Window` keeps all its existing members.
- Live control `window` operation: read and set the placement (`x`, `y`, `width`, `height`, `state`, or a whole placement as `text`).
- A Windows game is built as a GUI-subsystem program with no console window by default (a `win-*` runtime identifier, or a Release build on Windows). `OneGameConsole=true` keeps the console and `OneGameConsole=false` makes a Debug build GUI too; a game that sets `OutputType` to `WinExe` itself is unchanged. `ParentConsole.Attach()`, called by `GameApp.Run`, prints to the terminal that started the program; redirected output is untouched.

### Fixed

- Captures taken in a windowed game (`capture_frame`, `capture_sequence`, scenario and `onegame capture` captures) could miss art the window had not drawn before, and could show stale glyph pages. The capture rendered in a different pixel format than the window's swapchain, which gave it a second renderer, and the art atlas hands each upload (large standalone rasters, changed pages) to one renderer only. Captures now render in the swapchain's format, so there is one renderer and one set of textures, and the read-back swaps the channels.

## [1.18.0] - 2026-11-09

Cluster scenarios reach every followed scope, servers report per-connection view hashes, damaged save slots read as missing, and 3D views gain portals, runtime emissive and opacity, frame timing in the overlay and camera/pick on hand-filled scenes. `RecursiveAI.OneGame.Physics` ships as 1.18.0-preview.1.

### Added

- Cluster scenarios can command a followed scope: `command: { entity: Arena, name: Hit, scope: 2 }` on a client node (and the `command_send` op's new `scope` argument) looks the entity up in that scope's replica and forwards the command to its authority, whose trust checks apply as for any player. Unfollowed scopes fail with `not_found`.
- `net_status.followed` lists every other scope the client's node is subscribed to, including ones the game subscribed to itself, and each entry carries `fullHash` and `filtered`; for a filtered replica `hash` is the view hash the authority computes for that connection.
- Server `hash_at` names the connection of each view (`clients`) and takes an optional `client`, returning that connection's view hash as `view`; `ServerHost.HashesAt(tick, client)` is the API form.
- A 3D view can be shown inside another (portal windows, monitors, mirrors): `new Offscreen3D(renderer, w, h, format, sampled: true)` renders another `Scene3D` into a target that `Material3D.SetTexture(MaterialSlot3D, texture)` binds to any material texture slot, with no model re-upload. `ResetTexture` restores the file's texture.
- Runtime material changes without reloading a model: `Material3D.Emissive`, `EmissiveStrength` and `Opacity`, and `ModelInstance3D.Opacity` (plus `Label`). Translucent surfaces use the existing blended path (far to near, no depth write, no shadow or outline).
- The debug overlay shows per-frame timing: CPU frame, simulation and draw times with a history bar chart, and GPU pass times where the backend has timestamps. The `overlay` and `info` operations return the same numbers as `timing`. `GameInstance.Timings`, `GameInstance.ReadGpuTimings` and `RenderGraph.ReadPassTimes` are the API; timing runs only while the overlay is shown.
- `camera`, `pick` and `project` work for a `Render3DStage` that has no `SceneView3D`: `Render3DStage.Manual` (`ManualScene3D`, `IScene3DProbe`) answers over the stage's `Scene`, and games with their own stage implement `IScene3DStage.Manual`.

### Fixed

- `SaveSlots.Exists` is false for a slot whose save bytes are damaged (truncated or garbled): it now checks the recorded payload hash, without restoring a world. `List` leaves such slots out too.

## [1.17.1] - 2026-11-09

A fix for Wayland windows presenting with vsync. `RecursiveAI.OneGame.Physics` ships as 1.17.1-preview.1.

### Fixed

- A Wayland window with vsync presenting while its compositor withholds frame callbacks (covered, minimized or on another desktop) no longer parks the thread that runs the simulation and network pump, so a connected game keeps ticking and stays connected. The host now paces vsync on Wayland itself with the window's own frame callbacks and queue-replacing presents, skips drawing while a callback is outstanding, and never waits more than 100 ms for a swapchain image in a realtime run. `Window.PresentWouldBlock` and `Window.OnPresenting` are the platform hooks.

## [1.17.0] - 2026-11-09

Screens with more art than the atlas pages hold draw correctly, and an image that truly cannot be drawn fails loudly instead of showing as magenta. `RecursiveAI.OneGame.Physics` ships as 1.17.0-preview.1.

### Fixed

- A screen with more art than the four atlas pages hold (about a hundred portraits at once) no longer draws the overflow as magenta boxes: rasters that find every page in use get a texture of their own, inside the same 256 MiB budget as large rasters, so GPU memory stays bounded as before.

### Added

- An image the atlas truly cannot draw is reported: `ArtAtlas.Refusals`, `LastRefusal` and `Warning` (written to stderr by the host), an `atlas` section in the `info` op, and an automatic `atlas` failure in any scenario whose run refused an image.

## [1.16.0] - 2026-11-08

Recorded clips get their sound, scenario checks see hidden ancestors, scenarios can reset view-side state, and 3D views can draw debug lines, points and arrows. `RecursiveAI.OneGame.Physics` ships as 1.16.0-preview.1.

### Added

- 3D debug drawing: `SceneWriter.DebugLine`, `DebugPoint` and `DebugArrow` (and `Scene3D.Debug`, a `DebugDraw3D`) draw instanced lines, points and arrows sized in pixels or world units, depth-tested or on top of the scene; `SceneWriter` collects them only while debug drawing is on (`DebugVisible`).
- `capture_sequence` records sample-aligned mixed WAV audio alongside its unchanged PNG frames and manifest. When ffmpeg is on PATH it also produces an MP4 with sound; otherwise the reply explains why video was skipped.

### Fixed

- Scenario `ui.visible` checks and the `hidden` state in UI queries now include hidden ancestors, `display: none` and `visibility: hidden`, rather than only the element's own visible flag.

### Changed

- The build requires .NET SDK 10.0.403 (runtime 10.0.14), whose NativeAOT compiler has the fix for CVE-2026-111497; older SDKs fail NativeAOT publishes with a NuGet audit error.
- Scenario runs call `GameDefinition.OnScenarioStarting(game)` before setup and rebuild the current screen afterwards. Games can restore view-side defaults in this hook without recreating graphics resources between scenarios; an in-scenario world reset does not call it.

## [1.15.2] - 2026-10-29

`RecursiveAI.OneGame.Physics` moves to OnePhysics 0.2.0-preview.1 and ships as 1.15.2-preview.1. The other 31 packages are unchanged apart from the version.

### Changed

- `OneGame.Physics` uses OnePhysics 0.2.0-preview.1. Results move: 2D positions now sit on the Q31.32 grid and broadphase boxes are tighter, so the same inputs end in different positions and hashes, and physics saves and replays made with an earlier preview do not carry over. The probe values (`tools/OneGame.PhysicsProbe/expected.txt`) are re-pinned; the `examples/physics` captures are unchanged. `docs/physics.md` says so.
- The world hash no longer grows its buffer as a physics snapshot grows: the physics table sizes it, and its own snapshot buffer, with room to spare, so a settling pile hashes with no allocation. `SyncWriter.Reserve(int)` is new (it writes nothing). World hash values are unchanged.

## [1.15.1] - 2026-10-29

A fix for sim code that finds entities removed earlier in the same tick. `RecursiveAI.OneGame.Physics` ships as 1.15.1-preview.1.

### Fixed

- An entity removed during a tick no longer appears in `World.Entities` for the rest of that tick. The world keeps it until the tick ends, so a second command or a later loop in the same tick used to find it again and could apply the same change twice (for example, adding a leaving player's holdings to a total once per queued request). `World.Entities` keeps its type and returns a live view of the entities in id order. Saves, snapshots, deltas and hashes are unchanged, because they are produced between ticks.

## [1.15.0] - 2026-10-29

`RecursiveAI.OneGame.Physics` joins as a **preview** (`1.15.0-preview.1`): it is built on OnePhysics 0.1.0-preview.5, itself a preview, so its API may change. The other 31 packages are stable at 1.15.0 and never depend on it.

### Added

- `RecursiveAI.OneGame.Physics` (preview, built on OnePhysics 0.1.0-preview.5): 2D rigid bodies, characters, triggers and ray casts for sim code on `Fixed` positions. `PhysicsSpace2D` is a hidden hot table of the world plus a system that steps it once per tick, so the physics world is saved, restored, replayed and hashed with the world; `BodyRef` and `CharacterRef` are synced values; events reach sim code in a fixed order; `PhysicsDebugShapes` draws the world on the debug overlay; `Warmup()` and capacity options make steps, ray casts and the space's hash allocation-free from the first step. A save the library refuses (damaged, or from another format) is refused as `SyncError.InvalidValue` and leaves the world unchanged. See `docs/physics.md`.
- Example `examples/physics` in the gallery: a character that walks, jumps between platforms and pushes crates into a goal zone.
- CI: a physics scene must end in the same hashes and positions on Linux, Windows and WebAssembly (`tools/ci/physics-probe.sh`).

### Changed

- `tools/ci/check-packages.py` allows exactly one prerelease package, `RecursiveAI.OneGame.Physics` at `<engine version>-preview.N`, and still refuses any other.

## [1.14.0] - 2026-10-28

`RecursiveAI.OneGame.Navigation` is stable, on OnePath 1.0.0: every package now ships at the engine version.

### Changed

- `RecursiveAI.OneGame.Navigation` is stable: it is built on OnePath 1.0.0 and ships at the engine version like every other package, no longer as a prerelease. `tools/ci/check-packages.py` now refuses any prerelease package. `NavGrid.Stamp` reads OnePath's cached `Grid.ContentHash` directly.
- `OneGame.Navigation` moves to OnePath 1.0.0. Search and crowd results and saved bytes are unchanged. `NavCrowd` reads agents with `Crowd.CopyAgents(Span<Agent>)`, treats any `OnePathFormatException` from a bad crowd state as a refused snapshot, and `TryAdd` snaps to the nearest walkable point of the whole map (it used to refuse a spot too far from ground).

### Fixed

- Lanterns sample: a player who reconnects to an island that restarted from its save keeps their player. Before, the island could remove them as "left" just after they came back.

## [1.13.0] - 2026-10-28

Crowds as synced state in `RecursiveAI.OneGame.Navigation` (1.13.0-preview.1, still a preview on a preview of OnePath). The other packages have no API changes.

### Added

- `OneGame.Navigation`: crowds as synced state. `NavCrowd` is OnePath's crowd (avoidance of walls and of each other) on a mesh built from a `NavGrid`, kept in the world as the hot table `og.nav.crowd`: stepped once per tick, saved, hashed, replayed and replicated with the world, 0 B per step; sim code sees `Fixed`/`FVec2` only (`NavAgent`, `NavAgentOptions`, `NavCrowdOptions`). See `docs/navigation.md` and `examples/crowd`.

### Changed

- `OneGame.Navigation` moves to OnePath 0.5.0-preview.1: searches run on `GridSearch`, path costs are OnePath's exact Q31.32 values, and `NavGrid.Stamp` is `Grid.ContentHash`. A `NavState` saved by 1.12 still loads; its map stamp differs, so a path or search from such a save is found again from where the unit stands.
- CI checks that root motion bakes to the same fixed-point numbers on Linux, Windows and WebAssembly, and runs the hot reload check on Windows as well as Linux.

### Fixed

- Web runs (`onegame web test`, the web export check) start headless Chrome reliably on a busy machine: Chrome picks its own debugging port, start-up waits longer and retries, and a failed start prints Chrome's own output.

## [1.12.0] - 2026-10-28

A 3D animation layer for characters: blend spaces, a state machine animator, root motion and two-bone IK on glTF clips. `RecursiveAI.OneGame.Navigation` moves to 1.12.0-preview.1.

### Added

- 3D animation layer in `OneGame.Gltf`: `Rig`, pose blending with masks (`PoseBlend`, `PoseMask`), `Crossfade`, 1D and 2D blend spaces, `Motion`s, two-bone IK (`TwoBoneIk`), and an `Animator` with parameters, triggers, states and transitions (conditions, exit time, crossfade). `SceneWriter.Model(name, transform, pose)` draws a model in an animated pose. See `docs/animation.md` and `examples/character`.
- Root motion: `RootMotion.Bake` turns a clip's root bone movement into a `RootMotionTrack` (new, in `OneGame.Core`), fixed-point steps per tick that sim code applies deterministically.
- Analyzer rule OG0023: animation types (`OneGame.Gltf`) cannot be used in sim code.

### Changed

- `OneGame.Gltf` now references `OneGame.Core` (for the root motion bake). No new package.

## [1.11.0] - 2026-10-28

Pathfinding for deterministic simulations, as a preview package.

### Added

- `RecursiveAI.OneGame.Navigation`: grid pathfinding on `Fixed` positions for sim code, built on OnePath 0.4. Published as a prerelease (`<engine version>-preview.N`), so no stable package depends on it. `NavGrid`, one-shot `FindPath`, and `NavState` (a synced value) with `NavSearcher` for searches spread over ticks, saved and restored with the world and covered by the state hash; walking with `NavState.Follow`; `NavRangeException` for positions beyond +-2^21. See `docs/navigation.md` and `examples/pathing`.
- `Draw2D.DebugVisible`: true while the development overlay is shown, so views can draw debug geometry such as navigation paths.
- Example `examples/pathing` in the gallery.
- A package check (`tools/ci/check-packages.py`) in the package scripts: every stable package is at the engine version and none depends on a prerelease.

### Changed

- The API reference shows `--prerelease` in the install line of a prerelease package.

## [1.10.0] - 2026-10-28

Encrypted, authenticated network links: every UDP and WebSocket connection is encrypted and the server is authenticated by its key, and a player keeps their identity across reconnects.

**Networking in 1.10 does not interoperate with 1.9 or earlier.** Every transport now needs a `Security` setting; use `NetSecurity.Insecure` only for tests and local tools.

### Added

- Encrypted, authenticated UDP and WebSocket links built on the Noise Protocol Framework (`Noise_IK_25519_ChaChaPoly_SHA256`) inside the existing cookie handshake, with ChaCha20-Poly1305 packets and records, authenticated disconnects, handshake rate limits per source and replay protection. X25519, ChaCha20-Poly1305 and HKDF are managed code in `OneGame.Core`, so links work everywhere the transport runs, including Android, iOS and the browser. Each UDP packet grows by 16 bytes. Design and threat model: [docs/interfaces/transport-security.md](docs/interfaces/transport-security.md).
- `NetSecurity` (`ForServer`, `ForClient`, `Insecure`, `AlsoTrusting`) on `UdpOptions.Security`, `WebSocketOptions.Security` and `ConnectOptions.Security`; `NetKeyPair`, `PeerKey`, `PeerIdentity`, `INetConnection.ChannelBinding` (for binding a login ticket to the session) and `PlayerRegistry`, which maps a client key to a player id that survives reconnects.
- `onegame keys` makes a key pair (`.key` private, `.pub` public). Server flags `--key` (repeatable, for rotation) and `--game-id`; client flags `--server-key` (several allowed), `--identity`, `--server-id` and `--game-id`. Both accept `--insecure`, which needs a development run (`ONEGAME_DEV=1`).
- Development runs need no setup: `onegame run --connect`, `onegame server run`, `onegame cluster`, test clusters and the samples make development keys and pass them on. Release builds refuse development keys and insecure links.
- A client's key is derived per server, so servers cannot link a player by key. `NetSecurity.ServerId` keeps a player's identity on a server across that server's key rotation.
- Fuzz targets for the handshake, AEAD open, encrypted UDP datagrams and secure WebSocket records. Test vectors from RFC 7748, 8439 and 5869, Project Wycheproof and the Noise vectors.

### Changed

- `WebSocketTransport.Listen` and `Connect` return interfaces.
- `onegame export` refuses to build when a content folder holds a private key file.
- Lanterns runs encrypted over UDP and WebSocket, and identifies players by key.
- OneRender, OneRender.Image and OneRender.Fonts 1.3.1 (with OneHtml 1.5.1). Settings slider thumbs have hover and active states, and pointer moves over a page allocate nothing.

## [1.9.0] - 2026-10-28

Hot reload of game code: edit C# while the game runs and keep the world.

### Added

- `onegame run` applies C# edits to a running Debug game through .NET hot reload (`dotnet watch` metadata deltas): view, sim and screen-builder method bodies, new methods and new synced members go live with the world kept, and open screens are rebuilt. An edit that cannot be applied live restarts the game and loads its world back; a compile error leaves the game on the last good code. `--no-hot` turns it off. See [docs/hot-reload.md](docs/hot-reload.md), including what a live sim edit means for replays and cluster hashes.
- `hot_reload` live-control operation (applied edits, last error, restarts, whether the run is still replay-safe) and `GameInstance.RefreshScreens()`.
- `tools/hot-reload/check.py`, an end-to-end check of hot reload that runs in the Linux package smoke.

### Changed

- OneRender, OneRender.Image and OneRender.Fonts 1.3.0 (with OneHtml 1.5.0).
- Solitaire's settings sliders are styled like the kit: a gold fill drawn by a value-driven gradient on the track, and a white thumb.

Releases and exports contain none of the hot-reload code.

## [1.8.0] - 2026-10-28

An API reference for every package, generated from the code and its documentation.

### Added

- API reference: [`docs/api/`](docs/api/index.md) documents the public API of every package (types, members, C# signatures, docs, examples and cross-links), generated from the built assemblies and their XML docs by `tools/OneGame.ApiDocs`. CI regenerates it and fails when it is out of date. README, the first-game guide and the other guides link to it.
- [`docs/api/sdk.md`](docs/api/sdk.md): every MSBuild property and item a game project can set on `RecursiveAI.OneGame.Sdk`, with defaults and effects.

### Changed

- XML documentation rewritten for the reference across the game-facing runtime (`GameDefinition`, `GameApp`, `GameInstance`, `Entity`, `World`, `Ui`, `UiKit`, `Draw2D`, `SpriteSheet`, `Camera2DRig`, `ActionMap`, `AudioService`, `Cluster` and more), the test API (`TestCluster`, `TestClient`, scenarios, `ProcessCluster`, `TestServer`), the control and live layers, the server host and plugin API, the 3D renderer, glTF loading and the GPU device. Entry points gain compiled examples; parameters, units, ranges, exceptions and the sim-versus-view rules are stated.

### Fixed

- Process clusters (`OneGame.Testing`) no longer fail when a server's port is taken between choosing it and binding it, or when a long test process runs out of fresh port numbers: a start that loses the race or finds no free port is retried on new ports, a restarted node retries its own port, and a cluster gives its ports back when it goes down. `onegame web` picks another port when its listener's port is taken.

## [1.7.0] - 2026-10-28

Every export works from packages: a game made by `onegame new` from the installed tool exports for the web, Android (with Google Play Billing), iOS and the plugin-capable server with no engine checkout.

### Added

- `onegame new --server` also writes `plugin/`, a server plugin project (the SDK's new `OneGameKind` `Plugin`) and a configuration that loads it.
- CI jobs `packages-web`, `packages-android`, `packages-ios` and `packages-server` build each export from packages alone; the web page and the iOS Simulator app end in the same world hashes as the desktop run, and an exported client agrees with the exported plugin host.

### Changed

- The `onegame` tool carries the web page and script files, the Play Billing Java wrapper and the plugin-capable server host (`server-host/`), so `onegame export --target web`, `onegame web test`, `export --target android` with `AndroidPlayBilling`, `onegame server run` and `export --server --plugins` no longer need an engine checkout.
- Web, Android and iOS exports pack the game's `fonts/`, `art/` and `data/` folders with the same filter as the desktop export, and the game reads them as loose files (the web page and the APK unpack them at start), so games that load files from disk run unchanged on every target.
- Device scripts in `tools/devices/` tear down everything they start on finish, failure, Ctrl-C and SIGTERM (apps, installed packages, port forwards, tunnels, transient units, background commands); `android.sh` no longer removes every forward on the adb server and restores the stay-awake setting.
- The package smoke scripts install the tool from the local feed explicitly.
- OneRender, OneRender.Image and OneRender.Fonts 1.2.0 (with OneHtml 1.4.0). Solitaire's settings controls take their colours from `color-scheme: dark` and `accent-color` instead of a CSS filter.

## [1.6.1] - 2026-10-28

### Changed

- Solitaire's settings page styles its native controls dark and gold like the kit (dark tracks with a gold fill, gold rings and dots), with CSS on the page only.

## [1.6.0] - 2026-10-28

Pages: a game screen can be an HTML/CSS page or a style-tree page from OneRender, drawn through the 2D renderer.

### Added

- `OneGame.Pages` (a new package): OneRender pages (`PageScreen.FromHtml` / `FromTree`, `game.Pages`) drawn through the render graph. Images, SVG (through the art atlas) and glyphs (through the glyph atlas, sharing faces with the game's text) go through the engine's atlases; host scopes place a page under or over the 2D pass; filters, masks, backdrop filters and blend groups run as render-graph passes. Input (pointer, keyboard, wheel, controller focus) goes to a focused page first, and CSS transitions run on view time. No managed allocation per steady frame.
- Harness: `pages` control operation; `ui_query`, `ui_click` and `ui_invoke`, and the scenario steps `click:`, `type:` and `ui:`, find page elements by name.
- Analyzers: lambdas, local functions and method groups registered as OneRender DOM event listeners are checked as view code (OG0008).
- `GameInstance.Fonts`; `Renderer2D.FrameNumber` is public.
- User guide: `docs/pages.md`.

### Changed

- Solitaire's settings screen is an HTML page, with live slider readouts; it runs on desktop and in the browser.
- OneRender 1.1.0 (`SetTextContent` without allocation; class and attribute edits).

## [1.5.2] - 2026-10-27

Android audio on the device package, and fixes from real Windows hardware.

### Changed

- Android audio output runs on `RecursiveAI.OneAudio.Devices` 1.3.0's AAudio backend: data-callback streams, device-clock frame counters, reopen on route change. `AAudioBackend` stays as a name for it; the engine's own AAudio interop is removed. On Android `device.deviceState` reads `running` (was `started`).
- `RecursiveAI.OneAudio` and `RecursiveAI.OneAudio.Devices` updated to 1.3.0.

### Fixed

- Render3D: a scene with no point or spot lights read light counts from a buffer nobody wrote, or from an earlier frame. On some drivers that looped over garbage and hung the frame; with stale counts it lit the scene with lights that were gone. The shader now ignores the buffer unless the cluster pass ran.
- Vulkan: pooled command encoders are pre-sized for a busy frame, so steady frames no longer allocate a few hundred bytes to 2 KB now and then on slower GPUs.
- Tests run on Windows hardware: an edge-sharpness check accepts a sharper result than the reference, and the cluster-kill test no longer needs `sleep`.

### Added

- Device checks for Windows (`tools/devices/windows.sh`); the live check is strict about device counters on every backend, WASAPI included.

## [1.5.1] - 2026-10-27

Fixes from building a game from the packages alone.

### Changed

- Linux NativeAOT exports link against glibc 2.31 by default (a build sysroot is fetched once, checked against a pinned hash and cached), so they run on SteamOS, the Steam Linux Runtime and the Steam Deck when built on a current system. `--host-glibc` links against the host instead; `--sysroot <dir>` uses your own.
- An export that needs a newer glibc than 2.31 now fails with `glibc_too_new` instead of warning.
- Games using NuGet central package management (`Directory.Packages.props`) build with the SDK: the engine packages are versioned with `VersionOverride`.

### Added

- `ArtAtlas.Preload`, `PreloadFitted`, `PumpPreload`, `CompletePreload`, `ArtPreload`, `BackgroundThreads`, `PreloadFrameBudget` and the `SyncRasterizations`/`PreloadRasterizations` counters: art can be decoded and rasterized before its first draw, off the frame thread (spread over frames where there are no threads). `GameInstance.Art` exposes the game's atlas.
- Docs: detecting the Steam Deck, and the deprecation of `IsSteamRunningOnSteamDeck`.

## [1.5.0] - 2026-10-27

An examples gallery and a debug overlay.

### Added

- `examples/`: six runnable one-feature games (sprite, input actions, tween, particles, saves, a networked counter) with scenarios, built with the solution and run by CI through `tools/ci/examples.sh`.
- An on-screen debug overlay in development builds, toggled with F3 or Back+Start: frame time, tick and entities, net transport, round trip, bytes per second and drift, audio device counters. `overlay` control operation and `overlay: on|off|toggle` scenario step; `ONEGAME_OVERLAY`, `ONEGAME_OVERLAY_KEY` and `ONEGAME_OVERLAY_CHORD` configure it.

### Changed

- Exports fail with `dev_tools_present` if overlay code is linked in; exports and release builds do not contain it.

## [1.4.0] - 2026-10-27

Game feel and persistence: tweens and style transitions, particles in 2D and 3D, and a storage service for save slots, settings and files, in the browser too.

### Added

- Tweens: `OneGame.Tweens` with `Easing` (the CSS timing functions, cubic-bezier and steps, plus named in/out curves), `TweenSet`/`Tween` with sequences, parallel groups, delay, repeat and yoyo, callbacks and cancel; no allocation per frame. `game.Tweens` runs them on view time, so the simulation and replays are unaffected.
- The style `transition` property now animates, following CSS Transitions.
- Particles: `OneGame.Particles` emitters (a new package) with `Draw2D.Particles` and `SceneWriter.Particles`, built on the new `Renderer2D.AdditiveMaterial` and 3D camera-facing billboards (`BillboardBatch3D`); seedable, view-only, no allocation per frame.
- Storage: `game.Storage` gives named save slots built on world saves (label, kind, time and game version for a load screen), quick-save, rotating autosave, typed settings (`SettingsSchema<T>`) and plain files (`IByteStore`). Writes are atomic; readers check limits before allocating.
- Browser persistence through IndexedDB for saves, settings and files.
- Harness: `tweens`, `particles` and `saves` control operations; scenario steps `tweens:` and `saves:` and expectations `tweens:`, `particles:` and `saves:`; `--data` and `--persist-data` arguments; `onegame web test --profile --port`.
- User guides: `docs/tweens.md`, `docs/particles.md`, `docs/saves.md`.

### Changed

- OneShader 1.3.0. Layouts come from `ToBindings`, `UnionOptions` and vertex stream splitting alone (`ShaderLayouts.Bindings(..., options)`, `ShaderLayouts.VertexStreams`); the IBL passes share one layout and the 3D instance vertex layout is derived from the shader.
- The solitaire and bricks samples use the engine storage service in place of their own file stores.

## [1.3.0] - 2026-10-27

Audio devices from the one* audio library, shader layouts from the one* shader library, and user guides for the 1.2 happy paths.

### Changed

- Audio output on Linux, Windows, macOS and iOS runs on `RecursiveAI.OneAudio.Devices` 1.2.1: device-clock frame counters, default-device following, reopen after a lost device. The browser backend plugs into the same device interface as a manually driven device (no threads). `AlsaBackend`, `WasapiBackend` and `AudioUnitBackend` remain as names for the package backends; `device.deviceState` reads `running` or `suspended`. Android keeps its own AAudio path for now.
- The harness's audio capture runs on the package's manual device; its device report now shows real frames played.
- OneShader 1.2.0. `ShaderLayouts.Bindings` builds one layout over several entry points with set widening and dynamic bindings, and `ShaderLayouts.Vertex` derives vertex layouts with format overrides; the 2D renderer's binding and vertex tables and the 3D mesh vertex layout are now derived from the shaders.

### Removed

- The engine's own ALSA, WASAPI and Audio Unit/Objective-C output code.

### Added

- User guides: `docs/2d.md`, `docs/input.md`, `docs/assets.md`.

## [1.2.0] - 2026-10-27

Happy-path layers over the low-level API: a first game now has a moving sprite, action-mapped input and a hot-reloaded asset in about 90 lines.

### Added

- 2D drawing from view code: entities implementing `IView2D` are drawn by the default `Render2DStage` through `Draw2D` (sprites, sprite-sheet frames and clips, rectangles, lines, circles, text), batched onto the existing `Scene2D`. `SpriteSheet`, `SpriteClip` (frame animation on view time) and `Camera2DRig` (follow, smoothing, bounds, zoom, screen and world conversion). `UiCanvas.Text` and `TextQuadBuilder.BuildLine` draw text outside a UI list. Control operation `draw_view`.
- `Draw2D.Texture(path)` and `d.Sprite("art/ship.png", ...)` draw from an asset path; a grid `SpriteSheet` over an asset texture is cut again when a reload changes the image size.
- Input actions: `ActionMap` with contexts, bindings, composites (`Wasd`, `Arrows`, sticks, pairs) and queries, declared in `GameDefinition.DefineActions` and read from `GameInstance.Actions`; rebinding with a bindings file and `ActionUi`; `SteamActionSource`. Control operations `actions` and `input_pad`, scenario steps `action:` and `pad:`, `press:` with `state`, and the `action:` expectation.
- `Assets`: cached, reference-counted handles for textures, SVG, YAML data, sounds, models and fonts, background loads and size limits; `GameDefinition.CreateAssetSource`; development hot reload (textures, sounds and models swap in at once; YAML data is staged and applied at a boundary); the `assets` operation, `assets:` step and `asset:` expectation; `Renderer2D.ReplaceTexture`.
- `onegame new` writes a ship game: a `Ship` entity with fixed-point position drawn from `art/ship.png`, `DefineActions` for move and boost, a scenario using `action:`, and a capture scenario with its reference. `--server` keeps the counter scaffold.
- `tools/OneGame.Corpus` (`onegame-corpus`): a seeded, deterministic corpus of real frames, snapshots, saves, recordings and transfer bundles from Lanterns, Bricks and Solitaire, for compression work.

### Changed

- OneShader 1.1.0. `ShaderSource.Layout` replaces the `Bindings`, `ComputeGroup*`, `Msl*` and `Wgsl*` members (breaking for a hand-built `ShaderSource`); `ShaderLayouts.Bindings` and `ShaderLayouts.PushConstants` expose the generated layouts.

## [1.1.2] - 2026-10-27

### Added

- `OneGame.Platform.RunningOn` (`Android`, `IOS`, `Browser`, `Phone`, `Mobile`, `Desktop`): one way for a game to ask what it runs on.

### Fixed

- Android builds reported themselves as Linux to platform checks (`OperatingSystem.IsAndroid()` is false in a NativeAOT Android app); the engine now asks `RunningOn`, and Bricks hides Quit on Android. Scenario captures on Android use `name-mobile.png` references like iOS and the browser.

## [1.1.1] - 2026-10-26

### Fixed

- Linux audio output: the ALSA sink never reported its device side, so the `audio` report showed `framesWritten`, `framesPlayed` and `peakPlayed` as 0 on every ALSA device even while sound was playing, and a device that really took nothing could not be told apart. It now reports frames written, frames the device has consumed (from `snd_pcm_delay`), the peak of consumed frames, its PCM state and underrun count.
- The device checks in `tools/devices` now fail unless `framesWritten` and `framesPlayed` are above 0, not on a `Running` state alone.

## [1.1.0] - 2026-10-26

A game builds from nuget.org alone, with no engine checkout.

### Added

- `RecursiveAI.OneGame.Cli`: the `onegame` command as a framework-dependent dotnet tool (`dotnet tool install -g RecursiveAI.OneGame.Cli`).
- `RecursiveAI.OneGame.Sdk`: an MSBuild SDK for games. It sets the target framework and the rules, references the engine libraries at its own version, adds the development tools in Debug builds, and carries the export targets and entry files. `onegame new` writes a project on it (`<Project Sdk="RecursiveAI.OneGame.Sdk/1.1.0">`), and with `--server` a dedicated-server project (`<OneGameKind>Server</OneGameKind>`).
- `onegame export --target linux|win` as short names for `linux-x64` and `win-x64`.
- `tools/ci/package-smoke.sh` and a CI job that pack the engine into a local feed, install the tool from it and run new, run, test and a NativeAOT export on Linux and Windows.

### Changed

- Games inside an engine checkout (`--engine`, `ONEGAME_ROOT`, the samples) build against project references as before.
- Not yet from packages: the web export, Play Billing in Android builds and plugin servers need a game created with `--engine`.

## [1.0.0] - 2026-10-25

The first release: a C# game engine with no editor, driven from a terminal.

### Programming model

- Entities are partial classes holding synced state, commands, deterministic simulation and `[View]` members. Source generators emit change tracking, binary codecs, deltas, replica types, versioned snapshots and command stubs.
- Analyzers make the rules compile errors: no floats, wall-clock reads, threads, I/O or hash-order iteration in simulation code; views read state and send commands but never write it.
- Fixed-point numbers, named random streams and `Timed<T>` interpolation values.

### Sync and networking

- Nodes, scopes, authorities and subscriptions; per-connection visibility (everyone, owner, team, hidden) enforced at the byte level; drift hashes always on.
- Prediction with rollback, atomic authority transfer, relaying and escrow for changes that span scopes.
- Reliable UDP and WebSocket transports, reconnection, dedicated server hosts and hot-reloadable server plugins.
- Single-player is a one-node cluster: saves are snapshots and the action log is the command log, so replays are bit-exact.

### Rendering

- A GPU layer over Vulkan (Windows, Linux, Android), Metal (macOS, iOS) and WebGPU (browsers), with a render graph.
- A 2D renderer (sprites, tilemaps, parallax, custom materials) and a 3D renderer (glTF 2.0 metal/rough PBR, clustered forward lighting, image-based lighting, cascaded shadows, SSAO, bloom, tone mapping, instancing, a quality ladder).
- HLSL shaders compiled during the build with generated C# bindings.
- glTF 2.0 import with limits checked before allocating.

### UI

- A retained UI tree styled on the CSS model, flexbox and anchor layout, controller focus navigation, input glyphs, world-anchored UI and tweens.
- A component kit: table, slider, tabs, tooltip, list, card, nine-slice frame and text input.
- Text shaping through OneFont; SVG and images through OneImage.

### Platforms

- Windows (Win32, XInput, WASAPI), Linux (Wayland and X11, evdev pads, ALSA), macOS (Cocoa), iOS (UIKit), Android (native activity) and the browser (.NET on WebAssembly).
- Steamworks and Steam Input bindings, StoreKit and Google Play Billing bindings, and a scriptable fake store.
- `onegame export` for Windows, Linux, macOS, iOS, Android and the web: NativeAOT where the host allows it.

### Harness

- The `onegame` command (`new`, `build`, `run`, `test`, `capture`, `replay`, `export`, `cluster`, and more) with JSON output.
- A C# test API (`TestCluster`, scenarios, captures, recordings) and live control over MCP in development builds; release builds leave the listener out.

### Samples

- Solitaire: a UI-heavy 2D game with scenarios, replays and captures.
- Lanterns: two island servers and a master, entity hand-off, escrowed trades, predicted movement, owner-only inventories, in 2D and 3D.
