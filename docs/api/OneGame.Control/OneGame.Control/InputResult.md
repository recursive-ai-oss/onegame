[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# InputResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`InputResult`](InputResult.md)&gt;

```csharp
public sealed record InputResult : IEquatable<InputResult>
```

Result of the input operations (`input_key`, `input_pointer`, `ui_click` and the like).

## Constructors

| Member | Description |
| --- | --- |
| [`InputResult(string?, string?, long)`](#m-ctor-string-string-long) | Result of the input operations (`input_key`, `input_pointer`, `ui_click` and the like). |

### <a id="m-ctor-string-string-long"></a>InputResult(string?, string?, long)

```csharp
public InputResult(string? Focused, string? Hovered, long Tick);
```

Result of the input operations (`input_key`, `input_pointer`, `ui_click` and the like).

**Parameters**

- `Focused` (`string?`): The focused UI element, or null.
- `Hovered` (`string?`): The element under the pointer, or null.
- `Tick` (`long`): The world's tick; input is applied at the next frame, so step to see its effect.

## Properties

| Member | Description |
| --- | --- |
| [`Focused`](#m-focused) | The focused UI element, or null. |
| [`Hovered`](#m-hovered) | The element under the pointer, or null. |
| [`Tick`](#m-tick) | The world's tick; input is applied at the next frame, so step to see its effect. |

### <a id="m-focused"></a>Focused

```csharp
public string? Focused { get; init; }
```

The focused UI element, or null.

**Type:** `string?`

### <a id="m-hovered"></a>Hovered

```csharp
public string? Hovered { get; init; }
```

The element under the pointer, or null.

**Type:** `string?`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's tick; input is applied at the next frame, so step to see its effect.

**Type:** `long`
