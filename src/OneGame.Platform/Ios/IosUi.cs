using System.Runtime.InteropServices;

namespace OneGame.Platform;

/// <summary>
/// The UIKit side of the iOS backend: the app and scene delegates, the view controller and the game's view, as Objective-C classes
/// registered at start-up with managed functions as their methods. Everything here runs on the main thread and only queues events for
/// the game thread (see <see cref="IosAppHost"/>). Behaviour follows Apple's UIKit documentation: the scene lifecycle
/// (UIWindowSceneDelegate), touch handling (UIResponder), hardware keyboard presses (UIPress, UIKey) and safe area (UIView.safeAreaInsets).
/// </summary>
internal static unsafe class IosUi
{
    internal const string AppDelegateClass = "OneGameAppDelegate";
    private const string SceneDelegateClass = "OneGameSceneDelegate";

    private static readonly Dictionary<nint, int> TouchSlots = [];
    private static readonly HashSet<int> KeysDown = [];
    private static nint _viewClass;
    private static nint _textClass;
    private static nint _window;
    private static nint _view;
    private static nint _textView;
    private static nint _scene;
    private static IosEvent _posted;

    /// <summary>The game's view (main thread).</summary>
    internal static nint View => _view;

    /// <summary>The hidden view that takes text input (main thread).</summary>
    internal static nint TextView => _textView;

    /// <summary>The scene's window (main thread).</summary>
    internal static nint Window => _window;

    /// <summary>The window scene (main thread).</summary>
    internal static nint Scene => _scene;

    internal static void Register()
    {
        DefineClassesOnce();
    }

    private static bool _defined;

