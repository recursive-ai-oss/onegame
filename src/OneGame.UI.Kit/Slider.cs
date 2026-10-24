namespace OneGame.UI.Kit;

public static partial class UiKit
{
    /// <summary>
    /// A slider (kind <c>Slider</c>, role <c>slider</c>) for a number between <paramref name="min"/> and <paramref name="max"/>, snapped to multiples of <paramref name="step"/> from <paramref name="min"/>.
    /// Drag the thumb or click the track; with focus, Left/Right (arrow keys or d-pad) change the value by one step, PageUp/PageDown by ten, Home/End jump to the ends.
    /// Up and Down move focus away. Parts: <c>id/track</c>, <c>id/fill</c>, <c>id/thumb</c>, <c>id/value</c>. <see cref="Element.Value"/> is the formatted number.
    /// </summary>
    /// <param name="get">Reads the value, every frame.</param>
    /// <param name="set">Called with the new snapped value, only when it differs from the current one.</param>
    /// <param name="min">The smallest value.</param>
    /// <param name="max">The largest value; must be greater than <paramref name="min"/>.</param>
    /// <param name="step">The snapping step; must be positive.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="format">Formats the value for the label and <see cref="Element.Value"/>; the default prints whole numbers for whole steps and up to two decimals otherwise.</param>
    /// <param name="showValue">Show the number beside the track.</param>
    public static Element Slider(Func<float> get, Action<float> set, float min, float max, float step = 1, string? id = null, Func<float, string>? format = null, bool showValue = true)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        if (!(max > min))
        {
            throw new ArgumentOutOfRangeException(nameof(max), "A slider's maximum must be greater than its minimum.");
        }

        if (!(step > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(step), "A slider's step must be positive.");
        }

        var wholeSteps = step == MathF.Round(step) && min == MathF.Round(min);
        format ??= v => v.ToString(wholeSteps ? "0" : "0.##", System.Globalization.CultureInfo.InvariantCulture);
        var root = Make("Slider", "slider", "kit-slider", id, focusable: true);
        var track = Part("Box", "kit-slider-track", id, "track");
        var fill = Part("Box", "kit-slider-fill", id, "fill");
        var thumb = Part("Box", "kit-slider-thumb", id, "thumb");
        track.With(fill, thumb);
        root.Add(track);
        Element? valueLabel = null;
        if (showValue)
        {
            valueLabel = Part("Label", "kit-slider-value", id, "value");
            root.Add(valueLabel);
        }

        float Snap(float v)
        {
            var n = MathF.Round((v - min) / step);
            return Math.Clamp(min + (n * step), min, max);
        }

        void Change(float v)
        {
            var snapped = Snap(v);
            if (snapped != get())
            {
                set(snapped);
            }
        }

        void FromPointer(float x)
        {
            var b = track.AbsoluteBounds;
            if (b.Width <= 0)
            {
                return;
            }

            var t = Math.Clamp((x - b.X) / b.Width, 0f, 1f);
            Change(min + (t * (max - min)));
        }

        root.On(UiEventKind.PointerDown, (t, e) =>
        {
            if (e.Button != 0)
            {
                return false;
            }

            t.Tree?.CapturePointer(t);
            FromPointer(e.X);
            return true;
        });
        root.On(UiEventKind.PointerMove, (t, e) =>
        {
            if (t.Tree?.PointerCaptor != t)
            {
                return false;
            }

            FromPointer(e.X);
            return true;
        });
        NavigationKeys.OnStep(root, true, d =>
        {
            Change(get() + (d * step));
            return true;
        });
        root.On(UiEventKind.KeyDown, (_, e) =>
        {
            switch (e.Key)
            {
                case "PageUp": Change(get() + (10 * step)); return true;
                case "PageDown": Change(get() - (10 * step)); return true;
                case "Home": Change(min); return true;
                case "End": Change(max); return true;
                default: return false;
            }
        });

        var last = float.NaN;
        root.OnUpdate(e =>
        {
            var v = Math.Clamp(get(), min, max);
            if (v == last)
            {
                return;
            }

            last = v;
            var pct = (v - min) / (max - min) * 100f;
            fill.Width(Length.Percent(pct));
            thumb.Left(Length.Percent(pct));
            var text = format(v);
            e.Value = text;
            if (valueLabel is not null)
            {
                valueLabel.Text = text;
            }
        });
        return root;
    }

    /// <summary>A slider over whole numbers; see <see cref="Slider(Func{float}, Action{float}, float, float, float, string?, Func{float, string}?, bool)"/>.</summary>
    public static Element Slider(Func<int> get, Action<int> set, int min, int max, int step = 1, string? id = null, bool showValue = true)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        Func<float> read = () => get();
        Action<float> write = v => set((int)MathF.Round(v));
        return Slider(read, write, (float)min, (float)max, (float)step, id, null, showValue);
    }

    /// <summary>
    /// A number stepper (kind <c>Stepper</c>, role <c>spinbutton</c>): minus and plus buttons around the value. With focus, Left/Right change the value by one step,
    /// PageUp/PageDown by ten, Home/End jump to the ends; holding a key repeats. Parts: <c>id/minus</c>, <c>id/value</c>, <c>id/plus</c>.
    /// </summary>
    /// <param name="get">Reads the value, every frame.</param>
    /// <param name="set">Called with the new value, already clamped to the range.</param>
    /// <param name="min">The smallest value.</param>
    /// <param name="max">The largest value; must be at least <paramref name="min"/>.</param>
    /// <param name="step">The change per press; must be positive.</param>
    /// <param name="id">The id, or null.</param>
    /// <param name="format">Formats the number; the default is the plain integer.</param>
    public static Element NumberStepper(Func<int> get, Action<int> set, int min, int max, int step = 1, string? id = null, Func<int, string>? format = null)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        if (max < min)
        {
            throw new ArgumentOutOfRangeException(nameof(max), "A stepper's maximum must not be below its minimum.");
        }

        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step), "A stepper's step must be positive.");
        }

        format ??= v => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var root = Make("Stepper", "spinbutton", "kit-stepper", id, focusable: true);
        var minus = Part("Button", "kit-stepper-button", id, "minus");
        minus.Role = "button";
        minus.Add(Ui.Label("-"));
        var plus = Part("Button", "kit-stepper-button", id, "plus");
        plus.Role = "button";
        plus.Add(Ui.Label("+"));
        var value = Part("Label", "kit-stepper-value", id, "value");
        root.With(minus, value, plus);

        void Change(long v)
        {
            var clamped = (int)Math.Clamp(v, min, max);
            if (clamped != get())
            {
                set(clamped);
            }
        }

        minus.OnClick(() => Change((long)get() - step));
        plus.OnClick(() => Change((long)get() + step));
        NavigationKeys.OnStep(root, true, d =>
        {
            Change((long)get() + ((long)d * step));
            return true;
        });
        root.On(UiEventKind.KeyDown, (_, e) =>
        {
            switch (e.Key)
            {
                case "PageUp": Change((long)get() + (10L * step)); return true;
                case "PageDown": Change((long)get() - (10L * step)); return true;
                case "Home": Change(min); return true;
                case "End": Change(max); return true;
                default: return false;
            }
        });

        var last = int.MinValue;
        var started = false;
        root.OnUpdate(e =>
        {
            var v = Math.Clamp(get(), min, max);
            if (started && v == last)
            {
                return;
            }

            started = true;
            last = v;
            var text = format(v);
            value.Text = text;
            e.Value = text;
            minus.IsEnabled = v > min;
            plus.IsEnabled = v < max;
        });
        return root;
    }
}
