namespace OneGame.Core.Tests.Samples;

/// <summary>An entity that spins and drifts through 3D with fixed-point quaternion math, to check the transform types sync and stay deterministic.</summary>
[Entity]
public partial class Probe : Entity
{
    [Synced] public partial Transform3 Pose { get; set; }

    [Synced] public partial Timed<Transform3> Glide { get; set; }

    [Synced] public partial FVec3 Velocity { get; set; }

    [Synced] public partial FQuat Spin { get; set; }

    [Synced] public partial int Bounces { get; set; }

    protected override void OnTick()
    {
        var p = Pose;
        var position = p.Position + Velocity;
        if (position.Y < Fixed.Zero)
        {
            position = new FVec3(position.X, -position.Y, position.Z);
            Velocity = new FVec3(Velocity.X, -Velocity.Y, Velocity.Z);
            Bounces++;
        }

        // Gravity, a random shove from a named stream every 16 ticks, and a turn that composes every tick.
        Velocity -= new FVec3(Fixed.Zero, Fixed.FromRatio(1, 64), Fixed.Zero);
        if (World!.Tick % 16 == 0)
        {
            Velocity += new FVec3(Fixed.FromRatio(World.Rng["shove"].NextInt(-4, 4), 64), Fixed.Zero, Fixed.FromRatio(World.Rng["shove"].NextInt(-4, 4), 64));
        }

        Pose = new Transform3(position, (Spin * p.Rotation).Normalized(), p.Scale);
        if (World.Tick % 50 == 0)
        {
            Glide = new Timed<Transform3>(Pose, Transform3.At(position + FVec3.UnitX * 3, FQuat.FromAxisAngle(FVec3.UnitY, Fixed.One)), World.Tick, World.Tick + 20);
        }
    }
}
