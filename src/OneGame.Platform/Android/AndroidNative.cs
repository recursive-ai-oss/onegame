using System.Runtime.InteropServices;

namespace OneGame.Platform;

// The NDK's public C API (android/native_activity.h, input.h, looper.h, native_window.h, configuration.h, log.h), called through
// source-generated imports. Struct layouts follow the headers documented at developer.android.com/ndk/reference.

/// <summary><c>ANativeActivity</c>: what the system hands to <c>ANativeActivity_onCreate</c> (native_activity.h).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ANativeActivity
{
    public ANativeActivityCallbacks* Callbacks;
    public nint Vm;
    public nint Env;
    public nint Clazz;
    public byte* InternalDataPath;
    public byte* ExternalDataPath;
    public int SdkVersion;
    public nint Instance;
    public nint AssetManager;
    public byte* ObbPath;
}

/// <summary><c>ANativeActivityCallbacks</c>: the entry points the activity calls on the process's main (UI) thread.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ANativeActivityCallbacks
{
    public delegate* unmanaged<nint, void> OnStart;
    public delegate* unmanaged<nint, void> OnResume;
    public delegate* unmanaged<nint, nuint*, nint> OnSaveInstanceState;
    public delegate* unmanaged<nint, void> OnPause;
    public delegate* unmanaged<nint, void> OnStop;
    public delegate* unmanaged<nint, void> OnDestroy;
    public delegate* unmanaged<nint, int, void> OnWindowFocusChanged;
    public delegate* unmanaged<nint, nint, void> OnNativeWindowCreated;
    public delegate* unmanaged<nint, nint, void> OnNativeWindowResized;
    public delegate* unmanaged<nint, nint, void> OnNativeWindowRedrawNeeded;
    public delegate* unmanaged<nint, nint, void> OnNativeWindowDestroyed;
    public delegate* unmanaged<nint, nint, void> OnInputQueueCreated;
    public delegate* unmanaged<nint, nint, void> OnInputQueueDestroyed;
    public delegate* unmanaged<nint, int*, void> OnContentRectChanged;
    public delegate* unmanaged<nint, void> OnConfigurationChanged;
    public delegate* unmanaged<nint, void> OnLowMemory;
}

internal static unsafe partial class AndroidNative
{
    private const string Android = "libandroid.so";
    private const string Log = "liblog.so";

    // looper.h
    public const int LooperPrepareAllowNonCallbacks = 1;
    public const int LooperPollWake = -1;
    public const int LooperPollCallback = -2;
    public const int LooperPollTimeout = -3;
    public const int LooperPollError = -4;
    public const int LooperIdentInput = 1;

    // input.h
    public const int EventTypeKey = 1;
    public const int EventTypeMotion = 2;
    public const int KeyActionDown = 0;
    public const int KeyActionUp = 1;
    public const int MotionActionMask = 0xff;
    public const int MotionActionPointerIndexMask = 0xff00;
    public const int MotionActionPointerIndexShift = 8;
    public const int MotionDown = 0;
    public const int MotionUp = 1;
    public const int MotionMove = 2;
    public const int MotionCancel = 3;
    public const int MotionPointerDown = 5;
    public const int MotionPointerUp = 6;
    public const int MotionHoverMove = 7;
    public const int MotionScroll = 8;
    public const int AxisX = 0;
    public const int AxisY = 1;
    public const int AxisVScroll = 9;
    public const int AxisHScroll = 10;
    public const int AxisZ = 11;
    public const int AxisRx = 12;
    public const int AxisRy = 13;
    public const int AxisRz = 14;
    public const int AxisHatX = 15;
    public const int AxisHatY = 16;
    public const int AxisLTrigger = 17;
    public const int AxisRTrigger = 18;
    public const int AxisGas = 22;
    public const int AxisBrake = 23;
    public const int SourceKeyboard = 0x101;
    public const int SourceGamepad = 0x401;
    public const int SourceTouchscreen = 0x1002;
    public const int SourceMouse = 0x2002;
    public const int SourceJoystick = 0x01000010;
    public const int ToolTypeMouse = 3;
    public const int ButtonPrimary = 1;
    public const int ButtonSecondary = 2;
    public const int ButtonTertiary = 4;

    // native_activity.h
    public const int ShowSoftInputForced = 2;

    // ---- looper ----

    [LibraryImport(Android)]
    public static partial nint ALooper_prepare(int options);

    [LibraryImport(Android)]
    public static partial nint ALooper_forThread();

    [LibraryImport(Android)]
    public static partial void ALooper_acquire(nint looper);

