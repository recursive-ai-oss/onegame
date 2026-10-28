[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# RebindResult record

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`RebindResult`](RebindResult.md)&gt;

```csharp
public sealed record RebindResult : IEquatable<RebindResult>
```

The outcome of a rebinding capture.

## Constructors

| Member | Description |
| --- | --- |
| [`RebindResult(string, int, InputSource, bool, IReadOnlyList<string>)`](#m-ctor-string-int-inputsource-bool-ireadonlylist-string) | The outcome of a rebinding capture. |

### <a id="m-ctor-string-int-inputsource-bool-ireadonlylist-string"></a>RebindResult(string, int, InputSource, bool, IReadOnlyList&lt;string&gt;)

```csharp
public RebindResult(string Action, int Slot, InputSource Source, bool Cancelled, IReadOnlyList<string> Conflicts);
```

The outcome of a rebinding capture.

**Parameters**

- `Action` (`string`): The action name.
- `Slot` (`int`): The binding slot that changed.
- `Source` ([`InputSource`](InputSource.md)): The captured source, or none when cancelled.
- `Cancelled` (`bool`): True when the player cancelled (Escape) and nothing changed.
- `Conflicts` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`): Other actions in the same context that already used the source (before the policy was applied).

## Properties

| Member | Description |
| --- | --- |
| [`Action`](#m-action) | The action name. |
| [`Cancelled`](#m-cancelled) | True when the player cancelled (Escape) and nothing changed. |
| [`Conflicts`](#m-conflicts) | Other actions in the same context that already used the source (before the policy was applied). |
| [`Slot`](#m-slot) | The binding slot that changed. |
| [`Source`](#m-source) | The captured source, or none when cancelled. |

### <a id="m-action"></a>Action

```csharp
public string Action { get; init; }
```

The action name.

**Type:** `string`

### <a id="m-cancelled"></a>Cancelled

```csharp
public bool Cancelled { get; init; }
```

True when the player cancelled (Escape) and nothing changed.

**Type:** `bool`

### <a id="m-conflicts"></a>Conflicts

```csharp
public IReadOnlyList<string> Conflicts { get; init; }
```

Other actions in the same context that already used the source (before the policy was applied).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-slot"></a>Slot

```csharp
public int Slot { get; init; }
```

The binding slot that changed.

**Type:** `int`

### <a id="m-source"></a>Source

```csharp
public InputSource Source { get; init; }
```

The captured source, or none when cancelled.

**Type:** [`InputSource`](InputSource.md)
