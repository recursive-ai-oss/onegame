[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ControlResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ControlResult`](ControlResult.md)&gt;

```csharp
public sealed record ControlResult : IEquatable<ControlResult>
```

What an operation returns: a JSON text and, for captures, the PNG bytes.

## Constructors

| Member | Description |
| --- | --- |
| [`ControlResult(string, byte[]?)`](#m-ctor-string-byte) | What an operation returns: a JSON text and, for captures, the PNG bytes. |

### <a id="m-ctor-string-byte"></a>ControlResult(string, byte\[\]?)

```csharp
public ControlResult(string Json, byte[]? Png = null);
```

What an operation returns: a JSON text and, for captures, the PNG bytes.

**Parameters**

- `Json` (`string`): The result object as JSON text.
- `Png` (`byte[]?`): PNG bytes of a captured frame, when the operation made one.

## Properties

| Member | Description |
| --- | --- |
| [`Json`](#m-json) | The result object as JSON text. |
| [`Png`](#m-png) | PNG bytes of a captured frame, when the operation made one. |

### <a id="m-json"></a>Json

```csharp
public string Json { get; init; }
```

The result object as JSON text.

**Type:** `string`

### <a id="m-png"></a>Png

```csharp
public byte[]? Png { get; init; }
```

PNG bytes of a captured frame, when the operation made one.

**Type:** `byte[]?`
