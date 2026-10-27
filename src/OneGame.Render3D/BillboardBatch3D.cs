using OneGame.Gpu;

namespace OneGame.Render3D;

/// <summary>One camera-facing quad of a <see cref="BillboardBatch3D"/>.</summary>
public struct Billboard3D
{
    /// <summary>Centre in world space.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Width and height in world units.</summary>
    public Vector2 Size { get; set; }

    /// <summary>Rotation in radians about the view direction; positive turns clockwise as seen from the camera.</summary>
    public float Rotation { get; set; }

    /// <summary>Linear-light RGB with straight alpha. RGB above 1 is allowed: the scene is high dynamic range, so bright quads bloom.</summary>
    public Vector4 Color { get; set; }

    /// <summary>The part of the texture to show as (u, v, width, height) in 0..1 with the origin at the top-left; a zero rectangle means the whole texture.</summary>
    public Vector4 Uv { get; set; }

    /// <summary>True to add the colour to the scene instead of blending it over; additive quads need no sorting.</summary>
    public bool Additive { get; set; }
}

/// <summary>
/// Quads that always face the camera, drawn after the opaque and blended surfaces: particles, glows, lens spots, markers. They are depth
/// tested against the scene but write no depth, so they can be seen through each other. Blended quads of a batch are sorted far to near
/// each frame; additive ones are order independent. The batch reuses its storage, so refilling it each frame allocates nothing once grown.
/// Add it to <see cref="Scene3D.Billboards"/>.
/// </summary>
public sealed class BillboardBatch3D
{
    private Billboard3D[] _items = new Billboard3D[64];

    /// <summary>The image the quads show; null shows the renderer's soft round spot (<see cref="Renderer3D.SoftSpot"/>).</summary>
    public BillboardTexture3D? Texture { get; set; }

    /// <summary>Whether the batch is drawn.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>A name for debugging and the render graph report.</summary>
    public string Name { get; set; } = "billboards";

    /// <summary>Number of quads.</summary>
    public int Count { get; private set; }

    internal Billboard3D[] Raw => _items;

    /// <summary>Adds a quad.</summary>
    /// <param name="quad">The quad.</param>
    public void Add(in Billboard3D quad)
    {
        if (Count == _items.Length)
        {
            Array.Resize(ref _items, Count * 2);
        }

        _items[Count++] = quad;
    }

    /// <summary>Removes every quad.</summary>
    public void Clear() => Count = 0;
}

/// <summary>An image for billboards, uploaded with its colours premultiplied by alpha in linear light. Create it with <see cref="Renderer3D.CreateBillboardTexture"/> and dispose it before the renderer.</summary>
public sealed class BillboardTexture3D : IDisposable
{
    internal BillboardTexture3D(GpuTexture texture, GpuBindGroup group, int width, int height)
    {
        Texture = texture;
        Group = group;
        Width = width;
        Height = height;
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    internal GpuTexture Texture { get; }

    internal GpuBindGroup Group { get; }

    /// <summary>Releases the GPU image.</summary>
    public void Dispose()
    {
        Group.Dispose();
        Texture.Dispose();
    }
}
