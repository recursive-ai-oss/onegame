[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# JointIndices struct

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`JointIndices`](JointIndices.md)&gt;

```csharp
public readonly struct JointIndices : IEquatable<JointIndices>
```

Four joint indices of a skinned vertex.

## Constructors

| Member | Description |
| --- | --- |
| [`JointIndices(ushort, ushort, ushort, ushort)`](#m-ctor-ushort-ushort-ushort-ushort) | Four joint indices of a skinned vertex. |

### <a id="m-ctor-ushort-ushort-ushort-ushort"></a>JointIndices(ushort, ushort, ushort, ushort)

```csharp
public JointIndices(ushort A, ushort B, ushort C, ushort D);
```

Four joint indices of a skinned vertex.

**Parameters**

- `A` (`ushort`): First joint.
- `B` (`ushort`): Second joint.
- `C` (`ushort`): Third joint.
- `D` (`ushort`): Fourth joint.

## Properties

| Member | Description |
| --- | --- |
| [`A`](#m-a) | First joint. |
| [`B`](#m-b) | Second joint. |
| [`C`](#m-c) | Third joint. |
| [`D`](#m-d) | Fourth joint. |

### <a id="m-a"></a>A

```csharp
public ushort A { get; init; }
```

First joint.

**Type:** `ushort`

### <a id="m-b"></a>B

```csharp
public ushort B { get; init; }
```

Second joint.

**Type:** `ushort`

### <a id="m-c"></a>C

```csharp
public ushort C { get; init; }
```

Third joint.

**Type:** `ushort`

### <a id="m-d"></a>D

```csharp
public ushort D { get; init; }
```

Fourth joint.

**Type:** `ushort`
