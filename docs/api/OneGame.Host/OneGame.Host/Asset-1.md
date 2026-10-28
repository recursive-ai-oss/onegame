[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# Asset&lt;T&gt; class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Inherits:** [`Asset`](Asset.md)

```csharp
public sealed class Asset<T> : Asset where T : class
```

A loaded file of type `T`.

**Type parameters**

- `T`: The loaded type: [`Texture2D`](../../OneGame.Render2D/OneGame.Render2D/Texture2D.md), a data type, [`Sound`](../../OneGame.Audio/OneGame.Audio/Sound.md), [`GltfModel`](../../OneGame.Gltf/OneGame.Gltf/GltfModel.md) or [`LoadedFont`](LoadedFont.md).

## Properties

| Member | Description |
| --- | --- |
| [`Value`](#m-value) | The content now. |

### <a id="m-value"></a>Value

```csharp
public T Value { get; }
```

The content now. Finishes a background load first (waiting for the decode). Textures are updated in place, so the same object keeps being returned after a reload.

**Type:** `T`

**Exceptions**

- [`AssetException`](AssetException.md): The load failed, or the content needs a device that is not attached yet.
