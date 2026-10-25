using System.Numerics;
using OneGame.Core;
using OneGame.Host;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    private ControlResult Dispatch3D(string op, Args a)
    {
        var view = (Game.Stage as IScene3DStage)?.View
            ?? throw new ControlException("no_scene3d", "This game does not draw entities in 3D. Return a Render3DStage built with a SceneView3D from GameDefinition.CreateRenderStage.");
        view.Prepare(Game.World, Game.World.Tick, default);
        return op switch
        {
            "camera" => Json(Camera3D(view, a), ControlJson.Default.CameraResult),
            "pick" => Json(Pick3D(view, a), ControlJson.Default.PickResult),
            _ => Json(Project3D(view, a), ControlJson.Default.ProjectResult),
        };
    }

    /// <summary>Finds an entity by numeric id, type name or <c>Type#n</c>, as operations do.</summary>
    /// <param name="spec">The entity.</param>
    /// <returns>Its id.</returns>
    /// <exception cref="ControlException">There is no such entity.</exception>
    public EntityId ResolveEntityId(string spec) => ResolveEntity(spec).Id;

    private CameraResult Camera3D(SceneView3D view, Args a)
    {
        if (a.Bool("reset", false))
        {
            view.CameraOverride = null;
        }
        else if (a.Has("position") || a.Has("target") || a.Has("fovDegrees") || a.Has("near") || a.Has("far") || a.Has("orthoHeight"))
        {
            var current = view.Camera;
            var position = a.Numbers("position", 3) is { } p ? new Vector3((float)p[0], (float)p[1], (float)p[2]) : current.Position;
            var world = current.World;
            if (a.Numbers("target", 3) is { } t)
            {
                var target = new Vector3((float)t[0], (float)t[1], (float)t[2]);
                if ((target - position).LengthSquared() < 1e-12f)
                {
                    throw new ControlException("bad_args", "target must differ from the camera position.");
                }

                // Looking straight up or down needs another up vector.
                var look = Vector3.Normalize(target - position);
                var up = MathF.Abs(look.Y) > 0.999f ? -Vector3.UnitZ : Vector3.UnitY;
                Matrix4x4.Invert(Matrix4x4.CreateLookAt(position, target, up), out world);
            }
            else
            {
                world.Translation = position;
            }

            var fov = (float)a.Double("fovDegrees", current.YFov * 180f / MathF.PI);
            if (fov is <= 0f or >= 180f)
            {
                throw new ControlException("bad_args", "fovDegrees must be between 0 and 180.");
            }

            var near = (float)a.Double("near", current.Near);
            var far = (float)a.Double("far", current.Far);
            if (near <= 0f || far <= near)
            {
                throw new ControlException("bad_args", "near must be positive and far greater than near.");
            }

            var ortho = (float)a.Double("orthoHeight", current.OrthoHeight);
            view.CameraOverride = new ViewCamera(world, fov * MathF.PI / 180f, MathF.Max(ortho, 0f), near, far, default);
        }

        var cam = view.Camera;
        var forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, cam.World));
        var upAxis = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, cam.World));
        string? entity = cam.Source.IsValid && Game.World.Find(cam.Source) is { } e ? Label(e) : null;
        return new CameraResult(Round3(cam.Position), Round3(forward), Round3(upAxis), Math.Round(cam.YFov * 180.0 / Math.PI, 4), Math.Round(cam.OrthoHeight, 5), Math.Round(cam.Near, 5), Math.Round(cam.Far, 5), entity, view.CameraOverride is not null);
    }

    private PickResult Pick3D(SceneView3D view, Args a)
    {
        PickRay ray;
        if (a.Has("origin") || a.Has("direction"))
        {
            var o = a.Numbers("origin", 3) ?? throw new ControlException("bad_args", "origin is required with direction.");
            var d = a.Numbers("direction", 3) ?? throw new ControlException("bad_args", "direction is required with origin.");
            var dir = new Vector3((float)d[0], (float)d[1], (float)d[2]);
            if (dir.LengthSquared() < 1e-12f)
            {
                throw new ControlException("bad_args", "direction must not be zero.");
            }

            ray = new PickRay(new Vector3((float)o[0], (float)o[1], (float)o[2]), Vector3.Normalize(dir));
        }
        else
        {
            if (!a.Has("x") || !a.Has("y"))
            {
                throw new ControlException("bad_args", "Give x and y (pixels), or origin and direction.");
            }

            var (w, h) = TargetSize(a);
            ray = view.Ray((float)a.Double("x", 0), (float)a.Double("y", 0), w, h);
        }

        var hit = view.Cast(ray);
        var tick = Game.World.Tick;
        if (hit is not { } found)
        {
            return new PickResult(false, null, null, null, null, null, null, Round3(ray.Origin), Round3(ray.Direction), tick);
        }

        var entity = Game.World.Find(found.Entity);
        return new PickResult(true, entity is null ? null : Label(entity), found.Entity.Value, entity is null ? null : Address(entity), found.Model, Math.Round(found.Distance, 5), Round3(found.Point), Round3(ray.Origin), Round3(ray.Direction), tick);
    }

    private ProjectResult Project3D(SceneView3D view, Args a)
    {
        var p = a.Numbers("point", 3) ?? throw new ControlException("bad_args", "point is required: [x, y, z].");
        var (w, h) = TargetSize(a);
        var inFront = view.TryProject(new Vector3((float)p[0], (float)p[1], (float)p[2]), w, h, out var pixel, out var depth);
        var on = inFront && pixel.X >= 0 && pixel.Y >= 0 && pixel.X <= w && pixel.Y <= h;
        return new ProjectResult(on, inFront, Math.Round(pixel.X, 3), Math.Round(pixel.Y, 3), Math.Round(depth, 5), w, h);
    }

    private (int W, int H) TargetSize(Args a)
    {
        var size = a.Str("size");
        if (size is null)
        {
            return (Game.Window.Width, Game.Window.Height);
        }

        return Resolution.TryParse(size, out var w, out var h)
            ? (w, h)
            : throw new ControlException("bad_args", $"'{size}' is not a size. Use WIDTHxHEIGHT or a name: 360p 480p 720p 1080p 1440p 4k deck phone tablet.");
    }

    // Adding zero turns -0 into 0.
    private string Address(Entity e)
    {
        var nth = 1;
        foreach (var other in Game.World.Entities)
        {
            if (other == e)
            {
                break;
            }

            if (other.TypeId == e.TypeId)
            {
                nth++;
            }
        }

        return e.TypeName + "#" + nth.ToString(CultureInfo.InvariantCulture);
    }

    private static double[] Round3(Vector3 v) => [Math.Round(v.X, 5) + 0.0, Math.Round(v.Y, 5) + 0.0, Math.Round(v.Z, 5) + 0.0];
}
