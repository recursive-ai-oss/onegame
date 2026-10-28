[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamControllerInfo record

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamControllerInfo`](SteamControllerInfo.md)&gt;

```csharp
public sealed record SteamControllerInfo : IEquatable<SteamControllerInfo>
```

A controller, for the report.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamControllerInfo(int, string, string?)`](#m-ctor-int-string-string) | A controller, for the report. |

### <a id="m-ctor-int-string-string"></a>SteamControllerInfo(int, string, string?)

```csharp
public SteamControllerInfo(int Index, string Type, string? ActiveSet);
```

A controller, for the report.

**Parameters**

- `Index` (`int`): Its index.
- `Type` (`string`): Its type name.
- `ActiveSet` (`string?`): The set Steam says is active on it.

## Properties

| Member | Description |
| --- | --- |
| [`ActiveSet`](#m-activeset) | The set Steam says is active on it. |
| [`Index`](#m-index) | Its index. |
| [`Type`](#m-type) | Its type name. |

### <a id="m-activeset"></a>ActiveSet

```csharp
public string? ActiveSet { get; init; }
```

The set Steam says is active on it.

**Type:** `string?`

### <a id="m-index"></a>Index

```csharp
public int Index { get; init; }
```

Its index.

**Type:** `int`

### <a id="m-type"></a>Type

```csharp
public string Type { get; init; }
```

Its type name.

**Type:** `string`
