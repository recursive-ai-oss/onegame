using System.Runtime.InteropServices;
using System.Threading;

namespace OneGame.Audio;

/// <summary>
/// The iOS AVAudioSession side of audio output (AVAudioSession class reference): choosing the category, activating the session, and
/// listening for interruption, route-change and media-services-reset notifications. One session exists per process, so this is static;
/// the open output unit attaches its handlers here.
/// </summary>
internal static unsafe class AudioSession
{
    private const string Framework = "/System/Library/Frameworks/AVFAudio.framework/AVFAudio";

    private static readonly Lock s_gate = new();
    private static nint s_lib;
    private static nint s_observer;
    private static Action<bool>? s_onInterruption;
    private static Action? s_onRoute;
    private static Action? s_onLost;

    private static nint Lib
    {
        get
        {
            if (s_lib == 0)
            {
                s_lib = NativeLibrary.Load(Framework);
            }

            return s_lib;
        }
    }

    private static nint Session => ObjC.Send(ObjC.GetClass("AVAudioSession"), ObjC.Sel("sharedInstance"));

    /// <summary>Sets the category and activates the session; fails (and says why) while a call or another app holds the audio hardware.</summary>
    public static bool TryActivate(AudioSessionCategory category, out string reason)
    {
        var pool = ObjC.PoolPush();
        try
        {
            var session = Session;
            var symbol = category switch
            {
                AudioSessionCategory.Playback => "AVAudioSessionCategoryPlayback",
                AudioSessionCategory.SoloAmbient => "AVAudioSessionCategorySoloAmbient",
                _ => "AVAudioSessionCategoryAmbient",
            };
            if (!ObjC.SendPtrUIntErr(session, ObjC.Sel("setCategory:withOptions:error:"), ObjC.ConstantString(Lib, symbol), 0, out var error))
            {
                reason = "AVAudioSession refused the category: " + Describe(error);
                return false;
            }

            // A short hardware buffer keeps taps and sound close together; 10 ms is well inside what a phone sustains.
            ObjC.SendDoubleErr(session, ObjC.Sel("setPreferredIOBufferDuration:error:"), 0.010, out _);
            if (!ObjC.SendBoolErr(session, ObjC.Sel("setActive:error:"), true, out error))
            {
                reason = "AVAudioSession could not become active: " + Describe(error);
                return false;
            }

            reason = string.Empty;
            return true;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            reason = "AVFAudio is not available: " + e.Message;
            return false;
        }
        finally
        {
            ObjC.PoolPop(pool);
        }
    }

    /// <summary>The hardware buffer duration the session settled on, in seconds.</summary>
    public static double IoBufferSeconds()
    {
        var pool = ObjC.PoolPush();
        try
        {
            var seconds = ObjC.SendDouble(Session, ObjC.Sel("IOBufferDuration"));
            return seconds is > 0.0005 and < 0.1 ? seconds : 0.01;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return 0.01;
        }
        finally
        {
            ObjC.PoolPop(pool);
        }
    }

    /// <summary>A line for the harness: category, hardware rate and the first output port of the current route.</summary>
    public static string Describe(AudioSessionCategory category)
    {
        var pool = ObjC.PoolPush();
        try
        {
            var session = Session;
            var rate = ObjC.SendDouble(session, ObjC.Sel("sampleRate"));
            var outputs = ObjC.Send(ObjC.Send(session, ObjC.Sel("currentRoute")), ObjC.Sel("outputs"));
            var first = ObjC.Send(outputs, ObjC.Sel("firstObject"));
            var port = first == 0 ? "none" : ObjC.ToManaged(ObjC.Send(first, ObjC.Sel("portType"))) + " \"" + ObjC.ToManaged(ObjC.Send(first, ObjC.Sel("portName"))) + "\"";
            return $"session={category}; hardwareRate={rate:0}; route={port}";
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return "session unavailable";
        }
        finally
        {
            ObjC.PoolPop(pool);
        }
    }

    private static string Describe(nint error) => error == 0 ? "unknown error" : ObjC.ToManaged(ObjC.Send(error, ObjC.Sel("localizedDescription"))) ?? "unknown error";

