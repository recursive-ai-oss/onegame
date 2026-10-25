using System.Numerics;
using OneGame.Core;
using OneGame.Host;

namespace Lanterns.Views;

/// <summary>Where things are in the 3D world. The two islands lie side by side along X with the bridge between; ground is the X-Z plane, +Z toward the camera.</summary>
internal static class Layout
{
    /// <summary>Metres per simulation unit: an island is 16 x 8 m.</summary>
    public const float Unit = 1f / 24f;

    /// <summary>X of the south island's play-area corner; the north island's is 0.</summary>
    public const float OriginB = 24.85f;

    /// <summary>Height of the sea surface; islands rise 0.34 m above it at the beach.</summary>
    public const float WaterY = -0.5f;

    public static float X(uint island, float units) => (island == Rules.IslandB ? OriginB : 0f) + (units * Unit);

    public static float Z(float units) => units * Unit;

    /// <summary>The middle of an island's play area.</summary>
    public static Vector3 Center(uint island) => new(X(island, Rules.Width / 2f), 0f, Z(Rules.Height / 2f));
}

/// <summary>
/// Draws the game in 3D: two islands joined by a bridge, walkers, lanterns on the ground that light their surroundings, and a camera that
/// follows my player. It is view code: it reads the replicas (the predicted copy for the island I walk on) and writes only into the scene.
/// Where a walker is drawn comes from <see cref="Motion"/>, so a prediction correction blends in instead of popping.
/// </summary>
[View]
internal sealed class IslandScene
{
    /// <summary>The most names the screen can show at once.</summary>
    public const int MaxLabels = 16;

    // The models are drawn a little larger than life so people and lanterns read from the camera's height.
    private const float WalkerSize = 1.3f;
    private const float LanternSize = 1.5f;
    private const int MaxCarried = 6;
    private const float CameraHeight = 10.2f;
    private const float CameraBack = 8.6f;
    private const float CameraShift = 2.2f;

    // Linear colours and strengths of the light a lantern throws, by colour index.
    private static readonly Vector3[] LightColors = [new(1f, 0.16f, 0.10f), new(0.18f, 0.5f, 1f), new(1f, 0.66f, 0.16f)];
    private static readonly string[] LanternModels = ["lantern-red", "lantern-blue", "lantern-gold"];
    private static readonly string[] PropModels = ["pine", "oak", "rock", "bush"];

    private readonly Motion _motion = new();
    private readonly Prop[] _props;
    private readonly uint[] _drawn = new uint[64];
    private readonly Player?[] _labelPlayers = new Player?[MaxLabels];
    private readonly Vector3[] _labelPoints = new Vector3[MaxLabels];
    private Session? _session;
    private int _drawnCount;
    private int _labelCount;

    public IslandScene()
    {
        var props = new List<Prop>();
        Decorate(props, Rules.IslandA, 11);
        Decorate(props, Rules.IslandB, 29);
        _props = [.. props];
    }

    /// <summary>The camera the last frame used, as the point on the ground it looks at.</summary>
    public Vector2 Focus { get; private set; } = new(Layout.Center(Rules.IslandA).X, Layout.Z(Rules.Height / 2f));

    /// <summary>Where each walker is drawn, in metres, as of the last frame: the view positions tests compare across corrections.</summary>
    public Motion Motion => _motion;

    /// <summary>How many walkers the last frame drew a name for.</summary>
    public int LabelCount => _labelCount;

    public void Attach(Session session)
    {
        _session = session;
        _motion.Reset();
    }

    public Player? LabelPlayer(int slot) => slot < _labelCount ? _labelPlayers[slot] : null;

    /// <summary>The point above a walker's head where its name floats, or null for an unused slot.</summary>
    public FVec3? LabelPoint(int slot)
    {
        if (slot >= _labelCount)
        {
            return null;
        }

        var p = _labelPoints[slot];
        return new FVec3(Fixed.FromDouble(p.X), Fixed.FromDouble(p.Y), Fixed.FromDouble(p.Z));
    }

