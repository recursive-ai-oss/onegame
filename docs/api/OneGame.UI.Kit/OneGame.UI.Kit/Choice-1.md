[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# Choice&lt;T&gt; record

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Choice`](Choice-1.md)`<T>>`

```csharp
public sealed record Choice<T> : IEquatable<Choice<T>>
```

One option of a radio group or drop-down: the value it stands for and the text shown.

**Type parameters**

- `T`: The value type.

## Constructors

| Member | Description |
| --- | --- |
| [`Choice(T, string)`](#m-ctor-t-string) | One option of a radio group or drop-down: the value it stands for and the text shown. |

### <a id="m-ctor-t-string"></a>Choice(T, string)

```csharp
public Choice(T Value, string Label);
```

One option of a radio group or drop-down: the value it stands for and the text shown.

**Type parameters**

- `T`: The value type.

**Parameters**

- `Value` (`T`): The value the option stands for.
- `Label` (`string`): The text shown for it.

## Properties

| Member | Description |
| --- | --- |
| [`Label`](#m-label) | The text shown for it. |
| [`Value`](#m-value) | The value the option stands for. |

### <a id="m-label"></a>Label

```csharp
public string Label { get; init; }
```

The text shown for it.

**Type:** `string`

### <a id="m-value"></a>Value

```csharp
public T Value { get; init; }
```

The value the option stands for.

**Type:** `T`
