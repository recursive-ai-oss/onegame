[API reference](../index.md) › OneGame.Host

# OneGame.Host

The game host: window, input, fixed-step simulation, UI and render stage for one game, windowed or headless.

**Package:** `RecursiveAI.OneGame.Host`

```sh
dotnet add package RecursiveAI.OneGame.Host
```

## OneGame.Host

| Type | Kind | Description |
| --- | --- | --- |
| [`ActionUi`](OneGame.Host/ActionUi.md) | static class | Ties the action layer to the UI kit: glyph tables that follow the current bindings, and a ready-made rebinding list. |
| [`Asset`](OneGame.Host/Asset.md) | class | A loaded file. |
| [`AssetEvent`](OneGame.Host/AssetEvent.md) | record | One thing that happened to an asset, for the control layer and tests. |
| [`AssetException`](OneGame.Host/AssetException.md) | class | A file could not be loaded or reloaded. |
| [`AssetKind`](OneGame.Host/AssetKind.md) | enum | What kind of content an [`Asset`](OneGame.Host/Asset.md) holds. |
| [`Asset<T>`](OneGame.Host/Asset-1.md) | class | A loaded file of type `T`. |
| [`Assets`](OneGame.Host/Assets.md) | class | The asset service: loads files from the game's content folders into the types the engine draws, plays and reads, caches them by path and, in development, swaps an edited file in under the existing handle. |
| [`BrowserByteStore`](OneGame.Host/BrowserByteStore.md) | class | The browser's IndexedDB as an [`IByteStore`](../OneGame.Core/OneGame.Storage/IByteStore.md). |
| [`BrowserWebSocketConnection`](OneGame.Host/BrowserWebSocketConnection.md) | class | A client connection over the browser's WebSocket, behind the same interface as the native transports, so a web build subscribes to a server's scopes exactly like a desktop client does. |
| [`CapturedFrame`](OneGame.Host/CapturedFrame.md) | record | A captured frame: RGBA8 pixels, top row first. |
| [`ConnectOptions`](OneGame.Host/ConnectOptions.md) | record | Where a game connects instead of running its own world: a server address and how. |
| [`DisplayListRaster`](OneGame.Host/DisplayListRaster.md) | static class | A small CPU rasterizer for display lists: flat rectangles, borders and one bar per text line, with clips and group opacity. |
| [`Draw2D`](OneGame.Host/Draw2D.md) | class | Immediate-mode 2D drawing for view code: call it every frame to say what to show, and the stage batches it onto the same [`Scene2D`](../OneGame.Render2D/OneGame.Render2D/Scene2D.md) and [`Renderer2D`](../OneGame.Render2D/OneGame.Render2D/Renderer2D.md) a hand-built stage would use. |
| [`DriftInfo`](OneGame.Host/DriftInfo.md) | record | One drift report: a replica whose state differed from the server's at the same tick. |
| [`ExitCodes`](OneGame.Host/ExitCodes.md) | static class | Process exit codes shared by games and the `onegame` command. |
| [`FileAssetSource`](OneGame.Host/FileAssetSource.md) | class | Reads content from folders. |
| [`FixtureSet`](OneGame.Host/FixtureSet.md) | class | The named fixtures of a game. |
| [`FollowedScope`](OneGame.Host/FollowedScope.md) | record | One scope a client follows besides its main one (served by its server's relay), for `net_status`. |
| [`FrameInfo`](OneGame.Host/FrameInfo.md) | struct | What a render stage is given for one frame. |
| [`GameApp`](OneGame.Host/GameApp.md) | static class | The entry point of a game: parse the command line, create the instance, run it. |
| [`GameArgs`](OneGame.Host/GameArgs.md) | class | The command line of a game: `--name value`, `--name=value` and bare `--flag`. |
| [`GameDefinition`](OneGame.Host/GameDefinition.md) | class | What a game is, as far as the host cares: its entity types, how its world starts, its screens and its named fixtures. |
| [`GameInstance`](OneGame.Host/GameInstance.md) | class | One running game: the world, the UI tree, the window and event queue, and the render stage, advanced one frame at a time. |
| [`GameOptions`](OneGame.Host/GameOptions.md) | record | Options of a [`GameInstance`](OneGame.Host/GameInstance.md). |
| [`GameStorage`](OneGame.Host/GameStorage.md) | class | A game's saves and settings: the happy path over [`IByteStore`](../OneGame.Core/OneGame.Storage/IByteStore.md), [`SaveSlots`](../OneGame.Sync/OneGame.Storage/SaveSlots.md) and [`SettingsFile<T>`](../OneGame.Core/OneGame.Storage/SettingsFile-1.md). |
| [`GpuMode`](OneGame.Host/GpuMode.md) | enum | How the host picks a graphics device. |
| [`HostResources`](OneGame.Host/HostResources.md) | record | What the host has ready when it asks the game for a render stage. |
| [`HotShader`](OneGame.Host/HotShader.md) | record | One shader a game wants to see edited while it runs: the HLSL file, the entry point of its pixel stage, the interface the game was built against and what to do with a recompiled version. |
| [`HotShaders`](OneGame.Host/HotShaders.md) | class | The shaders of a running game that may be recompiled when their files change. |
| [`IAssetSource`](OneGame.Host/IAssetSource.md) | interface | Where an [`Assets`](OneGame.Host/Assets.md) service reads content from. |
| [`IDevTools`](OneGame.Host/IDevTools.md) | interface | The hook through which development builds add their tools (live control, scenarios, capture, replay). |
| [`IFrameOverlay`](OneGame.Host/IFrameOverlay.md) | interface | Something drawn on top of the finished frame, after the game's UI and sprites: the development overlay is the one user. |
| [`IRenderStage`](OneGame.Host/IRenderStage.md) | interface | Draws one frame. |
| [`IScene3DStage`](OneGame.Host/IScene3DStage.md) | interface | What a render stage that draws a 3D scene shows, for tools that ask about it (the `camera` and `pick` operations). |
| [`IView2D`](OneGame.Host/IView2D.md) | interface | Implemented by an entity that draws itself in 2D. |
| [`IView3D`](OneGame.Host/IView3D.md) | interface | Implemented by an entity that shows itself in 3D. |
| [`KeyNames`](OneGame.Host/KeyNames.md) | static class | Key names: the platform's physical [`Key`](../OneGame.Platform/OneGame.Platform/Key.md) to the DOM-style key strings the UI uses, and back. |
| [`LoadedFont`](OneGame.Host/LoadedFont.md) | record | A font added to a library by [`Assets.LoadFont()`](OneGame.Host/Assets.md#m-loadfont-string-fontlibrary). |
| [`ModelLibrary`](OneGame.Host/ModelLibrary.md) | class | The models a game's entities can show, by name. |
| [`NetStatus`](OneGame.Host/NetStatus.md) | record | A snapshot of a client's connection, for `net_status`. |
| [`OverlayCanvas`](OneGame.Host/OverlayCanvas.md) | class | The few drawing calls an [`IFrameOverlay`](OneGame.Host/IFrameOverlay.md) has: filled rectangles and one-line text, in framebuffer pixels. |
| [`PickHit`](OneGame.Host/PickHit.md) | struct | What a pick found. |
| [`PickRay`](OneGame.Host/PickRay.md) | struct | A ray in world space. |
| [`PlatformEventFilter`](OneGame.Host/PlatformEventFilter.md) | delegate | A look at a platform event before the game handles it. |
| [`Png`](OneGame.Host/Png.md) | static class | PNG reading and writing for captures, through OneImage. |
| [`RecordingFile`](OneGame.Host/RecordingFile.md) | static class | Reads and writes recording files. |
| [`RemoteSession`](OneGame.Host/RemoteSession.md) | class | A game's connection to a server: a node holding one replica scope, reached through a transport connection ([`Node.ConnectVia()`](../OneGame.Sync/OneGame.Sync/Node.md#m-connectvia-imessagechannel-string)). |
| [`Render2DStage`](OneGame.Host/Render2DStage.md) | class | The default render stage: the 2D renderer draws the UI display list into the host's target. |
| [`Render3DStage`](OneGame.Host/Render3DStage.md) | class | A render stage that draws a 3D scene first and the 2D scene and UI over it. |
| [`Resolution`](OneGame.Host/Resolution.md) | static class | Named capture sizes. |
| [`SceneView3D`](OneGame.Host/SceneView3D.md) | class | Turns a world of [`IView3D`](OneGame.Host/IView3D.md) entities into a 3D scene and answers questions about it: which camera is in use, where a world point lands on screen, which entity is under a screen point. |
| [`SceneWriter`](OneGame.Host/SceneWriter.md) | class | What an entity's [`IView3D.Describe3D()`](OneGame.Host/IView3D.md#m-describe3d-scenewriter) writes into: models, lights and cameras for one frame. |
| [`ScreenSet`](OneGame.Host/ScreenSet.md) | class | The named screens of a game. |
| [`SessionRecorder`](OneGame.Host/SessionRecorder.md) | class | Records a session, following the game across world replacements and state patches. |
| [`SessionRecording`](OneGame.Host/SessionRecording.md) | class | A whole session: one [`Recording`](../OneGame.Sync/OneGame.Sync/Recording.md) per world the game ran on. |
| [`SessionReplayResult`](OneGame.Host/SessionReplayResult.md) | record | The outcome of replaying a [`SessionRecording`](OneGame.Host/SessionRecording.md). |
| [`StubStage`](OneGame.Host/StubStage.md) | class | The stand-in stage: the GPU clears the target to a flat color, and for captures the UI's rectangles, borders and text lines are painted over the read-back pixels by a small deterministic CPU rasterizer. |
| [`UnknownModelException`](OneGame.Host/UnknownModelException.md) | class | A model name that no [`ModelLibrary`](OneGame.Host/ModelLibrary.md) entry has. |
| [`ViewCamera`](OneGame.Host/ViewCamera.md) | struct | A camera as resolved for a frame: where it is and how it projects. |
| [`WorldAnchor`](OneGame.Host/WorldAnchor.md) | static class | Pins UI elements to points in a 3D world. |
