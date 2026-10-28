[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ControlOps static class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)

```csharp
public static class ControlOps
```

The operation table. Every face (CLI, test API, MCP) is a view of this list.

## Properties

| Member | Description |
| --- | --- |
| [`All`](#m-all) | All operations. |

### <a id="m-all"></a>All

```csharp
public static IReadOnlyList<ControlOp> All { get; }
```

All operations.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ControlOp`](ControlOp.md)&gt;
