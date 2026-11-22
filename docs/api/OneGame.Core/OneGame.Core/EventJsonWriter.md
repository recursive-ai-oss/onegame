[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EventJsonWriter delegate

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public delegate void EventJsonWriter(ref SyncReader reader, JsonOut json);
```

Writes the JSON of an event payload, for the harness.

**Parameters**

- `reader`: Reads the payload.
- `json`: Receives the JSON value.
