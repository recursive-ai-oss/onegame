namespace OneGame.UI;

internal sealed class Rule(Selector selector, Declaration[] declarations)
{
    public Selector Selector { get; } = selector;

    public Declaration[] Declarations { get; } = declarations;
}

/// <summary>An ordered list of style rules. Add sheets to a <see cref="UiTree"/>; later sheets win ties in specificity.</summary>
public sealed class StyleSheet
{
    private readonly List<Rule> _rules = [];

    internal IReadOnlyList<Rule> Rules => _rules;

    /// <summary>Adds a rule. <paramref name="selectors"/> is a comma-separated list of selectors built from a type (the element kind), <c>#id</c>, <c>.class</c>,
    /// <c>:hover :focus :active :disabled :root :first-child :last-child :nth-child(odd|even|N)</c>, with descendant (space) and child (<c>&gt;</c>) combinators.</summary>
    /// <exception cref="UiStyleException">A selector does not parse.</exception>
    public StyleSheet Rule(string selectors, Action<StyleBuilder> declare)
    {
        ArgumentNullException.ThrowIfNull(declare);
        var b = new StyleBuilder();
        declare(b);
        var decls = b.Declarations.ToArray();
        foreach (var sel in Selector.ParseList(selectors))
        {
            _rules.Add(new Rule(sel, decls));
        }

        return this;
    }

    /// <summary>Appends all rules of another sheet, so a game can extend a shared UI library's sheet.</summary>
    public StyleSheet Include(StyleSheet other)
    {
        ArgumentNullException.ThrowIfNull(other);
        _rules.AddRange(other._rules);
        return this;
    }
}
