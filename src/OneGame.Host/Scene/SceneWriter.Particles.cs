using System.Numerics;
using OneGame.Core;
using OneGame.Particles;
using OneGame.Render3D;

namespace OneGame.Host;

public sealed partial class SceneWriter
{
    private readonly List<(ParticleEmitter Emitter, BillboardTexture3D? Texture)> _emitters = [];
    private readonly List<ParticleEmitter> _emitterRefs = [];
    private long _lastTick = long.MinValue;
    private float _lastTime;

    /// <summary>View time in seconds: the shown tick and fraction at the simulation's rate. Particles read it, so captures and replays repeat exactly.</summary>
    public float Time { get; private set; }

    /// <summary>View time since the previous prepared frame, in seconds; zero when the same moment is described again or time went backwards.</summary>
    public float DeltaTime { get; private set; }

    /// <summary>The emitters handed to <c>Particles</c> in the last prepared frame, in call order. The harness reads it.</summary>
    public IReadOnlyList<ParticleEmitter> ParticleEmitters => _emitterRefs;

    internal List<(ParticleEmitter Emitter, BillboardTexture3D? Texture)> Emitters => _emitters;

    // Called once per Prepare, before the entities are asked.
    internal void BeginTime()
    {
        Time = (Tick + Alpha.ToSingle()) / GameInstance.TickRate;
        DeltaTime = _lastTick == long.MinValue || Time <= _lastTime ? 0 : Time - _lastTime;
        _lastTick = Tick;
        _lastTime = Time;
    }

    /// <summary>
    /// Shows a particle emitter in the scene as camera-facing quads. Call it every frame from view code, with the emitter you keep in a field:
    /// the first call of a frame moves it to <paramref name="position"/> and steps it by the frame's <see cref="DeltaTime"/>. Its settings should use
    /// <see cref="ParticleSpace.Volume"/>. Quads are depth tested against the scene and sorted far to near; additive emitters add their light and
    /// glow through bloom when <see cref="ParticleSettings.Emission"/> is above 1. An emitter that is not passed in a frame neither moves nor draws.
    /// </summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="position">Where it is, in world units.</param>
    /// <param name="texture">The image or sheet its settings describe; null shows a soft round spot.</param>
    public void Particles(ParticleEmitter emitter, Vector3 position, BillboardTexture3D? texture = null)
    {
        ArgumentNullException.ThrowIfNull(emitter);
        emitter.Position = position;
        for (var i = 0; i < _emitters.Count; i++)
        {
            if (ReferenceEquals(_emitters[i].Emitter, emitter))
            {
                return;
            }
        }

        _emitters.Add((emitter, texture));
        _emitterRefs.Add(emitter);
        emitter.Step(DeltaTime);
    }

    /// <summary>Shows a particle emitter at an entity transform's position; see <see cref="Particles(ParticleEmitter, Vector3, BillboardTexture3D?)"/>.</summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="at">Where it is.</param>
    /// <param name="texture">The image or sheet its settings describe; null shows a soft round spot.</param>
    public void Particles(ParticleEmitter emitter, Transform3 at, BillboardTexture3D? texture = null) =>
        Particles(emitter, ToVector(at.Position), texture);
}
