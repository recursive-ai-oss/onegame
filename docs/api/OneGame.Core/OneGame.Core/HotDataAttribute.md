[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# HotDataAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Struct`

```csharp
public sealed class HotDataAttribute : Attribute
```

Marks a struct whose public fields become the columns of a generated struct-of-arrays table (`<Name>Table`) for hot sims.

## Constructors

| Member | Description |
| --- | --- |
| [`HotDataAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>HotDataAttribute()

```csharp
public HotDataAttribute();
```

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The table's name in snapshots. |
| [`Visibility`](#m-visibility) | Whether subscribers receive the table. |

### <a id="m-name"></a>Name

```csharp
public string? Name { get; set; }
```

The table's name in snapshots. Defaults to the struct's name.

**Type:** `string?`

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; set; }
```

Whether subscribers receive the table. Only [`Visibility.Everyone`](Visibility.md#m-everyone) (default) and [`Visibility.Hidden`](Visibility.md#m-hidden) apply: rows have no owner.

**Type:** [`Visibility`](Visibility.md)