    /// <summary>Called by the scene view for each frame (and for each name label that asks where it is): says what the world looks like.</summary>
    public void Describe(World world, SceneWriter w)
    {
        var alpha = w.Alpha.ToSingle();
        _motion.Begin(w.Tick);
        _drawnCount = 0;
        _labelCount = 0;
        World? north;
        World? south = null;
        bool linked;
        if (_session is { Networked: true } s)
        {
            north = s.WorldFor(Rules.IslandA);
            south = s.WorldFor(Rules.IslandB);
            linked = true;
        }
        else
        {
            north = world;
            linked = IsLinked(world);
        }

        w.DirectionalLight(new Vector3(-0.35f, -0.8f, -0.45f), new Vector3(0.62f, 0.72f, 1f), 1.1f);
        w.Model("water", Matrix4x4.CreateTranslation(Layout.OriginB / 2f + 8f, Layout.WaterY, 4f), pickable: false);
        w.Model("island-a", Matrix4x4.CreateTranslation(Layout.X(Rules.IslandA, Rules.Width / 2f), 0f, Layout.Z(Rules.Height / 2f)), pickable: false);
        if (linked)
        {
            w.Model("island-b", Matrix4x4.CreateTranslation(Layout.X(Rules.IslandB, Rules.Width / 2f), 0f, Layout.Z(Rules.Height / 2f)), pickable: false);
            w.Model("bridge", Matrix4x4.CreateTranslation(20.35f, 0f, Layout.Z((Rules.BridgeTop + Rules.BridgeBottom) / 2f)), pickable: false);
        }
        else
        {
            w.Model("bridge-stub", Matrix4x4.CreateTranslation(18.4f, 0f, Layout.Z((Rules.BridgeTop + Rules.BridgeBottom) / 2f)), pickable: false);
        }

        var shown = linked ? _props.Length : _props.Length / 2;
        for (var i = 0; i < shown; i++)
        {
            w.Model(PropModels[_props[i].Model], _props[i].World, pickable: false);
        }

        Lamps(w, Rules.IslandA);
        if (linked)
        {
            Lamps(w, Rules.IslandB);
        }

        var mine = default(Vector2?);
        if (north is not null)
        {
            Describe(north, Rules.IslandA, w, alpha, ref mine);
        }

        if (south is not null)
        {
            Describe(south, Rules.IslandB, w, alpha, ref mine);
        }

        var focus = Focus;
        if (mine is { } me)
        {
            var t = _motion.Follow(me);
            focus = t.At(alpha);
        }

        focus = new Vector2(Math.Clamp(focus.X, 3f, linked ? Layout.OriginB + 13f : 13f), Math.Clamp(focus.Y, 2.2f, 5.8f));
        Focus = focus;
        var eye = new Vector3(focus.X, CameraHeight, focus.Y + CameraBack);
        var look = new Vector3(focus.X + CameraShift, 0f, focus.Y);
        if (Matrix4x4.Invert(Matrix4x4.CreateLookAt(eye, look, Vector3.UnitY), out var camera))
        {
            w.Camera(camera, 40f, 0, 1f, 250f);
        }

        _motion.End();
    }

