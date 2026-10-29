# onegame API reference

The public API of every onegame package, generated from the built assemblies and their documentation comments. Look a type up by package below, or by name in [all types](types.md). The guides in [docs/](../) explain how the pieces fit together; this is the detail behind them.

How the pages are laid out, which documentation tags they support and how to regenerate them: [README](README.md).

## Packages

| Package | Namespaces | Types | Members | What it is |
| --- | --- | --- | --- | --- |
| [RecursiveAI.OneGame.Atlas](OneGame.Atlas/index.md) | 1 | 14 | 111 | Atlas pages and the SVG and image atlas for the 2D renderer. |
| [RecursiveAI.OneGame.Audio](OneGame.Audio/index.md) | 1 | 57 | 445 | Audio device output and the audio service that drives a mixer. |
| [RecursiveAI.OneGame.Cli](OneGame.Cli/index.md) | 0 | 0 | 0 | The onegame command: create, build, run, test, capture and replay games. |
| [RecursiveAI.OneGame.Control](OneGame.Control/index.md) | 1 | 52 | 440 | The control layer shared by the command line, the test API and live control. |
| [RecursiveAI.OneGame.Core](OneGame.Core/index.md) | 2 | 66 | 546 | Entity runtime: fixed-point numbers, random streams and the attributes that declare entities. |
| [RecursiveAI.OneGame.Gltf](OneGame.Gltf/index.md) | 1 | 62 | 371 | glTF 2.0 import: scenes, meshes, materials, skins and animations, safe on untrusted input. |
| [RecursiveAI.OneGame.Gpu](OneGame.Gpu/index.md) | 2 | 91 | 574 | Graphics API abstraction and render graph. |
| [RecursiveAI.OneGame.Gpu.Backends](OneGame.Gpu.Backends/index.md) | 1 | 1 | 6 | Picks the graphics backend for the machine: Metal on Apple platforms, WebGPU in a browser, Vulkan elsewhere. |
| [RecursiveAI.OneGame.Gpu.Metal](OneGame.Gpu.Metal/index.md) | 1 | 1 | 3 | Metal backend of the graphics layer. |
| [RecursiveAI.OneGame.Gpu.Vulkan](OneGame.Gpu.Vulkan/index.md) | 1 | 2 | 4 | Vulkan backend of the graphics layer. |
| [RecursiveAI.OneGame.Gpu.WebGpu](OneGame.Gpu.WebGpu/index.md) | 1 | 1 | 3 | WebGPU backend of the graphics layer, for the browser. |
| [RecursiveAI.OneGame.Host](OneGame.Host/index.md) | 1 | 60 | 498 | The game host: window, input, fixed-step simulation, UI and render stage for one game, windowed or headless. |
| [RecursiveAI.OneGame.Live](OneGame.Live/index.md) | 1 | 3 | 12 | Development-build tools of a running game: live control over MCP, scenario runs, captures and replays. Release builds do not reference this project. |
| [RecursiveAI.OneGame.Navigation](OneGame.Navigation/index.md) | 1 | 21 | 164 | Pathfinding for deterministic simulations: grid searches on fixed-point positions, budgeted over several ticks, and crowds with avoidance, with state that is saved, restored and hashed with the world. |
| [RecursiveAI.OneGame.Net](OneGame.Net/index.md) | 1 | 28 | 227 | Network transports behind the message-channel seam: reliable UDP, WebSocket, and a network conditioner for tests. |
| [RecursiveAI.OneGame.Pages](OneGame.Pages/index.md) | 2 | 34 | 245 | Draws OneRender pages (builder trees and HTML) through the render graph, with input, host scopes and harness support. |
| [RecursiveAI.OneGame.Particles](OneGame.Particles/index.md) | 1 | 8 | 72 | Particle emitters: a seedable, allocation-free simulation for sparks, smoke, dust and trails, drawn by the 2D and 3D renderers. |
| [RecursiveAI.OneGame.Platform](OneGame.Platform/index.md) | 1 | 48 | 530 | Windows, input, controllers and the app lifecycle per operating system. |
| [RecursiveAI.OneGame.Purchases](OneGame.Purchases/index.md) | 1 | 21 | 159 | In-app purchases: one store-neutral API over StoreKit, Google Play Billing and a scriptable fake store. |
| [RecursiveAI.OneGame.Render2D](OneGame.Render2D/index.md) | 2 | 65 | 400 | Sprite, tilemap and text renderer. |
| [RecursiveAI.OneGame.Render3D](OneGame.Render3D/index.md) | 2 | 141 | 904 | Physically based 3D renderer: glTF materials, clustered forward lighting, image-based lighting, bloom and tone mapping on the render graph. |
| [RecursiveAI.OneGame.Server](OneGame.Server/index.md) | 1 | 33 | 201 | The headless server host: fixed-step scopes, YAML configuration, logging, and the plugin API. |
| [RecursiveAI.OneGame.Server.Live](OneGame.Server.Live/index.md) | 1 | 10 | 89 | Live control of a running game server over the same MCP surface as a development game: plugin and scope operations. |
| [RecursiveAI.OneGame.Server.Plugins](OneGame.Server.Plugins/index.md) | 1 | 1 | 2 | Loads server plugins into collectible load contexts. CoreCLR only: it uses reflection and assembly loading, so NativeAOT hosts leave it out. |
| [RecursiveAI.OneGame.Steam](OneGame.Steam/index.md) | 4 | 52 | 283 | Steamworks binding and the store-neutral services (achievements, stats, cloud saves, presence, overlay, ownership) that sit in front of it. |
| [RecursiveAI.OneGame.Sync](OneGame.Sync/index.md) | 3 | 79 | 568 | Nodes, scopes, loopback transport, saves, command log, replay and drift hashes. |
| [RecursiveAI.OneGame.Testing](OneGame.Testing/index.md) | 1 | 43 | 408 | The C\# test API: in-process clusters, scenarios, recordings and captures. |
| [RecursiveAI.OneGame.Text](OneGame.Text/index.md) | 1 | 11 | 66 | Text for the UI: font library, shaping, measurement and the glyph atlas. |
| [RecursiveAI.OneGame.UI](OneGame.UI/index.md) | 2 | 57 | 596 | Retained UI tree with CSS-model styling and flex layout. |
| [RecursiveAI.OneGame.UI.Kit](OneGame.UI.Kit/index.md) | 1 | 25 | 181 | Base UI component kit: buttons, tables, lists, tabs, text input and more, themed with tokens. |
| [RecursiveAI.OneGame.Sdk](sdk.md) | – | – | – | MSBuild properties and items, not .NET types. |

The MSBuild SDK's properties and items (`OneGameKind` and the rest) are on the [SDK page](sdk.md).
