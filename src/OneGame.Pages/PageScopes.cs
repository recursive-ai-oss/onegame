using OneGame.Render2D;

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

    /// <summary>The scope's content is drawn apart and passed through a 2D material's pixel shader.</summary>
    Material,
}

/// <summary>A resolved host-scope binding.</summary>
/// <param name="Kind">What the scope does.</param>
/// <param name="Layer">The layer a <see cref="PageScopeKind.Layer"/> scope draws with.</param>
/// <param name="Material">The material of a <see cref="PageScopeKind.Material"/> scope.</param>
public readonly record struct PageScopeBinding(PageScopeKind Kind, int Layer = 0, Material? Material = null);

/// <summary>
/// Gives the numbers a page sets with <c>--host-scope</c> (or <c>StyleDeclaration.SetHostScope</c>) their meaning. An unbound number draws as if the
/// scope were absent, so a page can be written before the game decides what its scopes do.
/// </summary>
public sealed class PageScopes
{
    private readonly Dictionary<int, PageScopeBinding> _bindings = [];

    /// <summary>How many numbers are bound.</summary>
    public int Count => _bindings.Count;

    /// <summary>Draws the content of scope <paramref name="scope"/> when <paramref name="layer"/> is drawn instead of with layer 0.</summary>
    /// <param name="scope">The host-scope number.</param>
    /// <param name="layer">A layer number; <c>PageRenderer.AddPasses</c> draws one layer per call.</param>
    public void Layer(int scope, int layer) => _bindings[scope] = new PageScopeBinding(PageScopeKind.Layer, layer);

    /// <summary>Draws the content of scope <paramref name="scope"/> apart and adds it to what is below it, for glows and light.</summary>
    /// <param name="scope">The host-scope number.</param>
    public void Additive(int scope) => _bindings[scope] = new PageScopeBinding(PageScopeKind.Additive);

    /// <summary>Draws the content of scope <paramref name="scope"/> apart and passes it through the pixel shader of a 2D material.</summary>
    /// <param name="scope">The host-scope number.</param>
    /// <param name="material">A material from <see cref="Renderer2D.CreateMaterial"/> or <see cref="Renderer2D.AdditiveMaterial"/>.</param>
    public void Material(int scope, Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        _bindings[scope] = new PageScopeBinding(PageScopeKind.Material, 0, material);
    }

    /// <summary>Removes a binding.</summary>
    /// <param name="scope">The host-scope number.</param>
    /// <returns>True when it was bound.</returns>
    public bool Clear(int scope) => _bindings.Remove(scope);

    internal PageScopeBinding Get(int scope) => _bindings.TryGetValue(scope, out var b) ? b : default;
}
