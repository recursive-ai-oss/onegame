# MSBuild reference: `RecursiveAI.OneGame.Sdk`

> Written by hand from `build/*.targets` and `src/OneGame.Sdk/Sdk/*`, the files the SDK package ships, and from the settings `onegame export` reads. Whoever changes a property in those files updates this page in the same commit.

A game project names the SDK and sets what differs from the defaults:

```xml
<Project Sdk="RecursiveAI.OneGame.Sdk/1.8.0">
  <PropertyGroup>
    <AssemblyName>MyGame</AssemblyName>
    <AndroidPackage>com.example.mygame</AndroidPackage>
  </PropertyGroup>
</Project>
```

Inside an engine checkout the project uses `Microsoft.NET.Sdk` instead, sets `OneGameRoot` and imports `$(OneGameRoot)/build/Game.targets` (or `Server.targets`); everything below applies the same way. `onegame new` writes either form.

## Properties you set

| Property | Default | Effect |
|---|---|---|
| `OneGameKind` | `Game` | What the project is. `Game`: a client or single-player game (imports the game settings). `Server`: a dedicated server, an executable that references `OneGame.Server` and `OneGame.Server.Live`, with no window, GPU or audio. `Plugin`: a server plugin, a class library the plugin host loads (not published NativeAOT). Any other value behaves like `Game`. |
| `OneGameDev` | `true` when `Configuration` is `Debug`, `false` otherwise | Development tools: live control over MCP, scenarios, capture and replay. When true the project references `OneGame.Live` (or the `RecursiveAI.OneGame.Live` package), defines `ONEGAME_DEV` and compiles a start-up hook. When false none of it is in the program. `onegame export` sets it to `false` unless asked for a dev export. Games only; servers keep their live control in every build. |
| `OneGameAudio` | not set (audio on) | Set to `false` to leave `OneGame.Audio` out of a game that plays no sound. Any other value keeps it. Games only. |
| `OneGameAot` | not set | `true` turns on `PublishAot`. Set on the command line (`-p:OneGameAot=true`) so it does not reach the generator project. |
| `OneGameTrim` | not set | `true` turns on `PublishTrimmed` (and `PublishReadyToRun` for games): the self-contained CoreCLR publish `onegame export` uses where NativeAOT is not possible. |
| `OneGameRoot` | not set | Path of an engine checkout. When set, the engine comes from its projects (`src/OneGame.*`); when empty, from nuget.org at `OneGameVersion`. A trailing slash is accepted. |
| `OneGameVersion` | the SDK package's version | Set by the SDK package itself; the engine libraries a game references are pinned to it. Only set it for a game that imports the targets files directly. If neither `OneGameRoot` nor `OneGameVersion` is set, the build stops with `OGBUILD002`. |
| `OneGameShips` | not set | Engine projects only: `true` turns on the package guard (`OGBUILD001`). A game leaves it unset. |
| `ManagePackageVersionsCentrally` | the game's own setting | The standard NuGet switch. When true the engine references go in as `VersionOverride`, so a central `Directory.Packages.props` does not trigger NU1008. |

## Android

Used when publishing for a `linux-bionic-*` runtime identifier, which `onegame export --target android` does for you.

| Property | Default | Effect |
|---|---|---|
| `AndroidPackage` | `dev.onegame.<assembly name, lower-case letters and digits>` | The application id in the APK manifest. `--package` on the command line wins. |
| `AndroidLabel` | the project name | The name under the app icon. `--label` wins. |
| `AndroidOrientation` | `unspecified` | Screen orientation requested in the manifest. `--orientation` wins. |
| `AndroidInternet` | `false` | `true` adds the `INTERNET` permission, needed by a game that connects to servers. Debug exports always add it for live control. |
| `AndroidPlayBilling` | `false` | `true` adds Google Play Billing to the APK: a `classes.dex` with the Billing Library and the engine's small wrapper, the `BILLING` permission and Play's purchase activities. Needs a JDK and the SDK's `d8.jar` at export time; see `docs/interfaces/store.md`. |
| `AndroidSdkPath` | `$(ANDROID_HOME)`, else `$(ANDROID_SDK_ROOT)` | The Android SDK folder. |
| `AndroidNdkPath` | `$(ANDROID_NDK)` | The NDK folder whose clang and sysroot link the library. Missing or wrong: error `OGANDROID001`. `onegame export` passes the NDK it found. |
| `OneGameAndroid` | set to `true` for `linux-bionic-*` identifiers | Read-only in practice. It turns the game into a NativeAOT shared library (`PublishAot`, `NativeLib=Shared`, `OutputType=Library`), compiles the native-activity entry point, defines `ONEGAME_ANDROID` and points the compiler at the NDK and at the downloaded `linux-bionic` runtime packs. |

