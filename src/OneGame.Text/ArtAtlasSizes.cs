using OneGame.Atlas;
using OneGame.UI;

namespace OneGame.Text;

/// <summary>Lets the UI ask an <see cref="ArtAtlas"/> how big its art is: assign it to <see cref="UiTree.ArtSizes"/>.</summary>
/// <param name="atlas">The atlas the display list's image ids refer to.</param>
public sealed class ArtAtlasSizes(ArtAtlas atlas) : IArtSizes
{
    private readonly ArtAtlas _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));

    /// <inheritdoc/>
    public bool TryGetNaturalSize(string id, out float width, out float height)
    {
        if (_atlas.TryGetNaturalSize(id, out var w, out var h))
        {
            width = w;
            height = h;
            return true;
        }

        width = height = 0;
        return false;
    }
}
