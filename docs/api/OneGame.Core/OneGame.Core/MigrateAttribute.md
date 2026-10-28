[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# MigrateAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Method`

```csharp
public sealed class MigrateAttribute : Attribute
```

Marks a static method on an entity that loads state saved at an older schema version into the current layout: `static void Name(ref SyncReader old, MyEntity entity)`.

## Constructors

| Member | Description |
| --- | --- |
| [`MigrateAttribute(uint)`](#m-ctor-uint) | Creates the attribute for state saved at `fromVersion`. |

### <a id="m-ctor-uint"></a>MigrateAttribute(uint)

```csharp
public MigrateAttribute(uint fromVersion);
```

Creates the attribute for state saved at `fromVersion`.

**Parameters**

- `fromVersion` (`uint`)

## Properties

| Member | Description |
| --- | --- |
| [`FromVersion`](#m-fromversion) | The older schema version this method reads. |

### <a id="m-fromversion"></a>FromVersion

```csharp
public uint FromVersion { get; }
```

The older schema version this method reads.

**Type:** `uint`
