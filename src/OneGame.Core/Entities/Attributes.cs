namespace OneGame.Core;

/// <summary>Declares a partial class as an entity: synced properties, commands and sim logic in one class. The class must derive from <see cref="Entity"/> directly.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EntityAttribute : Attribute
{
    /// <summary>The schema version of the synced state. Raise it whenever properties are added, removed, reordered or retyped, and add a <see cref="MigrateAttribute"/> method for each older version still in saved data. Defaults to 1.</summary>
    public uint Schema { get; set; } = 1;

    /// <summary>A fixed type id (default 0 means "derive it"). By default the id is a hash of the full type name, which stays stable until the type is renamed or moved.</summary>
    public uint Id { get; set; }
}

/// <summary>Marks a partial property as synced state. It becomes tracked, serialized, hashed and replicated. Properties are written in declaration order.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SyncedAttribute : Attribute
{
    /// <summary>Who may read the property. Recorded for the sync layer; nothing is enforced in the entity model yet.</summary>
    public Visibility Visibility { get; set; } = Visibility.Everyone;
}

/// <summary>Who may see a synced property. The sync layer will enforce it; the entity model only records it.</summary>
public enum Visibility
{
    /// <summary>Every subscriber.</summary>
    Everyone = 0,

    /// <summary>Only the owner of the entity.</summary>
    Owner = 1,

    /// <summary>Only the owner's team.</summary>
    Team = 2,

    /// <summary>Never leaves the authority.</summary>
    Hidden = 3,
}

/// <summary>Marks an entity method as a command: a request that a view or test sends and the authority validates and runs. Call it through <c>entity.Send.Name(...)</c>; calling the method directly runs it here and now, which only sim code should do.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
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
