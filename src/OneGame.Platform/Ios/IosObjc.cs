using System.Runtime.InteropServices;
using System.Text;

namespace OneGame.Platform;

// The Objective-C runtime as the iOS backend uses it: typed calls through objc_msgSend (every call casts the function pointer to the
// method's real signature, so argument types must match the Objective-C ones exactly: BOOL is byte, CGFloat is double, NSInteger is
// nint), a way to register classes whose methods are managed functions, and a hop to the main thread. Public documentation:
// Objective-C Runtime Reference (objc_allocateClassPair, class_addMethod) and the Dispatch framework (dispatch_async_f).

/// <summary>CGPoint.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CGPoint
{
    public double X;
    public double Y;
}

/// <summary>CGSize.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CGSize
{
    public double Width;
    public double Height;
}

/// <summary>CGRect.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CGRect
{
    public CGPoint Origin;
    public CGSize Size;
}

/// <summary>UIEdgeInsets (top, left, bottom, right, in that order).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UIEdgeInsets
{
    public double Top;
    public double Left;
    public double Bottom;
    public double Right;
}

/// <summary>NSRange.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NSRange
{
    public nuint Location;
    public nuint Length;
}

/// <summary>Struct passed to objc_msgSendSuper.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ObjcSuper
{
    public nint Receiver;
    public nint SuperClass;
}

internal static unsafe partial class IosObjc
{
    private const string LibObjc = "/usr/lib/libobjc.A.dylib";
    private const string LibSystem = "/usr/lib/libSystem.B.dylib";
    private const string LibUIKit = "/System/Library/Frameworks/UIKit.framework/UIKit";

    private static readonly nint MsgSend = NativeLibrary.GetExport(NativeLibrary.Load(LibObjc), "objc_msgSend");
    private static readonly nint MsgSendSuper = NativeLibrary.GetExport(NativeLibrary.Load(LibObjc), "objc_msgSendSuper");
    private static readonly nint MainQueue = NativeLibrary.GetExport(NativeLibrary.Load(LibSystem), "_dispatch_main_q");

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint objc_getClass(string name);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint objc_getProtocol(string name);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint sel_registerName(string name);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint objc_allocateClassPair(nint superclass, string name, nuint extraBytes);

    [LibraryImport(LibObjc)]
    private static partial void objc_registerClassPair(nint cls);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    private static partial byte class_addMethod(nint cls, nint name, nint imp, string types);

    [LibraryImport(LibObjc)]
    private static partial byte class_addProtocol(nint cls, nint protocol);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    private static partial byte class_addIvar(nint cls, string name, nuint size, byte alignment, string types);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint class_getInstanceVariable(nint cls, string name);

    [LibraryImport(LibObjc)]
    internal static partial nint ivar_getOffset(nint ivar);

    [LibraryImport(LibObjc)]
    internal static partial nint object_getClass(nint obj);

    [LibraryImport(LibObjc)]
    internal static partial nint class_getSuperclass(nint cls);

    [LibraryImport(LibObjc)]
    internal static partial nint object_getIvar(nint obj, nint ivar);

    [LibraryImport(LibObjc)]
    internal static partial void object_setIvar(nint obj, nint ivar, nint value);

    [LibraryImport(LibObjc)]
    internal static partial nint objc_retain(nint obj);

    [LibraryImport(LibObjc)]
    internal static partial void objc_release(nint obj);

    [LibraryImport(LibObjc)]
    internal static partial nint objc_autoreleasePoolPush();

    [LibraryImport(LibObjc)]
    internal static partial void objc_autoreleasePoolPop(nint pool);

    [LibraryImport(LibSystem)]
    private static partial void dispatch_async_f(nint queue, nint context, nint work);

    [LibraryImport(LibSystem)]
    private static partial void dispatch_sync_f(nint queue, nint context, nint work);

    [LibraryImport(LibSystem)]
    private static partial int pthread_main_np();

    [LibraryImport(LibSystem)]
    internal static partial void exit(int status);

    [LibraryImport(LibUIKit)]
    private static partial int UIApplicationMain(int argc, byte** argv, nint principalClassName, nint delegateClassName);

    /// <summary>True on the process's main thread, where UIKit must be used.</summary>
    internal static bool IsMainThread => pthread_main_np() != 0;

    /// <summary>Loads the frameworks whose classes are looked up by name.</summary>
    internal static void LoadFrameworks()
    {
        foreach (var name in new[] { "UIKit", "QuartzCore", "Foundation", "GameController" })
        {
            NativeLibrary.TryLoad($"/System/Library/Frameworks/{name}.framework/{name}", out _);
        }
    }

