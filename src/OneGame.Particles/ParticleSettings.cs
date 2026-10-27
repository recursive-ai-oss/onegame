namespace OneGame.Particles;

/// <summary>How particles are combined with what is behind them.</summary>
public enum ParticleBlend
{
    /// <summary>Drawn over the background with their opacity: smoke, dust, leaves.</summary>
    Alpha,

    /// <summary>Added to the background, brighter where they overlap and order independent: sparks, fire, glows, magic.</summary>
    Additive,
}

/// <summary>Whether particles move in a flat plane (2D games) or in space.</summary>
public enum ParticleSpace
{
    /// <summary>X and Y only. Directions spread by an angle in the plane and the spawn area is a disc. Use for <c>Draw2D</c>.</summary>
    Plane,

    /// <summary>X, Y and Z. Directions spread in a cone and the spawn area is a ball. Use for 3D scenes.</summary>
    Volume,
}

/// <summary>
/// Everything that describes how an emitter looks and moves. Set what differs from the defaults when you make it; the values are read as the
/// emitter runs, so changing one (a rate that follows a throttle, a gravity that flips) takes effect on the next step. Units are the world's
/// own: pixels in 2D (+Y down), metres in 3D. The emitter has a fixed <see cref="Capacity"/>; spawns past it are dropped, never reallocated.
/// </summary>
public sealed class ParticleSettings
{
    /// <summary>The most particles alive at once; fixed when the emitter is made. Default 256.</summary>
    public int Capacity { get; init; } = 256;

    /// <summary>Particles per second while the emitter is playing; 0 emits only bursts.</summary>
    public float Rate { get; set; }

    /// <summary>True to emit until stopped; false to emit for <see cref="Duration"/> seconds after <c>Play</c> and then stop.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Seconds a non-looping emitter emits for.</summary>
    public float Duration { get; set; } = 1f;

    /// <summary>Seconds to run the emitter for before it is first shown, so a steady effect (smoke, a campfire) is already full on the first frame. Capped at 30.</summary>
    public float Prewarm { get; set; }

    /// <summary>Seconds each particle lives.</summary>
    public FloatRange Lifetime { get; set; } = new(0.5f, 1f);

    /// <summary>Initial speed in world units per second.</summary>
    public FloatRange Speed { get; set; } = new(50f, 100f);

    /// <summary>The direction particles are launched in (need not be normalised). Default up on screen in 2D (-Y).</summary>
    public Vector3 Direction { get; set; } = -Vector3.UnitY;

    /// <summary>Half-angle in radians around <see cref="Direction"/> that launches are spread over: 0 is a beam, pi is every direction.</summary>
    public float Spread { get; set; } = 0.5f;

    /// <summary>Acceleration in world units per second squared (+Y is down in 2D, up in 3D).</summary>
    public Vector3 Gravity { get; set; }

    /// <summary>Air resistance: the fraction of velocity lost per second is about this much (0 = none, 3 = settles within a second).</summary>
    public float Drag { get; set; }

    /// <summary>Radius of the disc or ball particles are born in around the emitter.</summary>
    public float SpawnRadius { get; set; }

    /// <summary>Plane for 2D, Volume for 3D.</summary>
    public ParticleSpace Space { get; init; } = ParticleSpace.Plane;

    /// <summary>True to keep particles in the emitter's own frame, so they move with it; false (the default) leaves them where they were born, so a moving emitter draws a trail.</summary>
    public bool LocalSpace { get; set; }

    /// <summary>Particle size at birth, in world units (width and height; sprites are square).</summary>
    public FloatRange Size { get; set; } = 8f;

    /// <summary>Multiplier on the size over the particle's life.</summary>
    public ParticleCurve SizeOverLife { get; set; } = ParticleCurve.Constant(1f);

    /// <summary>Colour over the life, sRGB with straight alpha. Default opaque white.</summary>
    public ParticleGradient Color { get; set; } = ParticleGradient.Constant(Vector4.One);

    /// <summary>Brightness multiplier for 3D, where values above 1 glow through bloom. 2D ignores it.</summary>
    public float Emission { get; set; } = 1f;

    /// <summary>Starting angle in radians.</summary>
    public FloatRange Rotation { get; set; }

    /// <summary>Spin in radians per second.</summary>
    public FloatRange AngularVelocity { get; set; }

    /// <summary>Columns of frames in the texture (1 for a single image).</summary>
    public int SheetColumns { get; set; } = 1;

    /// <summary>Rows of frames in the texture.</summary>
    public int SheetRows { get; set; } = 1;

    /// <summary>How many frames of the sheet to use, counting row by row; 0 uses all of them.</summary>
    public int FrameCount { get; set; }

    /// <summary>True to play the frames once over each particle's life (an explosion animation); false to give each particle one random frame.</summary>
    public bool AnimateFrames { get; set; }

    /// <summary>How particles are combined with the background.</summary>
    public ParticleBlend Blend { get; set; } = ParticleBlend.Alpha;
}
