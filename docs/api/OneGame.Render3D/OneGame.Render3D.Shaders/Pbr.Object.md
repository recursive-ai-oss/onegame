[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D.Shaders

# Pbr.Object struct

**Namespace:** `OneGame.Render3D.Shaders`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Declared in:** [`Pbr`](Pbr.md)

```csharp
public struct Pbr.Object
```

## Fields

| Member | Description |
| --- | --- |
| [`Model`](#m-model) |  |
| [`NormalMat`](#m-normalmat) |  |
| [`ObjInfo_W`](#m-objinfo-w) |  |
| [`ObjInfo_X`](#m-objinfo-x) |  |
| [`ObjInfo_Y`](#m-objinfo-y) |  |
| [`ObjInfo_Z`](#m-objinfo-z) |  |
| [`Size`](#m-size) |  |

### <a id="m-model"></a>Model

```csharp
public Matrix4x4 Model;
```

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

### <a id="m-normalmat"></a>NormalMat

```csharp
public Matrix4x4 NormalMat;
```

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

### <a id="m-objinfo-w"></a>ObjInfo\_W

```csharp
public uint ObjInfo_W;
```

**Type:** `uint`

### <a id="m-objinfo-x"></a>ObjInfo\_X

```csharp
public uint ObjInfo_X;
```

**Type:** `uint`

### <a id="m-objinfo-y"></a>ObjInfo\_Y

```csharp
public uint ObjInfo_Y;
```

**Type:** `uint`

### <a id="m-objinfo-z"></a>ObjInfo\_Z

```csharp
public uint ObjInfo_Z;
```

**Type:** `uint`

### <a id="m-size"></a>Size

```csharp
public const uint Size = 144;
```

**Type:** `uint`
