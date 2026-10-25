using OneGame.Core;
using OneGame.Host;

namespace Scene3DGame;

/// <summary>A box that circles a home point, turning as it goes. Radius 0 makes it stand still.</summary>
[Entity]
public partial class Crate : Entity, IView3D
{
    [Synced] public partial Transform3 Transform { get; private set; }

    [Synced] public partial FVec3 Home { get; private set; }

    [Synced] public partial Fixed Radius { get; private set; }

    [Synced] public partial Fixed Angle { get; private set; }

    [Synced] public partial Fixed Speed { get; private set; }

    [Synced] public partial Fixed Size { get; private set; }

    /// <summary>Sets the crate up before the first tick.</summary>
    public void Init(FVec3 home, Fixed radius, Fixed speed, Fixed size)
    {
        Home = home;
        Radius = radius;
        Speed = speed;
        Size = size;
        Transform = new Transform3(home, FQuat.Identity, FVec3.One * size);
    }

    /// <summary>Shifts the circle's centre sideways.</summary>
    [Command]
    public void Nudge(Fixed dx) => Home += new FVec3(dx, Fixed.Zero, Fixed.Zero);

    protected override void OnTick()
    {
        Angle += Speed;
        var offset = new FVec3(Fixed.Cos(Angle) * Radius, Fixed.Zero, Fixed.Sin(Angle) * Radius);
        Transform = new Transform3(Home + offset, FQuat.FromAxisAngle(FVec3.UnitY, Angle * 2) * FQuat.FromAxisAngle(FVec3.UnitX, Fixed.FromRatio(1, 4)), FVec3.One * Size);
    }

    [View]
    public void Describe3D(SceneWriter scene) => scene.Model("box", Transform);
}

/// <summary>A flat slab to stand things on.</summary>
[Entity]
public partial class Slab : Entity, IView3D
{
    [Synced] public partial Transform3 Transform { get; private set; }

    /// <summary>Sets the slab up before the first tick.</summary>
    public void Init(Transform3 transform) => Transform = transform;

    [View]
    public void Describe3D(SceneWriter scene) => scene.Model("box", Transform);
}

/// <summary>The camera.</summary>
[Entity]
public partial class Rig : Entity, IView3D
{
    [Synced] public partial Transform3 Transform { get; private set; }

    [Synced] public partial Fixed Fov { get; private set; }

    /// <summary>Sets the camera up before the first tick.</summary>
    public void Init(FVec3 eye, FVec3 target, Fixed fovDegrees)
    {
        Transform = Transform3.LookAt(eye, target);
        Fov = fovDegrees;
    }

    /// <summary>Moves the camera to a place and turns it to the middle of the scene.</summary>
    [Command]
    public void MoveTo(Fixed x, Fixed y, Fixed z) => Transform = Transform3.LookAt(new FVec3(x, y, z), new FVec3(Fixed.Zero, Fixed.Half, Fixed.Zero));

    [View]
    public void Describe3D(SceneWriter scene) => scene.Camera(Transform, Fov.ToSingle());
}

/// <summary>A sun and a lamp.</summary>
[Entity]
public partial class Lamp : Entity, IView3D
{
    [Synced] public partial Transform3 Transform { get; private set; }

    /// <summary>Sets the lamp up before the first tick.</summary>
    public void Init(FVec3 position) => Transform = Transform3.At(position);

    [View]
    public void Describe3D(SceneWriter scene)
    {
        scene.DirectionalLight(Transform3.LookAt(FVec3.Zero, new FVec3(-1, -3, -2)), new System.Numerics.Vector3(1f, 0.96f, 0.9f), 1.6f);
        scene.PointLight(Transform, new System.Numerics.Vector3(1f, 0.8f, 0.6f), 14f, 12f);
    }
}

/// <summary>A grid of small boxes drawn as instances; not part of the default setup.</summary>
[Entity]
public partial class Grove : Entity, IView3D
{
    [Synced] public partial FVec3 Origin { get; private set; }

    [Synced] public partial int Side { get; private set; }

    /// <summary>Sets the grove up before the first tick.</summary>
    public void Init(FVec3 origin, int side)
    {
        Origin = origin;
        Side = side;
    }

    [View]
    public void Describe3D(SceneWriter scene)
    {
        for (var z = 0; z < Side; z++)
        {
            for (var x = 0; x < Side; x++)
            {
                var at = Origin + new FVec3(x, Fixed.Zero, z);
                scene.Instance("box", new Transform3(at, FQuat.Identity, FVec3.One * Fixed.FromRatio(1, 4)), new System.Numerics.Vector4(0.2f, 0.9f, 0.3f, 1f));
            }
        }
    }
}
