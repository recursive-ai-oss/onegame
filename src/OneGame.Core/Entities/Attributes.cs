namespace OneGame.Core;

/// <summary>
/// Declares a partial class as an entity: synced properties, commands and sim logic in one class. The class must derive from <see cref="Entity"/> directly.
/// The generator completes it with change tracking, codecs, deltas, snapshots and a <c>Send</c> property for commands; add the generated types to an <see cref="EntityRegistry"/> with the
/// generated <c>EntityTypes.Register(registry)</c> (a game definition does this in <c>RegisterEntities</c>).
/// </summary>
/// <example>
/// A counter whose state changes only through a command and the per-tick step:
/// <code>
/// [Entity]
/// public partial class Counter : Entity
/// {
///     [Synced] public partial int Value { get; private set; }
///     [Synced] public partial int Step { get; private set; }
///
///     [Command]
///     public void SetStep(int step) =&gt; Step = step;
///
///     protected override void OnTick() =&gt; Value += Step;
///
///     [View]
///     public string Label() =&gt; $"count {Value}";   // views read state; assigning Value here is a compile error
/// }
///
/// // From a view or a test: counter.Send.SetStep(2);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EntityAttribute : Attribute
{
    /// <summary>The schema version of the synced state. Raise it whenever properties are added, removed, reordered or retyped, and add a <see cref="MigrateAttribute"/> method for each older version still in saved data. Defaults to 1.</summary>
    public uint Schema { get; set; } = 1;

    /// <summary>A fixed type id (default 0 means "derive it"). By default the id is a hash of the full type name, which stays stable until the type is renamed or moved.</summary>
    public uint Id { get; set; }

    /// <summary>Who may know the entity exists at all. Subscribers outside the audience never receive the entity. Members can be narrower still (see <see cref="SyncedAttribute.Visibility"/>). <see cref="Visibility.Owner"/> and <see cref="Visibility.Team"/> need an <see cref="OwnerKeyAttribute"/> or <see cref="TeamKeyAttribute"/> property.</summary>
    public Visibility Visibility { get; set; } = Visibility.Everyone;
}

/// <summary>Marks a partial property as synced state. It becomes tracked, serialized, hashed and replicated. Properties are written in declaration order.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SyncedAttribute : Attribute
{
    /// <summary>Who may receive the property. The authority leaves it out of the snapshots and deltas it builds for everyone else, so a replica holds the type's default there. Defaults to everyone who may read the scope.</summary>
    public Visibility Visibility { get; set; } = Visibility.Everyone;
}

/// <summary>
/// Marks the synced <see cref="uint"/> or <see cref="int"/> property that says which player owns the entity. A connection whose
/// trust names the same player (<c>Audience.Player</c>) is the entity's owner. Zero means nobody. At most one per entity.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class OwnerKeyAttribute : Attribute
{
}

/// <summary>
/// Marks the synced <see cref="uint"/> or <see cref="int"/> property that says which team the entity belongs to. A connection whose
/// trust names the same team (<c>Audience.Team</c>) is on the entity's team, and so is the owner. Zero means no team. At most one per entity.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class TeamKeyAttribute : Attribute
{
}

/// <summary>Who may receive an entity or one of its synced properties. The authority enforces it when it builds snapshots and deltas for a subscriber; a value that is not sent never reaches the subscriber's bytes at all. Every level is on top of the scope's read trust.</summary>
public enum Visibility
{
    /// <summary>Every subscriber that may read the scope.</summary>
    Everyone = 0,

    /// <summary>Only the owner of the entity (the connection whose player matches the entity's <see cref="OwnerKeyAttribute"/> property).</summary>
    Owner = 1,

    /// <summary>The owner and everyone on the entity's team (<see cref="TeamKeyAttribute"/> property matches the connection's team).</summary>
    Team = 2,

    /// <summary>Never leaves the authority: no subscriber receives it, whatever its trust.</summary>
    Hidden = 3,
}