    // An indexed loop: foreach over the entity list would allocate an enumerator every frame.
    private static bool IsLinked(World world)
    {
        var entities = world.Entities;
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is Island island)
            {
                return island.Linked;
            }
        }

        return false;
    }

    private void Describe(World world, uint island, SceneWriter w, float alpha, ref Vector2? mine)
    {
        var entities = world.Entities;
        var tick = w.Tick + alpha;
        for (var i = 0; i < entities.Count; i++)
        {
            switch (entities[i])
            {
                case Lantern { Taken: false } l:
                    {
                        var pos = new Vector3(Layout.X(island, l.X), 0.03f + (0.025f * MathF.Sin((tick * 0.05f) + (l.Id.Value * 1.7f))), Layout.Z(l.Y));
                        var m = Matrix4x4.CreateScale(LanternSize) * Matrix4x4.CreateRotationY((tick * 0.02f) + l.Id.Value) * Matrix4x4.CreateTranslation(pos);
                        w.Entity = l.Id;
                        w.Model(LanternModels[l.Color % Rules.Colors], m);
                        w.Entity = default;
                        var glow = LightColors[l.Color % Rules.Colors];
                        w.PointLight(pos + new Vector3(0f, 0.62f * LanternSize, 0f), glow, 2.2f, 3.4f);
                        break;
                    }

                case Player p:
                    Walker(p, island, w, alpha, ref mine);
                    break;
            }
        }
    }

    private void Walker(Player p, uint island, SceneWriter w, float alpha, ref Vector2? mine)
    {
        for (var i = 0; i < _drawnCount; i++)
        {
            if (_drawn[i] == p.Owner)
            {
                return;
            }
        }

        if (_drawnCount < _drawn.Length)
        {
            _drawn[_drawnCount++] = p.Owner;
        }

        // A walker waiting on the bridge already holds its place on the island it is going to.
        var frame = p.Dest != 0 ? p.Dest : island;
        var target = new Vector2(Layout.X(frame, p.X), Layout.Z(p.Y));
        var t = _motion.Touch(p.Owner, target, frame, p.Dx, p.Dy);
        var at = t.At(alpha);
        var mineNow = _session?.IsMine(p) ?? p.Owner == 1;
        if (mineNow)
        {
            mine = at;
        }

        // Steps bob the body; a standing walker settles.
        var bob = 0.055f * MathF.Abs(MathF.Sin(t.Phase * (MathF.PI / 0.62f))) * MathF.Min(1f, t.Speed / 0.05f);
        var m = Matrix4x4.CreateScale(WalkerSize) * Matrix4x4.CreateRotationY(t.Yaw) * Matrix4x4.CreateTranslation(at.X, bob, at.Y);
        w.Entity = p.Id;
        w.Model(mineNow ? "walker-me" : "walker-other", m);
        w.Entity = default;
        w.Model("blob", Matrix4x4.CreateScale(WalkerSize) * Matrix4x4.CreateTranslation(at.X, 0.025f, at.Y), pickable: false);
        Carried(p, mineNow, w, at, bob, tick: w.Tick + alpha);
        if (_labelCount < MaxLabels)
        {
            _labelPlayers[_labelCount] = p;
            _labelPoints[_labelCount] = new Vector3(at.X, (1.12f * WalkerSize) + bob, at.Y);
            _labelCount++;
        }
    }

    // The lanterns a walker holds circle above its head. Only the owner's own copy knows the colours; everyone else sees pale ones, the right number.
    private static void Carried(Player p, bool mine, SceneWriter w, Vector2 at, float bob, float tick)
    {
        var count = Math.Min(p.Total, MaxCarried);
        for (var i = 0; i < count; i++)
        {
            var name = "lantern-plain";
            if (mine)
            {
                var color = i < p.Red ? 0 : i < p.Red + p.Blue ? 1 : 2;
                name = LanternModels[color];
            }

            var a = (tick * 0.045f) + (i * MathF.Tau / count);
            var pos = new Vector3(at.X + (0.62f * MathF.Cos(a)), 0.95f + bob + (0.05f * MathF.Sin((tick * 0.07f) + i)), at.Y + (0.62f * MathF.Sin(a)));
            w.Model(name, Matrix4x4.CreateScale(0.6f) * Matrix4x4.CreateTranslation(pos), pickable: false);
        }
    }

    private static void Lamps(SceneWriter w, uint island)
    {
        // Two lamp posts on the landing pad, as the model has them (see art/Program.cs).
        var side = island == Rules.IslandA ? 1f : -1f;
        var px = Layout.X(island, Rules.Width / 2f) + (side * (8f + 1.5f));
        var cz = Layout.Z(Rules.Height / 2f);
        w.PointLight(new Vector3(px, 1.3f, cz - 1.15f), new Vector3(1f, 0.62f, 0.25f), 9f, 7f);
        w.PointLight(new Vector3(px, 1.3f, cz + 1.15f), new Vector3(1f, 0.62f, 0.25f), 9f, 7f);
    }

    // ---- scenery ---------------------------------------------------------------------------------------------------

    private readonly record struct Prop(int Model, Matrix4x4 World);

    // Trees and rocks stand on the rim around the play area, tall ones along the far (north) edge so they never hide the ground the camera
    // looks at; the south edge only gets rocks and bushes. A few rocks sit out in the water. Placement is a fixed function of the island.
    private static void Decorate(List<Prop> props, uint island, uint seed)
    {
        var origin = Layout.Center(island);
        static float Next(ref uint s)
        {
            s = (s * 1664525u) + 1013904223u;
            return (s >> 8) / 16777216f;
        }

        void Add(int model, Vector2 local, float scale, float y, ref uint s)
        {
            var yaw = Next(ref s) * MathF.Tau;
            var m = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(origin.X + local.X, y, origin.Z + local.Y);
            props.Add(new Prop(model, m));
        }

        var side = island == Rules.IslandA ? 1f : -1f;
        foreach (var p in Resample(0.75f, 1.35f, ref seed))
        {
            if (MathF.Sign(p.X) == side && MathF.Abs(p.Y) < 2.0f && MathF.Abs(p.X) > 6.5f)
            {
                continue;
            }

            if (p.Y > 1.7f)
            {
                Add(Next(ref seed) < 0.5f ? 2 : 3, p, 0.45f + (Next(ref seed) * 0.3f), 0f, ref seed);
            }
            else
            {
                Add(Next(ref seed) < 0.55f ? 0 : 1, p, 0.62f + (Next(ref seed) * 0.28f), 0f, ref seed);
            }
        }

        foreach (var p in Resample(-1.3f, 3.1f, ref seed))
        {
            Add(2, p, 0.9f + (Next(ref seed) * 1.1f), Layout.WaterY - 0.02f, ref seed);
        }
    }

    // Points along the island's outline pushed inward by <inset> metres, about <spacing> apart with a little jitter. The outline is the
    // rounded rectangle of the island model: 18.8 x 10.8 m with 2.4 m corners, about the middle of the play area.
    private static List<Vector2> Resample(float inset, float spacing, ref uint seed)
    {
        const float hx = 8f + 1.4f;
        const float hz = 4f + 1.4f;
        const float rc = 2.4f;
        var r = rc - inset;
        var path = new List<Vector2>();
        Vector2[] centers = [new(hx - rc, hz - rc), new(-(hx - rc), hz - rc), new(-(hx - rc), -(hz - rc)), new(hx - rc, -(hz - rc))];
        for (var c = 0; c < 4; c++)
        {
            for (var k = 0; k <= 6; k++)
            {
                var t = ((c * 90f) + (k / 6f * 90f)) * MathF.PI / 180f;
                path.Add(centers[c] + (new Vector2(MathF.Cos(t), MathF.Sin(t)) * r));
            }
        }

        var points = new List<Vector2>();
        var carry = 0f;
        for (var i = 0; i < path.Count; i++)
        {
            var a = path[i];
            var b = path[(i + 1) % path.Count];
            var len = Vector2.Distance(a, b);
            var at = spacing - carry;
            while (at <= len)
            {
                seed = (seed * 1664525u) + 1013904223u;
                var jitter = (((seed >> 8) / 16777216f) - 0.5f) * spacing * 0.5f;
                points.Add(Vector2.Lerp(a, b, MathF.Min(1f, MathF.Max(0f, (at + jitter) / len))));
                at += spacing;
            }

            carry = len - (at - spacing);
        }

        return points;
    }
}
