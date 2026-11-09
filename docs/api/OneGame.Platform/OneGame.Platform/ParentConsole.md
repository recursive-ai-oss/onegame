[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ParentConsole static class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public static class ParentConsole
```

Gives a program built for the Windows GUI subsystem (no console window of its own, the default for released Windows games) somewhere to print when it was started from a terminal. Everywhere else, and when the parent already redirected the output to a pipe or a file, it does nothing.

## Methods

| Member | Description |
| --- | --- |
| [`Attach()`](#m-attach) | Attaches to the console of the process that started this one, so [`Console.Out`](https://learn.microsoft.com/dotnet/api/system.console) and [`Console.Error`](https://learn.microsoft.com/dotnet/api/system.console) reach the terminal. |

### <a id="m-attach"></a>Attach()

```csharp
public static bool Attach();
```

Attaches to the console of the process that started this one, so [`Console.Out`](https://learn.microsoft.com/dotnet/api/system.console) and [`Console.Error`](https://learn.microsoft.com/dotnet/api/system.console) reach the terminal. Call it before the first use of [`Console`](https://learn.microsoft.com/dotnet/api/system.console); `GameApp.Run` does. Output the parent redirected is left alone, and a program started from Explorer or a game launcher has no parent console, so the call fails quietly and nothing is printed.

**Returns:** `bool` — True when a console was attached.
