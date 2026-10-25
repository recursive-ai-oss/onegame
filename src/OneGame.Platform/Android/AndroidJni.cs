namespace OneGame.Platform;

/// <summary>
/// The few Java calls the native activity cannot avoid, made through the JNI function table (JNI specification, "JNI Functions": the
/// table index of each function is part of the published interface). Every call checks for a pending Java exception and clears it.
/// </summary>
internal sealed unsafe class AndroidJni
{
    // Indexes into JNINativeInterface (JNI specification, "Table of JNI Functions").
    private const int GetVersionIndex = 4;
    private const int FindClassIndex = 6;
    private const int ExceptionClearIndex = 17;
    private const int DeleteLocalRefIndex = 23;
    private const int GetObjectClassIndex = 31;
    private const int GetMethodIdIndex = 33;
    private const int CallObjectMethodAIndex = 36;
    private const int CallBooleanMethodAIndex = 39;
    private const int CallIntMethodAIndex = 51;
    private const int CallVoidMethodAIndex = 63;
    private const int GetStaticMethodIdIndex = 113;
    private const int CallStaticObjectMethodAIndex = 116;
    private const int NewStringUtfIndex = 167;
    private const int GetStringUtfCharsIndex = 169;
    private const int ReleaseStringUtfCharsIndex = 170;
    private const int ExceptionCheckIndex = 228;

    // Indexes into JNIInvokeInterface.
    private const int AttachCurrentThreadIndex = 4;
    private const int DetachCurrentThreadIndex = 5;

    private readonly nint _vm;
    private readonly nint _env;

    private AndroidJni(nint vm, nint env)
    {
        _vm = vm;
        _env = env;
    }

    /// <summary>The environment of the thread that received the activity's callback (the main thread).</summary>
    public static AndroidJni ForMainThread(nint vm, nint env) => new(vm, env);

    /// <summary>Attaches the calling thread to the Java VM and returns its environment; null if the VM refuses.</summary>
    public static AndroidJni? Attach(nint vm)
    {
        nint env = 0;
        var table = *(nint**)vm;
        var attach = (delegate* unmanaged<nint, nint*, nint, int>)table[AttachCurrentThreadIndex];
        return attach(vm, &env, 0) == 0 ? new AndroidJni(vm, env) : null;
    }

    /// <summary>Detaches the calling thread from the Java VM.</summary>
    public void Detach()
    {
        var table = *(nint**)_vm;
        ((delegate* unmanaged<nint, int>)table[DetachCurrentThreadIndex])(_vm);
    }

    /// <summary>The JNI version the VM reports; 0x00010006 or later on every supported Android.</summary>
    public int Version => ((delegate* unmanaged<nint, int>)Table[GetVersionIndex])(_env);

    private nint* Table => *(nint**)_env;

