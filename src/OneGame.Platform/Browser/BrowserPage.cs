using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace OneGame.Platform;

/// <summary>The page-level services a browser host needs around the platform backend: the animation-frame loop.</summary>
[SupportedOSPlatform("browser")]
public static partial class BrowserPage
{
    private static Func<string, Task<string?>>? _control;

    /// <summary>
    /// Lets the page's script send one control message (an MCP JSON-RPC text) to the running game and wait for the answer: the page's
    /// <c>__control</c> function. A development build sets it; a release build leaves it null and the function answers with an error text.
    /// </summary>
    public static void SetControlHandler(Func<string, Task<string?>>? handler) => _control = handler;

    /// <summary>Called by the page's script (<c>exports.OneGame.Platform.BrowserPage.Control</c>).</summary>
    [JSExport]
    public static async Task<string> Control(string message)
    {
        var handler = _control;
        return handler is null ? "{\"error\":\"This build has no control channel.\"}" : await handler(message).ConfigureAwait(true) ?? "";
    }

    /// <summary>
    /// Calls <paramref name="frame"/> once per animation frame, with the time in milliseconds, until <see cref="StopFrames"/>.
    /// The browser owns the loop: a managed thread cannot wait for the next frame.
    /// </summary>
    public static void StartFrames(Action<double> frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        PlatformJs.StartFrames(frame);
    }

    /// <summary>Stops the loop started by <see cref="StartFrames"/>.</summary>
    public static void StopFrames() => PlatformJs.StopFrames();
}
