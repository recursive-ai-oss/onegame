[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# SchemaEntry struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SchemaEntry`](SchemaEntry.md)&gt;

```csharp
public readonly struct SchemaEntry : IEquatable<SchemaEntry>
```

The schema version a save holds for one entity type.

## Constructors

| Member | Description |
| --- | --- |
| [`SchemaEntry(uint, string, uint)`](#m-ctor-uint-string-uint) | The schema version a save holds for one entity type. |

### <a id="m-ctor-uint-string-uint"></a>SchemaEntry(uint, string, uint)

```csharp
public SchemaEntry(uint TypeId, string TypeName, uint Version);
```

The schema version a save holds for one entity type.

**Parameters**

- `TypeId` (`uint`): The entity type id.
- `TypeName` (`string`): The type's name.
- `Version` (`uint`): The schema version it was written with.

## Properties

| Member | Description |
| --- | --- |
| [`TypeId`](#m-typeid) | The entity type id. |
| [`TypeName`](#m-typename) | The type's name. |
| [`Version`](#m-version) | The schema version it was written with. |

### <a id="m-typeid"></a>TypeId

```csharp
public uint TypeId { get; init; }
```

The entity type id.

**Type:** `uint`

### <a id="m-typename"></a>TypeName

```csharp
public string TypeName { get; init; }
```

The type's name.

**Type:** `string`

### <a id="m-version"></a>Version

```csharp
public uint Version { get; init; }
```

The schema version it was written with.

**Type:** `uint`
