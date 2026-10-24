# Exporting a game

`onegame export` turns a game project into what ships: a folder and an archive holding the program, its content, the platform libraries it needs and a licence-notices file. Nothing else goes in.

```
onegame export --target linux-x64 --json              # ./export/<game>-<version>-linux-x64/ and .tar.gz
onegame export --target win-x64 --steam --json        # ./export/<game>-<version>-win-x64/ and .zip, with steam_api64.dll
```

Options: `--target linux-x64|win-x64` (default: the host's), `--steam`, `--steam-redist <dir>`, `--out <dir>` (default `<project>/export`), `--project <dir|csproj>`, `--no-archive`, `--no-aot`. The version in the names is the project's `<Version>` (1.0.0 when unset). The JSON result is in `docs/interfaces/control.md`; read its `warnings`.

## What the build is

Always the Release configuration, with the development tools left out of the program: no live-control listener, no scenario parser, no capture or replay options (`--live` exits 5 with `no_dev_tools`). The export fails with `dev_tools_present` if the program mentions the live-control assembly anyway, and `devTools` in the result is `false`.

| Target | Host | Result |
|---|---|---|
| linux-x64 | Linux | NativeAOT: one executable, about 6 MB |
| win-x64 | Windows | NativeAOT, same command |
| win-x64 | Linux or macOS | self-contained, trimmed, ReadyToRun CoreCLR: `<game>.exe` with the runtime beside it (about 25 MB). NativeAOT links with the target OS's toolchain, so it needs a matching host |
| linux-x64 | Windows or macOS, or `--no-aot` | the same self-contained CoreCLR build |

`runtime` in the result says which one was made, and a `not_native_aot` warning says why. Crash-dump and debugger helpers, debug symbols and the engine's XML documentation are removed from the output.

## Layout

```
demo-1.0.0-linux-x64/
  Demo                       the program (Demo.exe on Windows; the name is the project's AssemblyName)
  run.sh                     Linux only: starts the program from its own folder, from any current directory
  libsteam_api.so            with --steam (steam_api64.dll on Windows)
  THIRD-PARTY-NOTICES.txt    always
  fonts/  art/  data/        the game's content folders, same names as in the project
```

Content folders are copied as they are. `fonts/` takes `.ttf .otf` and their licence texts (`.txt .md`); `art/` takes `.svg .png .jpg .jpeg .psd .webp`; `data/` takes anything. Files of other types in `fonts/` or `art/` are not copied and are listed in `skipped`. Other project folders are left out and listed in `skipped` too, except build output, `scenarios/`, `captures/`, `export/` and dot-folders. A file over 64 MiB is copied with a `large_file` warning. The game finds `fonts/` next to the program (`GameDefinition.DefineFonts`), so run it by its path or through `run.sh`; the working directory does not matter.

The Linux archive is a `.tar.gz` with one top folder, owner root, a fixed timestamp and sorted entries (the same folder gives the same archive); the program and `run.sh` are executable. Windows gets a `.zip` the same way. Unpack anywhere and run `run.sh` or the program.

## Steam

`--steam` copies Valve's library next to the program, from a copy you supply (it is never in this repository and never downloaded):

1. `--steam-redist <dir>`, else the `ONEGAME_STEAM_REDIST` variable: a folder holding `linux-x64/libsteam_api.so` and `win-x64/steam_api64.dll` (or the file itself);
2. else `ONEGAME_STEAM_SDK`: the unpacked Steamworks SDK (`redistributable_bin/linux64`, `redistributable_bin/win64`).

None found: `steam_redist_missing`, nothing is built. The game must itself use `OneGame.Steam` for the library to be loaded (`docs/interfaces/steam.md`).

`steam_appid.txt` is never shipped: the export deletes any in the output and does not copy the project's. With it present Steam's relaunch check always passes and the game would run outside the Steam client. A shipped game starts Steam with `RelaunchThroughSteam = true` and its app id; started directly, it asks Steam to launch it and exits (`RelaunchRequested`). To run an exported build by hand for testing, put `steam_appid.txt` with the id (480 is Valve's test app) in the current directory with the Steam client signed in, as for `dotnet run`.

In Steam's depot, set the Linux launch option to `run.sh` (or the program) and the Windows one to `<game>.exe`. Uploading depots (SteamPipe) is not part of the export.

## Steam Deck and the Linux runtime

Steam runs Linux games against its Steam Linux Runtime 3 ("sniper"), which has **glibc 2.31**. A program linked on a newer system (Ubuntu 24.04 has 2.39) will not start there: `version 'GLIBC_2.38' not found`. The export reads the version names in the program and warns `glibc_too_new` when it needs more than 2.31. Build the Linux export in Valve's SDK image or on a system with an old enough glibc.

`tools/steamrt.sh` does that with bubblewrap and Valve's public runtime images (no Steam account or client):

```
tools/steamrt.sh fetch                                   # pinned images, checked by SHA-256, into ~/.cache/onegame/steamrt
mkdir -p work && cp -r mygame work/            # the project's OneGameRoot must be /engine (the checkout, mounted read-only)
env DOTNET_ROOT=$HOME/.dotnet ONEGAME_ARTIFACTS=/work/artifacts ONEGAME_STEAM_REDIST=/work/redist \
  tools/steamrt.sh build work dotnet /work/cli/onegame.dll export --project /work/mygame --target linux-x64 --steam --out /work/export --json
tools/steamrt.sh run <unpacked-export-folder> /game/run.sh --headless --frames 60 --json
```

`run` is the check for the Steam Deck: the same runtime image, the game folder read-only at `/game`, no network. A passing run prints the same state hash as `onegame run --headless --frames 60 --json` on the development machine. It does not replace a run on the device (controller, display, Vulkan driver).

## Shaders

The 2D renderer's shaders come from a test-only compiler until the shader build package lands, and the engine never ships SPIR-V made by a third-party tool. So today an export contains no renderer shaders: the game draws the plain UI preview (text as bars), the result says `"renderer":"ui-preview"` and carries a `shaders_not_included` warning, and a `shaders/` folder in the project is listed in `skipped`. Gameplay, state and hashes are unaffected. When the package lands, its shaders are compiled into the engine: set `ExportAssets.GeneratedShaders` to `true` in `src/OneGame.Cli/Verbs/ExportVerb.cs` and the warning and `ui-preview` go away. Nothing else in the layout changes.

## Licences

Every export carries `THIRD-PARTY-NOTICES.txt`: the engine's list (`THIRD-PARTY-NOTICES.md` in the repository: the Wayland protocol files and the Vulkan registry the generated code derives from, and the .NET runtime), then every text file found in the game's `fonts/` (put each font's licence there; `onegame new` starts a game with Noto Sans Regular and its SIL Open Font License text), then a line for the Steam library when it ships. Fonts without a licence text give a `font_licence_missing` warning. If you add other non-code content with licence terms (art, sounds), add its notice to the file yourself, for example by shipping it under `data/` and in your own list; the export only knows fonts.

## What a Windows run must prove

Not run from the Linux host that builds the Windows export, so still open:

1. `Demo.exe --headless --frames 60 --json` from another directory prints the same hash as on Linux, and `--live` exits 5.
2. The folder runs on a machine without .NET installed.
3. With `--steam`: the game started from Steam loads `steam_api64.dll`; started directly it relaunches through Steam.
4. The NativeAOT export (`onegame export --target win-x64` on Windows) builds without warnings, starts, and opens a window.
