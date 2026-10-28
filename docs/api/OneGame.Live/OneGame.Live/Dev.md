[API reference](../../index.md) › [OneGame.Live](../index.md) › OneGame.Live

# Dev static class

**Namespace:** `OneGame.Live`  
**Package:** [`RecursiveAI.OneGame.Live`](../index.md)

```csharp
public static class Dev
```

Turns on the development tools of a game. The scaffolded game calls this from a module initializer in debug builds only.

## Methods

| Member | Description |
| --- | --- |
| [`Enable()`](#m-enable) | Makes the development options (`--live`, `--scenario`, `--capture`, `--replay`...) work in [`GameApp.Run()`](../../OneGame.Host/OneGame.Host/GameApp.md#m-run-gamedefinition-string). |

### <a id="m-enable"></a>Enable()

```csharp
public static void Enable();
```

Makes the development options (`--live`, `--scenario`, `--capture`, `--replay`...) work in [`GameApp.Run()`](../../OneGame.Host/OneGame.Host/GameApp.md#m-run-gamedefinition-string).
