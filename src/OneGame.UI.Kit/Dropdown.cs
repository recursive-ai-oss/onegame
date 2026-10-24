namespace OneGame.UI.Kit;

public static partial class UiKit
{
    /// <summary>
    /// A drop-down select (kind <c>Dropdown</c>, role <c>combobox</c>): a button showing the chosen option that opens a list of options below it (above it when there is no room).
    /// Enter, Space, the confirm button or a click opens it and focuses the chosen option; Up/Down (arrow keys or d-pad) move through the options, which trap focus while open;
    /// Enter or a click chooses, Escape or the back button closes without choosing, and focus returns to the button either way.
    /// While open the element has the class <c>open</c> and its <see cref="Element.Value"/> is still the chosen label. Parts: <c>id/head</c> (the button), <c>id/popup</c>, <c>id/option/n</c>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="choices">The options, in order.</param>
    /// <param name="get">Reads the chosen value, every frame.</param>
    /// <param name="set">Called with the value the player chose.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="placeholder">Shown while the value matches no option.</param>
    public static Element Dropdown<T>(IReadOnlyList<Choice<T>> choices, Func<T> get, Action<T> set, string? id = null, string placeholder = "")
    {
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        var root = Make("Dropdown", "combobox", "kit-dropdown", id);
        var head = Make("Button", "button", "kit-dropdown-head", id is null ? null : id + "/head", focusable: true);
        var headLabel = Ui.Label(placeholder).Class("kit-dropdown-label");
        var arrow = Part("Box", "kit-dropdown-arrow", id, "arrow");
        head.With(headLabel, arrow);
        var popup = Part("Box", "kit-dropdown-popup", id, "popup");
        popup.Role = "listbox";
        popup.IsVisible = false;
        var options = new Element[choices.Count];
        for (var i = 0; i < options.Length; i++)
        {
            var index = i;
            var option = Make("Option", "option", "kit-option", id is null ? null : id + "/option/" + Invariant(i), focusable: true);
            option.Value = choices[i].Label;
            option.Add(Ui.Label(choices[i].Label).Class("kit-option-label"));
            option.OnClick(() =>
            {
                set(choices[index].Value);
                Close(refocus: true);
            });
            options[i] = option;
            popup.Add(option);
        }

        root.With(head, popup);
        var flipped = false;

        int Selected()
        {
            var current = get();
            for (var i = 0; i < options.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(current, choices[i].Value))
                {
                    return i;
                }
            }

            return -1;
        }

        void Open()
        {
            var tree = root.Tree;
            if (tree is null || popup.IsVisible || options.Length == 0)
            {
                return;
            }

            popup.IsVisible = true;
            popup.TrapsFocus = true;
            root.SetClass("open", true);
            root.Style(s => s.ZIndex(50));
            var sel = Selected();
            tree.Focus(options[sel < 0 ? 0 : sel]);
        }

        void Close(bool refocus)
        {
            if (!popup.IsVisible)
            {
                return;
            }

            popup.IsVisible = false;
            popup.TrapsFocus = false;
            root.SetClass("open", false);
            root.Style(s => s.ZIndex(0));
            if (refocus)
            {
                root.Tree?.Focus(head);
            }
        }

        head.OnClick(() =>
        {
            if (popup.IsVisible)
            {
                Close(refocus: true);
            }
            else
            {
                Open();
            }
        });
        popup.OnCancel(() => Close(refocus: true));
        root.OnUpdate(r =>
        {
            var sel = Selected();
            var text = sel < 0 ? placeholder : choices[sel].Label;
            if (!ReferenceEquals(headLabel.Text, text))
            {
                headLabel.Text = text;
            }

            r.Value = sel < 0 ? null : choices[sel].Label;
            for (var i = 0; i < options.Length; i++)
            {
                options[i].SetClass("selected", i == sel);
            }

            var tree = r.Tree;
            if (tree is null)
            {
                return;
            }

            // Focus moving out of the open list (a click elsewhere) closes it without taking focus back.
            if (popup.IsVisible && tree.Focused is { } f && !popup.Contains(f) && f != head)
            {
                Close(refocus: false);
            }

            if (popup.IsVisible)
            {
                // Open upward when the list would run off the bottom of the screen and there is more room above.
                var h = head.AbsoluteBounds;
                var ph = popup.Bounds.Height;
                var below = tree.Height - h.Bottom;
                var shouldFlip = ph > below && h.Y > below;
                if (shouldFlip != flipped)
                {
                    flipped = shouldFlip;
                    popup.SetClass("up", shouldFlip);
                }
            }
        });
        return root;
    }
}
