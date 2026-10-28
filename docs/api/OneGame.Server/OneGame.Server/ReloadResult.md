[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ReloadResult record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ReloadResult`](ReloadResult.md)&gt;

```csharp
public sealed record ReloadResult : IEquatable<ReloadResult>
```

The result of a hot reload.

## Constructors

| Member | Description |
| --- | --- |
| [`ReloadResult(string, string, string, uint[], bool, bool)`](#m-ctor-string-string-string-uint-bool-bool) | The result of a hot reload. |

### <a id="m-ctor-string-string-string-uint-bool-bool"></a>ReloadResult(string, string, string, uint\[\], bool, bool)

```csharp
public ReloadResult(string Name, string OldBuild, string NewBuild, uint[] Scopes, bool Migrated, bool Collected);
```

The result of a hot reload.

**Parameters**

- `Name` (`string`): The plugin's name.
- `OldBuild` (`string`): The build that was replaced.
- `NewBuild` (`string`): The build now running.
- `Scopes` (`uint[]`): The scopes whose state went through a save and restore.
- `Migrated` (`bool`): True when a stored entity went through a schema migration.
- `Collected` (`bool`): True when the replaced build's load context was garbage collected.

## Properties

| Member | Description |
| --- | --- |
| [`Collected`](#m-collected) | True when the replaced build's load context was garbage collected. |
| [`Migrated`](#m-migrated) | True when a stored entity went through a schema migration. |
| [`Name`](#m-name) | The plugin's name. |
| [`NewBuild`](#m-newbuild) | The build now running. |
| [`OldBuild`](#m-oldbuild) | The build that was replaced. |
| [`Scopes`](#m-scopes) | The scopes whose state went through a save and restore. |

### <a id="m-collected"></a>Collected

```csharp
public bool Collected { get; init; }
```

True when the replaced build's load context was garbage collected.

**Type:** `bool`

### <a id="m-migrated"></a>Migrated

```csharp
public bool Migrated { get; init; }
```

True when a stored entity went through a schema migration.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The plugin's name.

**Type:** `string`

### <a id="m-newbuild"></a>NewBuild

```csharp
public string NewBuild { get; init; }
```

The build now running.

**Type:** `string`

### <a id="m-oldbuild"></a>OldBuild

```csharp
public string OldBuild { get; init; }
```

The build that was replaced.

**Type:** `string`

### <a id="m-scopes"></a>Scopes

```csharp
public uint[] Scopes { get; init; }
```

The scopes whose state went through a save and restore.

**Type:** `uint[]`
