namespace OneGame.UI;

/// <summary>Tells the UI how big an image or SVG is, so it can size and tile backgrounds (<c>background-size</c>, <c>background-repeat</c>) without knowing how art is stored.</summary>
public interface IArtSizes
{
    /// <summary>Gets the natural size of the art with this asset id: pixels for a raster image, the document's own size for an SVG. Both are in UI units at scale 1.</summary>
    /// <param name="id">The asset id used by <c>background-image</c>.</param>
    /// <param name="width">Natural width.</param>
    /// <param name="height">Natural height.</param>
    /// <returns>False when the id is unknown.</returns>
    bool TryGetNaturalSize(string id, out float width, out float height);
}
