[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# PlatformEventFilter delegate

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public delegate bool PlatformEventFilter(in PlatformEvent e);
```

A look at a platform event before the game handles it.

**Parameters**

- `e`: The event.

**Returns:** True to consume the event.
