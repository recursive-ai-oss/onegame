using System.Runtime.InteropServices;
using OneGame.Gpu;
using OneGame.Gpu.Graph;

namespace OneGame.Render3D;

// Shadow maps: every shadow-casting light gets tiles of one depth atlas (a cascade, a spot map, a point light face), drawn by one graph
// pass with a viewport per tile. The forward shader looks the tiles up with a comparison sampler and a percentage-closer filter.
public sealed partial class Renderer3D
{
    private struct TilePlan
    {
        public Matrix4x4 View;
        public Matrix4x4 Proj;
        public Matrix4x4 ViewProj;
    }

    private static readonly Vector3[] FaceDirections = [Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ];

    private readonly TilePlan[] _tiles = new TilePlan[MaxShadowTiles];
    private readonly ShadowRecord[] _shadowRecords = new ShadowRecord[MaxShadowLights];
    private readonly Light3D?[] _shadowLights = new Light3D?[MaxShadowLights];
    private readonly int[] _shadowTileCounts = new int[MaxShadowLights];
    private readonly bool[] _shadowPlanned = new bool[MaxShadowLights];
    private readonly Vector4[] _planes = new Vector4[MaxViews * 6];
    private readonly ShadowPass _shadowPass = new();
    private GpuBuffer _shadowBuffer = null!;
    private GpuSampler _shadowSampler = null!;
    private GpuTexture _shadowDummy = null!;
    private int _tileCount;
    private int _shadowCount;
    private int _tileSize;
    private int _atlasCols = 1;
    private int _atlasW = 1;
    private int _atlasH = 1;

    private void InitShadows()
    {
        var device = Device;
        _shadowBuffer = Own(device.CreateBuffer(new BufferDesc((ulong)(MaxShadowLights * Marshal.SizeOf<ShadowRecord>()), BufferUsage.Storage | BufferUsage.CopyDestination) { Label = "3d shadow records" }));
        _shadowSampler = Own(device.CreateSampler(SamplerDesc.Linear with { Compare = CompareFunction.LessEqual, MaxLod = 0f }));

        // Bound wherever no atlas exists (frames without shadows, and the shadow pass itself, which must not sample what it writes).
        _shadowDummy = Own(device.CreateTexture(new TextureDesc(1, 1, DepthFormat, TextureUsage.Sampled | TextureUsage.RenderTarget) { Label = "3d shadow placeholder" }));
        var encoder = device.BeginCommands();
        var pass = encoder.BeginRenderPass(ReadOnlySpan<ColorAttachment>.Empty, new DepthAttachment { Target = _shadowDummy, Load = LoadOp.Clear, Store = StoreOp.Store, ClearDepth = 1f });
        pass.EndPass();
        device.Submit(encoder);
    }

    // Decides which lights get shadow tiles this frame (directional first, in light order, within the tile budget) and sizes the atlas.
    private void BeginShadows(Scene3D scene, in QualitySettings q, int width, int height, float aspect, float near, float far)
    {
        _ = (width, height, aspect, near, far);
        var lights = 0;
        var tiles = 0;
        for (var pass = 0; pass < 2; pass++)
        {
            foreach (var l in scene.Lights)
            {
                if ((l.Kind == LightKind.Directional) != (pass == 0) || !l.CastShadows || lights == MaxShadowLights)
                {
                    continue;
                }

                var need = l.Kind switch
                {
                    LightKind.Directional => Math.Min(q.MaxCascades, MaxShadowTiles - tiles),
                    LightKind.Spot => 1,
                    _ => 6,
                };
                if (need <= 0 || tiles + need > MaxShadowTiles)
                {
                    continue;
                }

                _shadowLights[lights] = l;
                _shadowTileCounts[lights] = need;
                _shadowPlanned[lights] = false;
                lights++;
                tiles += need;
            }
        }

        _shadowCount = lights;
        _tileCount = 0;
        _atlasCols = Math.Max(1, (int)MathF.Ceiling(MathF.Sqrt(tiles)));
        var rows = Math.Max(1, (tiles + _atlasCols - 1) / _atlasCols);
        _tileSize = q.ShadowTileSize;
        var max = (int)Device.Capabilities.MaxTextureSize;
        while (_tileSize > 64 && (_tileSize * _atlasCols > max || _tileSize * rows > max))
        {
            _tileSize /= 2;
        }

        _atlasW = _atlasCols * _tileSize;
        _atlasH = rows * _tileSize;
    }

