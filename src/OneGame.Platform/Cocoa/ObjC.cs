using System.Runtime.InteropServices;
using System.Text;

namespace OneGame.Platform.Cocoa;

/// <summary>An Objective-C <c>CGPoint</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CGPoint
{
    public double X, Y;
}

/// <summary>An Objective-C <c>CGSize</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CGSize
{
    public double Width, Height;
}

/// <summary>An Objective-C <c>CGRect</c> (<c>NSRect</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CGRect
{
    public CGPoint Origin;
    public CGSize Size;
}

/// <summary>An Objective-C <c>NSRange</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NSRange
{
    public nuint Location, Length;

    /// <summary><c>NSNotFound</c>: NSIntegerMax.</summary>
    public static readonly nuint NotFound = nuint.MaxValue >> 1;
}

/// <summary>The Objective-C runtime: classes, selectors, class creation and the few Foundation helpers the Cocoa backend needs.</summary>
internal static unsafe partial class ObjC
{
    private const string LibObjc = "/usr/lib/libobjc.A.dylib";

    private const string AppKitPath = "/System/Library/Frameworks/AppKit.framework/AppKit";
    private const string FoundationPath = "/System/Library/Frameworks/Foundation.framework/Foundation";
    private const string QuartzCorePath = "/System/Library/Frameworks/QuartzCore.framework/QuartzCore";
    private const string GameControllerPath = "/System/Library/Frameworks/GameController.framework/GameController";

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint objc_getClass(string name);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint objc_getProtocol(string name);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint sel_registerName(string name);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint objc_allocateClassPair(nint superclass, string name, nuint extraBytes);

    [LibraryImport(LibObjc)]
    public static partial void objc_registerClassPair(nint cls);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool class_addMethod(nint cls, nint sel, nint imp, string types);

    [LibraryImport(LibObjc)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool class_addProtocol(nint cls, nint protocol);

    [LibraryImport(LibObjc)]
    public static partial nint objc_retain(nint obj);

    [LibraryImport(LibObjc)]
    public static partial void objc_release(nint obj);

    [LibraryImport(LibObjc)]
    public static partial nint objc_autoreleasePoolPush();

    [LibraryImport(LibObjc)]
    public static partial void objc_autoreleasePoolPop(nint pool);

    [LibraryImport(LibObjc)]
    public static partial nint object_getClassName(nint obj);

    [LibraryImport("/usr/lib/libSystem.B.dylib")]
    public static partial int pthread_main_np();

    [LibraryImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    public static partial int CGAssociateMouseAndMouseCursorPosition([MarshalAs(UnmanagedType.U1)] bool connected);

    /// <summary>True when the process can run the Cocoa backend: macOS on arm64 (struct returns through objc_msgSend rely on it).</summary>
    public static bool IsSupported => OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

    /// <summary>Loads the frameworks whose classes are looked up by name.</summary>
    public static void LoadFrameworks()
    {
        foreach (var path in new[] { FoundationPath, AppKitPath, QuartzCorePath })
        {
            if (!NativeLibrary.TryLoad(path, out _))
            {
                throw new PlatformException($"Could not load {path}.");
            }
        }
    }

    /// <summary>Loads GameController. False when the framework is missing.</summary>
    public static bool LoadGameController() => NativeLibrary.TryLoad(GameControllerPath, out _);

    /// <summary>Reads a global <c>NSString *</c> constant exported by Foundation (such as <c>NSDefaultRunLoopMode</c>).</summary>
    public static nint FoundationString(string symbol)
    {
        var lib = NativeLibrary.Load(FoundationPath);
        return NativeLibrary.TryGetExport(lib, symbol, out var p) ? *(nint*)p : 0;
    }

    public static nint Sel(string name) => sel_registerName(name);

    public static nint Class(string name) => objc_getClass(name);

    /// <summary>Sends a message with no arguments that returns an object.</summary>
    public static nint Send(nint o, string sel) => Msg.Obj(o, Sel(sel));

    /// <summary>An autoreleased NSString. Call it inside a <see cref="Pool"/>.</summary>
    public static nint NSString(string s)
    {
        var max = Encoding.UTF8.GetMaxByteCount(s.Length) + 1;
        var buf = max <= 512 ? stackalloc byte[max] : new byte[max];
        var n = Encoding.UTF8.GetBytes(s, buf);
        buf[n] = 0;
        fixed (byte* p = buf)
        {
            return Msg.Obj(Class("NSString"), Sel("stringWithUTF8String:"), (nint)p);
        }
    }

    /// <summary>Reads an NSString (or nil) into a managed string.</summary>
    public static string ToManaged(nint nsString)
    {
        if (nsString == 0)
        {
            return "";
        }

        var p = Msg.Obj(nsString, Sel("UTF8String"));
        return p == 0 ? "" : Marshal.PtrToStringUTF8(p) ?? "";
    }

    /// <summary>The string of an NSString or NSAttributedString.</summary>
    public static string TextOf(nint stringOrAttributed)
    {
        if (stringOrAttributed == 0)
        {
            return "";
        }

        if (Msg.Flag(stringOrAttributed, Sel("isKindOfClass:"), Class("NSAttributedString")) != 0)
        {
            stringOrAttributed = Msg.Obj(stringOrAttributed, Sel("string"));
        }

        return ToManaged(stringOrAttributed);
    }

    /// <summary>Makes a subclass of <paramref name="superName"/> with the given methods and protocols, or returns the class if it already exists.</summary>
    public static nint DefineClass(string name, string superName, ReadOnlySpan<(string Selector, nint Implementation, string Types)> methods, params string[] protocols)
    {
        var existing = objc_getClass(name);
        if (existing != 0)
        {
            return existing;
        }

        var cls = objc_allocateClassPair(Class(superName), name, 0);
        if (cls == 0)
        {
            throw new PlatformException($"Could not create the Objective-C class {name}.");
        }

        foreach (var (selector, imp, types) in methods)
        {
            if (!class_addMethod(cls, Sel(selector), imp, types))
            {
                throw new PlatformException($"Could not add {selector} to {name}.");
            }
        }

        foreach (var p in protocols)
        {
            var protocol = objc_getProtocol(p);
            if (protocol != 0)
            {
                class_addProtocol(cls, protocol);
            }
        }

        objc_registerClassPair(cls);
        return cls;
    }

    public static void Release(ref nint obj)
    {
        if (obj != 0)
        {
            objc_release(obj);
            obj = 0;
        }
    }
}

/// <summary>An autorelease pool for the current scope: temporaries created inside are released when it ends.</summary>
internal readonly struct Pool : IDisposable
{
    private readonly nint _pool;

    public Pool() => _pool = ObjC.objc_autoreleasePoolPush();

    public void Dispose() => ObjC.objc_autoreleasePoolPop(_pool);
}
