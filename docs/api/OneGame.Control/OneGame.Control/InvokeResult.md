[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# InvokeResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`InvokeResult`](InvokeResult.md)&gt;

```csharp
public sealed record InvokeResult : IEquatable<InvokeResult>
```

Result of `ui_invoke`.

## Constructors

| Member | Description |
| --- | --- |
| [`InvokeResult(string, bool, long)`](#m-ctor-string-bool-long) | Result of `ui_invoke`. |

### <a id="m-ctor-string-bool-long"></a>InvokeResult(string, bool, long)

```csharp
public InvokeResult(string Target, bool Invoked, long Tick);
```

Result of `ui_invoke`.

**Parameters**

- `Target` (`string`)
- `Invoked` (`bool`)
- `Tick` (`long`)

## Properties

| Member | Description |
| --- | --- |
| [`Invoked`](#m-invoked) |  |
| [`Target`](#m-target) |  |
| [`Tick`](#m-tick) |  |

### <a id="m-invoked"></a>Invoked

```csharp
public bool Invoked { get; init; }
```

**Type:** `bool`

### <a id="m-target"></a>Target

```csharp
public string Target { get; init; }
```

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

**Type:** `long`
