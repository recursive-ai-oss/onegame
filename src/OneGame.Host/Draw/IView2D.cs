namespace OneGame.Host;

/// <summary>
/// Implemented by an entity that draws itself in 2D. The host calls <see cref="Draw"/> on every such entity once per drawn frame, in entity
/// order, and the 2D render stage shows what was drawn. Mark the implementation <c>[View]</c> so the analyzers check that it only reads state.
/// </summary>
public interface IView2D
{
    /// <summary>Draws this entity as it looks right now. Read state; never change it.</summary>
    /// <param name="d">Collects the sprites, shapes and text. <see cref="Draw2D.Tick"/>, <see cref="Draw2D.Alpha"/> and <see cref="Draw2D.Time"/> say which moment to show.</param>
    void Draw(Draw2D d);
}
