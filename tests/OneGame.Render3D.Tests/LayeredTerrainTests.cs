using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>A blocky terrain drawn through layered materials: a committed reference, and the draw count against one material per texture.</summary>
public class LayeredTerrainTests
{
    private const int ChunkSize = 12;

    private static int Hash(int x, int z)
    {
        var h = (uint)(x * 73856093) ^ (uint)(z * 19349663);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        return (int)(h & 0x7fffffff);
    }

    private static int Height(int x, int z) => 1 + (Hash(x / 3, z / 3) % 3) + (Hash(x, z) % 2);

    // 16x16 block texture: a base colour per layer with a darker border and a few dark specks, so repeats and texel edges are visible.
    private static byte[] BlockImage(int layer)
    {
        var px = new byte[16 * 16 * 4];
        var hue = layer * 1.0f / 8f;
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                var edge = x == 0 || y == 0 || x == 15 || y == 15;
                var speck = Hash(x + (layer * 16), y) % 7 == 0;
                var k = edge ? 0.45f : speck ? 0.7f : 1f;
                var o = ((y * 16) + x) * 4;
                px[o] = (byte)(k * (60 + (190 * MathF.Abs(MathF.Sin(hue * 6.28f)))));
                px[o + 1] = (byte)(k * (60 + (190 * MathF.Abs(MathF.Sin((hue * 6.28f) + 2f)))));
                px[o + 2] = (byte)(k * (60 + (190 * MathF.Abs(MathF.Sin((hue * 6.28f) + 4f)))));
                px[o + 3] = 255;
            }
        }

        return px;
    }

    private static int LayerAt(int x, int y, int z, int layers) => Hash(x + (y * 31), z + (y * 17)) % layers;

    /// <summary>Top and side faces of the columns in one chunk. layerOf picks the layer per face; it is also the texture the face needs in a per-texture world.</summary>
    private static void Chunk(int cx, int cz, int layers, Action<int, MeshVertex3D[], uint[]> emit, bool perTexture)
    {
        var meshes = new Dictionary<int, (List<MeshVertex3D> V, List<uint> I)>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, int layer)
        {
            var key = perTexture ? layer : 0;
            if (!meshes.TryGetValue(key, out var m))
            {
                m = ([], []);
                meshes[key] = m;
            }

            var i = (uint)m.V.Count;
            m.V.Add(new MeshVertex3D(a, n, new Vector2(0, 0), Vector4.One, layer));
            m.V.Add(new MeshVertex3D(b, n, new Vector2(1, 0), Vector4.One, layer));
            m.V.Add(new MeshVertex3D(c, n, new Vector2(1, 1), Vector4.One, layer));
            m.V.Add(new MeshVertex3D(d, n, new Vector2(0, 1), Vector4.One, layer));
            m.I.AddRange([i, i + 2, i + 1, i, i + 3, i + 2]);
        }

        for (var lx = 0; lx < ChunkSize; lx++)
        {
            for (var lz = 0; lz < ChunkSize; lz++)
            {
                int x = (cx * ChunkSize) + lx, z = (cz * ChunkSize) + lz;
                var h = Height(x, z);
                Quad(new Vector3(x, h, z), new Vector3(x + 1, h, z), new Vector3(x + 1, h, z + 1), new Vector3(x, h, z + 1), Vector3.UnitY, LayerAt(x, h, z, layers));

                // Side walls where the neighbour is lower (down to the neighbour's top), one quad per block.
                foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var nh = Height(x + dx, z + dz);
                    for (var y = Math.Max(nh, 0); y < h; y++)
                    {
                        var n = new Vector3(dx, 0, dz);
                        var (p0, p1) = (dx, dz) switch
                        {
                            (1, 0) => (new Vector3(x + 1, 0, z + 1), new Vector3(x + 1, 0, z)),
                            (-1, 0) => (new Vector3(x, 0, z), new Vector3(x, 0, z + 1)),
                            (0, 1) => (new Vector3(x, 0, z + 1), new Vector3(x + 1, 0, z + 1)),
                            _ => (new Vector3(x + 1, 0, z), new Vector3(x, 0, z)),
                        };
                        Quad(p0 + new Vector3(0, y + 1, 0), p1 + new Vector3(0, y + 1, 0), p1 + new Vector3(0, y, 0), p0 + new Vector3(0, y, 0), n, LayerAt(x, y, z, layers));
                    }
                }
            }
        }

        foreach (var (key, m) in meshes)
        {
            emit(key, [.. m.V], [.. m.I]);
        }
    }

    private static Scene3D Scene(float aspect, Vector3 centre)
    {
        var scene = new Scene3D { Camera = Camera3D.LookAt(centre + new Vector3(18, 22, 26), centre, Vector3.UnitY), Samples = 1 };
        scene.Camera.Far = 500f;
        scene.Environment.Intensity = 0.4f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = new Vector3(0.25f, 0.4f, 0.6f);
        scene.Post.Bloom = false;
        scene.Lights.Add(Light3D.Directional(new Vector3(-0.4f, -1f, -0.3f), Vector3.One, 3f));
        scene.Fog.Enabled = true;
        scene.Fog.Color = new Vector3(0.25f, 0.4f, 0.6f);
        scene.Fog.Start = 40;
        scene.Fog.End = 90;
        scene.Fog.MaxDensity = 0.8f;
        return scene;
    }

    private static ReadOnlyMemory<byte>[] Images(int n) => [.. Enumerable.Range(0, n).Select(l => (ReadOnlyMemory<byte>)BlockImage(l))];

    [Fact]
    public void TerrainWithEightLayersMatchesItsReference()
    {
        const int w = 320, h = 240;
        using var rig = new Rig(w, h, GpuFormat.Rgba8UnormSrgb);
        var material = rig.Renderer.CreateLayeredMaterial("blocks", 16, 16, Images(8));
        var scene = Scene(w / (float)h, new Vector3(ChunkSize, 2, ChunkSize));
        for (var cz = 0; cz < 2; cz++)
        {
            for (var cx = 0; cx < 2; cx++)
            {
                Chunk(cx, cz, 8, (_, v, i) => scene.Add(rig.Own(rig.Renderer.CreateMesh(material, v, i, "chunk")).Model), false);
            }
        }

        var pixels = rig.Capture(scene, "layered-terrain");
        Assert.Equal(4, rig.Renderer.LastStats.DrawCalls);
        Reference.Compare("layered-terrain", pixels, w, h, meanTolerance: 0.6, maxTolerance: 16);
    }

    [Fact]
    public void OneLayeredMaterialCutsTheDrawsToOnePerChunkAndSteadyFramesAllocateNothing()
    {
        const int layers = 32, chunks = 6;
        using var rig = new Rig(160, 120, GpuFormat.Rgba8UnormSrgb);
        var images = Images(layers);

        // Before: one material (and so one mesh and one draw) per texture per chunk.
        var perTexture = new Material3D[layers];
        for (var l = 0; l < layers; l++)
        {
            perTexture[l] = rig.Renderer.CreateLayeredMaterial("single" + l, 16, 16, new[] { images[l] });
        }

        var before = Scene(160 / 120f, new Vector3(chunks * ChunkSize / 2f, 2, chunks * ChunkSize / 2f));
        var after = Scene(160 / 120f, new Vector3(chunks * ChunkSize / 2f, 2, chunks * ChunkSize / 2f));
        var layered = rig.Renderer.CreateLayeredMaterial("blocks", 16, 16, images);
        for (var cz = 0; cz < chunks; cz++)
        {
            for (var cx = 0; cx < chunks; cx++)
            {
                // The per-texture meshes use layer 0 of their one-layer material: the vertex layer is clamped to it.
                Chunk(cx, cz, layers, (key, v, i) => before.Add(rig.Own(rig.Renderer.CreateMesh(perTexture[key], v, i, "chunk")).Model), true);
                Chunk(cx, cz, layers, (_, v, i) => after.Add(rig.Own(rig.Renderer.CreateMesh(layered, v, i, "chunk")).Model), false);
            }
        }

        rig.Target.Render(before);
        var batchesBefore = rig.Renderer.LastStats.DrawCalls;
        rig.Target.Render(after);
        var batchesAfter = rig.Renderer.LastStats.DrawCalls;
        TestContext.Current.SendDiagnosticMessage($"draws per frame: {batchesBefore} with one material per texture, {batchesAfter} with one layered material ({chunks * chunks} chunks, {layers} textures)");
        Assert.Equal(chunks * chunks, batchesAfter);
        Assert.True(batchesBefore > batchesAfter * 10, $"{batchesBefore} vs {batchesAfter}");

        for (var i = 0; i < 4; i++)
        {
            rig.Target.Render(after);
        }

        rig.Device.WaitIdle();
        rig.Target.Render(after);
        var a0 = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 30; i++)
        {
            rig.Target.Render(after);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - a0;
        rig.Device.WaitIdle();
        Assert.True(allocated == 0, $"30 steady frames allocated {allocated} bytes");
    }
}
