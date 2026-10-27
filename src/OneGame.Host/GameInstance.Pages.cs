using OneGame.Pages;
using OneGame.Platform;
using OneRender.Interaction;

namespace OneGame.Host;

public sealed partial class GameInstance
{
    /// <summary>
    /// OneRender pages shown as screens (builder trees or HTML and CSS), drawn over the 2D pass and the <c>OneGame.UI</c> tree. They run on the view clock
    /// the tweens use and see input before the UI does. Pages are view-only: they read nothing from the simulation and change no hash.
    /// </summary>
    public PageSet Pages { get; } = new();

    private void AdvancePages(double viewSeconds)
    {
        if (Pages.Any)
        {
            Pages.Prepare(Window.Width, Window.Height, UiScale);
            Pages.Advance(viewSeconds);
        }
    }

    // True when a shown page took the event.
    private bool RoutePages(in PlatformEvent e)
    {
        if (!Pages.Any)
        {
            return false;
        }

        Pages.Prepare(Window.Width, Window.Height, UiScale);
        switch (e.Kind)
        {
            case PlatformEventKind.PointerMoved:
                return Pages.SendPointer(PointerAction.Move, e.X, e.Y);
            case PlatformEventKind.PointerButtonDown:
                return Pages.SendPointer(PointerAction.Down, e.X, e.Y, DomButton(e.Button));
            case PlatformEventKind.PointerButtonUp:
                return Pages.SendPointer(PointerAction.Up, e.X, e.Y, DomButton(e.Button));
            case PlatformEventKind.PointerWheel:
                return Pages.Wheel(e.X, e.Y, -e.DeltaX * 48f, -e.DeltaY * 48f);
            case PlatformEventKind.KeyDown or PlatformEventKind.KeyUp:
                var shift = (e.Modifiers & OneGame.Platform.KeyModifiers.Shift) != 0;
                var mods = OneRender.Interaction.KeyModifiers.None;
                if (shift)
                {
                    mods |= OneRender.Interaction.KeyModifiers.Shift;
                }

                if ((e.Modifiers & OneGame.Platform.KeyModifiers.Control) != 0)
                {
                    mods |= OneRender.Interaction.KeyModifiers.Control;
                }

                if ((e.Modifiers & OneGame.Platform.KeyModifiers.Alt) != 0)
                {
                    mods |= OneRender.Interaction.KeyModifiers.Alt;
                }

                return Pages.Key(KeyNames.ToUiKey(e.Key, shift), null, e.Kind == PlatformEventKind.KeyDown, mods);
            case PlatformEventKind.TextInput when !string.IsNullOrEmpty(e.Text):
                return Pages.Text(e.Text);
            default:
                return false;
        }
    }

    private static int DomButton(MouseButton b) => b switch
    {
        MouseButton.Left => 0,
        MouseButton.Middle => 1,
        MouseButton.Right => 2,
        _ => 3,
    };
}
