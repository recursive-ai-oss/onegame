using OneGame.Core;
using OneGame.Physics;

namespace OneGame.AotSmoke;

/// <summary>Physics under the native publish: a space in a world with a falling box, a ball, a trigger and a character; a save taken mid-fall restores to the same hashes.</summary>
internal static class PhysicsSmoke
{
    public static string? Run()
    {
        var three = Run3D();
        if (three is not null) return three;
        var registry = new EntityRegistry();
        var world = new World(1, registry);
        var space = PhysicsSpace2D.AddTo(world);
        space.AddBox(new FVec2(40, 1), new FVec2(0, -1), new BodyOptions { Kind = BodyKind.Static });
        var zone = space.AddBox(new FVec2(4, 4), new FVec2(0, 3), new BodyOptions { Kind = BodyKind.Static, IsTrigger = true });
        var box = space.AddBox(new FVec2(1, 1), new FVec2(0, 9));
        space.AddCircle(Fixed.FromRatio(1, 2), new FVec2(3, 5), new BodyOptions { Restitution = Fixed.FromRatio(1, 2) });
        var hero = space.AddCharacter(new FVec2(-4, Fixed.FromRatio(11, 10)), new CharacterOptions { Height = Fixed.FromInt(2) });
        var entered = false;
        var step = new FVec2(Fixed.FromRatio(1, 20), Fixed.FromRatio(-1, 10));

        for (var i = 0; i < 40; i++)
        {
            space.Move(hero, step);
            world.Step();
        }

        var writer = new SyncWriter();
        world.WriteSnapshot(writer);
        var copy = new World(1, new EntityRegistry());
        var copySpace = PhysicsSpace2D.AddTo(copy);
        if (copy.ReadSnapshot(writer.Written) != SyncError.None)
        {
            return "the snapshot did not load";
        }

        for (var i = 0; i < 300; i++)
        {
            space.Move(hero, step);
            copySpace.Move(hero, step);
            world.Step();
            copy.Step();
            if (world.ComputeHash() != copy.ComputeHash())
            {
                return $"restored world diverged at tick {world.Tick}";
            }

            foreach (var e in space.Events)
            {
                entered |= e.Kind == PhysicsEventKind.TriggerEnter && e.Involves(zone) && e.Involves(box);
            }
        }

        if (!entered)
        {
            return "the box never entered the trigger";
        }

        if (!space.Raycast(new FVec2(8, 8), new FVec2(0, -1), Fixed.FromInt(50), out var hit) || hit.Distance < Fixed.FromInt(8))
        {
            return "the ray did not reach the ground";
        }

        return space.Position(box).Y < Fixed.FromInt(-1) ? "the box fell through the ground" : null;
    }
    private static string? Run3D()
    {
        var world = new World(1, new EntityRegistry());
        var space = PhysicsSpace3D.AddTo(world);
        space.AddBox(new FVec3(40, 1, 40), new FVec3(0, -1, 0), new BodyOptions3D { Kind = BodyKind.Static });
        var box = space.AddSphere(Fixed.One, new FVec3(0, 8, 0));
        var character = space.AddCharacter(new FVec3(-5, 1, 0));
        var chassis = space.AddBox(new FVec3(2, Fixed.FromRatio(1, 2), 4), new FVec3(6, 1, 0), new BodyOptions3D { Density = 100 });
        var vehicle = space.AddVehicle(chassis);
        for (var i = 0; i < 30; i++) world.Step();
        var snapshot = new SyncWriter(); world.WriteSnapshot(snapshot);
        var other = new World(1, new EntityRegistry()); var copy = PhysicsSpace3D.AddTo(other);
        if (other.ReadSnapshot(snapshot.Written) != SyncError.None) return "3D snapshot refused";
        for (var i = 0; i < 120; i++)
        {
            var move = new FVec3(Fixed.FromRatio(1, 100), Fixed.FromRatio(-1, 10), 0);
            space.Move(character, move); copy.Move(character, move);
            space.Drive(vehicle, Fixed.One); copy.Drive(vehicle, Fixed.One);
            world.Step(); other.Step();
            if (world.ComputeHash() != other.ComputeHash()) return "3D restored state drifted";
        }
        var fork = space.Fork(); fork.Step(resimulating: true); space.Step();
        if (fork.Hash != space.Hash) return "3D fork drifted";
        if (!space.Grounded(character) || space.Position(box).Y < Fixed.FromInt(-1)) return "3D objects missed ground";
        if (space.Wheel(vehicle, 0).Radius <= Fixed.Zero) return "3D wheel missing";
        var drawing = new OneGame.Render3D.DebugDraw3D(); var debug = new PhysicsDebug3D();
        var hash = space.Hash;
        debug.Draw(space, drawing, false);
        if (drawing.Count != 0) return "3D debug switch ignored";
        debug.Draw(space, drawing, true);
        if (drawing.Count < 100 || space.Hash != hash) return "3D debug geometry missing or changed state";
        return null;
    }

}
