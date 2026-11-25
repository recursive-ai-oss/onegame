namespace OneGame.Platform;

/// <summary>
/// Touch feedback on Android through <c>View.performHapticFeedback</c> on the activity's content view (Android reference:
/// <c>HapticFeedbackConstants</c>). It needs no permission, and the system drops the effect by itself when the user turned touch feedback off,
/// in which case the call returns false. Called on the game thread, whose JNI environment the activity host keeps.
/// </summary>
internal static class AndroidHaptics
{
    // HapticFeedbackConstants values.
    private const int LongPress = 0;
    private const int VirtualKey = 1;
    private const int ContextClick = 6;
    private const int ClockTick = 4;
    private const int Confirm = 16; // API 30
    private const int Reject = 17; // API 30

    public static bool Play(AndroidActivityHost host, HapticKind kind)
    {
        var jni = host.GameJni;
        // A JNI environment is only valid on the thread it was made for, so a call from any other thread is not delivered.
        if (jni is null || jni.ThreadId != Environment.CurrentManagedThreadId)
        {
            return false;
        }

        var (constant, fallback) = kind switch
        {
            HapticKind.Tick => (ClockTick, VirtualKey),
            HapticKind.Click => (VirtualKey, VirtualKey),
            HapticKind.Buzz => (LongPress, LongPress),
            HapticKind.Success => (Confirm, LongPress),
            HapticKind.Warning => (ContextClick, LongPress),
            _ => (Reject, LongPress),
        };

        nint activityClass = 0, content = 0, contentClass = 0, view = 0, viewClass = 0;
        try
        {
            var activity = host.JavaActivity;
            activityClass = jni.GetObjectClass(activity);
            var find = jni.GetMethodId(activityClass, "findViewById", "(I)Landroid/view/View;");
            content = find == 0 ? 0 : jni.CallObject(activity, find, 0x01020002); // android.R.id.content
            if (content == 0)
            {
                return false;
            }

            contentClass = jni.GetObjectClass(content);
            var childAt = jni.GetMethodId(contentClass, "getChildAt", "(I)Landroid/view/View;");
            view = childAt == 0 ? 0 : jni.CallObject(content, childAt, 0);
            if (view == 0)
            {
                return false;
            }

            viewClass = jni.GetObjectClass(view);
            var perform = jni.GetMethodId(viewClass, "performHapticFeedback", "(I)Z");
            if (perform == 0)
            {
                return false;
            }

            // The newer effects answer false on older systems; the older constant stands in for them.
            return jni.CallBool(view, perform, constant) || (fallback != constant && jni.CallBool(view, perform, fallback));
        }
        finally
        {
            jni.DeleteLocal(viewClass);
            jni.DeleteLocal(view);
            jni.DeleteLocal(contentClass);
            jni.DeleteLocal(content);
            jni.DeleteLocal(activityClass);
        }
    }
}