    /// <summary>Starts routing the session's notifications to these handlers.</summary>
    public static void Attach(Action<bool> onInterruption, Action onRouteChange, Action onMediaServicesReset)
    {
        lock (s_gate)
        {
            s_onInterruption = onInterruption;
            s_onRoute = onRouteChange;
            s_onLost = onMediaServicesReset;
            if (s_observer != 0)
            {
                return;
            }

            var pool = ObjC.PoolPush();
            try
            {
                var cls = ObjC.GetClass("OneGameAudioObserver");
                if (cls == 0)
                {
                    cls = ObjC.AllocateClassPair(ObjC.GetClass("NSObject"), "OneGameAudioObserver", 0);
                    ObjC.AddMethod(cls, ObjC.Sel("onInterruption:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&OnInterruptionNotification, "v@:@");
                    ObjC.AddMethod(cls, ObjC.Sel("onRouteChange:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&OnRouteChangeNotification, "v@:@");
                    ObjC.AddMethod(cls, ObjC.Sel("onReset:"), (nint)(delegate* unmanaged<nint, nint, nint, void>)&OnResetNotification, "v@:@");
                    ObjC.RegisterClassPair(cls);
                }

                var observer = ObjC.Send(ObjC.Send(cls, ObjC.Sel("alloc")), ObjC.Sel("init"));
                var center = Center();
                var add = ObjC.Sel("addObserver:selector:name:object:");
                ObjC.SendObserver(center, add, observer, ObjC.Sel("onInterruption:"), ObjC.ConstantString(Lib, "AVAudioSessionInterruptionNotification"), 0);
                ObjC.SendObserver(center, add, observer, ObjC.Sel("onRouteChange:"), ObjC.ConstantString(Lib, "AVAudioSessionRouteChangeNotification"), 0);
                ObjC.SendObserver(center, add, observer, ObjC.Sel("onReset:"), ObjC.ConstantString(Lib, "AVAudioSessionMediaServicesWereResetNotification"), 0);
                s_observer = observer;
            }
            finally
            {
                ObjC.PoolPop(pool);
            }
        }
    }

    /// <summary>Stops routing notifications (the observer stays registered and ignores them).</summary>
    public static void Detach()
    {
        lock (s_gate)
        {
            s_onInterruption = null;
            s_onRoute = null;
            s_onLost = null;
        }
    }

    private static nint Center() => ObjC.Send(ObjC.GetClass("NSNotificationCenter"), ObjC.Sel("defaultCenter"));

    /// <summary>Posts an interruption-began or -ended notification the way the system would.</summary>
    public static bool PostInterruption(bool began)
    {
        if (!OperatingSystem.IsIOS())
        {
            return false;
        }

        var pool = ObjC.PoolPush();
        try
        {
            var number = ObjC.SendUIntArg(ObjC.GetClass("NSNumber"), ObjC.Sel("numberWithUnsignedInteger:"), began ? 1u : 2u);
            var info = ObjC.SendDictionary(ObjC.GetClass("NSDictionary"), ObjC.Sel("dictionaryWithObject:forKey:"), number, ObjC.ConstantString(Lib, "AVAudioSessionInterruptionTypeKey"));
            ObjC.SendPost(Center(), ObjC.Sel("postNotificationName:object:userInfo:"), ObjC.ConstantString(Lib, "AVAudioSessionInterruptionNotification"), Session, info);
            return true;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
        finally
        {
            ObjC.PoolPop(pool);
        }
    }

    // Notification handlers. They run on whichever thread posted, so each opens its own autorelease pool and none may throw into Objective-C.
    [UnmanagedCallersOnly]
    private static void OnInterruptionNotification(nint self, nint command, nint notification)
    {
        var pool = ObjC.PoolPush();
        try
        {
            var info = ObjC.Send(notification, ObjC.Sel("userInfo"));
            var number = ObjC.SendPtr(info, ObjC.Sel("objectForKey:"), ObjC.ConstantString(Lib, "AVAudioSessionInterruptionTypeKey"));
            if (number != 0)
            {
                // AVAudioSessionInterruptionTypeBegan = 1, Ended = 2.
                var type = ObjC.SendUInt(number, ObjC.Sel("unsignedIntegerValue"));
                s_onInterruption?.Invoke(type == 1);
            }
        }
        catch (Exception)
        {
            // A failed handler must not unwind into the system's notification code.
        }
        finally
        {
            ObjC.PoolPop(pool);
        }
    }

    [UnmanagedCallersOnly]
    private static void OnRouteChangeNotification(nint self, nint command, nint notification)
    {
        try
        {
            s_onRoute?.Invoke();
        }
        catch (Exception)
        {
            // See above.
        }
    }

    [UnmanagedCallersOnly]
    private static void OnResetNotification(nint self, nint command, nint notification)
    {
        try
        {
            s_onLost?.Invoke();
        }
        catch (Exception)
        {
            // See above.
        }
    }
}
