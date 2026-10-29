# Engine-internal interfaces

These files are the written contracts between areas of the engine (the sync stack, the GPU layer, the platform layer, the renderers, the control layer and so on). They are for people working on the engine itself: each says what one area promises the next and where the code lives. They are not the user guide.

To make a game, start with [first-game.md](../first-game.md) and [driving-a-game.md](../driving-a-game.md). Each file here describes the current contract only; history is in git.

## Index

**Entities, sync and networking:** [entity-sync](entity-sync.md) (the entity model and generated sync surface), [sync](sync.md) (nodes, scopes, saves, replay, drift), [transport](transport.md) (reliable UDP and WebSocket), [transport-security](transport-security.md), [cluster](cluster.md) (dedicated servers and process clusters), [server-host](server-host.md) (server host and plugins).

**Graphics:** [gpu](gpu.md) (device API and the Vulkan, Metal and WebGPU backends), [render-graph](render-graph.md), [render2d](render2d.md), [render3d](render3d.md), [scene3d](scene3d.md) (entities in 3D), [gltf](gltf.md) (importer), [particles](particles.md), [atlas](atlas.md) (glyph, SVG and image atlases).

**UI:** [ui-render](ui-render.md) (the display list), [ui-input-inspect](ui-input-inspect.md), [ui-kit](ui-kit.md), [onerender](onerender.md) (HTML and CSS pages), [tween](tween.md) (tweens, easing and style transitions).

**Platform and services:** [platform](platform.md) (windows, events, input devices), [input](input.md) (actions and rebinding), [audio](audio.md), [assets](assets.md), [storage](storage.md), [steam](steam.md), [store](store.md) (in-app purchases), [android](android.md), [ios](ios.md).

**Harness:** [control](control.md) (the control layer and its three faces).
