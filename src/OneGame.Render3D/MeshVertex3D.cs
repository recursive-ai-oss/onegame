using System.Numerics;
using System.Runtime.InteropServices;

namespace OneGame.Render3D;

/// <summary>
/// One vertex of a <see cref="DynamicMesh3D"/>. Positions are in mesh space; the normal should be unit length. Colour (linear RGBA) multiplies the
/// material's base colour, so a mesher can bake shading or ambient occlusion into it.
/// </summary>
/// <param name="Position">Where the vertex is.</param>
/// <param name="Normal">Surface direction.</param>
/// <param name="Uv">Texture coordinates of the base colour, emissive and other textures.</param>
/// <param name="Color">Linear RGBA multiplied into the base colour (white leaves it unchanged).</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct MeshVertex3D(Vector3 Position, Vector3 Normal, Vector2 Uv, Vector4 Color)
{
    /// <summary>A vertex with no vertex colour.</summary>
    /// <param name="position">Where the vertex is.</param>
    /// <param name="normal">Surface direction.</param>
    /// <param name="uv">Texture coordinates.</param>
    public MeshVertex3D(Vector3 position, Vector3 normal, Vector2 uv)
        : this(position, normal, uv, Vector4.One)
    {
    }

    /// <summary>A vertex that picks a layer of a layered material (<see cref="Renderer3D.CreateLayeredMaterial"/>).</summary>
    /// <param name="position">Where the vertex is.</param>
    /// <param name="normal">Surface direction.</param>
    /// <param name="uv">Texture coordinates within the layer; they repeat outside 0 to 1.</param>
    /// <param name="color">Linear RGBA multiplied into the base colour.</param>
    /// <param name="layer">Layer index. Give all three vertices of a triangle the same one.</param>
    public MeshVertex3D(Vector3 position, Vector3 normal, Vector2 uv, Vector4 color, int layer)
        : this(position, normal, uv, color)
    {
        Layer = layer;
    }

    /// <summary>
    /// Which layer of a layered material this vertex samples (rounded to the nearest whole number; out-of-range values use the first or last
    /// layer). Ignored by other materials. 0 by default.
    /// </summary>
    public float Layer { get; init; }
}