    /// <summary>Calls UIApplicationMain with the named delegate class; it returns only when the app ends.</summary>
    internal static int ApplicationMain(string delegateClass)
    {
        var name = NSString(delegateClass);
        // The system parses the arguments for its own switches (-AppleLanguages and the like); the game's own come through managed code.
        byte** argv = stackalloc byte*[2];
        var program = (byte*)Marshal.StringToCoTaskMemUTF8("onegame");
        argv[0] = program;
        argv[1] = null;
        return UIApplicationMain(1, argv, 0, name);
    }

    // ---- typed message sends ----

    internal static nint Cls(string name) => objc_getClass(name);

    internal static nint Sel(string name) => sel_registerName(name);

    internal static nint Obj(nint o, nint s) => ((delegate* unmanaged<nint, nint, nint>)MsgSend)(o, s);

    internal static nint Obj<T1>(nint o, nint s, T1 a1) where T1 : unmanaged => ((delegate* unmanaged<nint, nint, T1, nint>)MsgSend)(o, s, a1);

    internal static nint Obj<T1, T2>(nint o, nint s, T1 a1, T2 a2) where T1 : unmanaged where T2 : unmanaged => ((delegate* unmanaged<nint, nint, T1, T2, nint>)MsgSend)(o, s, a1, a2);

