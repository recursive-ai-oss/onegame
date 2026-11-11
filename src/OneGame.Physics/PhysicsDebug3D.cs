using System.Numerics;
using OneGame.Render3D;
using RecursiveAI.OneMath;
using RecursiveAI.OnePhysics.Debug;

namespace OneGame.Physics;

/// <summary>Reusable view-side adapter from 3D physics debug geometry to the render graph's debug primitives. Collects nothing while the debug switch is off.</summary>
public sealed class PhysicsDebug3D
{
    private readonly DebugGeometry _geometry = new();
    private readonly Dictionary<int, DebugBodyState> _states = [];
    private const int Segments = 24;

    /// <summary>Adds wire shapes, character capsules, wheels, suspension, joints and contact normals. Call after the scene view's Build, passing its DebugVisible switch. Does not clear the destination or mutate physics state.</summary>
    /// <param name="space">Authority space to inspect.</param>
    /// <param name="drawing">Scene debug list receiving the primitives.</param>
    /// <param name="debugVisible">The host view's debug switch; false emits nothing.</param>
    /// <param name="mode">Depth-tested by default; AlwaysOnTop is useful when inspecting occluded shapes.</param>
    public void Draw(PhysicsSpace3D space, DebugDraw3D drawing, bool debugVisible, DebugDepth3D mode = DebugDepth3D.DepthTested)
    {
        ArgumentNullException.ThrowIfNull(space); ArgumentNullException.ThrowIfNull(drawing);
        if (!debugVisible) return;
        space.CollectDebug(_geometry);
        _states.Clear();
        foreach (ref readonly var body in _geometry.Bodies) _states[body.Body.Index] = body.State;
        foreach (ref readonly var shape in _geometry.Shapes)
        {
            var color = Color(shape.IsSensor ? PhysicsDebugShapes.TriggerColor : _states.GetValueOrDefault(shape.Body.Index) switch
            {
                DebugBodyState.Static => PhysicsDebugShapes.StaticColor,
                DebugBodyState.Kinematic => PhysicsDebugShapes.KinematicColor,
                DebugBodyState.Asleep => PhysicsDebugShapes.AsleepColor,
                _ => PhysicsDebugShapes.AwakeColor,
            });
            var points = _geometry.Points.Slice(shape.FirstPoint, shape.PointCount);
            if (shape.IndexCount > 0)
            {
                var indices = _geometry.Indices.Slice(shape.FirstIndex, shape.IndexCount);
                for (var i = 0; i + 2 < indices.Length; i += 3)
                {
                    var a = Vector(points[indices[i]]); var b = Vector(points[indices[i + 1]]); var c = Vector(points[indices[i + 2]]);
                    drawing.Line(a, b, color, mode: mode); drawing.Line(b, c, color, mode: mode); drawing.Line(c, a, color, mode: mode);
                }
            }
            else if (shape.Kind == DebugShapeKind.Circle && points.Length == 1) Sphere(drawing, Vector(points[0]), shape.Radius, color, mode);
            else if (points.Length == 2)
            {
                var a = Vector(points[0]); var b = Vector(points[1]); var axis = b - a;
                if (axis.LengthSquared() < 1e-12f) { Sphere(drawing, a, shape.Radius, color, mode); continue; }
                axis = Vector3.Normalize(axis);
                Basis(axis, out var u, out var v);
                var r1 = shape.Radius; var r2 = shape.Kind == DebugShapeKind.TaperedCapsule ? shape.BottomRadius : r1;
                Circle(drawing, a, u * r1, v * r1, color, mode); Circle(drawing, b, u * r2, v * r2, color, mode);
                for (var i = 0; i < 4; i++)
                {
                    var d = i switch { 0 => u, 1 => -u, 2 => v, _ => -v };
                    drawing.Line(a + d * r1, b + d * r2, color, mode: mode);
                }
                if (shape.Kind != DebugShapeKind.Cylinder) { Sphere(drawing, a, r1, color, mode); Sphere(drawing, b, r2, color, mode); }
            }
        }
        var contactColor = Color(PhysicsDebugShapes.ContactColor);
        foreach (ref readonly var contact in _geometry.Contacts)
        {
            var p = Vector(contact.Point);
            drawing.Point(p, contactColor, 4, mode: mode);
            drawing.Arrow(p, p + new Vector3(contact.Normal.X, contact.Normal.Y, contact.Normal.Z) * 0.5f, contactColor, mode: mode);
        }
        foreach (ref readonly var joint in _geometry.Joints)
            drawing.Line(Vector(joint.AnchorA), Vector(joint.AnchorB), contactColor, 2, mode: mode);
        foreach (ref readonly var wheel in _geometry.Wheels)
        {
            var center = Vector(wheel.Center);
            var axle = new Vector3(wheel.Axle.X, wheel.Axle.Y, wheel.Axle.Z);
            Basis(axle, out var u, out var v);
            var color = Color(PhysicsDebugShapes.KinematicColor);
            Circle(drawing, center, u * wheel.Radius, v * wheel.Radius, color, mode);
            drawing.Line(Vector(wheel.SuspensionTop), center, color, mode: mode);
            if (wheel.IsInContact) drawing.Point(Vector(wheel.ContactPoint), contactColor, 4, mode: mode);
        }
        foreach (ref readonly var character in _geometry.Characters)
            if (character.IsGrounded)
            {
                var p = Vector(character.Center);
                drawing.Arrow(p, p + Vector(character.GroundNormal) * 0.6f, contactColor, mode: mode);
            }
    }

    private static void Sphere(DebugDraw3D drawing, Vector3 center, float radius, Vector4 color, DebugDepth3D mode)
    {
        Circle(drawing, center, Vector3.UnitX * radius, Vector3.UnitY * radius, color, mode);
        Circle(drawing, center, Vector3.UnitX * radius, Vector3.UnitZ * radius, color, mode);
        Circle(drawing, center, Vector3.UnitY * radius, Vector3.UnitZ * radius, color, mode);
    }
    private static void Circle(DebugDraw3D drawing, Vector3 center, Vector3 u, Vector3 v, Vector4 color, DebugDepth3D mode)
    {
        var previous = center + u;
        for (var i = 1; i <= Segments; i++)
        {
            var angle = i * (2 * MathF.PI / Segments);
            var next = center + u * MathF.Cos(angle) + v * MathF.Sin(angle);
            drawing.Line(previous, next, color, mode: mode); previous = next;
        }
    }
    private static void Basis(Vector3 axis, out Vector3 u, out Vector3 v)
    {
        axis = Vector3.Normalize(axis);
        u = Vector3.Normalize(Vector3.Cross(axis, MathF.Abs(axis.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX));
        v = Vector3.Cross(axis, u);
    }
    private static Vector3 Vector(Vector3D p) => new((float)p.X, (float)p.Y, (float)p.Z);
    private static Vector4 Color(uint rgba) => new(((rgba >> 24) & 255) / 255f, ((rgba >> 16) & 255) / 255f, ((rgba >> 8) & 255) / 255f, (rgba & 255) / 255f);
}

public sealed partial class PhysicsSpace3D
{
    internal void CollectDebug(DebugGeometry geometry) => _world.GetDebugGeometry(DebugContent.Shapes | DebugContent.Bodies | DebugContent.Contacts | DebugContent.Characters | DebugContent.Vehicles | DebugContent.Joints, geometry);
}
