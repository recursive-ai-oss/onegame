using System.Numerics;
using RecursiveAI.OnePhysics.Debug;
using RecursiveAI.OnePhysics.Physics2D;

namespace OneGame.Physics;

/// <summary>A line of debug geometry in world units.</summary>
/// <param name="From">First end.</param>
/// <param name="To">Second end.</param>
/// <param name="Rgba">Colour as <c>0xRRGGBBAA</c>.</param>
/// <param name="Width">Thickness in world units.</param>
public readonly record struct PhysicsDebugLine(Vector2 From, Vector2 To, uint Rgba, float Width);

/// <summary>
/// Debug geometry for a <see cref="PhysicsSpace2D"/>: every shape's outline (colour tells static, kinematic, awake, asleep and trigger
/// apart), contact points with their normals, as plain lines in world units. It is view-side data (floats, never state): collect it in a
/// view while the debug overlay is on and draw the lines with the 2D calls.
/// <code>
/// if (d.DebugVisible)
/// {
///     _shapes.Clear();
///     space.AddDebugShapes(_shapes);
///     foreach (var l in _shapes.Lines) { d.Line(l.From, l.To, Color.Rgba(l.Rgba), l.Width, 9); }
/// }
/// </code>
/// Only the authority holds a space, so replicas have nothing to collect.
/// </summary>
public sealed class PhysicsDebugShapes
{
    /// <summary>Colour of static shapes.</summary>
    public const uint StaticColor = 0x8CA0B4FF;

    /// <summary>Colour of kinematic shapes (characters, moving platforms).</summary>
    public const uint KinematicColor = 0x60A0F0FF;

    /// <summary>Colour of awake dynamic shapes.</summary>
    public const uint AwakeColor = 0x9CE860FF;

    /// <summary>Colour of sleeping dynamic shapes.</summary>
    public const uint AsleepColor = 0x587058FF;

    /// <summary>Colour of triggers.</summary>
    public const uint TriggerColor = 0x30E0A0FF;

    /// <summary>Colour of contact points and normals.</summary>
    public const uint ContactColor = 0xFF5050FF;

    private const int CircleSegments = 16;

    private readonly List<PhysicsDebugLine> _lines = [];
    private readonly DebugGeometry _geometry = new();
    private readonly Dictionary<int, DebugBodyState> _states = [];

    /// <summary>The lines collected so far.</summary>
    public IReadOnlyList<PhysicsDebugLine> Lines => _lines;

    /// <summary>Forgets everything collected.</summary>
    public void Clear() => _lines.Clear();

    internal void Collect(PhysicsWorld2D world)
    {
        world.GetDebugGeometry(DebugContent.Shapes | DebugContent.Contacts | DebugContent.Bodies, _geometry);
        _states.Clear();
        foreach (ref readonly var b in _geometry.Bodies)
        {
            _states[b.Body.Index] = b.State;
        }

        var points = _geometry.Points;
        foreach (ref readonly var s in _geometry.Shapes)
        {
            var color = s.IsSensor ? TriggerColor : _states.TryGetValue(s.Body.Index, out var st) ? ColorOf(st) : StaticColor;
            var first = s.FirstPoint;
            switch (s.Kind)
            {
                case DebugShapeKind.Circle:
                    Circle(P(points[first]), s.Radius, color);
                    break;
                case DebugShapeKind.Capsule:
                {
                    var a = P(points[first]);
                    var c = P(points[first + 1]);
                    Circle(a, s.Radius, color);
                    Circle(c, s.Radius, color);
                    var axis = c - a;
                    if (axis.LengthSquared() > 0f)
                    {
                        var n = Vector2.Normalize(new Vector2(-axis.Y, axis.X)) * s.Radius;
                        Add(a + n, c + n, color);
                        Add(a - n, c - n, color);
                    }

                    break;
                }

                case DebugShapeKind.Polygon:
                    for (var i = 0; i < s.PointCount; i++)
                    {
                        Add(P(points[first + i]), P(points[first + ((i + 1) % s.PointCount)]), color);
                    }

                    break;
                default:
                    if (s.PointCount >= 2)
                    {
                        Add(P(points[first]), P(points[first + 1]), color);
                    }

                    break;
            }
        }

        foreach (ref readonly var c in _geometry.Contacts)
        {
            var p = P(c.Point);
            var n = new Vector2(c.Normal.X, c.Normal.Y) * 0.3f;
            Add(p, p + n, ContactColor);
            Add(p - new Vector2(0.05f, 0f), p + new Vector2(0.05f, 0f), ContactColor);
        }
    }

    private static Vector2 P(RecursiveAI.OneMath.Vector3D v) => new((float)v.X, (float)v.Y);

    private static uint ColorOf(DebugBodyState state) => state switch
    {
        DebugBodyState.Static => StaticColor,
        DebugBodyState.Kinematic => KinematicColor,
        DebugBodyState.Awake => AwakeColor,
        _ => AsleepColor,
    };

    private void Add(Vector2 from, Vector2 to, uint rgba) => _lines.Add(new PhysicsDebugLine(from, to, rgba, 0.06f));

    private void Circle(Vector2 center, float radius, uint color)
    {
        var prev = center + new Vector2(radius, 0f);
        for (var i = 1; i <= CircleSegments; i++)
        {
            var a = i * (MathF.PI * 2f / CircleSegments);
            var next = center + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius);
            Add(prev, next, color);
            prev = next;
        }
    }
}