    private (Vector3 Center, float Radius) SceneSphere()
    {
        if (_sceneMin.X > _sceneMax.X)
        {
            return (Vector3.Zero, 0f);
        }

        return ((_sceneMin + _sceneMax) * 0.5f, Vector3.Distance(_sceneMin, _sceneMax) * 0.5f);
    }

    private static Vector3 UpFor(Vector3 dir) => MathF.Abs(dir.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;

    // Builds the tiles of one light and its record; returns the record index, or -1 when the light got no budget.
    private int PlanShadow(Scene3D scene, Light3D light, Camera3D camera, float aspect, float near, float far, in QualitySettings q)
    {
        var index = -1;
        for (var i = 0; i < _shadowCount; i++)
        {
            if (ReferenceEquals(_shadowLights[i], light) && !_shadowPlanned[i])
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return -1;
        }

        _shadowPlanned[index] = true;
        ref var rec = ref _shadowRecords[index];
        rec = default;
        var tiles = _shadowTileCounts[index];
        var dir = light.Direction.LengthSquared() > 0 ? Vector3.Normalize(light.Direction) : -Vector3.UnitZ;
        var (sceneCenter, sceneRadius) = SceneSphere();
        var settings = scene.Shadows;
        rec.P1 = new Vector4(1f / _atlasW, 1f / _atlasH, q.ShadowFilterRadius, 0f);

        if (light.Kind == LightKind.Directional)
        {
            // Practical split scheme (Zhang et al., "Parallel-Split Shadow Maps"): a mix of logarithmic and uniform slices of the view range.
            var reach = MathF.Min(settings.MaxDistance, far);
            if (reach <= near * 1.01f)
            {
                reach = near * 2f;
            }

            var pad = Math.Clamp(sceneRadius * 2f, 5f, 4000f);
            var previous = near;
            var splits = new Vector4();
            var texels = new Vector4();
            for (var c = 0; c < tiles; c++)
            {
                var t = (c + 1) / (float)tiles;
                var split = Lerp(near + ((reach - near) * t), near * MathF.Pow(reach / near, t), Math.Clamp(settings.SplitLambda, 0f, 1f));
                SliceSphere(camera, aspect, previous, split, out var center, out var radius);
                radius = MathF.Ceiling(radius * 16f) / 16f;

                // Snap the sphere centre to the shadow map's texel grid so shadow edges do not crawl when the camera moves.
                var texel = 2f * radius / _tileSize;
                var rotation = Matrix4x4.CreateLookAt(Vector3.Zero, dir, UpFor(dir));
                var local = Vector3.Transform(center, rotation);
                local.X = MathF.Floor(local.X / texel) * texel;
                local.Y = MathF.Floor(local.Y / texel) * texel;
                Matrix4x4.Invert(rotation, out var back);
                center = Vector3.Transform(local, back);

                var eye = center - (dir * (radius + pad));
                var view = Matrix4x4.CreateLookAt(eye, eye + dir, UpFor(dir));
                var proj = Matrix4x4.CreateOrthographic(2f * radius, 2f * radius, 0f, (2f * radius) + pad);
                AddTile(ref rec, c, view, proj);
                SetComponent(ref splits, c, split);
                SetComponent(ref texels, c, texel);
                previous = split;
            }

            for (var c = tiles; c < 4; c++)
            {
                SetComponent(ref splits, c, GetComponent(splits, tiles - 1));
            }

            rec.Splits = splits;
            rec.Texels = texels;
            rec.P0 = new Vector4(0f, tiles, settings.NormalOffset, 0f);
            return index;
        }

        var farPlane = light.Range > 0f ? light.Range : MathF.Max(Vector3.Distance(light.Position, sceneCenter) + sceneRadius, 1f);
        var nearPlane = MathF.Min(0.05f, farPlane * 0.01f);
        if (light.Kind == LightKind.Spot)
        {
            var fov = MathF.Min((2f * light.OuterConeAngle) + 0.12f, 3f);
            var view = Matrix4x4.CreateLookAt(light.Position, light.Position + dir, UpFor(dir));
            AddTile(ref rec, 0, view, Matrix4x4.CreatePerspectiveFieldOfView(fov, 1f, nearPlane, farPlane));
            rec.Texels = new Vector4(2f * MathF.Tan(fov * 0.5f) / _tileSize, 0, 0, 0);
            rec.P0 = new Vector4(1f, 1f, settings.NormalOffset, 0f);
            return index;
        }

        // Point light: six 90-degree frusta, widened by a few texels so filtering near a face edge stays inside its tile.
        var tanHalf = 1f + ((2f * (q.ShadowFilterRadius + 3)) / _tileSize);
        var faceFov = 2f * MathF.Atan(tanHalf);
        for (var f = 0; f < 6; f++)
        {
            var d = FaceDirections[f];
            var up = f is 2 or 3 ? Vector3.UnitZ : Vector3.UnitY;
            var view = Matrix4x4.CreateLookAt(light.Position, light.Position + d, up);
            AddTile(ref rec, f, view, Matrix4x4.CreatePerspectiveFieldOfView(faceFov, 1f, nearPlane, farPlane));
        }

        rec.Texels = new Vector4(2f * tanHalf / _tileSize, 0, 0, 0);
        rec.P0 = new Vector4(2f, 6f, settings.NormalOffset, 0f);
        return index;
    }

    private static float Lerp(float a, float b, float t) => a + ((b - a) * t);

    private static void SetComponent(ref Vector4 v, int i, float value)
    {
        switch (i)
        {
            case 0: v.X = value; break;
            case 1: v.Y = value; break;
            case 2: v.Z = value; break;
            default: v.W = value; break;
        }
    }

    private static float GetComponent(in Vector4 v, int i) => i switch { 0 => v.X, 1 => v.Y, 2 => v.Z, _ => v.W };

    // The bounding sphere of the part of the camera frustum between two view distances.
    private static void SliceSphere(Camera3D camera, float aspect, float d0, float d1, out Vector3 center, out float radius)
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        var world = camera.World;
        var tan = MathF.Tan(camera.YFov * 0.5f);
        for (var i = 0; i < 2; i++)
        {
            var d = i == 0 ? d0 : d1;
            var hh = camera.Orthographic ? camera.OrthoHeight * 0.5f : d * tan;
            var hw = hh * aspect;
            corners[(i * 4)] = Vector3.Transform(new Vector3(-hw, -hh, -d), world);
            corners[(i * 4) + 1] = Vector3.Transform(new Vector3(hw, -hh, -d), world);
            corners[(i * 4) + 2] = Vector3.Transform(new Vector3(-hw, hh, -d), world);
            corners[(i * 4) + 3] = Vector3.Transform(new Vector3(hw, hh, -d), world);
        }

        center = Vector3.Zero;
        foreach (var c in corners)
        {
            center += c;
        }

        center /= 8f;
        radius = 0f;
        foreach (var c in corners)
        {
            radius = MathF.Max(radius, Vector3.Distance(center, c));
        }
    }

