namespace OneGame.Control;

/// <summary>One emitter in a <c>particles</c> report.</summary>
/// <param name="Name">The emitter's name (empty when it has none).</param>
/// <param name="Live">Particles alive.</param>
/// <param name="Capacity">Most particles it can hold.</param>
/// <param name="Emitting">Whether it is still producing particles from its rate.</param>
/// <param name="Spawned">Particles born so far.</param>
/// <param name="Dropped">Births dropped because it was full.</param>
/// <param name="Blend">alpha or additive.</param>
/// <param name="Space">plane (2D) or volume (3D).</param>
/// <param name="Position">Where the emitter is [x, y, z].</param>
public sealed record ParticleEmitterInfo(string Name, int Live, int Capacity, bool Emitting, long Spawned, long Dropped, string Blend, string Space, double[] Position);

/// <summary>Result of <c>particles</c>: the emitters the last drawn frame showed and how many particles are alive.</summary>
/// <param name="Stage">The render stage's name.</param>
/// <param name="Emitters">How many emitters were shown.</param>
/// <param name="Live">Live particles across them.</param>
/// <param name="Items">One entry per emitter, in the order the game showed them (filtered by <c>name</c> when given).</param>
public sealed record ParticlesReport(string Stage, int Emitters, int Live, ParticleEmitterInfo[] Items);

public static partial class ControlOps
{
    private static ControlOp[] ParticleOps =>
    [
        new("particles",
            "The particle emitters the game showed in the last drawn frame (Draw2D.Particles, SceneWriter.Particles): for each its name, live particles, capacity, whether it is still emitting, particles spawned and dropped (dropped > 0 means its capacity is too small), blend and position. 'name' limits the list to emitters of that name; the totals then cover only them. Fails with 'no_particles' when the game's stage draws neither 2D nor 3D views.",
            """{"type":"object","properties":{"name":{"type":"string"}}}""", false),
    ];
}
