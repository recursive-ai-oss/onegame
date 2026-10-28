[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SyncRead static class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public static class SyncRead
```

Reads synced values by type argument, which is how generated code reads types whose reader is an explicit interface member.

## Methods

| Member | Description |
| --- | --- |
| [`Value<T>(ref SyncReader)`](#m-value-t-syncreader) | Reads a `T` from `reader`. |

### <a id="m-value-t-syncreader"></a>Value&lt;T&gt;(ref SyncReader)

```csharp
public static T Value<T>(ref SyncReader reader) where T : ISyncValue<T>;
```

Reads a `T` from `reader`.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

**Returns:** `T`