    internal static nint Obj<T1, T2, T3>(nint o, nint s, T1 a1, T2 a2, T3 a3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged =>
        ((delegate* unmanaged<nint, nint, T1, T2, T3, nint>)MsgSend)(o, s, a1, a2, a3);

    internal static void Void(nint o, nint s) => ((delegate* unmanaged<nint, nint, void>)MsgSend)(o, s);

    internal static void Void<T1>(nint o, nint s, T1 a1) where T1 : unmanaged => ((delegate* unmanaged<nint, nint, T1, void>)MsgSend)(o, s, a1);

    internal static void Void<T1, T2>(nint o, nint s, T1 a1, T2 a2) where T1 : unmanaged where T2 : unmanaged => ((delegate* unmanaged<nint, nint, T1, T2, void>)MsgSend)(o, s, a1, a2);

    internal static bool Flag(nint o, nint s) => ((delegate* unmanaged<nint, nint, byte>)MsgSend)(o, s) != 0;

    internal static bool Flag<T1>(nint o, nint s, T1 a1) where T1 : unmanaged => ((delegate* unmanaged<nint, nint, T1, byte>)MsgSend)(o, s, a1) != 0;

    internal static nint Int(nint o, nint s) => ((delegate* unmanaged<nint, nint, nint>)MsgSend)(o, s);

    internal static nint Int<T1>(nint o, nint s, T1 a1) where T1 : unmanaged => ((delegate* unmanaged<nint, nint, T1, nint>)MsgSend)(o, s, a1);

    internal static nint Int<T1, T2>(nint o, nint s, T1 a1, T2 a2) where T1 : unmanaged where T2 : unmanaged => ((delegate* unmanaged<nint, nint, T1, T2, nint>)MsgSend)(o, s, a1, a2);

    internal static double Dbl(nint o, nint s) => ((delegate* unmanaged<nint, nint, double>)MsgSend)(o, s);

    internal static float Float(nint o, nint s) => ((delegate* unmanaged<nint, nint, float>)MsgSend)(o, s);

    internal static T Ret<T>(nint o, nint s) where T : unmanaged => ((delegate* unmanaged<nint, nint, T>)MsgSend)(o, s);

    internal static T Ret<T, T1>(nint o, nint s, T1 a1) where T : unmanaged where T1 : unmanaged => ((delegate* unmanaged<nint, nint, T1, T>)MsgSend)(o, s, a1);

    /// <summary>Calls the superclass implementation of a method the managed class overrides.</summary>
    internal static void SuperVoid<T1, T2>(nint self, nint cls, nint s, T1 a1, T2 a2) where T1 : unmanaged where T2 : unmanaged
    {
        var sup = new ObjcSuper { Receiver = self, SuperClass = class_getSuperclass(cls) };
        ((delegate* unmanaged<ObjcSuper*, nint, T1, T2, void>)MsgSendSuper)(&sup, s, a1, a2);
    }

    internal static void SuperVoid<T1>(nint self, nint cls, nint s, T1 a1) where T1 : unmanaged
    {
        var sup = new ObjcSuper { Receiver = self, SuperClass = class_getSuperclass(cls) };
        ((delegate* unmanaged<ObjcSuper*, nint, T1, void>)MsgSendSuper)(&sup, s, a1);
    }

    internal static void SuperVoid(nint self, nint cls, nint s)
    {
        var sup = new ObjcSuper { Receiver = self, SuperClass = class_getSuperclass(cls) };
        ((delegate* unmanaged<ObjcSuper*, nint, void>)MsgSendSuper)(&sup, s);
    }

    internal static nint Alloc(nint cls) => Obj(cls, Sel("alloc"));

    internal static nint Autorelease(nint o) => Obj(o, Sel("autorelease"));

    // ---- strings, arrays ----

    /// <summary>An autoreleased NSString.</summary>
    internal static nint NSString(string s)
    {
        fixed (char* p = s)
        {
            return Obj(Cls("NSString"), Sel("stringWithCharacters:length:"), (nint)p, (nint)s.Length);
        }
    }

    /// <summary>The text of an NSString (or NSAttributedString), or "" for nil.</summary>
    internal static string ToManaged(nint ns)
    {
        if (ns == 0)
        {
            return "";
        }

        if (Flag(ns, Sel("isKindOfClass:"), Cls("NSAttributedString")))
        {
            ns = Obj(ns, Sel("string"));
        }

        var utf8 = Obj(ns, Sel("UTF8String"));
        return utf8 == 0 ? "" : Marshal.PtrToStringUTF8(utf8) ?? "";
    }

    /// <summary>The objects of an NSSet or NSArray, in the collection's order.</summary>
    internal static List<nint> Items(nint collection)
    {
        var result = new List<nint>();
        if (collection == 0)
        {
            return result;
        }

        var array = Flag(collection, Sel("isKindOfClass:"), Cls("NSSet")) ? Obj(collection, Sel("allObjects")) : collection;
        var count = (int)Int(array, Sel("count"));
        var at = Sel("objectAtIndex:");
        for (var i = 0; i < count; i++)
        {
            result.Add(Obj(array, at, (nint)i));
        }

        return result;
    }

    // ---- classes implemented in managed code ----

    /// <summary>One method of a managed class: the selector, the function (an UnmanagedCallersOnly pointer) and its Objective-C type encoding.</summary>
    internal readonly record struct Method(string Selector, nint Function, string Encoding);

    /// <summary>Creates and registers a subclass whose methods are the given managed functions.</summary>
    internal static nint DefineClass(string name, string superclass, string[] protocols, Method[] instanceMethods, Method[]? classMethods = null, (string Name, nuint Size, byte Alignment, string Encoding)[]? ivars = null)
    {
        var super = Cls(superclass);
        if (super == 0)
        {
            throw new PlatformException($"The Objective-C class {superclass} was not found.");
        }

        var cls = objc_allocateClassPair(super, name, 0);
        if (cls == 0)
        {
            throw new PlatformException($"The Objective-C class {name} could not be created.");
        }

        foreach (var iv in ivars ?? [])
        {
            class_addIvar(cls, iv.Name, iv.Size, iv.Alignment, iv.Encoding);
        }

        foreach (var m in instanceMethods)
        {
            class_addMethod(cls, Sel(m.Selector), m.Function, m.Encoding);
        }

        foreach (var m in classMethods ?? [])
        {
            class_addMethod(object_getClass(cls), Sel(m.Selector), m.Function, m.Encoding);
        }

        foreach (var p in protocols)
        {
            var proto = objc_getProtocol(p);
            if (proto != 0)
            {
                class_addProtocol(cls, proto);
            }
        }

        objc_registerClassPair(cls);
        return cls;
    }

    // ---- the main thread ----

    private static readonly nint MainThunk = (nint)(delegate* unmanaged<nint, void>)&RunMainWork;

    /// <summary>Runs <paramref name="work"/> on the main thread without waiting for it.</summary>
    internal static void OnMain(Action work)
    {
        var handle = GCHandle.Alloc(work);
        dispatch_async_f(MainQueue, GCHandle.ToIntPtr(handle), MainThunk);
    }

    /// <summary>Runs <paramref name="work"/> on the main thread and waits for it. Runs it in place when already there.</summary>
    internal static void OnMainWait(Action work)
    {
        if (IsMainThread)
        {
            work();
            return;
        }

        var handle = GCHandle.Alloc(work);
        dispatch_sync_f(MainQueue, GCHandle.ToIntPtr(handle), MainThunk);
    }

    [UnmanagedCallersOnly]
    private static void RunMainWork(nint context)
    {
        var handle = GCHandle.FromIntPtr(context);
        var work = (Action)handle.Target!;
        handle.Free();
        try
        {
            work();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("onegame: unhandled exception on the main thread: " + ex);
        }
    }

    /// <summary>UTF-8 bytes of a managed string with a trailing NUL, for APIs that want a C string.</summary>
    internal static byte[] CString(string s) => [.. Encoding.UTF8.GetBytes(s), 0];
}
