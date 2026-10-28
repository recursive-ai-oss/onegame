[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamInputSetInfo record

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamInputSetInfo`](SteamInputSetInfo.md)&gt;

```csharp
public sealed record SteamInputSetInfo : IEquatable<SteamInputSetInfo>
```

A declared set, for the report.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamInputSetInfo(string, IReadOnlyList<string>, IReadOnlyList<string>)`](#m-ctor-string-ireadonlylist-string-ireadonlylist-string) | A declared set, for the report. |

### <a id="m-ctor-string-ireadonlylist-string-ireadonlylist-string"></a>SteamInputSetInfo(string, IReadOnlyList&lt;string&gt;, IReadOnlyList&lt;string&gt;)

```csharp
public SteamInputSetInfo(string Name, IReadOnlyList<string> Screens, IReadOnlyList<string> Actions);
```

A declared set, for the report.

**Parameters**

- `Name` (`string`): The set's name.
- `Screens` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`): The screens it is active on.
- `Actions` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`): Its action names.

## Properties

| Member | Description |
| --- | --- |
| [`Actions`](#m-actions) | Its action names. |
| [`Name`](#m-name) | The set's name. |
| [`Screens`](#m-screens) | The screens it is active on. |

### <a id="m-actions"></a>Actions

```csharp
public IReadOnlyList<string> Actions { get; init; }
```

Its action names.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The set's name.

**Type:** `string`

### <a id="m-screens"></a>Screens

```csharp
public IReadOnlyList<string> Screens { get; init; }
```

The screens it is active on.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`
