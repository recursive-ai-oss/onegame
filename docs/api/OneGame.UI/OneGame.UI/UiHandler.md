[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# UiHandler delegate

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public delegate bool UiHandler(Element target, UiEvent e);
```

An event handler. Return true when the event was handled, which stops it bubbling to ancestors and suppresses the default action.

**Parameters**

- `target`: The element the handler is attached to.
- `e`: The event.
