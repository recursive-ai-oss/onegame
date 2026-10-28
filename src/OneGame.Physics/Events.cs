using RecursiveAI.OnePhysics.Core;
using RecursiveAI.OnePhysics.Physics2D;

namespace OneGame.Physics;

/// <summary>What happened between two bodies during a step.</summary>
public enum PhysicsEventKind : byte
{
    /// <summary>Two bodies started touching.</summary>
    ContactBegin = 0,

    /// <summary>Two bodies are still touching (recorded only with <see cref="PhysicsOptions.StayEvents"/>).</summary>
    ContactStay = 1,

    /// <summary>Two bodies stopped touching.</summary>
    ContactEnd = 2,

    /// <summary>Two bodies met at speed (recorded only with <see cref="PhysicsOptions.HitEvents"/>); <see cref="PhysicsEvent.Speed"/> is the approach speed.</summary>
    ContactHit = 3,

    /// <summary>A body started overlapping a trigger. <see cref="PhysicsEvent.A"/> is the trigger.</summary>
    TriggerEnter = 4,

    /// <summary>A body stopped overlapping a trigger. <see cref="PhysicsEvent.A"/> is the trigger.</summary>
    TriggerExit = 5,

    /// <summary>A dynamic body fell asleep. <see cref="PhysicsEvent.A"/> is the body.</summary>
    Sleep = 6,

    /// <summary>A dynamic body woke up. <see cref="PhysicsEvent.A"/> is the body.</summary>
    Wake = 7,
}

/// <summary>One event of the last step, resolved to game types.</summary>
public readonly struct PhysicsEvent
{
    internal PhysicsEvent(PhysicsEventKind kind, BodyRef a, BodyRef b, FVec2 point, FVec2 normal, Fixed speed)
    {
        Kind = kind;
        A = a;
        B = b;
        Point = point;
        Normal = normal;
        Speed = speed;
    }

    /// <summary>What happened.</summary>
    public PhysicsEventKind Kind { get; }

    /// <summary>The first body (for a trigger event, the trigger; for sleep and wake, the body).</summary>
    public BodyRef A { get; }

    /// <summary>The second body; null for sleep and wake.</summary>
    public BodyRef B { get; }

    /// <summary>A point of contact for <see cref="PhysicsEventKind.ContactBegin"/>, <see cref="PhysicsEventKind.ContactStay"/> and <see cref="PhysicsEventKind.ContactHit"/>; zero otherwise.</summary>
    public FVec2 Point { get; }

    /// <summary>The unit contact normal pointing from <see cref="A"/> towards <see cref="B"/>, for the same kinds; zero otherwise.</summary>
    public FVec2 Normal { get; }

    /// <summary>The approach speed of a <see cref="PhysicsEventKind.ContactHit"/>; zero otherwise.</summary>
    public Fixed Speed { get; }

    /// <summary>True when <paramref name="body"/> is <see cref="A"/> or <see cref="B"/>.</summary>
    /// <param name="body">The body to look for.</param>
    public bool Involves(BodyRef body) => A == body || B == body;

    /// <summary>The other body of the pair, or the null reference when <paramref name="body"/> is not part of it.</summary>
    /// <param name="body">One of the two bodies.</param>
    public BodyRef Other(BodyRef body) => A == body ? B : B == body ? A : default;

    /// <inheritdoc />
    public override string ToString() => $"{Kind} {A} {B}";
}