    private static void DefineClassesOnce()
    {
        if (_defined)
        {
            return;
        }

        _defined = true;
        IosObjc.DefineClass(AppDelegateClass, "UIResponder", ["UIApplicationDelegate"],
        [
            new("application:didFinishLaunchingWithOptions:", (nint)(delegate* unmanaged<nint, nint, nint, nint, byte>)&AppDidFinishLaunching, "B@:@@"),
            new("applicationDidReceiveMemoryWarning:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&AppMemoryWarning, "v@:@"),
        ]);

        IosObjc.DefineClass(SceneDelegateClass, "UIResponder", ["UIWindowSceneDelegate"],
        [
            new("scene:willConnectToSession:options:", (nint)(delegate* unmanaged<nint, nint, nint, nint, nint, void>)&SceneWillConnect, "v@:@@@"),
            new("sceneDidBecomeActive:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&SceneDidBecomeActive, "v@:@"),
            new("sceneWillResignActive:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&SceneWillResignActive, "v@:@"),
            new("sceneDidEnterBackground:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&SceneDidEnterBackground, "v@:@"),
            new("sceneWillEnterForeground:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&SceneWillEnterForeground, "v@:@"),
            new("window", (nint)(delegate* unmanaged<nint, nint, nint>)&SceneWindow, "@@:"),
            new("setWindow:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&SceneSetWindow, "v@:@"),
        ]);

        IosObjc.DefineClass("OneGameViewController", "UIViewController", [],
        [
            new("prefersStatusBarHidden", (nint)(delegate* unmanaged<nint, nint, byte>)&ReturnYes, "B@:"),
            new("prefersHomeIndicatorAutoHidden", (nint)(delegate* unmanaged<nint, nint, byte>)&ReturnYes, "B@:"),
            new("preferredScreenEdgesDeferringSystemGestures", (nint)(delegate* unmanaged<nint, nint, nint>)&AllEdges, "Q@:"),
        ]);

        _viewClass = IosObjc.DefineClass("OneGameView", "UIView", [],
        [
            new("touchesBegan:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&TouchesBegan, "v@:@@"),
            new("touchesMoved:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&TouchesMoved, "v@:@@"),
            new("touchesEnded:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&TouchesEnded, "v@:@@"),
            new("touchesCancelled:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&TouchesCancelled, "v@:@@"),
            new("layoutSubviews", (nint)(delegate* unmanaged<nint, nint, void>)&LayoutSubviews, "v@:"),
            new("safeAreaInsetsDidChange", (nint)(delegate* unmanaged<nint, nint, void>)&SafeAreaChanged, "v@:"),
            new("canBecomeFirstResponder", (nint)(delegate* unmanaged<nint, nint, byte>)&ReturnYes, "B@:"),
            new("pressesBegan:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&PressesBegan, "v@:@@"),
            new("pressesEnded:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&PressesEnded, "v@:@@"),
            new("pressesCancelled:withEvent:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&PressesEnded, "v@:@@"),
        ],
        [
            new("layerClass", (nint)(delegate* unmanaged<nint, nint, nint>)&LayerClass, "#@:"),
        ]);

        _textClass = IosText.Define();
    }

    // ---- delegates ----

    [UnmanagedCallersOnly]
    private static byte AppDidFinishLaunching(nint self, nint cmd, nint application, nint options) => 1;

    [UnmanagedCallersOnly]
    private static void AppMemoryWarning(nint self, nint cmd, nint application) =>
        IosAppHost.Current?.Post(new IosEvent { Kind = IosEventKind.LowMemory });

    [UnmanagedCallersOnly]
    private static byte ReturnYes(nint self, nint cmd) => 1;

    [UnmanagedCallersOnly]
    private static nint AllEdges(nint self, nint cmd) => 15;

    [UnmanagedCallersOnly]
    private static nint LayerClass(nint self, nint cmd) => IosObjc.Cls("CAMetalLayer");

    [UnmanagedCallersOnly]
    private static nint SceneWindow(nint self, nint cmd) => _window;

    [UnmanagedCallersOnly]
    private static void SceneSetWindow(nint self, nint cmd, nint window)
    {
    }

    [UnmanagedCallersOnly]
    private static void SceneWillConnect(nint self, nint cmd, nint scene, nint session, nint options)
    {
        try
        {
            if (_window != 0 || !IosObjc.Flag(scene, IosObjc.Sel("isKindOfClass:"), IosObjc.Cls("UIWindowScene")))
            {
                return;
            }

            _scene = IosObjc.objc_retain(scene);
            var window = IosObjc.Obj(IosObjc.Alloc(IosObjc.Cls("UIWindow")), IosObjc.Sel("initWithWindowScene:"), scene);
            var bounds = IosObjc.Ret<CGRect>(window, IosObjc.Sel("bounds"));
            var view = IosObjc.Obj(IosObjc.Alloc(_viewClass), IosObjc.Sel("initWithFrame:"), bounds);
            IosObjc.Void(view, IosObjc.Sel("setMultipleTouchEnabled:"), (byte)1);
            IosObjc.Void(view, IosObjc.Sel("setOpaque:"), (byte)1);
            IosObjc.Void(view, IosObjc.Sel("setBackgroundColor:"), IosObjc.Obj(IosObjc.Cls("UIColor"), IosObjc.Sel("blackColor")));
            var layer = IosObjc.Obj(view, IosObjc.Sel("layer"));
            IosObjc.Void(layer, IosObjc.Sel("setOpaque:"), (byte)1);

            var text = IosObjc.Obj(IosObjc.Alloc(_textClass), IosObjc.Sel("initWithFrame:"), new CGRect { Size = new CGSize { Width = 1, Height = 1 } });
            IosObjc.Void(text, IosObjc.Sel("setAlpha:"), 0.0);
            IosObjc.Void(text, IosObjc.Sel("setUserInteractionEnabled:"), (byte)0);
            IosObjc.Void(view, IosObjc.Sel("addSubview:"), text);

            var controller = IosObjc.Obj(IosObjc.Alloc(IosObjc.Cls("OneGameViewController")), IosObjc.Sel("init"));
            IosObjc.Void(controller, IosObjc.Sel("setView:"), view);
            IosObjc.Void(window, IosObjc.Sel("setRootViewController:"), controller);
            IosObjc.Void(window, IosObjc.Sel("makeKeyAndVisible"));

            _window = window;
            _view = view;
            _textView = text;
            // Hardware keyboard presses reach the first responder; the soft keyboard stays away because the game's view takes no text input.
            IosObjc.Flag(view, IosObjc.Sel("becomeFirstResponder"));

            var host = IosAppHost.Current!;
            host.Layer = layer;
            PostLayout(view);
            host.SceneConnected();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: scene setup failed: " + ex);
        }
    }

    [UnmanagedCallersOnly]
    private static void SceneDidBecomeActive(nint self, nint cmd, nint scene) =>
        IosAppHost.Current?.Post(new IosEvent { Kind = IosEventKind.Focus, Flag = true });

    [UnmanagedCallersOnly]
    private static void SceneWillResignActive(nint self, nint cmd, nint scene) =>
        IosAppHost.Current?.Post(new IosEvent { Kind = IosEventKind.Focus, Flag = false });

    [UnmanagedCallersOnly]
    private static void SceneDidEnterBackground(nint self, nint cmd, nint scene)
    {
        var host = IosAppHost.Current;
        if (host is null)
        {
            return;
        }

        host.Post(new IosEvent { Kind = IosEventKind.Paused });
        // The system stops the GPU for an app in the background, and work that is still queued then ends the process. Hold the callback
        // (which is when the app is allowed to finish up) until the game has seen the pause and stopped submitting frames.
        host.WaitForGamePump(1000);
    }

    [UnmanagedCallersOnly]
    private static void SceneWillEnterForeground(nint self, nint cmd, nint scene) =>
        IosAppHost.Current?.Post(new IosEvent { Kind = IosEventKind.Resumed });

    // ---- layout ----

    [UnmanagedCallersOnly]
    private static void LayoutSubviews(nint self, nint cmd)
    {
        try
        {
            IosObjc.SuperVoid(self, _viewClass, cmd);
            PostLayout(self);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: layout failed: " + ex);
        }
    }

    [UnmanagedCallersOnly]
    private static void SafeAreaChanged(nint self, nint cmd)
    {
        try
        {
            IosObjc.SuperVoid(self, _viewClass, cmd);
            PostLayout(self);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: safe area update failed: " + ex);
        }
    }

    internal static void PostLayout(nint view)
    {
        var bounds = IosObjc.Ret<CGRect>(view, IosObjc.Sel("bounds"));
        var scale = IosObjc.Dbl(view, IosObjc.Sel("contentScaleFactor"));
        var inset = IosObjc.Ret<UIEdgeInsets>(view, IosObjc.Sel("safeAreaInsets"));
        var e = new IosEvent
        {
            Kind = IosEventKind.Layout,
            Width = (int)Math.Round(bounds.Size.Width * scale),
            Height = (int)Math.Round(bounds.Size.Height * scale),
            Scale = (float)scale,
            Insets = new EdgeInsets((int)Math.Round(inset.Left * scale), (int)Math.Round(inset.Top * scale), (int)Math.Round(inset.Right * scale), (int)Math.Round(inset.Bottom * scale)),
        };
        if (e.Width <= 0 || e.Height <= 0 || (_posted.Kind == IosEventKind.Layout && _posted.Width == e.Width && _posted.Height == e.Height && _posted.Scale == e.Scale && _posted.Insets == e.Insets))
        {
            return;
        }

        _posted = e;
        IosAppHost.Current?.Post(e);
    }

    // ---- touches ----

    [UnmanagedCallersOnly]
    private static void TouchesBegan(nint self, nint cmd, nint touches, nint ev) => Touches(self, touches, IosEventKind.TouchDown);

    [UnmanagedCallersOnly]
    private static void TouchesMoved(nint self, nint cmd, nint touches, nint ev) => Touches(self, touches, IosEventKind.TouchMove);

    [UnmanagedCallersOnly]
    private static void TouchesEnded(nint self, nint cmd, nint touches, nint ev) => Touches(self, touches, IosEventKind.TouchUp);

    [UnmanagedCallersOnly]
    private static void TouchesCancelled(nint self, nint cmd, nint touches, nint ev) => Touches(self, touches, IosEventKind.TouchCancel);

    private static void Touches(nint view, nint touches, IosEventKind kind)
    {
        try
        {
            var host = IosAppHost.Current;
            if (host is null)
            {
                return;
            }

            var scale = IosObjc.Dbl(view, IosObjc.Sel("contentScaleFactor"));
            foreach (var touch in IosObjc.Items(touches))
            {
                if (!TouchSlots.TryGetValue(touch, out var id))
                {
                    if (kind != IosEventKind.TouchDown)
                    {
                        continue;
                    }

                    id = FreeSlot();
                    TouchSlots[touch] = id;
                }

                var p = IosObjc.Ret<CGPoint, nint>(touch, IosObjc.Sel("locationInView:"), view);
                var pressure = 1f;
                var max = IosObjc.Dbl(touch, IosObjc.Sel("maximumPossibleForce"));
                if (max > 0)
                {
                    pressure = (float)Math.Clamp(IosObjc.Dbl(touch, IosObjc.Sel("force")) / max, 0.0, 1.0);
                    pressure = Math.Max(pressure, 0.01f);
                }

                host.Post(new IosEvent
                {
                    Kind = kind,
                    Id = id,
                    X = (float)(p.X * scale),
                    Y = (float)(p.Y * scale),
                    Pressure = kind is IosEventKind.TouchUp or IosEventKind.TouchCancel ? 0f : pressure,
                });
                if (kind is IosEventKind.TouchUp or IosEventKind.TouchCancel)
                {
                    TouchSlots.Remove(touch);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: touch handling failed: " + ex);
        }
    }

    // The lowest finger number not in use: fingers are small integers, as everywhere else in the platform layer.
    private static int FreeSlot()
    {
        for (var i = 0; ; i++)
        {
            if (!TouchSlots.ContainsValue(i))
            {
                return i;
            }
        }
    }

    // ---- hardware keyboard ----

    [UnmanagedCallersOnly]
    private static void PressesBegan(nint self, nint cmd, nint presses, nint ev) => Presses(self, cmd, presses, ev, true, _viewClass);

    [UnmanagedCallersOnly]
    private static void PressesEnded(nint self, nint cmd, nint presses, nint ev) => Presses(self, cmd, presses, ev, false, _viewClass);

    /// <summary>
    /// Posts the key events of UIPresses. In the text view the typed characters arrive through the text input protocol instead
    /// (<paramref name="textMode"/>), so printable keys, Enter and Backspace are left to it.
    /// </summary>
    internal static bool Presses(nint self, nint cmd, nint presses, nint ev, bool down, nint cls, bool textMode = false)
    {
        var any = false;
        try
        {
            var host = IosAppHost.Current;
            foreach (var press in IosObjc.Items(presses))
            {
                var key = IosObjc.Obj(press, IosObjc.Sel("key"));
                if (key == 0 || host is null)
                {
                    continue;
                }

                var usage = (int)IosObjc.Int(key, IosObjc.Sel("keyCode"));
                var flags = (int)IosObjc.Int(key, IosObjc.Sel("modifierFlags"));
                var k = IosKeys.ToKey(usage);
                if (k == Key.Unknown)
                {
                    continue;
                }

                any = true;
                var mods = IosKeys.ToModifiers(flags);
                var repeat = down && !KeysDown.Add(usage);
                if (!down)
                {
                    KeysDown.Remove(usage);
                }

                if (textMode && (IosKeys.IsPrintable(usage) || usage is 40 or 42 or 88))
                {
                    continue;
                }

                host.Post(new IosEvent { Kind = down ? IosEventKind.KeyDown : IosEventKind.KeyUp, Key = k, Modifiers = mods, Repeat = repeat });
                if (!textMode && down && !repeat && IosKeys.IsPrintable(usage) && (mods & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0)
                {
                    var chars = IosObjc.ToManaged(IosObjc.Obj(key, IosObjc.Sel("characters")));
                    if (IsTypedText(chars))
                    {
                        host.Post(new IosEvent { Kind = IosEventKind.Text, Text = chars, Modifiers = mods });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: key handling failed: " + ex);
        }

        if (!any || textMode)
        {
            IosObjc.SuperVoid(self, cls, cmd, presses, ev);
        }

        return any;
    }

    private static bool IsTypedText(string text)
    {
        foreach (var c in text)
        {
            if (c < 0x20 || c == 0x7f)
            {
                return false;
            }
        }

        return text.Length > 0;
    }

    // ---- requests from the game thread ----

    /// <summary>Shows or hides the soft keyboard by moving first-responder status between the text view and the game's view.</summary>
    internal static void SetTextInput(bool active, int x, int y, int width, int height)
    {
        IosObjc.OnMain(() =>
        {
            if (_textView == 0)
            {
                return;
            }

            IosText.SetCaretArea(x, y, width, height);
            if (active)
            {
                IosObjc.Flag(_textView, IosObjc.Sel("becomeFirstResponder"));
                IosObjc.Void(_textView, IosObjc.Sel("reloadInputViews"));
            }
            else
            {
                IosObjc.Flag(_textView, IosObjc.Sel("resignFirstResponder"));
                IosObjc.Flag(_view, IosObjc.Sel("becomeFirstResponder"));
            }
        });
    }

    /// <summary>Asks the system to rotate the scene to the given orientations (UIInterfaceOrientationMask bits). Only orientations the app's Info.plist lists can be reached.</summary>
    internal static string RequestOrientation(int mask)
    {
        string result = "requested";
        IosObjc.OnMainWait(() =>
        {
            var cls = IosObjc.Cls("UIWindowSceneGeometryPreferencesIOS");
            if (cls == 0 || _scene == 0)
            {
                result = "unsupported";
                return;
            }

            var prefs = IosObjc.Autorelease(IosObjc.Obj(IosObjc.Alloc(cls), IosObjc.Sel("initWithInterfaceOrientations:"), (nint)mask));
            IosObjc.Void(_scene, IosObjc.Sel("requestGeometryUpdateWithPreferences:errorHandler:"), prefs, (nint)0);
            var controller = IosObjc.Obj(_window, IosObjc.Sel("rootViewController"));
            IosObjc.Void(controller, IosObjc.Sel("setNeedsUpdateOfSupportedInterfaceOrientations"));
        });
        return result;
    }

    /// <summary>The application state (0 active, 1 inactive, 2 background).</summary>
    internal static int ApplicationState()
    {
        var state = 0;
        IosObjc.OnMainWait(() =>
        {
            var app = IosObjc.Obj(IosObjc.Cls("UIApplication"), IosObjc.Sel("sharedApplication"));
            state = (int)IosObjc.Int(app, IosObjc.Sel("applicationState"));
        });
        return state;
    }

    /// <summary>The scene's interface orientation (UIInterfaceOrientation: 1 portrait, 2 upside down, 3 landscape right, 4 landscape left).</summary>
    internal static int Orientation()
    {
        var value = 0;
        IosObjc.OnMainWait(() =>
        {
            if (_scene != 0)
            {
                value = (int)IosObjc.Int(_scene, IosObjc.Sel("interfaceOrientation"));
            }
        });
        return value;
    }
}
