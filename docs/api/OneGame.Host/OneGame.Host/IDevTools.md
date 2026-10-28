[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# IDevTools interface

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public interface IDevTools
```

The hook through which development builds add their tools (live control, scenarios, capture, replay). Release builds never set [`GameApp.DevTools`](GameApp.md#m-devtools), so none of that code is part of them.

## Methods

| Member | Description |
| --- | --- |
| [`Attach(GameInstance)`](#m-attach-gameinstance) | Called for every game started without development options, so tools such as the on-screen overlay work in an ordinary run. |
| [`Run(GameInstance, GameArgs, TextWriter)`](#m-run-gameinstance-gameargs-textwriter) | Runs a development command line against a game and returns the exit code. |

### <a id="m-attach-gameinstance"></a>Attach(GameInstance)

```csharp
void Attach(GameInstance game);
```

Called for every game started without development options, so tools such as the on-screen overlay work in an ordinary run.

**Parameters**

- `game` ([`GameInstance`](GameInstance.md)): The game about to run.

### <a id="m-run-gameinstance-gameargs-textwriter"></a>Run(GameInstance, GameArgs, TextWriter)

```csharp
int Run(GameInstance game, GameArgs args, TextWriter output);
```

Runs a development command line against a game and returns the exit code.

**Parameters**

- `game` ([`GameInstance`](GameInstance.md))
- `args` ([`GameArgs`](GameArgs.md))
- `output` ([`TextWriter`](https://learn.microsoft.com/dotnet/api/system.io.textwriter))

**Returns:** `int`
