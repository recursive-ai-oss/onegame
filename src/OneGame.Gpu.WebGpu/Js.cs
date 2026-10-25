using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace OneGame.Gpu.WebGpu;

/// <summary>The functions of <c>onegame-gpu.js</c>. The page registers the script as the module "onegame-gpu" before the game starts.</summary>
[SupportedOSPlatform("browser")]
internal static partial class Js
{
    private const string Module = "onegame-gpu";

    [JSImport("init", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string> Init(string requestJson);

    [JSImport("exec", Module)]
    public static partial void Exec([JSMarshalAs<JSType.MemoryView>] Span<byte> stream);

    [JSImport("takeMessages", Module)]
    public static partial string TakeMessages();

    [JSImport("configureCanvas", Module)]
    public static partial string ConfigureCanvas(string canvasId, int width, int height);

    [JSImport("setCanvasSize", Module)]
    public static partial void SetCanvasSize(string canvasId, int width, int height);

    // A read finishes with a ticket; the bytes are then copied out of the script with TakeRead.
    [JSImport("whenIdle", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task WhenIdle();

    [JSImport("readTexture", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
    public static partial Task<int> ReadTexture(int id, int mip, int layer, int width, int height, int depth, int bytesPerTexel);

    [JSImport("readBuffer", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
    public static partial Task<int> ReadBuffer(int id, double offset, double size);

    [JSImport("takeRead", Module)]
    public static partial void TakeRead(int ticket, [JSMarshalAs<JSType.MemoryView>] Span<byte> destination);
}
