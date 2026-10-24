namespace OneGame.UI;

/// <summary>The <c>display</c> property. Every element is a flex container; there is no block or inline flow.</summary>
public enum DisplayMode
{
    /// <summary>A flex container (the default).</summary>
    Flex,
    /// <summary>Not rendered and takes no space.</summary>
    None,
}

/// <summary>The <c>position</c> property.</summary>
public enum PositionMode
{
    /// <summary>In flow; <c>left/top/right/bottom</c> shift the box after layout.</summary>
    Relative,
    /// <summary>Out of flow, placed against the padding box of its parent.</summary>
    Absolute,
    /// <summary>Out of flow, placed against the screen (the root's size).</summary>
    Fixed,
}

/// <summary>The <c>box-sizing</c> property. The default is border-box, unlike CSS.</summary>
public enum BoxSizing
{
    /// <summary><c>width</c> and <c>height</c> include padding and border.</summary>
    BorderBox,
    /// <summary><c>width</c> and <c>height</c> size the content box.</summary>
    ContentBox,
}

/// <summary>The <c>flex-direction</c> property.</summary>
public enum FlexDirection
{
    /// <summary>Left to right.</summary>
    Row,
    /// <summary>Right to left.</summary>
    RowReverse,
    /// <summary>Top to bottom.</summary>
    Column,
    /// <summary>Bottom to top.</summary>
    ColumnReverse,
}

/// <summary>The <c>flex-wrap</c> property.</summary>
public enum FlexWrap
{
    /// <summary>One line.</summary>
    NoWrap,
    /// <summary>Multiple lines, first line at the cross start.</summary>
    Wrap,
    /// <summary>Multiple lines, first line at the cross end.</summary>
    WrapReverse,
}

/// <summary>Values for <c>justify-content</c>, <c>align-items</c>, <c>align-self</c> and <c>align-content</c>. <c>flex-start</c> and <c>start</c> are the same here, as are <c>flex-end</c> and <c>end</c>.</summary>
public enum Align
{
    /// <summary>For <c>align-self</c>: use the parent's <c>align-items</c>. For <c>align-content</c>: <c>normal</c>, which stretches.</summary>
    Auto,
    /// <summary>Pack toward the start.</summary>
    Start,
    /// <summary>Pack toward the end.</summary>
    End,
    /// <summary>Center.</summary>
    Center,
    /// <summary>Fill the cross axis (items without a cross size).</summary>
    Stretch,
    /// <summary>Align on text baselines. Baselines are not computed yet; this behaves as <see cref="Start"/>.</summary>
    Baseline,
    /// <summary>First and last at the edges, equal space between.</summary>
    SpaceBetween,
    /// <summary>Equal space around each item (half at the edges).</summary>
    SpaceAround,
    /// <summary>Equal space between and at the edges.</summary>
    SpaceEvenly,
}

/// <summary>The <c>overflow</c> property.</summary>
public enum OverflowMode
{
    /// <summary>Content spills out unclipped.</summary>
    Visible,
    /// <summary>Content is clipped to the padding box.</summary>
    Hidden,
    /// <summary>Clipped and scrollable.</summary>
    Scroll,
    /// <summary>Clipped and scrollable (scroll bars are drawn by the component kit, not here).</summary>
    Auto,
}

/// <summary>The <c>visibility</c> property.</summary>
public enum VisibilityMode
{
    /// <summary>Drawn.</summary>
    Visible,
    /// <summary>Takes space but is not drawn and cannot be hit.</summary>
    Hidden,
}

/// <summary>The <c>pointer-events</c> property.</summary>
public enum PointerEvents
{
    /// <summary>The element can be the target of pointer input.</summary>
    Auto,
    /// <summary>Pointer input passes through the element (its children still receive it).</summary>
    None,
}

/// <summary>The <c>cursor</c> property: a hint for the platform layer.</summary>
public enum CursorShape
{
    /// <summary>The ordinary arrow.</summary>
    Default,
    /// <summary>A hand, over something clickable.</summary>
    Hand,
    /// <summary>A text insertion beam.</summary>
    Text,
    /// <summary>An open hand, over something draggable.</summary>
    Grab,
    /// <summary>Not allowed.</summary>
    NotAllowed,
}

/// <summary>The <c>font-style</c> property.</summary>
public enum FontStyle
{
    /// <summary>Upright.</summary>
    Normal,
    /// <summary>Italic or oblique.</summary>
    Italic,
}

/// <summary>The <c>text-align</c> property (left to right text only for now).</summary>
public enum TextAlignment
{
    /// <summary>Line start.</summary>
    Start,
    /// <summary>Line end.</summary>
    End,
    /// <summary>Centered.</summary>
    Center,
}

/// <summary>The <c>white-space</c> property.</summary>
public enum WhiteSpace
{
    /// <summary>Wrap at break opportunities when the line is full.</summary>
    Normal,
    /// <summary>Never wrap.</summary>
    NoWrap,
    /// <summary>Never wrap, and break at newline characters.</summary>
    Pre,
}
