[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# TransmissionInfo class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class TransmissionInfo
```

KHR\_materials\_transmission.

## Constructors

| Member | Description |
| --- | --- |
| [`TransmissionInfo()`](#m-ctor) |  |

### <a id="m-ctor"></a>TransmissionInfo()

```csharp
public TransmissionInfo();
```

## Properties

| Member | Description |
| --- | --- |
| [`Factor`](#m-factor) | The fraction of light transmitted. |
| [`Texture`](#m-texture) | Transmission texture (red channel), or null. |

### <a id="m-factor"></a>Factor

```csharp
public float Factor { get; }
```

The fraction of light transmitted.

**Type:** `float`

### <a id="m-texture"></a>Texture

```csharp
public TextureRef? Texture { get; }
```

Transmission texture (red channel), or null.

**Type:** [`TextureRef`](TextureRef.md)?
