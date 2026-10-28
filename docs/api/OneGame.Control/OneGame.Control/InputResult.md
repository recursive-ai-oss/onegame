[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# InputResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`InputResult`](InputResult.md)&gt;

```csharp
public sealed record InputResult : IEquatable<InputResult>
```

Result of the input operations.

## Constructors

| Member | Description |
| --- | --- |
| [`InputResult(string?, string?, long)`](#m-ctor-string-string-long) | Result of the input operations. |

### <a id="m-ctor-string-string-long"></a>InputResult(string?, string?, long)

```csharp
public InputResult(string? Focused, string? Hovered, long Tick);
```

Result of the input operations.

**Parameters**

- `Focused` (`string?`)
- `Hovered` (`string?`)
- `Tick` (`long`)

## Properties

| Member | Description |
| --- | --- |
| [`Focused`](#m-focused) |  |
| [`Hovered`](#m-hovered) |  |
| [`Tick`](#m-tick) |  |

### <a id="m-focused"></a>Focused

```csharp
public string? Focused { get; init; }
```

**Type:** `string?`

### <a id="m-hovered"></a>Hovered

```csharp
public string? Hovered { get; init; }
```

**Type:** `string?`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

**Type:** `long`