    public nint FindClass(string name)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* p = bytes)
        {
            return Checked(((delegate* unmanaged<nint, byte*, nint>)Table[FindClassIndex])(_env, p));
        }
    }

    public nint GetObjectClass(nint obj) => Checked(((delegate* unmanaged<nint, nint, nint>)Table[GetObjectClassIndex])(_env, obj));

    public nint GetMethodId(nint cls, string name, string signature) => MethodId(GetMethodIdIndex, cls, name, signature);

    public nint GetStaticMethodId(nint cls, string name, string signature) => MethodId(GetStaticMethodIdIndex, cls, name, signature);

    /// <summary>Calls an instance method returning an object. Arguments are jvalues: an int or a reference widened to 64 bits.</summary>
    public nint CallObject(nint obj, nint method, params ReadOnlySpan<long> args)
    {
        fixed (long* a = args)
        {
            return Checked(((delegate* unmanaged<nint, nint, nint, long*, nint>)Table[CallObjectMethodAIndex])(_env, obj, method, a));
        }
    }

    /// <summary>Calls an instance method returning an int.</summary>
    public int CallInt(nint obj, nint method, params ReadOnlySpan<long> args)
    {
        fixed (long* a = args)
        {
            var r = ((delegate* unmanaged<nint, nint, nint, long*, int>)Table[CallIntMethodAIndex])(_env, obj, method, a);
            Clear();
            return r;
        }
    }

    /// <summary>Calls an instance method returning nothing.</summary>
    public void CallVoid(nint obj, nint method, params ReadOnlySpan<long> args)
    {
        fixed (long* a = args)
        {
            ((delegate* unmanaged<nint, nint, nint, long*, void>)Table[CallVoidMethodAIndex])(_env, obj, method, a);
            Clear();
        }
    }

    /// <summary>Calls an instance method returning a boolean.</summary>
    public bool CallBool(nint obj, nint method, params ReadOnlySpan<long> args)
    {
        fixed (long* a = args)
        {
            var r = ((delegate* unmanaged<nint, nint, nint, long*, byte>)Table[CallBooleanMethodAIndex])(_env, obj, method, a);
            Clear();
            return r != 0;
        }
    }

    /// <summary>Calls a static method returning an object.</summary>
    public nint CallStaticObject(nint cls, nint method, params ReadOnlySpan<long> args)
    {
        fixed (long* a = args)
        {
            return Checked(((delegate* unmanaged<nint, nint, nint, long*, nint>)Table[CallStaticObjectMethodAIndex])(_env, cls, method, a));
        }
    }

    /// <summary>Makes a Java string; pass the handle to a call as a long.</summary>
    public nint NewString(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text + "\0");
        fixed (byte* p = bytes)
        {
            return Checked(((delegate* unmanaged<nint, byte*, nint>)Table[NewStringUtfIndex])(_env, p));
        }
    }

    /// <summary>Reads a Java string. Null stays null.</summary>
    public string? GetString(nint jstring)
    {
        if (jstring == 0)
        {
            return null;
        }

        var chars = ((delegate* unmanaged<nint, nint, byte*, byte*>)Table[GetStringUtfCharsIndex])(_env, jstring, null);
        if (chars == null)
        {
            Clear();
            return null;
        }

        var length = 0;
        while (chars[length] != 0)
        {
            length++;
        }

        var text = System.Text.Encoding.UTF8.GetString(chars, length);
        ((delegate* unmanaged<nint, nint, byte*, void>)Table[ReleaseStringUtfCharsIndex])(_env, jstring, chars);
        return text;
    }

    public void DeleteLocal(nint reference)
    {
        if (reference != 0)
        {
            ((delegate* unmanaged<nint, nint, void>)Table[DeleteLocalRefIndex])(_env, reference);
        }
    }

    /// <summary>Reads the string extra <paramref name="key"/> of the activity's intent; null when absent.</summary>
    public string? GetIntentStringExtra(nint activity, string key)
    {
        var activityClass = GetObjectClass(activity);
        var getIntent = GetMethodId(activityClass, "getIntent", "()Landroid/content/Intent;");
        var intent = getIntent == 0 ? 0 : CallObject(activity, getIntent);
        if (intent == 0)
        {
            DeleteLocal(activityClass);
            return null;
        }

        var intentClass = GetObjectClass(intent);
        var getExtra = GetMethodId(intentClass, "getStringExtra", "(Ljava/lang/String;)Ljava/lang/String;");
        var jkey = NewString(key);
        var value = getExtra == 0 ? 0 : CallObject(intent, getExtra, jkey);
        var text = GetString(value);
        DeleteLocal(value);
        DeleteLocal(jkey);
        DeleteLocal(intentClass);
        DeleteLocal(intent);
        DeleteLocal(activityClass);
        return text;
    }

    /// <summary>
    /// Lets the activity's content view take focus. The system keyboard only attaches to a view that has focus (it logs "view is not served"
    /// and ignores the request otherwise), and the platform's native content view is not focusable on its own. Call on the main thread once
    /// the window exists. Returns whether the view ended up focused.
    /// </summary>
    public bool MakeContentFocusable(nint activity)
    {
        nint activityClass = 0, content = 0, contentClass = 0, view = 0, viewClass = 0;
        try
        {
            activityClass = GetObjectClass(activity);
            var find = GetMethodId(activityClass, "findViewById", "(I)Landroid/view/View;");
            content = find == 0 ? 0 : CallObject(activity, find, 0x01020002); // android.R.id.content
            if (content == 0)
            {
                return false;
            }

            contentClass = GetObjectClass(content);
            var childAt = GetMethodId(contentClass, "getChildAt", "(I)Landroid/view/View;");
            view = childAt == 0 ? 0 : CallObject(content, childAt, 0);
            if (view == 0)
            {
                return false;
            }

            viewClass = GetObjectClass(view);
            var setFocusable = GetMethodId(viewClass, "setFocusable", "(Z)V");
            var setTouchFocusable = GetMethodId(viewClass, "setFocusableInTouchMode", "(Z)V");
            var request = GetMethodId(viewClass, "requestFocus", "()Z");
            if (setFocusable == 0 || setTouchFocusable == 0 || request == 0)
            {
                return false;
            }

            CallVoid(view, setFocusable, 1);
            CallVoid(view, setTouchFocusable, 1);
            return CallBool(view, request);
        }
        finally
        {
            DeleteLocal(viewClass);
            DeleteLocal(view);
            DeleteLocal(contentClass);
            DeleteLocal(content);
            DeleteLocal(activityClass);
        }
    }

    /// <summary>
    /// The distances the window must keep from each screen edge: system bars, the display cut-out and the gesture area
    /// (<c>View.getRootWindowInsets().getSystemWindowInset*()</c>). All zero when the window is not attached yet.
    /// </summary>
    public EdgeInsets GetSystemInsets(nint activity)
    {
        nint activityClass = 0, window = 0, decor = 0, insets = 0, windowClass = 0, viewClass = 0, insetsClass = 0;
        try
        {
            activityClass = GetObjectClass(activity);
            var getWindow = GetMethodId(activityClass, "getWindow", "()Landroid/view/Window;");
            window = getWindow == 0 ? 0 : CallObject(activity, getWindow);
            if (window == 0)
            {
                return default;
            }

            windowClass = GetObjectClass(window);
            var getDecor = GetMethodId(windowClass, "getDecorView", "()Landroid/view/View;");
            decor = getDecor == 0 ? 0 : CallObject(window, getDecor);
            if (decor == 0)
            {
                return default;
            }

            viewClass = GetObjectClass(decor);
            var getInsets = GetMethodId(viewClass, "getRootWindowInsets", "()Landroid/view/WindowInsets;");
            insets = getInsets == 0 ? 0 : CallObject(decor, getInsets);
            if (insets == 0)
            {
                return default;
            }

            insetsClass = GetObjectClass(insets);
            int Read(string name)
            {
                var m = GetMethodId(insetsClass, name, "()I");
                return m == 0 ? 0 : CallInt(insets, m);
            }

            return new EdgeInsets(Read("getSystemWindowInsetLeft"), Read("getSystemWindowInsetTop"), Read("getSystemWindowInsetRight"), Read("getSystemWindowInsetBottom"));
        }
        finally
        {
            DeleteLocal(insetsClass);
            DeleteLocal(insets);
            DeleteLocal(viewClass);
            DeleteLocal(decor);
            DeleteLocal(windowClass);
            DeleteLocal(window);
            DeleteLocal(activityClass);
        }
    }

    /// <summary>
    /// The character a key produces in the device's layout (<c>KeyCharacterMap.load(deviceId).get(keyCode, metaState)</c>), or 0 for
    /// keys that type nothing. Dead keys and combining accents give 0 as well.
    /// </summary>
    public int GetUnicodeChar(int deviceId, int keyCode, int metaState)
    {
        nint mapClass = 0, map = 0;
        try
        {
            mapClass = FindClass("android/view/KeyCharacterMap");
            if (mapClass == 0)
            {
                return 0;
            }

            var load = GetStaticMethodId(mapClass, "load", "(I)Landroid/view/KeyCharacterMap;");
            map = load == 0 ? 0 : CallStaticObject(mapClass, load, deviceId);
            if (map == 0)
            {
                return 0;
            }

            var get = GetMethodId(mapClass, "get", "(II)I");
            var c = get == 0 ? 0 : CallInt(map, get, keyCode, metaState);
            return c < 0 || (c & 0x80000000) != 0 ? 0 : c;
        }
        finally
        {
            DeleteLocal(map);
            DeleteLocal(mapClass);
        }
    }

    private nint MethodId(int index, nint cls, string name, string signature)
    {
        var n = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        var s = System.Text.Encoding.UTF8.GetBytes(signature + "\0");
        fixed (byte* np = n)
        fixed (byte* sp = s)
        {
            return Checked(((delegate* unmanaged<nint, nint, byte*, byte*, nint>)Table[index])(_env, cls, np, sp));
        }
    }

    private bool Pending() => ((delegate* unmanaged<nint, byte>)Table[ExceptionCheckIndex])(_env) != 0;

    private void Clear()
    {
        if (Pending())
        {
            ((delegate* unmanaged<nint, void>)Table[ExceptionClearIndex])(_env);
        }
    }

    // A call that raised a Java exception returns 0 and leaves the exception pending, which would abort the next JNI call.
    private nint Checked(nint result)
    {
        if (Pending())
        {
            Clear();
            return 0;
        }

        return result;
    }
}
