[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# BindingsLoadResult record

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`BindingsLoadResult`](BindingsLoadResult.md)&gt;

```csharp
public sealed record BindingsLoadResult : IEquatable<BindingsLoadResult>
```

What [`ActionMap.LoadText()`](ActionMap.md#m-loadtext-string) did.

## Constructors

| Member | Description |
| --- | --- |
| [`BindingsLoadResult(int, IReadOnlyList<string>)`](#m-ctor-int-ireadonlylist-string) | What [`ActionMap.LoadText()`](ActionMap.md#m-loadtext-string) did. |

### <a id="m-ctor-int-ireadonlylist-string"></a>BindingsLoadResult(int, IReadOnlyList&lt;string&gt;)

```csharp
public BindingsLoadResult(int Applied, IReadOnlyList<string> Problems);
```

What [`ActionMap.LoadText()`](ActionMap.md#m-loadtext-string) did.

**Parameters**

- `Applied` (`int`): How many action lines were applied.
- `Problems` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`): One message per rejected line; empty when the whole file was understood.

## Properties

| Member | Description |
| --- | --- |
| [`Applied`](#m-applied) | How many action lines were applied. |
| [`Problems`](#m-problems) | One message per rejected line; empty when the whole file was understood. |

### <a id="m-applied"></a>Applied

```csharp
public int Applied { get; init; }
```

How many action lines were applied.

**Type:** `int`

### <a id="m-problems"></a>Problems

```csharp
public IReadOnlyList<string> Problems { get; init; }
```

One message per rejected line; empty when the whole file was understood.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`
