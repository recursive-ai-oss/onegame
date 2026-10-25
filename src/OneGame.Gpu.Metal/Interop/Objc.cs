using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: DisableRuntimeMarshalling]

namespace OneGame.Gpu.Metal.Interop;

/// <summary>The Objective-C runtime and the Foundation pieces the Metal backend needs.</summary>
internal static unsafe partial class Objc
{
    const string LibObjc = "/usr/lib/libobjc.A.dylib";
    const string LibMetal = "/System/Library/Frameworks/Metal.framework/Metal";
    const string LibSystem = "/usr/lib/libSystem.B.dylib";

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint objc_getClass(string name);

    [LibraryImport(LibObjc, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint sel_registerName(string name);

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

    [LibraryImport(LibMetal)]
    public static partial nint MTLCreateSystemDefaultDevice();

    [LibraryImport(LibSystem, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int setenv(string name, string value, int overwrite);

    /// <summary>Loads the frameworks whose classes the backend looks up by name. False when this is not an Apple arm64 platform.</summary>
    public static bool Load()
    {
        if (!(OperatingSystem.IsMacOS() || OperatingSystem.IsIOS()) || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
        {
            return false;
        }

        // QuartzCore carries CAMetalLayer; Foundation is already loaded with the runtime but naming it is harmless.
        return NativeLibrary.TryLoad(LibMetal, out _)
            && NativeLibrary.TryLoad("/System/Library/Frameworks/QuartzCore.framework/QuartzCore", out _)
            && NativeLibrary.TryLoad("/System/Library/Frameworks/Foundation.framework/Foundation", out _);
    }

    /// <summary>Exported function of the Metal framework, or 0 when this OS does not have it (MTLCopyAllDevices is macOS only).</summary>
    public static nint MetalExport(string name) =>
        NativeLibrary.TryLoad(LibMetal, out var lib) && NativeLibrary.TryGetExport(lib, name, out var p) ? p : 0;

    /// <summary>Creates an autoreleased NSString.</summary>
    public static nint NSString(string s)
    {
        var max = Encoding.UTF8.GetMaxByteCount(s.Length) + 1;
        Span<byte> buf = max <= 512 ? stackalloc byte[max] : new byte[max];
        var n = Encoding.UTF8.GetBytes(s, buf);
        buf[n] = 0;
        fixed (byte* p = buf)
        {
            return Msg.Obj(Cls.NSString, Sel.stringWithUTF8String_, (nint)p);
        }
    }

    /// <summary>Reads an NSString (or null) into a managed string.</summary>
    public static string ToManaged(nint nsString)
    {
        if (nsString == 0)
        {
            return "";
        }

        var p = Msg.Obj(nsString, Sel.UTF8String);
        return p == 0 ? "" : Marshal.PtrToStringUTF8(p) ?? "";
    }

    /// <summary>The description of an NSError, or an empty string.</summary>
    public static string ErrorText(nint error) =>
        error == 0 ? "" : ToManaged(Msg.Obj(error, Sel.localizedDescription));

    /// <summary>Name of the object's Objective-C class.</summary>
    public static string ClassName(nint obj) => Marshal.PtrToStringUTF8(object_getClassName(obj)) ?? "";

    /// <summary>Releases an object if it is not null.</summary>
    public static void Release(ref nint obj)
    {
        if (obj != 0)
        {
            objc_release(obj);
            obj = 0;
        }
    }
}

/// <summary>An Objective-C autorelease pool for the current scope: temporaries the calls inside create are released when it ends.</summary>
internal readonly struct Pool : IDisposable
{
    readonly nint _pool;

    public Pool() => _pool = Objc.objc_autoreleasePoolPush();

    public void Dispose() => Objc.objc_autoreleasePoolPop(_pool);
}

/// <summary>MTLSize.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MTLSize
{
    public nuint Width, Height, Depth;

    public MTLSize(nuint w, nuint h, nuint d)
    {
        Width = w;
        Height = h;
        Depth = d;
    }
}

/// <summary>MTLOrigin.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MTLOrigin
{
    public nuint X, Y, Z;

    public MTLOrigin(nuint x, nuint y, nuint z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

/// <summary>MTLViewport.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MTLViewport
{
    public double OriginX, OriginY, Width, Height, ZNear, ZFar;
}

/// <summary>MTLScissorRect.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MTLScissorRect
{
    public nuint X, Y, Width, Height;
}

/// <summary>MTLClearColor.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MTLClearColor
{
    public double Red, Green, Blue, Alpha;
}

/// <summary>CGSize.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CGSize
{
    public double Width, Height;
}

/// <summary>Numeric values of the Metal enumerations the backend uses (Metal headers; Apple documentation for each type).</summary>
internal static class MT
{
    // MTLPixelFormat
    public const nuint PixelInvalid = 0, R8Unorm = 10, R16Float = 25, Rg8Unorm = 30, R32Uint = 53, R32Float = 55, Rg16Float = 65,
        Rgba8Unorm = 70, Rgba8UnormSrgb = 71, Bgra8Unorm = 80, Bgra8UnormSrgb = 81, Rgb10A2Unorm = 90, Rg11B10Float = 92,
        Rg32Float = 105, Rgba16Float = 115, Rgba32Float = 125, Depth16Unorm = 250, Depth32Float = 252, Depth32FloatStencil8 = 260;

    // MTLTextureType
    public const nuint Tex2D = 2, Tex2DArray = 3, Tex2DMultisample = 4, TexCube = 5, TexCubeArray = 6, Tex3D = 7;

    // MTLTextureUsage
    public const nuint UsageShaderRead = 1, UsageShaderWrite = 2, UsageRenderTarget = 4, UsagePixelFormatView = 16;

    // MTLResourceOptions (storage mode lives in bits 4..7)
    public const nuint StorageShared = 0, StoragePrivate = 2 << 4;

    // MTLStorageMode
    public const nuint StorageModePrivate = 2;

    // MTLLoadAction / MTLStoreAction
    public const nuint LoadDontCare = 0, LoadLoad = 1, LoadClear = 2;
    public const nuint StoreDontCare = 0, StoreStore = 1, StoreResolve = 2, StoreAndResolve = 3;

    // MTLPrimitiveType
    public const nuint Point = 0, Line = 1, Triangle = 3, TriangleStrip = 4;

    // MTLIndexType
    public const nuint Index16 = 0, Index32 = 1;

    // MTLCullMode / MTLWinding / MTLDepthClipMode
    public const nuint CullNone = 0, CullFront = 1, CullBack = 2;
    public const nuint WindingClockwise = 0, WindingCounterClockwise = 1;
    public const nuint DepthClipClip = 0, DepthClipClamp = 1;

    // MTLCommandBufferStatus
    public const nuint StatusCompleted = 4, StatusError = 5;

    // MTLVertexStepFunction
    public const nuint StepPerVertex = 1, StepPerInstance = 2;

    // MTLBlitOption
    public const nuint BlitDepthFromDepthStencil = 1;
}