The SDK also fills in the NativeAOT toolchain for `linux-bionic-*` from the NDK and the downloaded packs, so a build needs no further setup. These are set unconditionally and are not meant to be overridden: `CppCompilerAndLinker` (the NDK's `clang`), `SysRoot` (the NDK sysroot), `ObjCopyName` (`llvm-objcopy`), `LinkerFlavor` (`lld`), `IlcToolsPath` (the host compiler pack), `IlcFrameworkPath`, `IlcFrameworkNativePath` and `IlcSdkPath` (the `linux-bionic` runtime pack). The compiler and runtime packs are version `BundledNETCoreAppPackageVersion`, found under `NuGetPackageRoot` (or `NUGET_PACKAGES`, then `~/.nuget/packages`).

## iOS

Used when publishing for `ios-arm64` or `iossimulator-arm64`, on a Mac with Xcode (`onegame export --target ios|ios-simulator`).

| Property | Default | Effect |
|---|---|---|
| `IosBundleId` | `dev.onegame.<assembly name, lower-case letters and digits>` | The bundle identifier. `--bundle-id` wins. |
| `IosLabel` | the project name | The display name. `--label` wins. |
| `IosOrientation` | `all` | Orientations the app supports. `--orientation` wins. |
| `OneGameIos` | set to `true` for the iOS identifiers | Read-only in practice. It turns on `PublishAot` and `SelfContained`, sets `StartupObject` to the UIKit entry point (`OneGameIosEntry`), defines `ONEGAME_IOS`, adds the iOS identifiers to the ILCompiler pack list (no iOS workload needed). |

## Fixed by the SDK

These are set for every game, server and plugin project. A project can override one in its own `PropertyGroup` after the SDK import, but the rules they enforce (analyzers, warnings as errors) are what make an invalid game a compile error, so leave them.

| Property | Value |
|---|---|
| `OutputType` | `Exe` (`Library` for `OneGameKind=Plugin`, and for Android) |
| `TargetFramework` | `net10.0` |
| `LangVersion` | `latest` |
| `Nullable` | `enable` |
| `ImplicitUsings` | `disable` (write your `using` lines) |
| `TreatWarningsAsErrors` | `true` |
| `AnalysisLevel` | `latest-recommended` |
| `InvariantGlobalization` | `true` |
| `Deterministic` | `true` |
| `EnableTrimAnalyzer`, `EnableAotAnalyzer`, `IsAotCompatible` | `true` |
| `JsonSerializerIsReflectionEnabledByDefault` | `false` (no reflection-based serialization under NativeAOT) |
| `TrimmerSingleWarn` | `false` |
| `PublishAot` | `true` only when `OneGameAot=true`, or for Android and iOS |
| `PublishTrimmed`, `PublishReadyToRun` | `true` only when `OneGameTrim=true` |
| `SelfContained` | `true` for iOS |
| `NativeLib` | `Shared` for Android |
| `DefineConstants` | adds `ONEGAME_DEV`, `ONEGAME_ANDROID`, `ONEGAME_IOS` as above |

## Items the SDK adds

| Item | When | What |
|---|---|---|
| `ProjectReference` to `OneGame.Host`, `OneGame.Core`, `OneGame.UI` and the generator (as an analyzer) | game, with `OneGameRoot` | The engine from the checkout. |
| `ProjectReference` to `OneGame.Audio` | game, `OneGameAudio` not `false` | Sound. |
| `ProjectReference` to `OneGame.Live` | game, `OneGameDev=true` | Live control. |
| `PackageReference` to `RecursiveAI.OneGame.Host`, `.Core`, `.UI` | game, without `OneGameRoot` | The same libraries from nuget.org at `OneGameVersion`. The generator and analyzers travel inside the Core package. |
| `PackageReference` to `RecursiveAI.OneGame.Audio`, `.Live` | as for the project references | |
| `PackageReference` to `RecursiveAI.OneGame.Server`, `.Server.Live`, `.Core` | `OneGameKind` `Server` or `Plugin`, without `OneGameRoot` | A server's libraries. With `OneGameRoot` the matching `ProjectReference` items are added instead. |
| `Compile` of `DevBoot.cs` | `OneGameDev=true`, not Android or iOS | The module initializer that starts the development tools. Android and iOS start them from their own entry points. |
| `Compile` of `AndroidEntry.cs` / `IosEntry.cs` | Android / iOS | The native-activity export `ANativeActivity_onCreate` (a game therefore needs a class `Program` with `static int Main(string[])`) / the UIKit entry point. |
| `PackageDownload` | Android | The NativeAOT compiler for the host and the `linux-bionic` runtime pack. |
| `KnownILCompilerPack` (update) | iOS | Adds the iOS identifiers to the list of NativeAOT targets. |

## Diagnostics from the build files

| ID | When |
|---|---|
| `OGBUILD001` | A project that ships with the engine references a package outside the one* family. Engine projects only; a game never sees it. |
| `OGBUILD002` | Neither `OneGameRoot` nor `OneGameVersion` is set, so the engine has no source. Create the project with `onegame new`. |
| `OGANDROID001` | An Android build cannot find the NDK. Set `ANDROID_NDK` or `-p:AndroidNdkPath=`. |

## Not MSBuild, but read at export

- `AssemblyName` and `Version` name the export folder and archive (`<game>-<version>-<rid>`).
- Content folders `fonts/`, `art/` and `data/` next to the project are copied as they are; see `docs/exporting.md` for the accepted file types.
- Environment: `ONEGAME_STEAM_REDIST` (a folder with `linux-x64/libsteam_api.so` and `win-x64/steam_api64.dll`) or `ONEGAME_STEAM_SDK` (the unpacked Steamworks SDK) for `onegame export --steam`; `ONEGAME_PLAY_BILLING_DIR` (cache for the Play Billing files, default `~/.local/share/onegame/play-billing`); `ANDROID_HOME`, `ANDROID_SDK_ROOT`, `ANDROID_NDK` for Android.