    [LibraryImport(Android)]
    public static partial int ALooper_pollOnce(int timeoutMillis, int* outFd, int* outEvents, nint* outData);

    [LibraryImport(Android)]
    public static partial void ALooper_wake(nint looper);

    // ---- input queue and events ----

    [LibraryImport(Android)]
    public static partial void AInputQueue_attachLooper(nint queue, nint looper, int ident, nint callback, nint data);

    [LibraryImport(Android)]
    public static partial void AInputQueue_detachLooper(nint queue);

    [LibraryImport(Android)]
    public static partial int AInputQueue_getEvent(nint queue, nint* outEvent);

    [LibraryImport(Android)]
    public static partial int AInputQueue_preDispatchEvent(nint queue, nint inputEvent);

    [LibraryImport(Android)]
    public static partial void AInputQueue_finishEvent(nint queue, nint inputEvent, int handled);

    [LibraryImport(Android)]
    public static partial int AInputEvent_getType(nint inputEvent);

    [LibraryImport(Android)]
    public static partial int AInputEvent_getDeviceId(nint inputEvent);

    [LibraryImport(Android)]
    public static partial int AInputEvent_getSource(nint inputEvent);

    [LibraryImport(Android)]
    public static partial int AKeyEvent_getAction(nint inputEvent);

    [LibraryImport(Android)]
    public static partial int AKeyEvent_getKeyCode(nint inputEvent);

    [LibraryImport(Android)]
    public static partial int AKeyEvent_getMetaState(nint inputEvent);

    [LibraryImport(Android)]
    public static partial int AKeyEvent_getRepeatCount(nint inputEvent);

    [LibraryImport(Android)]
    public static partial int AMotionEvent_getAction(nint inputEvent);

    [LibraryImport(Android)]
    public static partial nuint AMotionEvent_getPointerCount(nint inputEvent);

    [LibraryImport(Android)]
    public static partial int AMotionEvent_getPointerId(nint inputEvent, nuint index);

    [LibraryImport(Android)]
    public static partial float AMotionEvent_getX(nint inputEvent, nuint index);

    [LibraryImport(Android)]
    public static partial float AMotionEvent_getY(nint inputEvent, nuint index);

    [LibraryImport(Android)]
    public static partial float AMotionEvent_getPressure(nint inputEvent, nuint index);

    [LibraryImport(Android)]
    public static partial float AMotionEvent_getAxisValue(nint inputEvent, int axis, nuint index);

    [LibraryImport(Android)]
    public static partial int AMotionEvent_getToolType(nint inputEvent, nuint index);

    [LibraryImport(Android)]
    public static partial int AMotionEvent_getButtonState(nint inputEvent);

    // ---- window, configuration, activity ----

    [LibraryImport(Android)]
    public static partial int ANativeWindow_getWidth(nint window);

    [LibraryImport(Android)]
    public static partial int ANativeWindow_getHeight(nint window);

    [LibraryImport(Android)]
    public static partial void ANativeWindow_acquire(nint window);

    [LibraryImport(Android)]
    public static partial void ANativeWindow_release(nint window);

    [LibraryImport(Android)]
    public static partial nint AConfiguration_new();

    [LibraryImport(Android)]
    public static partial void AConfiguration_delete(nint config);

    [LibraryImport(Android)]
    public static partial void AConfiguration_fromAssetManager(nint config, nint assetManager);

    [LibraryImport(Android)]
    public static partial int AConfiguration_getDensity(nint config);

    [LibraryImport(Android)]
    public static partial void ANativeActivity_finish(nint activity);

    [LibraryImport(Android)]
    public static partial void ANativeActivity_showSoftInput(nint activity, uint flags);

    [LibraryImport(Android)]
    public static partial void ANativeActivity_hideSoftInput(nint activity, uint flags);

    // ---- log ----

    [LibraryImport(Log)]
    public static partial int __android_log_write(int priority, byte* tag, byte* text);

    public const int LogInfo = 4;
    public const int LogError = 6;

    /// <summary>Writes one line to logcat.</summary>
    public static void WriteLog(int priority, string tag, string text)
    {
        Span<byte> t = stackalloc byte[Math.Min(64, tag.Length * 3 + 1)];
        var tn = System.Text.Encoding.UTF8.GetBytes(tag.AsSpan(0, Math.Min(tag.Length, 20)), t);
        t[tn] = 0;
        var bytes = System.Text.Encoding.UTF8.GetBytes(text + "\0");
        fixed (byte* tp = t)
        fixed (byte* bp = bytes)
        {
            _ = __android_log_write(priority, tp, bp);
        }
    }
}
