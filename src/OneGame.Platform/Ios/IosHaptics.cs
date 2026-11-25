namespace OneGame.Platform;

/// <summary>
/// Touch feedback on iOS through UIKit's feedback generators (UIKit reference: UIImpactFeedbackGenerator, UISelectionFeedbackGenerator,
/// UINotificationFeedbackGenerator). UIKit wants the main thread, and the game runs on its own, so the call hops over without waiting.
/// The system plays nothing when the user has haptics off or the device has no haptic engine (the simulator).
/// </summary>
internal static class IosHaptics
{
    // UIImpactFeedbackStyle: light 0, medium 1, heavy 2. UINotificationFeedbackType: success 0, warning 1, error 2.
    public static bool Play(HapticKind kind)
    {
        IosObjc.OnMain(() =>
        {
            switch (kind)
            {
                case HapticKind.Tick:
                    Fire("UISelectionFeedbackGenerator", init: null, "selectionChanged", 0);
                    break;
                case HapticKind.Click:
                    Fire("UIImpactFeedbackGenerator", "initWithStyle:", "impactOccurred", 1);
                    break;
                case HapticKind.Buzz:
                    Fire("UIImpactFeedbackGenerator", "initWithStyle:", "impactOccurred", 2);
                    break;
                case HapticKind.Success:
                    Fire("UINotificationFeedbackGenerator", init: null, "notificationOccurred:", 0);
                    break;
                case HapticKind.Warning:
                    Fire("UINotificationFeedbackGenerator", init: null, "notificationOccurred:", 1);
                    break;
                default:
                    Fire("UINotificationFeedbackGenerator", init: null, "notificationOccurred:", 2);
                    break;
            }
        });
        return true;
    }

    // `argument` goes to the init selector when it takes one, otherwise to the fire selector when that ends in a colon.
    private static void Fire(string className, string? init, string fire, nint argument)
    {
        var cls = IosObjc.Cls(className);
        if (cls == 0)
        {
            return;
        }

        var generator = init is null
            ? IosObjc.Obj(IosObjc.Alloc(cls), IosObjc.Sel("init"))
            : IosObjc.Obj(IosObjc.Alloc(cls), IosObjc.Sel(init), argument);
        if (generator == 0)
        {
            return;
        }

        if (fire.EndsWith(':'))
        {
            IosObjc.Void(generator, IosObjc.Sel(fire), argument);
        }
        else
        {
            IosObjc.Void(generator, IosObjc.Sel(fire));
        }

        IosObjc.objc_release(generator);
    }
}