    private void AddTile(ref ShadowRecord rec, int local, in Matrix4x4 view, in Matrix4x4 proj)
    {
        var global = _tileCount++;
        var vp = view * proj;
        _tiles[global] = new TilePlan { View = view, Proj = proj, ViewProj = vp };
        var col = global % _atlasCols;
        var row = global / _atlasCols;
        rec.Rect[local] = new Vector4(col * (float)_tileSize / _atlasW, row * (float)_tileSize / _atlasH, (float)_tileSize / _atlasW, (float)_tileSize / _atlasH);
        rec.Col[local * 4] = new Vector4(vp.M11, vp.M21, vp.M31, vp.M41);
        rec.Col[(local * 4) + 1] = new Vector4(vp.M12, vp.M22, vp.M32, vp.M42);
        rec.Col[(local * 4) + 2] = new Vector4(vp.M13, vp.M23, vp.M33, vp.M43);
        rec.Col[(local * 4) + 3] = new Vector4(vp.M14, vp.M24, vp.M34, vp.M44);
        ExtractPlanes(vp, _planes.AsSpan((1 + global) * 6, 6));
    }

    private void FinishShadows()
    {
        if (_shadowCount > 0)
        {
            Device.WriteBuffer(_shadowBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<ShadowRecord>(_shadowRecords, 0, _shadowCount)));
        }
    }

    /// <summary>Planes of a view frustum, pointing inward (Gribb and Hartmann), for a row-vector matrix with depth 0 to 1: a point is inside when dot(plane.xyz, p) + plane.w is at least 0 for all six.</summary>
    private static void ExtractPlanes(in Matrix4x4 m, Span<Vector4> planes)
    {
        var c1 = new Vector4(m.M11, m.M21, m.M31, m.M41);
        var c2 = new Vector4(m.M12, m.M22, m.M32, m.M42);
        var c3 = new Vector4(m.M13, m.M23, m.M33, m.M43);
        var c4 = new Vector4(m.M14, m.M24, m.M34, m.M44);
        planes[0] = c4 + c1;
        planes[1] = c4 - c1;
        planes[2] = c4 + c2;
        planes[3] = c4 - c2;
        planes[4] = c3;
        planes[5] = c4 - c3;
        for (var i = 0; i < 6; i++)
        {
            var n = MathF.Sqrt((planes[i].X * planes[i].X) + (planes[i].Y * planes[i].Y) + (planes[i].Z * planes[i].Z));
            if (n > 0f)
            {
                planes[i] /= n;
            }
        }
    }

    private static bool Inside(ReadOnlySpan<Vector4> planes, Vector3 center, float radius)
    {
        for (var i = 0; i < 6; i++)
        {
            var p = planes[i];
            if ((p.X * center.X) + (p.Y * center.Y) + (p.Z * center.Z) + p.W < -radius)
            {
                return false;
            }
        }

        return true;
    }

    private GraphTexture AddShadowPass(RenderGraph graph)
    {
        if (_tileCount == 0)
        {
            return default;
        }

        var atlas = graph.CreateTexture("shadow_atlas", new TextureDesc((uint)_atlasW, (uint)_atlasH, DepthFormat, TextureUsage.None));
        _shadowPass.Owner = this;
        _shadowPass.Atlas = atlas;
        graph.AddPass(_shadowPass);
        return atlas;
    }

    private void DrawShadows(GpuRenderPass pass)
    {
        for (var t = 0; t < _tileCount; t++)
        {
            var x = (t % _atlasCols) * _tileSize;
            var y = (t / _atlasCols) * _tileSize;
            pass.SetViewport(x, y, _tileSize, _tileSize);
            pass.SetScissor(x, y, (uint)_tileSize, (uint)_tileSize);
            pass.SetBindGroup(0, Set0(1 + t, _shadowDummy, _white));
            DrawRegular(pass, _opaque, _opaqueCount, PipeKind.Shadow, 1, t);
            DrawBatches(pass, PipeKind.Shadow, 1, 1 + t, blend: false);
        }
    }

    private sealed class ShadowPass : IGraphPass
    {
        public Renderer3D? Owner;
        public GraphTexture Atlas;

        public string Name => "shadows";

        public void Setup(PassBuilder builder) => builder.DepthTarget(Atlas, LoadOp.Clear, 1f, StoreOp.Store);

        public void Execute(PassContext context) => Owner!.DrawShadows(context.Render);
    }
}
