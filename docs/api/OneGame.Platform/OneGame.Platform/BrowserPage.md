[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# BrowserPage static class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public static class BrowserPage
```

The page-level services a browser host needs around the platform backend: the animation-frame loop.

## Methods

| Member | Description |
| --- | --- |
| [`Control(string)`](#m-control-string) | Called by the page's script (`exports.OneGame.Platform.BrowserPage.Control`). |
| [`SetControlHandler(Func<string, Task<string?>>?)`](#m-setcontrolhandler-func-string-task-string) | Lets the page's script send one control message (an MCP JSON-RPC text) to the running game and wait for the answer: the page's `__control` function. |
| [`StartFrames(Action<double>)`](#m-startframes-action-double) | Calls `frame` once per animation frame, with the time in milliseconds, until [`BrowserPage.StopFrames()`](BrowserPage.md#m-stopframes). |
| [`StopFrames()`](#m-stopframes) | Stops the loop started by [`BrowserPage.StartFrames()`](BrowserPage.md#m-startframes-action-double). |

### <a id="m-control-string"></a>Control(string)

```csharp
public static Task<string> Control(string message);
```

Called by the page's script (`exports.OneGame.Platform.BrowserPage.Control`).

**Parameters**

- `message` (`string`)

**Returns:** [`Task`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task-1)`<string>`

### <a id="m-setcontrolhandler-func-string-task-string"></a>SetControlHandler(Func&lt;string, Task&lt;string?&gt;&gt;?)

```csharp
public static void SetControlHandler(Func<string, Task<string?>>? handler);
```

Lets the page's script send one control message (an MCP JSON-RPC text) to the running game and wait for the answer: the page's `__control` function. A development build sets it; a release build leaves it null and the function answers with an error text.

**Parameters**

- `handler` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<string,` [`Task`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task-1)`<string?>>?`)

### <a id="m-startframes-action-double"></a>StartFrames(Action&lt;double&gt;)

```csharp
public static void StartFrames(Action<double> frame);
```

Calls `frame` once per animation frame, with the time in milliseconds, until [`BrowserPage.StopFrames()`](BrowserPage.md#m-stopframes). The browser owns the loop: a managed thread cannot wait for the next frame.

**Parameters**

- `frame` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<double>`)

### <a id="m-stopframes"></a>StopFrames()

```csharp
public static void StopFrames();
```

Stops the loop started by [`BrowserPage.StartFrames()`](BrowserPage.md#m-startframes-action-double).
