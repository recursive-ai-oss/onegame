using System.Globalization;
using RecursiveAI.OneMath;
using RecursiveAI.OnePath;
using RecursiveAI.OnePath.Mesh;

namespace OneGame.Navigation;

/// <summary>
/// A crowd of agents walking a navigation mesh built from a <see cref="NavGrid"/>, with avoidance of walls and of each other, kept in the
/// world as a hot table so that saves, replays, replicas and the world hash carry it. The simulation steps it once per tick (it is also
/// stepped by a system the world runs); agents join, leave and get destinations through <see cref="TryAdd(FVec2, out int)"/>, <see cref="Remove"/> and
/// <see cref="SetDestination"/>, which a command handler calls. Sim code sees only <see cref="Fixed"/> and <see cref="FVec2"/>.
/// <code>
/// // while building the world, on every node, in the same order:
/// var crowd = NavCrowd.AddTo(world, map, new NavCrowdOptions { AgentRadius = Fixed.FromInt(3) });
///
/// // in a command handler or OnTick:
/// if (NavCrowd.Of(World!)!.TryAdd(spawn, out var id)) { NavCrowd.Of(World!)!.SetDestination(id, goal); }
/// </code>
/// What is synced: the crowd's complete state (every agent's position, velocity, settings, path and avoidance memory) is the table, written
/// with OnePath's byte-exact format and hashed with the world. The mesh is not state: it is built from the map, which is level data every
/// node builds the same way, so a crowd must be created with the same map and options everywhere. The views of the agents
/// (<see cref="Agents"/>) are a cache rebuilt from that state.
/// </summary>
public sealed class NavCrowd : IHotTable
{
    /// <summary>The default table name in snapshots.</summary>
    public const string TableName = "og.nav.crowd";

    private const int FormatVersion = 1;
    private const int HardMaxAgents = 65536;

    // Upper bound on a crowd state read from the wire: generous per agent (path corridors, cached walls) and never above 64 MiB.
    private const int BytesPerAgentLimit = 16 * 1024;

    private readonly World _world;
    private readonly NavMesh _mesh;
    private readonly CrowdOptions _crowdOptions;
    private readonly string _name;
    private readonly Visibility _visibility;
    private readonly double _dt;
    private readonly int _maxStateBytes;
    private readonly List<Agent> _list = [];
    private Crowd _crowd;
    private byte[] _scratch = new byte[1024];
    private NavAgent[] _views = new NavAgent[16];
    private int _count;

