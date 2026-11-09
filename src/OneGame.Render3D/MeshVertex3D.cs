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
}
