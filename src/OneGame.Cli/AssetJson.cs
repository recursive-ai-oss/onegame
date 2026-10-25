namespace OneGame.Cli;

/// <summary>An axis-aligned box as two [x, y, z] arrays.</summary>
internal sealed record AssetBox(float[] Min, float[] Max);

/// <summary>Totals over the whole model. Vertices and indices are those the engine draws (strips expanded, flat-shaded meshes unwelded).</summary>
internal sealed record AssetCounts(
    int Scenes, int Nodes, int Meshes, int Primitives, long Vertices, long Indices, long Triangles, long Lines, long Points, int MorphTargets,
    int Materials, int Textures, int Images, int Samplers, int Skins, int Animations, int Cameras, int Lights);

/// <summary>The node tree in brief.</summary>
internal sealed record AssetNodes(int Count, int MaxDepth, int RootCount, string[] Roots, int WithMesh, int Skinned, int WithCamera, int WithLight);

/// <summary>One mesh.</summary>
internal sealed record AssetMesh(int Index, string Name, int Primitives, int Vertices, int Indices, int MorphTargets, int[] Materials, bool HasTangents, bool Skinned, AssetBox Bounds);

/// <summary>One material; <c>Extensions</c> names the material extensions it uses.</summary>
internal sealed record AssetMaterial(int Index, string Name, string AlphaMode, bool DoubleSided, bool Unlit, int TextureSlots, string[] Extensions);

/// <summary>One image; no pixels are decoded for an inspection.</summary>
internal sealed record AssetImage(int Index, string Name, string Format, string MimeType, int Width, int Height, string Status, bool UsedAsColor, bool UsedAsData);

/// <summary>One animation clip.</summary>
internal sealed record AssetAnimation(int Index, string Name, float Duration, int Channels, string[] Paths, string[] Interpolations);

/// <summary>The extension lists of the file.</summary>
internal sealed record AssetExtensions(string[] Used, string[] Required);

/// <summary>Something that loaded but was degraded or ignored.</summary>
internal sealed record AssetWarning(string Code, string Message);

/// <summary>Result of <c>onegame asset inspect</c>. Lists hold at most <c>--max-items</c> entries and <c>Truncated</c> says when one was cut.</summary>
internal sealed record AssetInspectResult(
    bool Ok, string File, string Format, long Bytes, string Generator, string Copyright, AssetCounts Counts, AssetBox? Bounds, AssetNodes Nodes,
    AssetMesh[] Meshes, AssetMaterial[] Materials, AssetImage[] Images, AssetAnimation[] Animations, AssetExtensions Extensions,
    AssetWarning[] Warnings, bool Truncated, long OutputBytes, double Seconds);