/// <summary>
/// The events of the last step, in the order the physics world delivered them: by kind, then by the bodies' slots. The order is the same
/// on every node. Read them in <c>OnTick</c>; the list is replaced by the next step and is saved and restored with the space.
/// </summary>
public readonly struct PhysicsEvents
{
    private readonly PhysicsWorld2D? _world;

    internal PhysicsEvents(PhysicsWorld2D? world)
    {
        _world = world;
    }

    /// <summary>The number of events.</summary>
    public int Count => Total(_world);

    /// <summary>The event at an index, from 0 to <see cref="Count"/> - 1.</summary>
    /// <param name="index">The index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the list.</exception>
    public PhysicsEvent this[int index]
    {
        get
        {
            var world = _world;
            var visible = 0;
            if (world is not null)
            {
                var all = world.Events;
                for (var i = 0; i < all.Length; i++)
                {
                    if (!Exposed(all[i].Kind))
                    {
                        continue;
                    }

                    if (visible == index)
                    {
                        return Resolve(world, all[i]);
                    }

                    visible++;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index), index, "There is no event at this index.");
        }
    }

    /// <summary>Returns an enumerator over the events.</summary>
    public Enumerator GetEnumerator() => new(this);

    private static int Total(PhysicsWorld2D? world)
    {
        if (world is null)
        {
            return 0;
        }

        var n = 0;
        var all = world.Events;
        for (var i = 0; i < all.Length; i++)
        {
            if (Exposed(all[i].Kind))
            {
                n++;
            }
        }

        return n;
    }

    private static bool Exposed(EventKind kind) => kind != EventKind.JointBreak;

    private static BodyRef Ref(BodyHandle h) => h.Generation == 0 ? default : new BodyRef(h.Index, h.Generation);

    private static PhysicsEvent Resolve(PhysicsWorld2D world, in RecursiveAI.OnePhysics.Core.PhysicsEvent e)
    {
        switch (e.Kind)
        {
            case EventKind.ContactBegin:
            case EventKind.ContactPersist:
            case EventKind.ContactHit:
            {
                var hit = world.GetCollision(in e);
                var kind = e.Kind == EventKind.ContactBegin ? PhysicsEventKind.ContactBegin : e.Kind == EventKind.ContactPersist ? PhysicsEventKind.ContactStay : PhysicsEventKind.ContactHit;
                var point = hit.HasPoint ? new FVec2(PhysicsLimits.FromDouble(hit.Point.X), PhysicsLimits.FromDouble(hit.Point.Y)) : default;
                var normal = new FVec2(PhysicsLimits.FromSingle(hit.Normal.X), PhysicsLimits.FromSingle(hit.Normal.Y));
                var speed = e.Kind == EventKind.ContactHit ? PhysicsLimits.FromSingle(e.Value) : Fixed.Zero;
                return new PhysicsEvent(kind, Ref(e.BodyA), Ref(e.BodyB), point, normal, speed);
            }

            case EventKind.ContactEnd:
                return new PhysicsEvent(PhysicsEventKind.ContactEnd, Ref(e.BodyA), Ref(e.BodyB), default, default, default);
            case EventKind.SensorEnter:
                return new PhysicsEvent(PhysicsEventKind.TriggerEnter, Ref(e.BodyA), Ref(e.BodyB), default, default, default);
            case EventKind.SensorExit:
                return new PhysicsEvent(PhysicsEventKind.TriggerExit, Ref(e.BodyA), Ref(e.BodyB), default, default, default);
            case EventKind.Sleep:
                return new PhysicsEvent(PhysicsEventKind.Sleep, Ref(e.BodyA), default, default, default, default);
            default:
                return new PhysicsEvent(PhysicsEventKind.Wake, Ref(e.BodyA), default, default, default, default);
        }
    }

    /// <summary>Walks the events without allocating.</summary>
    public struct Enumerator
    {
        private readonly PhysicsWorld2D? _world;
        private int _position;

        internal Enumerator(PhysicsEvents events)
        {
            _world = events._world;
            _position = -1;
            Current = default;
        }

        /// <summary>The event the enumerator stands on.</summary>
        public PhysicsEvent Current { get; private set; }

        /// <summary>Moves to the next event.</summary>
        /// <returns>False when there are no more.</returns>
        public bool MoveNext()
        {
            var world = _world;
            if (world is null)
            {
                return false;
            }

            var all = world.Events;
            while (++_position < all.Length)
            {
                if (Exposed(all[_position].Kind))
                {
                    Current = Resolve(world, all[_position]);
                    return true;
                }
            }

            return false;
        }
    }
}
