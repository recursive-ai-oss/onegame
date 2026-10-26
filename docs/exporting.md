# Exporting a game

`onegame export` turns a game project into what ships: a folder and an archive holding the program, its content, the platform libraries it needs and a licence-notices file. Nothing else goes in.

```
onegame export --target linux --json                 # ./export/<game>-<version>-linux-x64/ and .tar.gz
onegame export --target win --steam --json        # ./export/<game>-<version>-win-x64/ and .zip, with steam_api64.dll
onegame export --target osx --json                   # ./export/<game>-<version>-osx-arm64/<Game>.app and .tar.gz (on an Apple-silicon Mac)
```

Options: `--target linux|win|osx` (default: the host's; `linux-x64` and `win-x64` also work), `--steam`, `--steam-redist <dir>`, `--out <dir>` (default `<project>/export`), `--project <dir|csproj>`, `--no-archive`, `--no-aot`. The version in the names is the project's `<Version>` (1.0.0 when unset). The JSON result is in `docs/interfaces/control.md`; read its `warnings`.

Desktop exports work for a game created with `onegame new` from the installed tool, with no engine checkout: the `RecursiveAI.OneGame.Sdk` package carries the export targets. The web export, Play Billing in an Android build and `--server --plugins` read engine sources and still need a game created with `--engine <checkout>`; the Android and iOS entry files ship in the SDK package, but those two exports have only been run from a checkout.

Branching on the platform: ask `OneGame.Platform.RunningOn` (`Android`, `IOS`, `Browser`, `Phone`, `Mobile`, `Desktop`), not `OperatingSystem.IsAndroid()`, which is false in a NativeAOT Android build (it runs on the Linux runtime). A scenario capture compares against `name-mobile.png` beside `name.png` on a phone or in a page when that file exists.

Android: `onegame export --target android [--abi arm64-v8a,x86_64] [--dev]` makes a debug-signed APK (NativeAOT shared library, no Java). A game that connects to servers sets `<AndroidInternet>true</AndroidInternet>` in its project file to get the INTERNET permission in release builds too. Setup, device commands and what is verified are in [android](interfaces/android.md).

iOS (on a Mac with Xcode): `onegame export --target ios-simulator [--dev]` makes an ad-hoc signed `.app` for the Simulator, `onegame export --target ios` an unsigned arm64 `.app` for a device, which the owner of an Apple Developer account signs (the steps are printed as `signingNeeds` and listed in [ios](interfaces/ios.md)). Both are NativeAOT executables; no workload is installed.

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

## Dedicated servers

`onegame export --server [--project <server project>] [--out <dir>]` packages a game's server project (a project importing `build/Server.targets`, made by `onegame new <name> --server`): NativeAOT for linux-x64, the same folder, archive and JSON report as a game export, with `renderer: "none"`, no fonts or Steam, and a `run.sh`. The server keeps its live-control socket (an operator interface on a local Unix socket, mode 0600). `--plugins` packages the engine's plugin-capable `onegame-server` instead: framework-dependent CoreCLR (`runtime: coreclr-framework-dependent`), started by `run.sh` through the installed `dotnet`. Servers export for linux-x64 only.

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

A windowed run needs a display and a graphics driver, and the sandbox has neither by default (no network, no `/tmp/.X11-unix`, and the image holds a Vulkan loader but no driver; Steam supplies the driver on a real system). Both are opt-in:

```
tools/steamrt.sh run --x11 --nvidia <folder> /game/run.sh --seed 7 --frames 200 --json    # DISPLAY=:N must name a local X server, e.g. Xvfb
```

- `--x11` binds the socket named by `$DISPLAY` (and `$XAUTHORITY`) and the host's keyboard layout data (`/usr/share/X11/xkb`, which the image lacks and which opening any window needs). `--wayland` does the same for `$WAYLAND_DISPLAY` in `$XDG_RUNTIME_DIR`.
- `--nvidia` binds the NVIDIA device nodes and driver libraries and points the Vulkan loader at them. Only this driver works: its libraries are built for an old glibc, while a Mesa (lavapipe, RADV) from a current host needs a newer glibc than the runtime's 2.31 and cannot load. Without `--nvidia` a windowed run opens its window but has no device, so it draws nothing.
- Time-dependent state differs between a windowed run (paced by the clock) and a headless one, so compare the hash at the same tick with the same `--seed`: a windowed run that ends at tick N prints the hash `--headless --frames N` prints.

## Shaders

The 2D renderer's shaders are HLSL compiled into the engine during its build, so they travel inside the program: an export draws with the real renderer (`"renderer":"2d"`) and needs no shader files. Shaders of your own game are not part of the export layout today; a `shaders/` folder in the project is listed in `skipped` like any other folder that is not content.

## Licences

Every export carries `THIRD-PARTY-NOTICES.txt`: the engine's list (`THIRD-PARTY-NOTICES.md` in the repository: the Wayland protocol files and the Vulkan registry the generated code derives from, and the .NET runtime), then every text file found in the game's `fonts/` (put each font's licence there; `onegame new` starts a game with Noto Sans Regular and its SIL Open Font License text), then a line for the Steam library when it ships. Fonts without a licence text give a `font_licence_missing` warning. If you add other non-code content with licence terms (art, sounds), add its notice to the file yourself, for example by shipping it under `data/` and in your own list; the export only knows fonts.

## What a Windows run must prove

Not run from the Linux host that builds the Windows export, so still open:

1. `Demo.exe --headless --frames 60 --json` from another directory prints the same hash as on Linux, and `--live` exits 5.
2. The folder runs on a machine without .NET installed.
3. With `--steam`: the game started from Steam loads `steam_api64.dll`; started directly it relaunches through Steam.
4. The NativeAOT export (`onegame export --target win-x64` on Windows) builds without warnings, starts, and opens a window.

## Web

`onegame export --target web [--dev] [--out <dir>]` builds the game for the browser: .NET on WebAssembly (the interpreter; no workload needed), WebGPU, the page and its script modules in one folder that any static file server can host (serve `.wasm` as `application/wasm`; Brotli and gzip copies sit beside the files). The export writes a small wrapper project under `obj/onegame-web/` that compiles the game's own files, resources and references with the WebAssembly SDK. `--dev` keeps the development tools (a Debug build). The Release export is trimmed and builds with zero trim warnings.

`onegame web test [--scenario <folder>] [--tolerance N] [--max-diff F] [--dir <exported folder>]` exports with development tools, serves the folder, opens it in headless Chrome (`ONEGAME_CHROME` names the browser) and runs the scenarios in the page. The page dials back over a WebSocket (a page cannot listen), the tool sends the scenario files and reads the result, which has the same shape as `onegame test --json`; actual and diff images of failed checks are saved to `<project>/artifacts/web/`. `--tolerance` and `--max-diff` loosen capture checks for a rasterizer that differs from the one the references came from. Sound plays through Web Audio and starts at the first click, tap or key press, as browsers require; until then the `audio` operation reports `awaiting_gesture` (see `audio.md`). A page opened with `?control=page` has no socket and exposes `__control(message)` (one MCP message in, the answer out) for scripts that inject real input.
## macOS

`--target osx` makes `<Game>.app` for Apple silicon (`osx-arm64`): `Contents/MacOS/<Game>` is the NativeAOT program (about 7.5 MB for Solitaire), `Contents/Resources` holds `fonts/`, `art/`, `data/` and the notices file (`fonts/` is found there by `GameDefinition.DefineFonts`), and `Contents/Info.plist` names the bundle. NativeAOT needs a Mac with Apple silicon as the host; anywhere else the bundle is a self-contained CoreCLR build and carries a `not_signed` warning. The bundle is signed **ad hoc** (`codesign --force --sign -`), which lets it run on the Mac that made it (`open Game.app`); the result carries an `ad_hoc_signature` warning. Giving it to other people needs a Developer ID signature and notarization from your Apple developer account, which the tool does not do. `--bundle-id` sets `CFBundleIdentifier` (default `com.example.<name>`). `--steam` is not available for osx. The archive is a `.tar.gz` that keeps the program's executable bit.
