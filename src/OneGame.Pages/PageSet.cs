using System.Collections.ObjectModel;
using OneRender.Interaction;

namespace OneGame.Pages;

/// <summary>
/// The page screens of a game (<c>game.Pages</c>): which are shown, in what order, the clock they run on and the input routed to them. Pages are
/// view-only; nothing here is simulation state.
/// </summary>
public sealed class PageSet
{
    private readonly List<PageScreen> _all = [];
    private readonly List<PageScreen> _ordered = [];
    private double _nowMs;
    private bool _dirty;

    /// <summary>What the page host scopes mean.</summary>
    public PageScopes Scopes { get; } = new();

    /// <summary>The shown screens, back to front.</summary>
    public IReadOnlyList<PageScreen> Screens
    {
        get
        {
            if (_dirty)
            {
                _ordered.Clear();
                foreach (var s in _all)
                {
                    if (s.Visible)
                    {
                        _ordered.Add(s);
                    }
                }

                // A stable insertion sort by Order keeps equal orders in the order they were shown.
                for (var i = 1; i < _ordered.Count; i++)
                {
                    var s = _ordered[i];
                    var j = i - 1;
                    while (j >= 0 && _ordered[j].Order > s.Order)
                    {
                        _ordered[j + 1] = _ordered[j];
                        j--;
                    }

                    _ordered[j + 1] = s;
                }

                _dirty = false;
            }

            return _ordered;
        }
    }

    /// <summary>Every screen, shown or not.</summary>
    public IReadOnlyList<PageScreen> All => _all;

    /// <summary>The page clock in milliseconds of view time.</summary>
    public double NowMs => _nowMs;

    /// <summary>True when some screen is shown.</summary>
    public bool Any
    {
        get
        {
            foreach (var s in _all)
            {
                if (s.Visible)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Shows a screen under a name; a screen already shown under that name is replaced.</summary>
    /// <param name="name">The name.</param>
    /// <param name="screen">The screen.</param>
    public PageScreen Show(string name, PageScreen screen)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(screen);
        Remove(name);
        screen.Name = name;
        screen.Visible = true;
        _all.Add(screen);
        _dirty = true;
        return screen;
    }

    /// <summary>The screen shown under a name, or null.</summary>
    /// <param name="name">The name.</param>
    public PageScreen? Get(string name) => _all.Find(s => s.Name == name);

    /// <summary>Stops showing a screen and forgets it.</summary>
    /// <param name="name">The name.</param>
    /// <returns>True when there was one.</returns>
    public bool Remove(string name)
    {
        var i = _all.FindIndex(s => s.Name == name);
        if (i < 0)
        {
            return false;
        }

        _all.RemoveAt(i);
        _dirty = true;
        return true;
    }

    /// <summary>Hides or shows a screen without forgetting it.</summary>
    /// <param name="name">The name.</param>
    /// <param name="visible">Whether it is drawn and takes input.</param>
    public void SetVisible(string name, bool visible)
    {
        if (Get(name) is { } s)
        {
            s.Visible = visible;
            _dirty = true;
        }
    }

    /// <summary>Finds a named element in the shown screens, topmost first.</summary>
    /// <param name="name">The element's name.</param>
    public PageScreen? ScreenWith(string name)
    {
        var screens = Screens;
        for (var i = screens.Count - 1; i >= 0; i--)
        {
            if (screens[i].Has(name))
            {
                return screens[i];
            }
        }

        return null;
    }

    /// <summary>Moves the page clock forward and updates what animates.</summary>
    /// <param name="viewSeconds">View time that passed; the same value the tweens get.</param>
    public void Advance(double viewSeconds)
    {
        if (!(viewSeconds >= 0) || double.IsInfinity(viewSeconds))
        {
            return;
        }

        _nowMs += viewSeconds * 1000.0;
        var screens = Screens;
        for (var i = 0; i < screens.Count; i++)
        {
            screens[i].Advance(_nowMs);
        }
    }

    /// <summary>Creates or resizes the shown screens for a window of this size.</summary>
    /// <param name="width">Window width in device pixels.</param>
    /// <param name="height">Window height in device pixels.</param>
    /// <param name="scale">Device pixels per CSS pixel.</param>
    public void Prepare(int width, int height, float scale)
    {
        if (width < 1 || height < 1 || !(scale > 0))
        {
            return;
        }

        var screens = Screens;
        for (var i = 0; i < screens.Count; i++)
        {
            screens[i].Prepare(width, height, scale, _nowMs);
        }
    }

    /// <summary>Offers a pointer event to the shown screens, topmost first. True when one took it.</summary>
    /// <param name="action">What the pointer did.</param>
    /// <param name="x">Device pixels.</param>
    /// <param name="y">Device pixels.</param>
    /// <param name="button">0 primary, 1 secondary, 2 middle.</param>
    public bool SendPointer(PointerAction action, float x, float y, int button = 0)
    {
        var screens = Screens;
        for (var i = screens.Count - 1; i >= 0; i--)
        {
            if (screens[i].Pointer(action, x, y, button))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Offers a wheel step to the shown screens, topmost first.</summary>
    public bool Wheel(float x, float y, float dx, float dy)
    {
        var screens = Screens;
        for (var i = screens.Count - 1; i >= 0; i--)
        {
            if (screens[i].Wheel(x, y, dx, dy))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Offers a key to the shown screens, topmost first. True when one has a focused element.</summary>
    public bool Key(string key, string? code, bool down, KeyModifiers modifiers = KeyModifiers.None)
    {
        var screens = Screens;
        for (var i = screens.Count - 1; i >= 0; i--)
        {
            if (screens[i].Key(key, code, down, modifiers))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Offers typed text to the shown screens, topmost first.</summary>
    public bool Text(string text)
    {
        var screens = Screens;
        for (var i = screens.Count - 1; i >= 0; i--)
        {
            if (screens[i].Text(text))
            {
                return true;
            }
        }

        return false;
    }
}
