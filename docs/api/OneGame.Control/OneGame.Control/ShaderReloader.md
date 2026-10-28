[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ShaderReloader class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)

```csharp
public sealed class ShaderReloader
```

Development-build shader hot reload. It watches the files of the game's [`HotShaders`](../../OneGame.Host/OneGame.Host/HotShaders.md), recompiles a changed one, checks that its interface still matches what the game was built with and swaps it in. A compile error or an interface change is reported and the shader that was running keeps running.

## Constructors

| Member | Description |
| --- | --- |
| [`ShaderReloader(HotShaders, TextWriter?)`](#m-ctor-hotshaders-textwriter) | Creates a reloader over a game's registrations. |

### <a id="m-ctor-hotshaders-textwriter"></a>ShaderReloader(HotShaders, TextWriter?)

```csharp
public ShaderReloader(HotShaders shaders, TextWriter? log = null);
```

Creates a reloader over a game's registrations.

**Parameters**

- `shaders` ([`HotShaders`](../../OneGame.Host/OneGame.Host/HotShaders.md)): The game's [`GameInstance.HotShaders`](../../OneGame.Host/OneGame.Host/GameInstance.md#m-hotshaders).
- `log` ([`TextWriter`](https://learn.microsoft.com/dotnet/api/system.io.textwriter)?): Where a failed reload is written as one line per problem. Defaults to standard error.

## Methods

| Member | Description |
| --- | --- |
| [`Check()`](#m-check) | Checks every registered file now and recompiles the ones that changed since the last check (all of them on the first check). |
| [`Poll()`](#m-poll) | Checks the files at most every 250 ms. |
| [`Status()`](#m-status) | The current state of every registered shader. |

### <a id="m-check"></a>Check()

```csharp
public IReadOnlyList<string> Check();
```

Checks every registered file now and recompiles the ones that changed since the last check (all of them on the first check).

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>` — The names that were recompiled.

### <a id="m-poll"></a>Poll()

```csharp
public IReadOnlyList<string> Poll();
```

Checks the files at most every 250 ms. Cheap enough to call on every pass of a game loop.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>` — The names that were recompiled.

### <a id="m-status"></a>Status()

```csharp
public ShaderStatus[] Status();
```

The current state of every registered shader.

**Returns:** [`ShaderStatus`](ShaderStatus.md)\[\]
