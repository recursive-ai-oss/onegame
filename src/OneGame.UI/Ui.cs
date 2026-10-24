namespace OneGame.UI;

/// <summary>Factory methods that read like a layout: <c>Ui.Column(Ui.Label("Hello"), Ui.Button("OK", Accept))</c>.</summary>
public static class Ui
{
    /// <summary>A plain container (kind <c>Box</c>) with children.</summary>
    public static Element Box(params Element[] children) => new Element("Box").With(children);

    /// <summary>A container that lays children out left to right.</summary>
    public static Element Row(params Element[] children) => new Element("Box").Row().With(children);

    /// <summary>A container that lays children out top to bottom.</summary>
    public static Element Column(params Element[] children) => new Element("Box").Column().With(children);

    /// <summary>A text element (kind <c>Label</c>).</summary>
    public static Element Label(string text) => new("Label") { Text = text };

    /// <summary>A text element whose text follows a lambda.</summary>
    public static Element Label(Func<string> text) => new Element("Label").BindText(text);

    /// <summary>A focusable container that runs <paramref name="onClick"/> when clicked or activated (kind <c>Button</c>), containing a label.</summary>
    public static Element Button(string text, Action onClick) =>
        new Element("Button").Focusable().Row().Align(Align.Center).Justify(Align.Center).With(Label(text)).OnClick(onClick);

    /// <summary>An element that shows the image or SVG with this asset id (kind <c>Image</c>). <paramref name="naturalWidth"/> and <paramref name="naturalHeight"/> size it when no width or height is set.</summary>
    public static Element Image(string assetId, float naturalWidth = 0, float naturalHeight = 0)
    {
        var e = new Element("Image") { IntrinsicSize = new System.Numerics.Vector2(naturalWidth, naturalHeight) };
        e.Style(s => s.BackgroundImage(assetId));
        return e;
    }

    /// <summary>An empty element that grows to fill free space in its parent.</summary>
    public static Element Spacer() => new Element("Spacer").Grow();

    /// <summary>A container with children chosen by a list that is re-read every frame; see <see cref="Element.BindChildren{T}"/>.</summary>
    public static Element Each<T>(Func<IReadOnlyList<T>> items, Func<T, Element> build) => new Element("Box").BindChildren(items, build);
}
