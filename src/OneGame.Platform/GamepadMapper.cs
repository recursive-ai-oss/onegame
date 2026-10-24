namespace OneGame.Platform;

/// <summary>
/// Turns a device's raw buttons, axes and hats into the standard gamepad layout through a <see cref="GamepadMapping"/>,
/// applying dead zones, and reports only what changed.
/// </summary>
internal sealed class GamepadMapper
{
    private const int ButtonCount = 21;
    private const int AxisCount = 6;

    private readonly GamepadMapping _mapping;
    private readonly int _gamepadId;
    private readonly float _stickDeadZone;
    private readonly float _triggerDeadZone;
    private Action<PlatformEvent> _sink;
    private readonly bool[] _rawButtons;
    private readonly float[] _rawAxes;
    private readonly int[] _rawHats;
    private readonly bool[] _buttons = new bool[ButtonCount];
    private readonly float[] _axes = new float[AxisCount];
    private readonly float[] _acc = new float[AxisCount];

    public GamepadMapper(GamepadMapping mapping, int gamepadId, int buttons, int axes, int hats, float stickDeadZone, float triggerDeadZone, Action<PlatformEvent> sink)
    {
        _mapping = mapping;
        _gamepadId = gamepadId;
        _stickDeadZone = stickDeadZone;
        _triggerDeadZone = triggerDeadZone;
        _sink = sink;
        _rawButtons = new bool[buttons];
        _rawAxes = new float[axes];
        _rawHats = new int[hats];
    }

    /// <summary>Redirects events to a different receiver.</summary>
    public void SetSink(Action<PlatformEvent> sink) => _sink = sink;

    /// <summary>Records a raw button. Call <see cref="Flush"/> after a batch of changes.</summary>
    public void SetButton(int index, bool down)
    {
        if ((uint)index < (uint)_rawButtons.Length)
        {
            _rawButtons[index] = down;
        }
    }

    /// <summary>Records a raw axis value normalised to -1..1.</summary>
    public void SetAxis(int index, float value)
    {
        if ((uint)index < (uint)_rawAxes.Length)
        {
            _rawAxes[index] = Math.Clamp(value, -1f, 1f);
        }
    }

    /// <summary>Records a hat as a bit mask: up 1, right 2, down 4, left 8.</summary>
    public void SetHat(int index, int mask)
    {
        if ((uint)index < (uint)_rawHats.Length)
        {
            _rawHats[index] = mask;
        }
    }

    /// <summary>Recomputes the standard state and sends events for everything that changed.</summary>
    public void Flush()
    {
        Span<bool> buttons = stackalloc bool[ButtonCount];
        Array.Clear(_acc);
        foreach (var b in _mapping.Bindings)
        {
            if (b.TargetIsAxis)
            {
                _acc[(int)b.Axis] += AxisContribution(b);
            }
            else if (ButtonPressed(b))
            {
                buttons[(int)b.Button] = true;
            }
        }

        for (var i = 0; i < ButtonCount; i++)
        {
            if (buttons[i] != _buttons[i])
            {
                _buttons[i] = buttons[i];
                _sink(new PlatformEvent
                {
                    Kind = buttons[i] ? PlatformEventKind.GamepadButtonDown : PlatformEventKind.GamepadButtonUp,
                    GamepadId = _gamepadId,
                    GamepadButton = (GamepadButton)i,
                });
            }
        }

        ApplyStick(GamepadAxis.LeftX, GamepadAxis.LeftY);
        ApplyStick(GamepadAxis.RightX, GamepadAxis.RightY);
        ApplyTrigger(GamepadAxis.LeftTrigger);
        ApplyTrigger(GamepadAxis.RightTrigger);
    }

    private static float SourceAxis(float raw, in GamepadMapping.Binding b) => b.Invert ? -raw : raw;

    private bool ButtonPressed(in GamepadMapping.Binding b)
    {
        switch (b.Source)
        {
            case GamepadMapping.SourceKind.Button:
                return (uint)b.Index < (uint)_rawButtons.Length && _rawButtons[b.Index];
            case GamepadMapping.SourceKind.Hat:
                return (uint)b.Index < (uint)_rawHats.Length && (_rawHats[b.Index] & b.HatMask) != 0;
            default:
                if ((uint)b.Index >= (uint)_rawAxes.Length)
                {
                    return false;
                }

                var v = SourceAxis(_rawAxes[b.Index], b);
                return b.SourceHalf < 0 ? v < -0.5f : v > 0.5f;
        }
    }

    private float AxisContribution(in GamepadMapping.Binding b)
    {
        var isTrigger = b.Axis is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger;
        if (b.Source != GamepadMapping.SourceKind.Axis)
        {
            // A button or hat drives the axis to its end stop (a half-axis target picks the direction).
            if (!ButtonPressed(b))
            {
                return 0f;
            }

            return isTrigger || b.TargetHalf >= 0 ? 1f : -1f;
        }

        if ((uint)b.Index >= (uint)_rawAxes.Length)
        {
            return 0f;
        }

        var v = SourceAxis(_rawAxes[b.Index], b);
        if (b.SourceHalf > 0)
        {
            v = Math.Max(v, 0f);
        }
        else if (b.SourceHalf < 0)
        {
            v = Math.Max(-v, 0f);
        }
        else if (isTrigger)
        {
            // A full-range axis driving a trigger rests at -1 and reaches +1 when pressed.
            return (v + 1f) * 0.5f;
        }

        return b.TargetHalf < 0 ? -v : v;
    }

    private void ApplyStick(GamepadAxis xAxis, GamepadAxis yAxis)
    {
        float x = Math.Clamp(_acc[(int)xAxis], -1f, 1f), y = Math.Clamp(_acc[(int)yAxis], -1f, 1f);
        var length = MathF.Sqrt((x * x) + (y * y));
        if (length <= 0f || length < _stickDeadZone)
        {
            x = 0f;
            y = 0f;
        }
        else
        {
            // Radial dead zone: rescale so travel just past the zone starts from zero and full travel still reaches 1.
            var scaled = Math.Min((length - _stickDeadZone) / (1f - _stickDeadZone), 1f) / length;
            x *= scaled;
            y *= scaled;
        }

        Emit(xAxis, x);
        Emit(yAxis, y);
    }

    private void ApplyTrigger(GamepadAxis axis)
    {
        var v = Math.Clamp(_acc[(int)axis], 0f, 1f);
        v = v < _triggerDeadZone ? 0f : (v - _triggerDeadZone) / (1f - _triggerDeadZone);
        Emit(axis, v);
    }

    private void Emit(GamepadAxis axis, float value)
    {
        if (MathF.Abs(value - _axes[(int)axis]) < 0.0005f && !(value == 0f && _axes[(int)axis] != 0f))
        {
            return;
        }

        _axes[(int)axis] = value;
        _sink(new PlatformEvent { Kind = PlatformEventKind.GamepadAxisMoved, GamepadId = _gamepadId, GamepadAxis = axis, Value = value });
    }
}
