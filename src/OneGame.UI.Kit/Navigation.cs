namespace OneGame.UI.Kit;

// Shared key handling: arrow keys and d-pad steps along one axis, as one handler pair.
internal static class NavigationKeys
{
    // The step (-1 or +1) an event asks for along an axis, or 0 when it is not an arrow key or d-pad step on that axis.
    public static int Delta(in UiEvent e, bool horizontal)
    {
        if (e.Kind == UiEventKind.Navigate)
        {
            return e.Direction switch
            {
                NavDirection.Left when horizontal => -1,
                NavDirection.Right when horizontal => 1,
                NavDirection.Up when !horizontal => -1,
                NavDirection.Down when !horizontal => 1,
                _ => 0,
            };
        }

        if (e.Kind == UiEventKind.KeyDown)
        {
            return e.Key switch
            {
                "ArrowLeft" when horizontal => -1,
                "ArrowRight" when horizontal => 1,
                "ArrowUp" when !horizontal => -1,
                "ArrowDown" when !horizontal => 1,
                _ => 0,
            };
        }

        return 0;
    }

    // Calls step(delta) for arrow keys and d-pad steps along the axis; step returns true when it used the event.
    public static void OnStep(Element target, bool horizontal, Func<int, bool> step)
    {
        UiHandler h = (_, e) =>
        {
            var d = Delta(e, horizontal);
            return d != 0 && step(d);
        };
        target.On(UiEventKind.KeyDown, h);
        target.On(UiEventKind.Navigate, h);
    }
}

/// <summary>Focus helpers: a focus group that keeps arrow keys inside a set of controls, a default focus target, and a way to give a screen its first focus.</summary>
public static class FocusHelpers
{
    /// <summary>
    /// Makes <paramref name="container"/> a focus group: the arrow keys and d-pad along <paramref name="horizontal"/>'s axis move focus between the
    /// focusable descendants in document order. At the first or last member the event is not used, so focus leaves the group in that direction
    /// (unless <paramref name="wrap"/> is set). The other axis and Tab behave as everywhere else.
    /// </summary>
    /// <param name="container">The element holding the group's members.</param>
    /// <param name="horizontal">True for a row of controls (Left/Right), false for a column (Up/Down).</param>
    /// <param name="wrap">True to wrap from the last member to the first.</param>
    /// <returns><paramref name="container"/>, for chaining.</returns>
    public static Element FocusGroup(Element container, bool horizontal, bool wrap = false)
    {
        ArgumentNullException.ThrowIfNull(container);
        container.Class("kit-focus-group");
        NavigationKeys.OnStep(container, horizontal, delta =>
        {
            var tree = container.Tree;
            var focused = tree?.Focused;
            if (tree is null || focused is null || !container.Contains(focused))
            {
                return false;
            }

            var members = new List<Element>();
            Collect(container, members);
            var at = members.IndexOf(focused);
            if (at < 0)
            {
                return false;
            }

            var to = at + delta;
            if (wrap)
            {
                to = (to + members.Count) % members.Count;
            }
            else if (to < 0 || to >= members.Count)
            {
                return false;
            }

            return tree.Focus(members[to]);
        });
        return container;
    }

    /// <summary>Marks an element as the one <see cref="FocusDefault"/> focuses first on its screen.</summary>
    /// <returns><paramref name="element"/>, for chaining.</returns>
    public static Element AsDefaultFocus(this Element element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.Class(UiKit.DefaultFocusClass);
    }

    /// <summary>
    /// Focuses the first element below <paramref name="screen"/> marked with <see cref="AsDefaultFocus"/> that can take focus, or else the first focusable element.
    /// Call it when a screen opens, so a controller player is never left with nothing focused.
    /// </summary>
    /// <returns>The element that took focus, or null when the screen has nothing focusable.</returns>
    public static Element? FocusDefault(UiTree tree, Element screen)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(screen);
        var target = FindDefault(screen);
        return target is not null && tree.Focus(target) ? target : null;
    }

    // The element marked as default focus, else the first focusable one that is shown and enabled.
    internal static Element? FindDefault(Element screen)
    {
        var all = new List<Element>();
        Collect(screen, all);
        foreach (var e in all)
        {
            if (e.HasClass(UiKit.DefaultFocusClass))
            {
                return e;
            }
        }

        return all.Count > 0 ? all[0] : null;
    }

    private static void Collect(Element root, List<Element> into)
    {
        var stack = new Stack<Element>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var e = stack.Pop();
            if (e != root && (!e.IsVisible || !e.IsEnabled))
            {
                continue;
            }

            if (e.IsFocusable)
            {
                into.Add(e);
            }

            for (var i = e.ChildCount - 1; i >= 0; i--)
            {
                stack.Push(e.ChildAt(i));
            }
        }
    }
}
