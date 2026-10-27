namespace OneGame.Pages;

/// <summary>What a host-scope number means to the page renderer.</summary>
public enum PageScopeKind
{
    /// <summary>The scope is drawn as if it were not there.</summary>
    None,

    /// <summary>The scope's content is drawn when its layer is drawn, not with layer 0.</summary>
    Layer,

    /// <summary>The scope's content is drawn apart and added to what is below (a glow).</summary>
    Additive,
}

/// <summary>A resolved host-scope binding.</summary>
/// <param name="Kind">What the scope does.</param>
/// <param name="Layer">The layer a <see cref="PageScopeKind.Layer"/> scope draws with.</param>
public readonly record struct PageScopeBinding(PageScopeKind Kind, int Layer = 0);

/// <summary>
/// Gives the numbers a page sets with <c>--host-scope</c> (or <c>StyleDeclaration.SetHostScope</c>) their meaning. An unbound number draws as if the
/// scope were absent, so a page can be written before the game decides what its scopes do.
/// </summary>
public sealed class PageScopes
{
    /// <summary>The layer drawn beneath the game's 2D pass: sprites and the <c>OneGame.UI</c> tree draw over it.</summary>
    public const int Under = -1;

    /// <summary>The layer drawn over the game's 2D pass, where pages normally are.</summary>
    public const int Over = 0;

    private readonly Dictionary<int, PageScopeBinding> _bindings = [];

    /// <summary>How many numbers are bound.</summary>
    public int Count => _bindings.Count;

    /// <summary>True when some scope is bound to the <see cref="Under"/> layer.</summary>
    public bool UsesUnder { get; private set; }

    /// <summary>Draws the content of scope <paramref name="scope"/> in <paramref name="layer"/> (<see cref="Under"/> or <see cref="Over"/>) instead of with the page.</summary>
    /// <param name="scope">The host-scope number.</param>
    /// <param name="layer">Where it is drawn.</param>
    /// <exception cref="ArgumentOutOfRangeException">The layer is neither <see cref="Under"/> nor <see cref="Over"/>.</exception>
    public void Layer(int scope, int layer)
    {
        if (layer is not (Under or Over))
        {
            throw new ArgumentOutOfRangeException(nameof(layer), "A page layer is PageScopes.Under (-1) or PageScopes.Over (0).");
        }

        _bindings[scope] = new PageScopeBinding(PageScopeKind.Layer, layer);
        UsesUnder |= layer == Under;
    }

    /// <summary>Draws the content of scope <paramref name="scope"/> apart and adds it to what is below it, for glows and light.</summary>
    /// <param name="scope">The host-scope number.</param>
    public void Additive(int scope) => _bindings[scope] = new PageScopeBinding(PageScopeKind.Additive);

    /// <summary>Removes a binding.</summary>
    /// <param name="scope">The host-scope number.</param>
    /// <returns>True when it was bound.</returns>
    public bool Clear(int scope) => _bindings.Remove(scope);

    internal PageScopeBinding Get(int scope) => _bindings.TryGetValue(scope, out var b) ? b : default;
}
