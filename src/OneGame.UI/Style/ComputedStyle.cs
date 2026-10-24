namespace OneGame.UI;

/// <summary>The resolved value of every style property of one element, after the cascade, inheritance and theme tokens.</summary>
public sealed class ComputedStyle
{
    internal readonly StyleValue[] V = new StyleValue[(int)StyleProp.Count];

    internal ComputedStyle() => ResetToInitial();

    /// <summary>The CSS names of every property, in a fixed order.</summary>
    public static IReadOnlyList<string> PropertyNames { get; } = Array.ConvertAll(PropertyTable.All, p => p.Name);

    /// <summary>The value of a property in CSS spelling, or null when the name is unknown.</summary>
    /// <param name="cssName">A property name such as <c>flex-direction</c>.</param>
    public string? Get(string cssName) =>
        PropertyTable.TryGet(cssName, out var p) ? PropertyTable.Format(p, V[(int)p]) : null;

    internal Length L(StyleProp p) => V[(int)p].AsLength;

    internal float N(StyleProp p) => V[(int)p].F;

    internal Color C(StyleProp p) => V[(int)p].AsColor;

    internal int E(StyleProp p) => V[(int)p].AsEnum;

    internal string T(StyleProp p) => V[(int)p].Text ?? "";

    internal void ResetToInitial()
    {
        for (var i = 0; i < V.Length; i++)
        {
            V[i] = PropertyTable.All[i].Initial;
        }
    }

    internal void CopyFrom(ComputedStyle other) => Array.Copy(other.V, V, V.Length);
}
