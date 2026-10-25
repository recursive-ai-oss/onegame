using System.Threading.Tasks;
using System.Runtime.Versioning;
using OneGame.Gpu;
using OneGame.Platform;
using WebGpuApi = OneGame.Gpu.WebGpu.WebGpu;

namespace OneGame.Host;

/// <summary>
/// Runs a game inside a web page. The page's script registers the platform and GPU modules and starts the program; the game creates the
/// WebGPU device (an asynchronous request), builds its instance on the page's canvas and then lets the browser's animation-frame
/// callback drive it. Development tools, when the page asks for them, attach to the same instance.
/// </summary>
[SupportedOSPlatform("browser")]
internal static class BrowserHost
{
    private static GameInstance? _instance;

    /// <summary>Starts the game; returns before the first frame. A failure is written to the browser console and to <see cref="Failure"/>.</summary>
    public static void Start(GameDefinition game, GameOptions options, GameArgs args, Func<IDevTools>? devTools, TextWriter output) =>
        _ = RunAsync(game, options, args, devTools, output);

    /// <summary>Why the game did not start, or null.</summary>
    public static string? Failure { get; private set; }

    private static async Task RunAsync(GameDefinition game, GameOptions options, GameArgs args, Func<IDevTools>? devTools, TextWriter output)
    {
        try
        {
            var device = await WebGpuApi.CreateDeviceAsync(new GpuDeviceOptions { ApplicationName = game.Name }).ConfigureAwait(true);
            var instance = GameInstance.Create(game, options with { Device = device, CanvasId = options.CanvasId ?? "onegame-canvas" });
            _instance = instance;
            if (devTools is not null)
            {
                // The tools run their own loop on the page (a control channel); they return once it is started.
                devTools().Run(instance, args, output);
            }

            BrowserPage.StartFrames(_ =>
            {
                if (!instance.QuitRequested)
                {
                    instance.RunHostedFrame(paused: instance.HostPaused);
                }
            });
        }
        catch (Exception ex)
        {
            Failure = ex.ToString();
            Console.Error.WriteLine("onegame: the game did not start: " + ex.Message);
        }
    }
}
