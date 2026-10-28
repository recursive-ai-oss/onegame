using OneGame.Core;

namespace OneGame.Navigation;

/// <summary>What an agent in a <see cref="NavCrowd"/> is doing.</summary>
public enum NavAgentStatus : byte
{
    /// <summary>Standing with no destination (or stopped).</summary>
    Idle = 0,

    /// <summary>Walking toward its destination.</summary>
    Walking = 1,

    /// <summary>Stopped at its destination.</summary>
    Arrived = 2,

    /// <summary>Stopped at the nearest reachable point because the destination itself cannot be reached.</summary>
    ArrivedShort = 3,

    /// <summary>Gave up after making no progress through every recovery.</summary>
    Stuck = 4,

    /// <summary>No route could be started or kept.</summary>
    NoPath = 5,
}

/// <summary>An agent's settings, in world units and units per second. They are saved with the agent.</summary>
public readonly record struct NavAgentOptions
{
    /// <summary>Body radius used between agents (two agents keep their centres apart by the sum of their radii). Above 0.</summary>
    public Fixed Radius { get; init; }

    /// <summary>Top speed in world units per second. Above 0.</summary>
    public Fixed MaxSpeed { get; init; }

    /// <summary>Largest change of velocity per second. Above 0.</summary>
    public Fixed MaxAcceleration { get; init; }

    /// <summary>Distance from the destination within which the agent has arrived. Zero or more.</summary>
    public Fixed ArrivalRadius { get; init; }

    /// <summary>Weight in avoidance: of two agents that must give way, the one with the greater priority gives way less. Above 0.</summary>
    public Fixed Priority { get; init; }
}

/// <summary>One agent of a <see cref="NavCrowd"/> as sim code sees it: fixed-point positions only. A snapshot taken after the last tick, add or removal.</summary>
public readonly record struct NavAgent
{
    /// <summary>The agent's number: 0, 1, 2 in the order agents were added, never reused.</summary>
    public int Id { get; init; }

    /// <summary>Where the agent stands (always on the walkable ground).</summary>
    public FVec2 Position { get; init; }

    /// <summary>How far and which way it moved in the last tick, in world units per second.</summary>
    public FVec2 Velocity { get; init; }

    /// <summary>Where it is going (its own position when it has no destination).</summary>
    public FVec2 Destination { get; init; }

    /// <summary>What it is doing.</summary>
    public NavAgentStatus Status { get; init; }

    /// <summary>Its body radius.</summary>
    public Fixed Radius { get; init; }
}

/// <summary>Settings of a <see cref="NavCrowd"/>. They are level data: build the crowd with the same ones on every node.</summary>
public readonly record struct NavCrowdOptions
{
    /// <summary>Radius the walkable ground is shrunk by, so agents up to this radius clear every wall. Zero means a quarter of a cell.</summary>
    public Fixed AgentRadius { get; init; }

    /// <summary>Most agents at once; 0 means 256. At most 65,536.</summary>
    public int MaxAgents { get; init; }

    /// <summary>Ticks per second of the simulation: every tick is one step of 1/TicksPerSecond seconds. 0 means 60.</summary>
    public int TicksPerSecond { get; init; }

    /// <summary>Whether subscribers receive the crowd. <see cref="Visibility.Everyone"/> (the default) sends the whole state, a few hundred bytes per agent, in every frame in which it changed; <see cref="Visibility.Hidden"/> keeps it on the authority.</summary>
    public Visibility Visibility { get; init; }
}