/// <summary>Marks an entity method as a command: a request that a view or test sends and the authority validates and runs. Call it through <c>entity.Send.Name(...)</c>; calling the method directly runs it here and now, which only sim code should do.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    /// When true, the authority accepts the command from a connection only if its trust owns the scope (<c>Trust.Own</c>) and its player is the target
    /// entity's owner (<see cref="OwnerKeyAttribute"/>). Others are refused before the handler runs. Needs an <see cref="OwnerKeyAttribute"/> property.
    /// Commands sent from the authority's own code are not checked.
    /// </summary>
    public bool OwnerOnly { get; set; }

    /// <summary>
    /// When true, a replica that has prediction enabled runs this command on its own predicted copy of the scope the moment it is sent, and
    /// again (rolled back and resimulated) whenever the authority's state arrives, until the authority has answered. The handler must only read
    /// and write what that replica holds: analyzers report reads of members the audience cannot see (OG0020), random draws without
    /// <see cref="UsesRandom"/> (OG0021) and writes to hidden members (OG0022).
    /// </summary>
    public bool Predicted { get; set; }

    /// <summary>
    /// Says that a <see cref="Predicted"/> handler draws from a named random stream. Replicas hold the streams only when the scope sends them
    /// (not <c>RandomStreams = Hidden</c>); when they do not, the command is sent without being predicted.
    /// </summary>
    public bool UsesRandom { get; set; }
}

/// <summary>Marks a member or a whole class as view code. Views read state and send commands; the analyzers make writing synced state from a view a compile error. View code may use floats and other things sim code may not.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ViewAttribute : Attribute
{
}

/// <summary>Marks a class, struct or method outside an entity as sim code, so the sim analyzers check it. Entities and <see cref="ISimSystem"/> types are sim code already.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class SimAttribute : Attribute
{
}

/// <summary>Marks a method that changes simulation state. View code may not call it.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MutatesStateAttribute : Attribute
{
}

/// <summary>Marks a partial struct whose public fields and properties form a synced value. The generator adds the codec, equality and JSON dump.</summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SyncedStructAttribute : Attribute
{
}

/// <summary>Marks a struct whose public fields become the columns of a generated struct-of-arrays table (<c>&lt;Name&gt;Table</c>) for hot sims.</summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class HotDataAttribute : Attribute
{
    /// <summary>The table's name in snapshots. Defaults to the struct's name.</summary>
    public string? Name { get; set; }

    /// <summary>Whether subscribers receive the table. Only <see cref="Visibility.Everyone"/> (default) and <see cref="Visibility.Hidden"/> apply: rows have no owner.</summary>
    public Visibility Visibility { get; set; } = Visibility.Everyone;
}

/// <summary>Marks a static method on an entity that loads state saved at an older schema version into the current layout: <c>static void Name(ref SyncReader old, MyEntity entity)</c>.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MigrateAttribute : Attribute
{
    /// <summary>Creates the attribute for state saved at <paramref name="fromVersion"/>.</summary>
    public MigrateAttribute(uint fromVersion) => FromVersion = fromVersion;

    /// <summary>The older schema version this method reads.</summary>
    public uint FromVersion { get; }
}

/// <summary>
/// Vouches that an API gives the same result on every machine and run, so sim code may call it. Put it on a method, a type or a
/// whole assembly. Sim code that calls an API outside its own project without this attribute (or a <see cref="SimTrustAttribute"/>
/// entry, or a built-in framework allowance) gets warning OG0015. Helpers in the same project need no attribute: the analyzers
/// read their bodies instead, and the attribute on a helper tells them to stop looking.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface
    | AttributeTargets.Enum | AttributeTargets.Delegate | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor,
    AllowMultiple = false, Inherited = false)]
public sealed class SimSafeAttribute : Attribute
{
}

/// <summary>
/// Lets sim code call a library you trust to be deterministic. <see cref="Name"/> is a namespace, a type's full name or an assembly
/// name. The same list can be given as <c>onegame_sim_trusted = A.B; C.D</c> in <c>.editorconfig</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class SimTrustAttribute : Attribute
{
    /// <summary>Trusts the namespace, type or assembly called <paramref name="name"/>.</summary>
    /// <param name="name">A namespace, a type's full name or an assembly name.</param>
    public SimTrustAttribute(string name) => Name = name;

    /// <summary>The trusted namespace, type or assembly name.</summary>
    public string Name { get; }
}
