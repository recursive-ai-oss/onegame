namespace OneGame.UI.Kit;

/// <summary>
/// A modal dialog (kind <c>Dialog</c>, role <c>dialog</c>): a full-screen scrim over everything with a panel in the middle. While it is open the pointer cannot reach what is behind it,
/// and focus, Tab and the d-pad stay inside it. Opening it focuses the element marked <see cref="FocusHelpers.AsDefaultFocus"/> (else the first button); closing it gives focus back to what had it.
/// Escape and the back button close it and call the cancel callback. Parts: <c>id/panel</c>, <c>id/title</c>, <c>id/buttons</c>.
/// Open and close it from game code with <see cref="Open"/> and <see cref="Close"/>, or follow a flag with <see cref="BindOpen"/>.
/// </summary>
public sealed class DialogView : Element
{
    private readonly Action? _onCancel;
    private Element? _previous;
    private Func<bool>? _isOpen;
    private bool _lastWanted;

    internal DialogView(string title, Element body, string? id, Action? onCancel)
        : base("Dialog")
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(body);
        _onCancel = onCancel;
        Id = id;
        Role = "dialog";
        Value = title;
        Class("kit-dialog");
        IsVisible = false;
        TrapsFocus = true;
        Style(s => s.Anchor(AnchorPoint.Fill, toScreen: true).ZIndex(100));
        Panel = UiKit.Part("Box", "kit-dialog-panel", id, "panel");
        Buttons = UiKit.Part("Box", "kit-dialog-buttons", id, "buttons");
        var heading = UiKit.Part("Label", "kit-dialog-title", id, "title");
        heading.Text = title;
        Panel.With(heading, body, Buttons);
        Add(Panel);
        // A press on the scrim or the panel's bare parts would drop focus out of the dialog; put it back so Escape and the d-pad keep working.
        On(UiEventKind.PointerDown, (t, _) =>
        {
            if (t.Tree is { } tree && (tree.Focused is null || !t.Contains(tree.Focused)))
            {
                tree.Focus(FocusHelpers.FindDefault(t));
            }

            return true;
        });
        On(UiEventKind.Cancel, (_, _) =>
        {
            Close();
            _onCancel?.Invoke();
            return true;
        });
    }

    /// <summary>The panel in the middle; add styling or extra content here.</summary>
    public Element Panel { get; }

    /// <summary>The row of buttons at the bottom of the panel.</summary>
    public Element Buttons { get; }

    /// <summary>True while the dialog is shown.</summary>
    public bool IsOpen => IsVisible;

    /// <summary>Shows the dialog and moves focus into it. Does nothing when it is already open.</summary>
    public void Open() => Show(open: true, deferFocus: false);

    /// <summary>Hides the dialog and gives focus back to the element that had it before it opened.</summary>
    public void Close() => Show(open: false, deferFocus: false);

    /// <summary>Makes the dialog follow a flag: open while <paramref name="isOpen"/> returns true. Focus moves at the end of the update that changes it.</summary>
    /// <returns>This dialog, for chaining.</returns>
    public DialogView BindOpen(Func<bool> isOpen)
    {
        _isOpen = isOpen ?? throw new ArgumentNullException(nameof(isOpen));
        OnUpdate(_ =>
        {
            var wanted = _isOpen();
            if (wanted != _lastWanted)
            {
                _lastWanted = wanted;
                Show(wanted, deferFocus: true);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a button to the bottom row. Clicking it closes the dialog and then calls <paramref name="onClick"/>.
    /// </summary>
    /// <param name="text">The label.</param>
    /// <param name="onClick">Called after the dialog closes; may be null.</param>
    /// <param name="kind">The look.</param>
    /// <param name="isDefault">True to make this the button focused when the dialog opens.</param>
    /// <param name="id">The id of the button, or null for <c>dialogId/buttons/n</c> when the dialog has an id.</param>
    /// <returns>The button.</returns>
    public Element AddButton(string text, Action? onClick, ButtonKind kind = ButtonKind.Normal, bool isDefault = false, string? id = null)
    {
        id ??= Id is null ? null : Id + "/buttons/" + UiKit.Invariant(Buttons.ChildCount);
        var b = UiKit.Button(text, () =>
        {
            Close();
            onClick?.Invoke();
        }, id, kind);
        if (isDefault)
        {
            b.AsDefaultFocus();
        }

        Buttons.Add(b);
        return b;
    }

    private void Show(bool open, bool deferFocus)
    {
        if (IsVisible == open)
        {
            return;
        }

        var tree = Tree;
        if (open)
        {
            _previous = tree?.Focused;
            IsVisible = true;
            if (tree is not null)
            {
                var target = FocusHelpers.FindDefault(this);
                if (deferFocus)
                {
                    tree.RequestFocus(target);
                }
                else
                {
                    tree.Focus(target);
                }
            }

            return;
        }

        IsVisible = false;
        if (tree is not null)
        {
            var back = _previous;
            _previous = null;
            if (deferFocus)
            {
                tree.RequestFocus(back);
            }
            else if (back is null || !tree.Focus(back))
            {
                tree.Focus(null);
            }
        }
    }
}

public static partial class UiKit
{
    /// <summary>A modal dialog with your content; add buttons with <see cref="DialogView.AddButton"/>. See <see cref="DialogView"/>.</summary>
    /// <param name="title">The heading.</param>
    /// <param name="body">The content between the heading and the buttons.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="onCancel">Called after Escape or the back button closes it.</param>
    /// <remarks>Add the dialog to the tree last (or give it its own screen layer) so it paints over everything; it is hidden until opened.</remarks>
    public static DialogView Dialog(string title, Element body, string? id = null, Action? onCancel = null) => new(title, body, id, onCancel);

    /// <summary>
    /// A confirm dialog with a message and two buttons (<c>id/buttons/0</c> cancel, <c>id/buttons/1</c> confirm). Both close the dialog; Escape cancels.
    /// </summary>
    /// <param name="title">The heading.</param>
    /// <param name="message">The question.</param>
    /// <param name="onConfirm">Called when the player confirms.</param>
    /// <param name="onCancel">Called when the player cancels or presses Escape; may be null.</param>
    /// <param name="confirmText">The confirm button's label.</param>
    /// <param name="cancelText">The cancel button's label.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="danger">Style the confirm button as destructive and focus Cancel first, so an accidental Enter does not confirm.</param>
    public static DialogView ConfirmDialog(string title, string message, Action onConfirm, Action? onCancel = null, string confirmText = "OK", string cancelText = "Cancel", string? id = null, bool danger = false)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(onConfirm);
        var dialog = Dialog(title, Ui.Label(message).Class("kit-dialog-message"), id, onCancel);
        dialog.AddButton(cancelText, onCancel, ButtonKind.Normal, isDefault: danger);
        dialog.AddButton(confirmText, onConfirm, danger ? ButtonKind.Danger : ButtonKind.Primary, isDefault: !danger);
        return dialog;
    }
}
