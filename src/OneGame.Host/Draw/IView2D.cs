namespace OneGame.Host;

/// <summary>
/// Implemented by an entity that draws itself in 2D. The host calls <see cref="Draw"/> on every such entity once per drawn frame, in entity
/// order, and the 2D render stage shows what was drawn. Mark the implementation <c>[View]</c> so the analyzers check that it only reads state.
/// Draw positions are in world units (+X right, +Y down, one unit is one pixel at camera zoom 1); convert synced <c>Fixed</c> values with
/// <c>ToSingle()</c> here, in the view, never in sim code.
/// </summary>
/// <example>
/// <code>
/// [Entity]
/// public partial class Ship : Entity, IView2D
/// {
///     [Synced] public partial Fixed X { get; private set; }
///     [Synced] public partial Fixed Y { get; private set; }
///
///     [View]
///     public void Draw(Draw2D d) =&gt;
///         d.Sprite("art/ship.png", new Vector2(X.ToSingle(), Y.ToSingle()), scale: 2);
/// }
/// </code>
/// </example>
public interface IView2D
{
    /// <summary>Draws this entity as it looks right now. Read state; never change it.</summary>
    /// <param name="d">Collects the sprites, shapes and text. <see cref="Draw2D.Tick"/>, <see cref="Draw2D.Alpha"/> and <see cref="Draw2D.Time"/> say which moment to show.</param>
    void Draw(Draw2D d);
}