    private NavCrowd(World world, NavGrid map, NavCrowdOptions options, string name)
    {
        _world = world;
        _name = name;
        Map = map;
        Options = options;
        _visibility = options.Visibility;
        if (_visibility is not (Visibility.Everyone or Visibility.Hidden))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Visibility, "A crowd is Everyone or Hidden: it is one table for the whole world.");
        }

        var maxAgents = options.MaxAgents == 0 ? 256 : options.MaxAgents;
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAgents, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxAgents, HardMaxAgents);
        var rate = options.TicksPerSecond == 0 ? 60 : options.TicksPerSecond;
        ArgumentOutOfRangeException.ThrowIfLessThan(rate, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rate, 1000);

        // 1.0 / rate is one IEEE division: the same step on every platform.
        _dt = 1.0 / rate;
        _maxStateBytes = (int)Math.Min(64L << 20, 4096L + ((long)maxAgents * BytesPerAgentLimit));
        var cell = map.Space.CellSize;
        var radius = options.AgentRadius.Raw == 0 ? Fixed.FromRaw(cell.Raw >> 2) : options.AgentRadius;
        if (radius.Raw <= 0 || radius.Raw >= cell.Raw * map.Width)
        {
            throw new ArgumentOutOfRangeException(nameof(options), radius.ToString(), "The agent radius must be above 0 and smaller than the map.");
        }

        AgentRadius = radius;
        var cellSize = NavConvert.ToDouble(cell);
        var walkable = new bool[map.Width * map.Height];
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                walkable[(y * map.Width) + x] = !map.IsBlocked(x, y);
            }
        }

        var origin = new Vector2D(NavConvert.ToDouble(map.Space.Origin.X), NavConvert.ToDouble(map.Space.Origin.Y));
        var settings = NavSettings.Humanoid with
        {
            AgentRadius = (float)NavConvert.ToDouble(radius),
            CellSize = cellSize / 4,
            // One tile covers the map: a power-of-two multiple of the cell, so it is exact in float32.
            TileSize = TileSizeFor(map, cellSize),
            OriginX = origin.X,
            OriginZ = origin.Y,
        };
        _mesh = NavMesh.Build2D(walkable, map.Width, map.Height, cellSize, origin, settings);
        _crowdOptions = new CrowdOptions { MaxAgents = maxAgents, MaxThreads = 1 };
        _crowd = _mesh.CreateCrowd(_crowdOptions);
        DefaultAgent = new NavAgentOptions
        {
            Radius = radius,
            MaxSpeed = Fixed.FromRaw(cell.Raw * 3),
            MaxAcceleration = Fixed.FromRaw(cell.Raw * 12),
            ArrivalRadius = Fixed.FromRaw(radius.Raw >> 1),
            Priority = Fixed.One,
        };
        MaxAgents = maxAgents;
    }

    /// <summary>The map the mesh was built from.</summary>
    public NavGrid Map { get; }

    /// <summary>The settings the crowd was made with.</summary>
    public NavCrowdOptions Options { get; }

    /// <summary>The radius the walkable ground was shrunk by.</summary>
    public Fixed AgentRadius { get; }

    /// <summary>Settings used by <see cref="TryAdd(FVec2, out int)"/>: the mesh radius, three cells per second, arrival within half a radius.</summary>
    public NavAgentOptions DefaultAgent { get; }

    /// <summary>Most agents at once.</summary>
    public int MaxAgents { get; }

    /// <summary>Steps taken since the crowd was made or last restored from a state (part of the state).</summary>
    public long StepCount => _crowd.StepCount;

    /// <summary>The agents in slot order, as of the last tick, add, removal or restore. Nothing is allocated reading it.</summary>
    public ReadOnlySpan<NavAgent> Agents => new(_views, 0, _count);

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc />
    public int Count => _count;

    /// <inheritdoc />
    public Visibility Visibility => _visibility;

    /// <summary>Makes a crowd over a map and adds it to a world as a table and as a system (it steps after the entities, once per tick).</summary>
    /// <param name="world">The world; add the crowd before any save is loaded, in the same order on every node.</param>
    /// <param name="map">The map. Its walls become the mesh; later edits do not change the mesh.</param>
    /// <param name="options">Settings; the same on every node.</param>
    /// <param name="name">The table name; give each crowd of a world its own.</param>
    /// <returns>The crowd.</returns>
    /// <exception cref="NavRangeException">The map reaches beyond ±2^21 units.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is out of range.</exception>
    public static NavCrowd AddTo(World world, NavGrid map, NavCrowdOptions options = default, string name = TableName)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentException.ThrowIfNullOrEmpty(name);
        var crowd = new NavCrowd(world, map, options, name);
        world.AddTable(crowd);
        world.AddSystem(new StepSystem(crowd));
        return crowd;
    }

    /// <summary>The world's crowd with this name, or null.</summary>
    /// <param name="world">The world.</param>
    /// <param name="name">The table name.</param>
    public static NavCrowd? Of(World world, string name = TableName)
    {
        ArgumentNullException.ThrowIfNull(world);
        var tables = world.Tables;
        for (var i = 0; i < tables.Count; i++)
        {
            if (tables[i] is NavCrowd c && c._name == name)
            {
                return c;
            }
        }

        return null;
    }

    /// <summary>Adds an agent with <see cref="DefaultAgent"/> at the walkable point nearest to a position.</summary>
    /// <param name="position">Where to put it.</param>
    /// <param name="id">The agent's number.</param>
    /// <returns>False when the crowd is full or no walkable ground is near.</returns>
    /// <exception cref="NavRangeException">The position is beyond ±2^21 units.</exception>
    /// <exception cref="ReplicaWriteException">The world is a replica.</exception>
    public bool TryAdd(FVec2 position, out int id) => TryAdd(position, DefaultAgent, out id);

    /// <summary>Adds an agent at the walkable point nearest to a position.</summary>
    /// <param name="position">Where to put it.</param>
    /// <param name="options">Its settings.</param>
    /// <param name="id">The agent's number.</param>
    /// <returns>False when the crowd is full or no walkable ground is near.</returns>
    /// <exception cref="NavRangeException">The position is beyond ±2^21 units.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A setting is out of range.</exception>
    /// <exception cref="ReplicaWriteException">The world is a replica.</exception>
    public bool TryAdd(FVec2 position, NavAgentOptions options, out int id)
    {
        AssertWritable();
        var p = ToMesh(position);
        var o = ToAgentOptions(options);
        id = -1;
        if (_crowd.Count >= MaxAgents)
        {
            return false;
        }

        Agent agent;
        try
        {
            agent = _crowd.Add(p, o);
        }
        catch (InvalidOperationException)
        {
            return false; // no walkable polygon near
        }

        if (agent.Id > int.MaxValue)
        {
            _crowd.Remove(agent);
            return false;
        }

        id = (int)agent.Id;
        Sync();
        return true;
    }

    /// <summary>Takes an agent out of the crowd.</summary>
    /// <param name="id">The agent's number.</param>
    /// <returns>False when there is no such agent.</returns>
    /// <exception cref="ReplicaWriteException">The world is a replica.</exception>
    public bool Remove(int id)
    {
        AssertWritable();
        var agent = _crowd.GetAgent(id);
        if (agent is null || !_crowd.Remove(agent))
        {
            return false;
        }

        Sync();
        return true;
    }

    /// <summary>Sends an agent to a position; a spot off the walkable ground means the nearest walkable point.</summary>
    /// <param name="id">The agent's number.</param>
    /// <param name="destination">Where to go.</param>
    /// <returns>False when there is no such agent.</returns>
    /// <exception cref="NavRangeException">The position is beyond ±2^21 units.</exception>
    /// <exception cref="ReplicaWriteException">The world is a replica.</exception>
    public bool SetDestination(int id, FVec2 destination)
    {
        AssertWritable();
        var p = ToMesh(destination);
        var agent = _crowd.GetAgent(id);
        if (agent is null)
        {
            return false;
        }

        agent.SetDestination(p);
        Sync();
        return true;
    }

    /// <summary>Stops an agent where it stands (other agents can still push it).</summary>
    /// <param name="id">The agent's number.</param>
    /// <returns>False when there is no such agent.</returns>
    /// <exception cref="ReplicaWriteException">The world is a replica.</exception>
    public bool Stop(int id)
    {
        AssertWritable();
        var agent = _crowd.GetAgent(id);
        if (agent is null)
        {
            return false;
        }

        agent.Stop();
        Sync();
        return true;
    }

    /// <summary>Finds an agent by number.</summary>
    /// <param name="id">The agent's number.</param>
    /// <param name="agent">Its view.</param>
    /// <returns>False when there is no such agent.</returns>
    public bool TryGet(int id, out NavAgent agent)
    {
        for (var i = 0; i < _count; i++)
        {
            if (_views[i].Id == id)
            {
                agent = _views[i];
                return true;
            }
        }

        agent = default;
        return false;
    }

    /// <summary>OnePath's 64-bit hash of the crowd state (FNV-1a of the saved bytes): equal on every node and platform when the states are equal.</summary>
    public ulong StateHash() => _crowd.StateHash();

    /// <summary>Steps the crowd once. The world does this every tick; call it yourself only for a crowd that is not in a world.</summary>
    public void Step()
    {
        _crowd.Step(_dt);
        Sync();
    }

    /// <inheritdoc />
    public void Clear()
    {
        _crowd = _mesh.CreateCrowd(_crowdOptions);
        Sync();
    }

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var size = _crowd.GetStateSize();
        if (_scratch.Length < size)
        {
            _scratch = new byte[Math.Max(size, _scratch.Length * 2)];
        }

        if (!_crowd.TrySaveState(_scratch, out var written))
        {
            throw new InvalidOperationException("The crowd state did not fit the buffer sized for it.");
        }

        writer.WriteByte(FormatVersion);
        writer.WriteBytes(_scratch.AsSpan(0, written));
    }

    /// <inheritdoc />
    public void Read(ref SyncReader reader)
    {
        var version = reader.ReadByte();
        var bytes = reader.ReadBytes(_maxStateBytes);
        if (!reader.Ok)
        {
            return;
        }

        if (version != FormatVersion)
        {
            reader.Fail(SyncError.InvalidValue);
            return;
        }

        try
        {
            _crowd.RestoreState(bytes); // all or nothing
        }
        catch (NavMeshFormatException)
        {
            reader.Fail(SyncError.InvalidValue);
            return;
        }

        Sync();
    }

    /// <inheritdoc />
    public void CopyFrom(IHotTable source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is not NavCrowd other)
        {
            throw new InvalidOperationException("A crowd table can only copy from another crowd table.");
        }

        var size = other._crowd.GetStateSize();
        if (_scratch.Length < size)
        {
            _scratch = new byte[Math.Max(size, _scratch.Length * 2)];
        }

        if (!other._crowd.TrySaveState(_scratch, out var written))
        {
            throw new InvalidOperationException("The crowd state did not fit the buffer sized for it.");
        }

        _crowd.RestoreState(_scratch.AsSpan(0, written));
        Sync();
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.BeginArray();
        for (var i = 0; i < _count; i++)
        {
            var a = _views[i];
            json.BeginObject();
            json.Name("id");
            json.Value(a.Id);
            json.Name("x");
            json.Raw(Number(a.Position.X));
            json.Name("y");
            json.Raw(Number(a.Position.Y));
            json.Name("vx");
            json.Raw(Number(a.Velocity.X));
            json.Name("vy");
            json.Raw(Number(a.Velocity.Y));
            json.Name("goalX");
            json.Raw(Number(a.Destination.X));
            json.Name("goalY");
            json.Raw(Number(a.Destination.Y));
            json.Name("status");
            json.Value(a.Status.ToString());
            json.EndObject();
        }

        json.EndArray();
    }

    /// <inheritdoc />
    public bool TrySetFromText(int row, string column, string text) => false;

    private static string Number(Fixed value) => value.ToDouble().ToString("R", CultureInfo.InvariantCulture);

    private static double TileSizeFor(NavGrid map, double cell)
    {
        var extent = Math.Max(map.Width, map.Height) * cell;
        var size = 1.0;
        while (size < extent)
        {
            size *= 2;
        }

        return size;
    }

    private void AssertWritable()
    {
        if (_world.Role == WorldRole.Replica)
        {
            throw new ReplicaWriteException("A replica world does not simulate. Change the crowd on its authority, through a command.");
        }
    }

    private static Vector3D ToMesh(FVec2 p) => new(NavConvert.ToDouble(p.X), 0, NavConvert.ToDouble(p.Y));

    private static AgentOptions ToAgentOptions(NavAgentOptions o)
    {
        var radius = NavConvert.ToDouble(o.Radius);
        var speed = NavConvert.ToDouble(o.MaxSpeed);
        var accel = NavConvert.ToDouble(o.MaxAcceleration);
        var arrival = NavConvert.ToDouble(o.ArrivalRadius);
        var priority = NavConvert.ToDouble(o.Priority);
        var result = Agent.Humanoid with
        {
            Radius = radius,
            MaxSpeed = speed,
            MaxAcceleration = accel,
            ArrivalRadius = arrival,
            Priority = priority,
        };
        result.Validate();
        return result;
    }

    // Rebuilds the cached views from the crowd. Positions and velocities come out of the crowd as doubles within ±2^21 and become Fixed by
    // round-to-nearest-even (NavConvert.FromDouble): a pure function of values that are bit-identical on every platform.
    private void Sync()
    {
        _list.Clear();
        _crowd.CopyAgents(_list);
        _count = _list.Count;
        if (_views.Length < _count)
        {
            _views = new NavAgent[Math.Max(_count, _views.Length * 2)];
        }

        for (var i = 0; i < _count; i++)
        {
            var a = _list[i];
            var p = a.Position;
            var v = a.Velocity;
            var d = a.Destination;
            _views[i] = new NavAgent
            {
                Id = (int)a.Id,
                Position = new FVec2(NavConvert.FromDouble(p.X), NavConvert.FromDouble(p.Z)),
                Velocity = new FVec2(NavConvert.FromDouble(v.X), NavConvert.FromDouble(v.Z)),
                Destination = new FVec2(NavConvert.FromDouble(d.X), NavConvert.FromDouble(d.Z)),
                Status = Status(a),
                Radius = NavConvert.FromDouble(a.Options.Radius),
            };
        }

    }

    private static NavAgentStatus Status(Agent a) => a.State switch
    {
        RecursiveAI.OnePath.Crowds.Corridors.FollowerState.Moving or RecursiveAI.OnePath.Crowds.Corridors.FollowerState.OffMeshLink => NavAgentStatus.Walking,
        RecursiveAI.OnePath.Crowds.Corridors.FollowerState.Arrived => a.ReachedTarget ? NavAgentStatus.Arrived : NavAgentStatus.ArrivedShort,
        RecursiveAI.OnePath.Crowds.Corridors.FollowerState.Stuck => NavAgentStatus.Stuck,
        RecursiveAI.OnePath.Crowds.Corridors.FollowerState.NoPath => NavAgentStatus.NoPath,
        _ => NavAgentStatus.Idle,
    };
}

/// <summary>Steps a crowd once per world tick. Kept apart from the table so that the table, which converts to and from doubles at the OnePath boundary, is not itself sim code.</summary>
internal sealed class StepSystem(NavCrowd crowd) : ISimSystem
{
    public void Tick(World world) => crowd.Step();
}
