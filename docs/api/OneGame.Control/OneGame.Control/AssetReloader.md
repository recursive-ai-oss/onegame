[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# AssetReloader class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)

```csharp
public sealed class AssetReloader
```

Development-build asset hot reload. It compares each loaded file's write time and length with what was last read and asks [`Assets.Reload()`](../../OneGame.Host/OneGame.Host/Assets.md#m-reload-asset) for the ones that changed. Exports leave the control layer out, so they carry no watcher.

## Constructors

| Member | Description |
| --- | --- |
| [`AssetReloader(Assets)`](#m-ctor-assets) | Development-build asset hot reload. |

### <a id="m-ctor-assets"></a>AssetReloader(Assets)

```csharp
public AssetReloader(Assets assets);
```

Development-build asset hot reload. It compares each loaded file's write time and length with what was last read and asks [`Assets.Reload()`](../../OneGame.Host/OneGame.Host/Assets.md#m-reload-asset) for the ones that changed. Exports leave the control layer out, so they carry no watcher.

**Parameters**

- `assets` ([`Assets`](../../OneGame.Host/OneGame.Host/Assets.md))

## Methods

| Member | Description |
| --- | --- |
| [`Check()`](#m-check) | Checks every loaded file now. |
| [`Poll()`](#m-poll) | Checks the files at most every 250 ms. |

### <a id="m-check"></a>Check()

```csharp
public IReadOnlyList<string> Check();
```

Checks every loaded file now.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>` — The paths that were read again.

### <a id="m-poll"></a>Poll()

```csharp
public IReadOnlyList<string> Poll();
```

Checks the files at most every 250 ms. Cheap enough for every pass of a game loop.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>` — The paths that were read again.
