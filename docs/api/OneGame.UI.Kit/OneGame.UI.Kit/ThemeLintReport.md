[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# ThemeLintReport record

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ThemeLintReport`](ThemeLintReport.md)&gt;

```csharp
public sealed record ThemeLintReport : IEquatable<ThemeLintReport>
```

The outcome of [`ThemeLint`](ThemeLint.md).

## Constructors

| Member | Description |
| --- | --- |
| [`ThemeLintReport(bool, int, IReadOnlyList<ContrastCheck>, IReadOnlyList<ContrastCheck>)`](#m-ctor-bool-int-ireadonlylist-contrastcheck-ireadonlylist-contrastcheck) | The outcome of [`ThemeLint`](ThemeLint.md). |

### <a id="m-ctor-bool-int-ireadonlylist-contrastcheck-ireadonlylist-contrastcheck"></a>ThemeLintReport(bool, int, IReadOnlyList&lt;ContrastCheck&gt;, IReadOnlyList&lt;ContrastCheck&gt;)

```csharp
public ThemeLintReport(bool Passed, int Checked, IReadOnlyList<ContrastCheck> Failures, IReadOnlyList<ContrastCheck> Checks);
```

The outcome of [`ThemeLint`](ThemeLint.md).

**Parameters**

- `Passed` (`bool`): True when every pair passed.
- `Checked` (`int`): How many pairs were checked.
- `Failures` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ContrastCheck`](ContrastCheck.md)&gt;): The pairs that failed, worst first.
- `Checks` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ContrastCheck`](ContrastCheck.md)&gt;): Every pair, in a fixed order.

## Properties

| Member | Description |
| --- | --- |
| [`Checked`](#m-checked) | How many pairs were checked. |
| [`Checks`](#m-checks) | Every pair, in a fixed order. |
| [`Failures`](#m-failures) | The pairs that failed, worst first. |
| [`Passed`](#m-passed) | True when every pair passed. |

### <a id="m-checked"></a>Checked

```csharp
public int Checked { get; init; }
```

How many pairs were checked.

**Type:** `int`

### <a id="m-checks"></a>Checks

```csharp
public IReadOnlyList<ContrastCheck> Checks { get; init; }
```

Every pair, in a fixed order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ContrastCheck`](ContrastCheck.md)&gt;

### <a id="m-failures"></a>Failures

```csharp
public IReadOnlyList<ContrastCheck> Failures { get; init; }
```

The pairs that failed, worst first.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ContrastCheck`](ContrastCheck.md)&gt;

### <a id="m-passed"></a>Passed

```csharp
public bool Passed { get; init; }
```

True when every pair passed.

**Type:** `bool`
