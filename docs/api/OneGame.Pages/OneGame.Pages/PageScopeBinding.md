[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageScopeBinding struct

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PageScopeBinding`](PageScopeBinding.md)&gt;

```csharp
public readonly struct PageScopeBinding : IEquatable<PageScopeBinding>
```

A resolved host-scope binding.

## Constructors

| Member | Description |
| --- | --- |
| [`PageScopeBinding(PageScopeKind, int)`](#m-ctor-pagescopekind-int) | A resolved host-scope binding. |

### <a id="m-ctor-pagescopekind-int"></a>PageScopeBinding(PageScopeKind, int)

```csharp
public PageScopeBinding(PageScopeKind Kind, int Layer = 0);
```

A resolved host-scope binding.

**Parameters**

- `Kind` ([`PageScopeKind`](PageScopeKind.md)): What the scope does.
- `Layer` (`int`): The layer a [`PageScopeKind.Layer`](PageScopeKind.md#m-layer) scope draws with.

## Properties

| Member | Description |
| --- | --- |
| [`Kind`](#m-kind) | What the scope does. |
| [`Layer`](#m-layer) | The layer a [`PageScopeKind.Layer`](PageScopeKind.md#m-layer) scope draws with. |

### <a id="m-kind"></a>Kind

```csharp
public PageScopeKind Kind { get; init; }
```

What the scope does.

**Type:** [`PageScopeKind`](PageScopeKind.md)

### <a id="m-layer"></a>Layer

```csharp
public int Layer { get; init; }
```

The layer a [`PageScopeKind.Layer`](PageScopeKind.md#m-layer) scope draws with.

**Type:** `int`
