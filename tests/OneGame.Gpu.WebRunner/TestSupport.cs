using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

namespace OneGame.Gpu.Vulkan.Tests;

/// <summary>The browser's counterpart of the native fixture: one shared WebGPU device, and a check that the tests left no validation messages.</summary>
internal sealed class GpuFixture : IDisposable
{
    public static GpuDevice? Shared { get; set; }

    public GpuDevice Device { get; } = Shared ?? throw new InvalidOperationException("No WebGPU device.");

    public void Dispose()
    {
        var messages = Device.TakeValidationMessages();
        Assert.True(messages.Length == 0, "WebGPU reported:\n" + string.Join("\n", messages));
    }
}

/// <summary>Compares captured pixels with the committed reference images, which are embedded in the runner.</summary>
internal static class Golden
{
    public const int Tolerance = 2;
    public const double EdgeBudget = 0.02;

    public static Dictionary<string, (int W, int H, byte[] Rgba)> Captures { get; } = [];

    public static (int MaxDiff, double OutsideShare) Compare(string name, int w, int h, byte[] actual)
    {
        Captures[name] = (w, h, actual.ToArray());
        using var stream = typeof(Golden).Assembly.GetManifestResourceStream("golden/" + name + ".png") ?? throw new InvalidOperationException("No reference image " + name);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var (gw, gh, expected) = Png.Decode(ms.ToArray());
        Assert.Equal((gw, gh), (w, h));
        var max = 0;
        var outside = 0;
        for (var p = 0; p < w * h; p++)
        {
            var worst = 0;
            for (var c = 0; c < 4; c++)
            {
                worst = Math.Max(worst, Math.Abs(actual[p * 4 + c] - expected[p * 4 + c]));
            }

            max = Math.Max(max, worst);
            if (worst > Tolerance)
            {
                outside++;
            }
        }

        return (max, outside / (double)(w * h));
    }

    public static void Assert_(string name, int w, int h, byte[] actual)
    {
        var (max, share) = Compare(name, w, h, actual);
        Results.Notes.Add($"{name}: max diff {max}, {share:P2} of pixels beyond {Tolerance}");
        Assert.True(share <= EdgeBudget, $"{name}: {share:P2} of pixels differ by more than {Tolerance} (max diff {max})");
    }
}

internal static class Results
{
    public static List<string> Notes { get; } = [];
}

public static partial class Runner
{
    // Tests the browser backend cannot run, with the reason.
    private static readonly Dictionary<string, string> Skipped = new(StringComparer.Ordinal)
    {
        ["PushConstantsReachTheShader"] = "push constants are emulated with uniform buffers only in the renderers",
        ["PassTimingWritesStartAndEndTimes"] = "timestamp queries cannot be read without blocking",
        ["SteadyStateFramesAllocateNothing"] = "allocation counts of the native backends",
        ["SteadyStateGraphOfTenPassesAllocatesNothing"] = "allocation counts of the native backends",
        ["ReportNamesGpuTimeForPasses"] = "timestamp queries cannot be read without blocking",
    };

    /// <summary>Runs the shared tests whose names contain <paramref name="filter"/> and returns a JSON report.</summary>
    [JSExport]
    public static async Task<string> Run(string filter)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var device = await WebGpu.WebGpu.CreateDeviceAsync();
        GpuFixture.Shared = device;
        var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteString("adapter", device.Adapter.Name);
            w.WriteStartArray("tests");
            int passed = 0, failed = 0, skipped = 0;
            foreach (var type in typeof(Runner).Assembly.GetTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (m.GetCustomAttribute<FactAttribute>() is null || !(type.Name + "." + m.Name).Contains(filter, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    w.WriteStartObject();
                    w.WriteString("name", type.Name + "." + m.Name);
                    if (Skipped.TryGetValue(m.Name, out var why))
                    {
                        skipped++;
                        w.WriteString("status", "skipped");
                        w.WriteString("reason", why);
                        w.WriteEndObject();
                        continue;
                    }

                    var t0 = sw.ElapsedMilliseconds;
                    string? error = null;
                    try
                    {
                        var instance = m.IsStatic ? null : Activator.CreateInstance(type, nonPublic: true);
                        if (m.Invoke(instance, null) is Task task)
                        {
                            await task.ConfigureAwait(true);
                        }

                        await WebGpu.WebGpu.SettleAsync(device).ConfigureAwait(true);
                        var late = device.TakeValidationMessages();
                        if (late.Length > 0)
                        {
                            error = "WebGPU reported:\n" + string.Join("\n", late);
                        }
                    }
                    catch (TargetInvocationException ex)
                    {
                        error = ex.InnerException?.ToString() ?? ex.Message;
                    }
                    catch (Exception ex)
                    {
                        error = ex.ToString();
                    }

                    if (error is null)
                    {
                        passed++;
                    }
                    else
                    {
                        failed++;
                    }

                    w.WriteString("status", error is null ? "passed" : "failed");
                    w.WriteNumber("ms", sw.ElapsedMilliseconds - t0);
                    if (error is not null)
                    {
                        w.WriteString("error", error);
                    }

                    w.WriteEndObject();
                }
            }

            w.WriteEndArray();
            w.WriteNumber("passed", passed);
            w.WriteNumber("failed", failed);
            w.WriteNumber("skipped", skipped);
            w.WriteStartArray("notes");
            foreach (var n in Results.Notes)
            {
                w.WriteStringValue(n);
            }

            w.WriteEndArray();
            w.WriteStartObject("captures");
            foreach (var (name, c) in Golden.Captures)
            {
                w.WriteStartObject(name);
                w.WriteNumber("w", c.W);
                w.WriteNumber("h", c.H);
                w.WriteString("rgba", Convert.ToBase64String(c.Rgba));
                w.WriteEndObject();
            }

            w.WriteEndObject();
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
