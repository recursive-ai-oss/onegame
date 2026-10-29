# Assets

`OneGame.Host.Assets` loads files from the game's content folders into the types the engine draws, plays and reads, caches them by path, and in development swaps an edited file in under the existing handle. It sits on the low-level loaders (OneImage, OneYaml, OneAudio, `GltfLoader`, `FontLibrary`), which stay usable directly. Code: `src/OneGame.Host/Assets/` (loading and reload logic), `src/OneGame.Control/ControlSurface.Assets.cs` (the file watcher and the `assets` operation).

A game gets one as `GameInstance.Assets`; the 2D stage attaches its renderer when it makes one. Outside a host, `new Assets(source)` works for everything but textures, which need `AttachRenderer`.

```csharp
var ship = assets.LoadTexture("art/ship.png");                           // Asset<Texture2D>
var ships = assets.LoadData("data/ships.yaml", GameYaml.Strict.Ships);   // Asset<Ships>, YAML into your type
var hit = assets.LoadSound("data/hit.ogg");                              // Asset<Sound>
sprite = Sprite.Create(ship.Value, pos);                                 // draw with the texture itself
```

## Loading

| Call | Gives | Notes |
|---|---|---|
| `LoadTexture(path, filter, background)` | `Asset<Texture2D>` | PNG, JPEG, WebP and the rest of OneImage's rasters; sRGB straight alpha like `Renderer2D.CreateTexture` |
| `LoadSvg(path, w, h, background)` | `Asset<Texture2D>` | drawn at that pixel size; another size is another asset |
| `LoadData<T>(path, typeInfo, background)` | `Asset<T>` | `typeInfo` is the generated `YamlTypeInfo<T>` of a `[YamlSerializable]` context; make the context with `UnmappedMemberHandling = Disallow` to reject unknown keys |
| `LoadSound(path, background)` | `Asset<Sound>` | needs an `AudioService` (`AudioService.Active`) |
| `LoadModel(path, background)` | `Asset<GltfModel>` | `.glb`, or `.gltf` with buffers and images beside it |
| `LoadFont(path, library)` | `Asset<LoadedFont>` | adds the face to the library; not hot reloaded |

Paths are relative to a content folder (`art/ship.png`, `data/ships.yaml`) with `/` separators and may not contain `..` or be absolute (`bad_path`). Failures are `AssetException` with a `Code`: `not_found`, `bad_path`, `too_large`, `bad_format`, `no_renderer`, `no_audio`. A foreground load that fails throws and caches nothing.

**Handles and lifetime.** Loading the same path (same filter, size or type) twice returns the same handle and counts a reference; `asset.Release()` gives one back, and at zero the asset leaves the cache and its texture is freed. Handles carry `Path`, `Kind`, `Version` (1, then +1 per applied reload), `Error`, `IsReady`, `IsPending`, `Digest`.

**Background loads.** `background: true` returns at once with `IsReady == false`; the file is read and decoded on another thread (sounds are decoded on the caller's thread when finished). `Assets.Update()` finishes completed ones (the host calls it every frame); reading `Value` finishes one by waiting for it. `asset.Ready` is a `Task` that faults with the `AssetException` on failure. Use it for big files, such as models or a level's art during a loading screen.

**`Value` for textures** is always the same `Texture2D`, updated in place by `Renderer2D.ReplaceTexture` on a reload, so sprites that hold the texture draw the new image from the next frame. Other kinds return the current object: hold the handle and read `Value` when you use it.

## Where files come from

`GameDefinition.CreateAssetSource()` returns an `IAssetSource` (`Read(path, maxBytes)`, `PhysicalPath(path)`). The default `FileAssetSource` tries, in order: the program's folder (an export keeps `art/` and `data/` next to the program, as `docs/exporting.md` says), `../Resources` (macOS bundle), the working directory (development, run from the project). So a development run edits the files in the project and an export reads its own copies; the game code is the same.

On Android, iOS and the browser the export packs `fonts/`, `art/` and `data/` and unpacks them as loose files at start, so the same `FileAssetSource` and the same game code work on every target. A game can still return its own `IAssetSource` (embedded or fetched content).

## Limits

Each file is checked against a size limit before it is read into memory: images 32 MiB, SVG 4 MiB, YAML 8 MiB, sounds 64 MiB, glTF `GltfLimits.MaxFileBytes`, fonts 64 MiB (`Assets.MaxImageBytes`, `MaxDataBytes`, `MaxSoundBytes` change the first three). Decoders run with the art atlas's budgets (8192 px per side and 16 M pixels for rasters), the glTF importer's own limits and OneYaml's. Damaged files are `bad_format`, never a crash. No new parser was added: the layer only calls the existing, already fuzzed ones, plus a seeded mutation test over PNG, glb and YAML.

## Hot reload (development builds only)

In a Debug build the control layer's `AssetReloader` compares each loaded file's write time and length with what was last read (every 250 ms from the live loop, and on every `capture_frame` and `assets` call) and calls `Assets.Reload(asset)`. A changed texture, SVG, sound or model is decoded and swapped in under the same handle; the next frame shows it, the tick carries on. **A failed reload** (damaged PNG, malformed YAML, a key the type does not have) leaves the old content in use, sets `Asset.Error`, and appears as a `failed` event; fixing the file reloads it. The watcher lives in `OneGame.Control`, which Release builds and exports leave out (an export has no `AssetReloader`; `devTools:false`), so nothing in a shipped game watches files. The glTF model's external buffers and textures are not watched, only the model file.

**Data and determinism.** Sim code reads YAML data, so swapping it mid-run would change behaviour and break replays and drift hashes silently. The rule: **a changed data file is read and checked at once (so mistakes show immediately) but only staged**. State `pending`, event `pending`, `Value` unchanged. It is swapped in by `Assets.ApplyPendingData()`: the `assets` operation with `apply_data=true`, the `assets: apply-data` scenario step, and automatically at the start of every scenario run (a new world, a boundary). The game decides when else; do not call it while a recording runs. `Assets.DataDigest` (also in the `assets` result) is a digest of all data files' bytes: record it with a replay or compare it in a test to know which data a run used. Textures, sounds and models are view-side and swap at once.

Shader hot reload (`HotShaders`) is left as it is: it needs a compile step and an interface check the asset path does not have.

## Harness

- Control op **`assets`**: `assets` (path, kind, version, references, state `ready|loading|pending|error|failed`, digest, file, error), `events` (`loaded|reloaded|failed|pending|applied|released` with sequence), `lastSequence`, `dataDigest`, `changed`, `applied`. Arguments: `check` (default true, read changed files now), `reload` (a path), `apply_data`, `since`.
- Scenario step **`assets: check | apply-data | {reload: path}`** and expectation **`asset: {path, state, version, error}`** (`error` matches a part of the message).
- C#: `Assets.Events`, `EventRaised`, `Items`, `Reload`, `ApplyPendingData`; `AssetReloader(assets).Check()` for a test or tool without a control surface.
