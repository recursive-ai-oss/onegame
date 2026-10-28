using OneGame.Core;
using OneGame.Physics;

namespace OneGame.AotSmoke;

/// <summary>Physics under the native publish: a space in a world with a falling box, a ball, a trigger and a character; a save taken mid-fall restores to the same hashes.</summary>
internal static class PhysicsSmoke
{
    public static string? Run()
    {
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
}
