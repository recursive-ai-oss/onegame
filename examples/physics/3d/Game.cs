using OneGame.Core;
using OneGame.Host;
using OneGame.Physics;
using OneGame.UI;

namespace Physics3DExample;

/// <summary>A falling pile, a walking character and a driven car in the same saved space.</summary>
[Entity]
public partial class Playground : Entity
{
    /// <summary>The box at the bottom of the pile.</summary>
    [Synced] public partial BodyRef Box { get; internal set; }
    /// <summary>The walking capsule.</summary>
    [Synced] public partial CharacterRef Character { get; internal set; }
    /// <summary>The car chassis.</summary>
    [Synced] public partial BodyRef Chassis { get; internal set; }
    /// <summary>The car drivetrain.</summary>
    [Synced] public partial VehicleRef Car { get; internal set; }
    /// <summary>Mirrored box position.</summary>
    [Synced] public partial FVec3 BoxPosition { get; private set; }
    /// <summary>Mirrored character position.</summary>
    [Synced] public partial FVec3 CharacterPosition { get; private set; }
    /// <summary>Mirrored car position.</summary>
    [Synced] public partial FVec3 CarPosition { get; private set; }
    /// <summary>Whether the character is on walkable ground.</summary>
    [Synced] public partial bool Grounded { get; private set; }
    /// <summary>Drive throttle.</summary>
    [Synced] public partial Fixed Throttle { get; private set; }
    /// <summary>Character horizontal speed.</summary>
    [Synced] public partial Fixed WalkSpeed { get; private set; }

    /// <summary>Sets the car throttle and walking speed.</summary>
    [Command]
    public void Drive(Fixed throttle, Fixed walkSpeed) { Throttle = throttle; WalkSpeed = walkSpeed; }

    /// <inheritdoc />
    protected override void OnTick()
    {
        var space = PhysicsSpace3D.Of(World!)!;
        space.Move(Character, new FVec3(WalkSpeed * space.StepSeconds, Fixed.FromRatio(-1, 10), Fixed.Zero));
        space.Drive(Car, Throttle);
        BoxPosition = space.Position(Box); CharacterPosition = space.Position(Character); CarPosition = space.Position(Chassis);
        Grounded = space.Grounded(Character);
    }
}

/// <summary>Small deterministic 3D physics playground.</summary>
public sealed class Physics3DGame : GameDefinition
{
    /// <inheritdoc />
    public override string Name => "Physics 3D";
    /// <inheritdoc />
    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.Physics3DExample.EntityTypes.Register(registry);
    /// <inheritdoc />
    public override void RegisterSystems(World world) => PhysicsSpace3D.AddTo(world);
    /// <inheritdoc />
    public override void Setup(World world)
    {
        var space = PhysicsSpace3D.Of(world)!;
        space.AddBox(new FVec3(40, 1, 60), new FVec3(0, -1, 0), new BodyOptions3D { Kind = BodyKind.Static });
        var box = space.AddBox(new FVec3(1, 1, 1), new FVec3(0, 1, 0));
        for (var i = 0; i < 4; i++) space.AddBox(new FVec3(1, 1, 1), new FVec3(0, 3 + i * 2, 0));
        var chassis = space.AddBox(new FVec3(2, Fixed.FromRatio(1, 2), 4), new FVec3(7, 1, 0), new BodyOptions3D { Density = 100 });
        world.Add(new Playground
        {
            Box = box, Character = space.AddCharacter(new FVec3(-5, 1, 0)), Chassis = chassis, Car = space.AddVehicle(chassis),
        });
    }
    /// <inheritdoc />
    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Label("3D physics: pile, character and car"));
}
