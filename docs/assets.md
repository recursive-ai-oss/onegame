# Assets

`game.Assets` loads files from your content folders (`art/`, `data/`, `fonts/`) into the types the engine draws, plays and reads. Loads are cached by path, textures are uploaded for you, and in development an edited file replaces what is loaded while the game runs.

```csharp
var ship = assets.LoadTexture("art/ship.png");                         // Asset<Texture2D>
var ships = assets.LoadData("data/ships.yaml", GameYaml.Strict.Ships);  // Asset<Ships>: your own type
var hit = assets.LoadSound("data/hit.ogg");                            // Asset<Sound>
var model = assets.LoadModel("art/ship.glb");                          // Asset<GltfModel>

var texture = ship.Value;                                              // read through the handle when you use it
```

For drawing you rarely need to call this: `d.Sprite("art/ship.png", ...)` and `d.Texture(path)` in `Draw2D` load through the same service ([2d.md](2d.md)). Paths are relative to a content folder, written with `/`, and may not be absolute or contain `..`.

## What loads

| Call | Gives |
|---|---|
| `LoadTexture(path, filter)` | `Asset<Texture2D>`: PNG, JPEG, WebP and other rasters. `TextureFilter.Nearest` for pixel art |
| `LoadSvg(path, width, height)` | `Asset<Texture2D>`: an SVG drawn at that pixel size, so it is crisp; another size is another asset |
| `LoadData<T>(path, typeInfo)` | `Asset<T>`: YAML into your own type |
| `LoadSound(path)` | `Asset<Sound>`: needs the game's audio service |
| `LoadModel(path)` | `Asset<GltfModel>`: `.glb`, or `.gltf` with its buffers and images beside it |
| `LoadFont(path, library)` | `Asset<LoadedFont>`: adds the face to a `FontLibrary`; not hot reloaded |

**YAML data** uses the metadata an OneYaml context generates. Make the context reject keys your type does not have, so a typo is an error and not a silent default:

```csharp
public sealed record Ships(string Name, int Gold);

[YamlSerializable(typeof(Ships))]
internal sealed partial class GameYaml : YamlSerializerContext
{
    internal static GameYaml Strict { get; } = new(new YamlSerializerOptions { UnmappedMemberHandling = YamlUnmappedMemberHandling.Disallow });
}
```

A file that is missing, too large (images 32 MiB, SVG 4 MiB, YAML 8 MiB, sounds 64 MiB), or damaged throws `AssetException` with a `Code` (`not_found`, `bad_path`, `too_large`, `bad_format`, `no_renderer`, `no_audio`) and caches nothing.

## Handles and release

Loading the same path again returns the same handle and counts a reference; `asset.Release()` gives one back. At zero the asset leaves the cache and its texture is freed. A handle has `Version` (1, then one more for each reload that was applied), `Error`, `IsReady` and `IsPending`. A texture handle's `Value` is always the same `Texture2D`, updated in place, so sprites that hold it show new pixels from the next frame. For other kinds hold the handle and read `Value` when you use it, because reload replaces the object.

## Big files: background loads

```csharp
var level = assets.LoadModel("art/level.glb", background: true);   // returns at once
// ... show a loading screen; the host finishes completed loads every frame
if (level.IsReady) Use(level.Value);
await level.Ready;                                                 // a Task; faults with the AssetException
```

The file is read and decoded on another thread. Reading `Value` before it is ready waits for it. Outside a host, call `assets.Update()` to finish loads yourself.

## Hot reload (development builds)

Run with `onegame run`, edit a file in the project, save. The next frame shows it and the game keeps running.

- **Textures, SVG, sounds and models** swap in at once, under the same handle. Voices already playing keep the old clip.
- **YAML data is staged, not swapped.** The sim reads it, so changing it mid-run would change what the game does and silently break replays and drift checks. A changed file is read and checked straight away (a mistake shows at once), then waits: state `pending`, `Value` unchanged. It is applied at a boundary you choose: `assets.ApplyPendingData()`, the `assets: apply-data` scenario step, or automatically when a scenario starts. Never call it while a recording runs. `assets.DataDigest` is a digest of all data files, so a replay can record which data it ran with.
- **A failed reload keeps the old asset.** A damaged PNG, malformed YAML or a key your type does not have leaves the old content in use, sets `Asset.Error` and raises a `failed` event. Fix the file and it reloads.
- For a model, only the model file is watched, not the buffers and textures beside it.
- Shader files reload through their own path (`docs/driving-a-game.md`).

**Exports do not watch files.** The watcher lives in the development tools, which Release builds leave out, so a shipped game has no file polling. It reads the `art/` and `data/` folders `onegame export` copies next to the program.

## Harness

The control operation `assets` lists every loaded asset (`path`, `kind`, `version`, `references`, `state` of `ready`, `loading`, `pending`, `error` or `failed`, `digest`, `error`), the recent events (`loaded`, `reloaded`, `failed`, `pending`, `applied`, `released`), and `dataDigest`. It reads changed files first; `reload` rereads one path, `apply_data` applies staged data, `since` returns only newer events.

```yaml
steps:
  - assets: check                                   # read changed files now
  - expect: { asset: { path: art/ship.png, state: ready, version: "2" } }
  - expect: { asset: { path: data/ships.yaml, state: pending } }
  - assets: apply-data
  - expect: { asset: { path: data/ships.yaml, state: ready, version: "2" } }
  - assets: { reload: art/ship.png }
```

`expect: asset` also takes `error`, matched against part of the message. A capture right after an edit shows the new picture, because `capture_frame` checks the files first.

## Where files come from, and platform limits

By default the game finds `art/` and `data/` next to the program (an export), in a macOS bundle's `Resources`, or in the working directory (development). Override `GameDefinition.CreateAssetSource()` to read from somewhere else.

This works on Windows, Linux and macOS. **Android, iOS and the browser do not load assets by path yet**: their builds keep art inside the program, and there is no source for embedded or fetched content. Use `DefineArt` and embedded resources there for now.
