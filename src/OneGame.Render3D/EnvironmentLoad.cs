using System.Runtime.InteropServices;
using RecursiveAI.OneGpu;
using OneGame.Gpu.Graph;

namespace OneGame.Render3D;

/// <summary>Where an <see cref="EnvironmentLoad"/> is.</summary>
public enum EnvironmentLoadState
{
    /// <summary>The panorama is being converted to half floats (on a worker thread, or in slices on the render thread).</summary>
    Converting,

    /// <summary>The converted panorama is being copied to the GPU in bands.</summary>
    Uploading,

    /// <summary>The filtering passes are being added to the frames a few at a time.</summary>
    Filtering,

    /// <summary>Every pass has been added; <see cref="EnvironmentLoad.Environment"/> is ready to use.</summary>
    Ready,

    /// <summary>The load stopped on an error, see <see cref="EnvironmentLoad.Error"/>.</summary>
    Failed,

    /// <summary>The load was cancelled or disposed before it finished.</summary>
    Canceled,
}

/// <summary>Tuning for <see cref="Renderer3D.CreateEnvironmentAsync"/>. The defaults suit a game that holds 60 frames per second.</summary>
public sealed class EnvironmentLoadOptions
{
    /// <summary>Face size of the specular cube, a power of two from 16 to 1024; 0 (the default) follows the scene's quality level.</summary>
    public int Size { get; init; }

    /// <summary>
    /// The most filtering work one frame takes on, in filter samples (a texel of one face of one mip costs 512). Each frame adds passes until the next
    /// would pass this, but always at least one. Lower it to spread the load over more frames. Default 40 million.
    /// </summary>
    public long GpuBudget { get; init; } = 40_000_000;

    /// <summary>The most panorama bytes copied to the GPU in one frame. Default 1 MiB.</summary>
    public int UploadBytesPerFrame { get; init; } = 1 << 20;

    /// <summary>Panorama pixels converted per frame when there is no worker thread (a browser). Default 262144.</summary>
    public int ConvertPixelsPerFrame { get; init; } = 1 << 18;

    /// <summary>Convert on a thread-pool thread. Ignored (false) in a browser, where one thread runs everything. Default true.</summary>
    public bool Background { get; init; } = true;
}

/// <summary>
/// An environment being made over several frames: the conversion runs off the render thread and the filtering passes are added a few per frame,
/// while the game keeps drawing its current map. Start one with <see cref="Renderer3D.CreateEnvironmentAsync"/>, keep drawing, and when
/// <see cref="IsReady"/> is true take the map with <see cref="Take"/> and set it as <c>Environment.Map</c> (or <c>NextMap</c> and fade with <c>Blend</c>).
/// The result is the same as <see cref="Renderer3D.CreateEnvironment"/> followed by <see cref="Renderer3D.PrepareEnvironment"/>.
/// Use it from the render thread only. Progress needs frames to be rendered through the renderer, and the panorama must not change until the load is ready.
/// </summary>
public sealed class EnvironmentLoad : IDisposable
{
    private readonly Renderer3D _renderer;
    private bool _taken;
    private bool _disposed;

    internal EnvironmentLoad(Renderer3D renderer, HdrImage image, EnvironmentLoadOptions options, int size, bool follows)
    {
        _renderer = renderer;
        Image = image;
        Options = options;
        Size = size;
        Follows = follows;
    }

    internal HdrImage Image { get; }

    internal EnvironmentLoadOptions Options { get; }

    internal int Size { get; set; }

    internal bool Follows { get; }

    internal Half[]? Halves { get; set; }

    internal Task? Task { get; set; }

    internal CancellationTokenSource? Cancellation { get; set; }

    internal int ConvertedPixels { get; set; }

    internal int UploadedRows { get; set; }

    internal GpuTexture? Equirect { get; set; }

    internal GpuTexture? Irradiance { get; set; }

    internal EnvironmentMap? Map { get; set; }

    internal Renderer3D.EnvironmentSet? Set { get; set; }

    internal int Step { get; set; }

    internal long CostDone { get; set; }

    internal long CostTotal { get; set; }

    /// <summary>Where the load is.</summary>
    public EnvironmentLoadState State { get; internal set; }

    /// <summary>True once every pass has been added: <see cref="Take"/> will return the map.</summary>
    public bool IsReady => State == EnvironmentLoadState.Ready;

    /// <summary>True while the load still needs frames (it is converting, uploading or filtering).</summary>
    public bool IsRunning => State <= EnvironmentLoadState.Filtering;

    /// <summary>The error that stopped the load, when <see cref="State"/> is <see cref="EnvironmentLoadState.Failed"/>.</summary>
    public Exception? Error { get; internal set; }

    /// <summary>How far along the load is, from 0 to 1 (conversion and upload count for a fifth, filtering for the rest, by estimated cost).</summary>
    public double Progress
    {
        get
        {
            switch (State)
            {
                case EnvironmentLoadState.Ready:
                    return 1;
                case EnvironmentLoadState.Converting:
                    return 0.1 * ConvertedFraction();
                case EnvironmentLoadState.Uploading:
                    return 0.1 + (0.1 * UploadedRows / Math.Max(Image.Height, 1));
                case EnvironmentLoadState.Filtering:
                    return 0.2 + (0.8 * CostDone / Math.Max(CostTotal, 1));
                default:
                    return 0;
            }
        }
    }

    private double ConvertedFraction()
    {
        if (Task is not null)
        {
            return 0;
        }

        return ConvertedPixels / (double)Math.Max(Image.Width * Image.Height, 1);
    }

    /// <summary>The finished map, still owned by this load until <see cref="Take"/>. Throws until <see cref="IsReady"/>.</summary>
    public EnvironmentMap Environment => IsReady && Map is not null && !_disposed ? Map : throw new InvalidOperationException("The environment is not ready (state " + State + ").");

    /// <summary>Hands the finished map to the caller, who disposes it before the renderer. After this, disposing the load leaves the map alone.</summary>
    /// <exception cref="InvalidOperationException">The load is not ready, or the map was taken already.</exception>
    public EnvironmentMap Take()
    {
        if (_taken)
        {
            throw new InvalidOperationException("The environment was already taken.");
        }

        var map = Environment;
        _taken = true;
        _renderer.ForgetLoad(this);
        return map;
    }

    /// <summary>Stops a load that is still running and frees what it made; the previous map is untouched and frames in flight are not hurt. Does nothing once ready.</summary>
    public void Cancel()
    {
        if (IsRunning)
        {
            _renderer.AbortLoad(this, EnvironmentLoadState.Canceled, null);
        }
    }

    /// <summary>Cancels a running load, and frees a finished map that was never taken. Safe at any time, also after the renderer is disposed.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Cancel();
        _disposed = true;
        if (IsReady && !_taken && Map is { } map)
        {
            _renderer.RetireEnvironment(map);
        }

        _renderer.ForgetLoad(this);
        Map = null;
    }

    internal void Abandon()
    {
        // The renderer is going away: its device is idle, so everything can be freed now.
        Cancellation?.Cancel();
        if (IsRunning)
        {
            State = EnvironmentLoadState.Canceled;
        }

        if (!_taken && Map is { } map)
        {
            map.Dispose();
        }
        else if (Equirect is not null)
        {
            Equirect.Dispose();
        }

        Map = null;
        Equirect = null;
        _disposed = true;
    }

    internal static void Convert(HdrImage image, Half[] halves, int from, int to, CancellationToken cancel)
    {
        // Half-float range tops out at 65504: clamp so a very bright sun disc cannot become infinity and smear through the filtering.
        for (var i = from; i < to; i++)
        {
            if ((i & 0x3FFF) == 0)
            {
                cancel.ThrowIfCancellationRequested();
            }

            halves[i * 4] = (Half)Math.Clamp(image.Rgb[i * 3], 0f, 60000f);
            halves[(i * 4) + 1] = (Half)Math.Clamp(image.Rgb[(i * 3) + 1], 0f, 60000f);
            halves[(i * 4) + 2] = (Half)Math.Clamp(image.Rgb[(i * 3) + 2], 0f, 60000f);
            halves[(i * 4) + 3] = (Half)1f;
        }
    }
}

public sealed partial class Renderer3D
{
    private readonly List<EnvironmentLoad> _loads = [];
    private RenderGraph? _loadGraph;
    private int _loadFrame;

    /// <summary>Number of environment loads that are still running.</summary>
    public int RunningEnvironmentLoads
    {
        get
        {
            var n = 0;
            foreach (var l in _loads)
            {
                if (l.IsRunning)
                {
                    n++;
                }
            }

            return n;
        }
    }

    /// <summary>
    /// Starts making an environment without a hitch: the panorama is converted on a worker thread (in slices on the render thread in a browser),
    /// copied to the GPU in bands, and filtered a few passes per frame, all driven by <see cref="Render"/>. The game keeps drawing its current
    /// map until <see cref="EnvironmentLoad.IsReady"/>, then swaps it in (or fades with <c>Environment.NextMap</c> and <c>Blend</c>).
    /// </summary>
    /// <param name="image">The panorama, linear radiance. Do not change it until the load is ready.</param>
    /// <param name="options">Size and per-frame budgets; null for the defaults.</param>
    /// <returns>The load. Dispose it (a finished, untaken map goes with it), or <see cref="EnvironmentLoad.Take"/> the map.</returns>
    public EnvironmentLoad CreateEnvironmentAsync(HdrImage image, EnvironmentLoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= new EnvironmentLoadOptions();
        ArgumentOutOfRangeException.ThrowIfLessThan(options.GpuBudget, 1L);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.UploadBytesPerFrame, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.ConvertPixelsPerFrame, 1);
        var size = options.Size;
        var follows = size == 0;
        if (follows)
        {
            size = QualitySettings.For(_quality).EnvironmentSize;
        }

        ValidateEnvironment(image, size, nameof(options));
        var load = new EnvironmentLoad(this, image, options, size, follows) { State = EnvironmentLoadState.Converting, Halves = new Half[checked(image.Width * image.Height * 4)] };
        if (options.Background && !OperatingSystem.IsBrowser())
        {
            var cts = new CancellationTokenSource();
            var halves = load.Halves;
            load.Cancellation = cts;
            load.Task = Task.Run(() => EnvironmentLoad.Convert(image, halves, 0, image.Width * image.Height, cts.Token), cts.Token);
        }

        _loads.Add(load);
        return load;
    }

    /// <summary>Frees a map that frames already recorded may still draw with, at the start of the next render. Use it for the map a swap replaced.</summary>
    /// <param name="environment">The map to free.</param>
    public void RetireEnvironment(EnvironmentMap environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _deferred.Add(environment);
    }

    internal void ForgetLoad(EnvironmentLoad load) => _loads.Remove(load);

    internal void AbortLoad(EnvironmentLoad load, EnvironmentLoadState state, Exception? error)
    {
        load.State = state;
        load.Error = error;
        load.Cancellation?.Cancel();
        load.Halves = null;
        if (load.Map is { } map)
        {
            // Frames already recorded may still run the passes that were added: free at the next render.
            _deferred.Add(map);
        }
        else
        {
            if (load.Equirect is not null)
            {
                _deferred.Add(load.Equirect);
            }

            if (load.Irradiance is not null)
            {
                _deferred.Add(load.Irradiance);
            }
        }

        RetireEnvironmentSet(load.Set);
        load.Map = null;
        load.Equirect = null;
        load.Irradiance = null;
        load.Set = null;
    }

    private void DisposeLoads()
    {
        foreach (var load in _loads)
        {
            load.Abandon();
        }

        _loads.Clear();
    }

    // Called by every render, before the frame's passes: advances each running load by one slice. Several renders in one graph frame (a
    // reflection or offscreen view beside the main view) count once.
    private void AdvanceLoads(RenderGraph graph)
    {
        if (_loads.Count == 0 || (ReferenceEquals(graph, _loadGraph) && graph.Frame == _loadFrame))
        {
            return;
        }

        _loadGraph = graph;
        _loadFrame = graph.Frame;
        for (var i = 0; i < _loads.Count; i++)
        {
            var load = _loads[i];
            if (!load.IsRunning)
            {
                continue;
            }

            try
            {
                StepLoad(graph, load);
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                AbortLoad(load, EnvironmentLoadState.Failed, e);
            }
        }
    }

    private void StepLoad(RenderGraph graph, EnvironmentLoad load)
    {
        var image = load.Image;
        switch (load.State)
        {
            case EnvironmentLoadState.Converting:
                if (load.Task is { } task)
                {
                    if (task.IsFaulted)
                    {
                        AbortLoad(load, EnvironmentLoadState.Failed, task.Exception!.GetBaseException());
                    }
                    else if (task.IsCompleted)
                    {
                        load.State = EnvironmentLoadState.Uploading;
                    }

                    return;
                }

                var pixels = image.Width * image.Height;
                var to = (int)Math.Min((long)load.ConvertedPixels + load.Options.ConvertPixelsPerFrame, pixels);
                EnvironmentLoad.Convert(image, load.Halves!, load.ConvertedPixels, to, CancellationToken.None);
                load.ConvertedPixels = to;
                if (to == pixels)
                {
                    load.State = EnvironmentLoadState.Uploading;
                }

                return;

            case EnvironmentLoadState.Uploading:
                if (load.Equirect is null)
                {
                    load.Equirect = Device.CreateTexture(new TextureDesc((uint)image.Width, (uint)image.Height, GpuFormat.Rgba16Float, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = "panorama" });
                }

                var rowBytes = image.Width * 8;
                var rows = Math.Clamp(load.Options.UploadBytesPerFrame / rowBytes, 1, image.Height - load.UploadedRows);
                var bytes = MemoryMarshal.AsBytes(load.Halves.AsSpan(load.UploadedRows * image.Width * 4, rows * image.Width * 4));
                Device.WriteTexture(load.Equirect, TextureRegion.Rect(0, 0, 0, (uint)load.UploadedRows, (uint)image.Width, (uint)rows), bytes);
                load.UploadedRows += rows;
                if (load.UploadedRows == image.Height)
                {
                    load.Halves = null;
                    load.State = EnvironmentLoadState.Filtering;
                }

                return;

            default:
                StepFiltering(graph, load);
                return;
        }
    }

    private static void MakeCube(GpuDevice device, int size, uint mips, string label, out GpuTexture texture) =>
        texture = device.CreateTexture(new TextureDesc((uint)size, (uint)size, GpuFormat.Rgba16Float, TextureUsage.Sampled | TextureUsage.Storage | TextureUsage.CopySource | TextureUsage.CopyDestination)
        {
            Dimension = TextureDimension.Cube,
            ArrayLayers = 6,
            MipLevels = mips,
            Label = label,
        });

    private void StepFiltering(RenderGraph graph, EnvironmentLoad load)
    {
        if (load.Follows && load.Map is not null && load.Size != QualitySettings.For(_quality).EnvironmentSize)
        {
            // The quality level changed mid-load: start the filtering again at the new size rather than hand back a map that rebuilds itself on first use.
            load.Size = QualitySettings.For(_quality).EnvironmentSize;
            _deferred.Add(load.Map.Source);
            _deferred.Add(load.Map.Specular);
            RetireEnvironmentSet(load.Set);
            load.Irradiance = load.Map.Irradiance;
            load.Equirect = load.Map.Equirect;
            load.Map = null;
            load.Set = null;
            load.Step = 0;
            load.CostDone = 0;
        }

        if (load.Map is null)
        {
            var size = load.Size;
            var mips = GpuTexture.FullMipCount((uint)size, (uint)size);
            var irradiance = load.Irradiance;
            if (irradiance is null)
            {
                MakeCube(Device, IrradianceSize, 1, "environment irradiance", out irradiance);
            }

            load.Irradiance = null;

            MakeCube(Device, size, mips, "environment source", out var source);
            MakeCube(Device, size, mips, "environment specular", out var specular);
            load.Map = Track(new EnvironmentMap(load.Equirect!, source, specular, irradiance, size, load.Follows) { Tracker = this });
            load.Set = BuildEnvironmentSet(load.Map);
            load.CostTotal = 0;
            for (var s = 0; s < EnvironmentStepCount((int)mips); s++)
            {
                load.CostTotal += EnvironmentStepCost(size, (int)mips, s);
            }

            return;
        }

        var map = load.Map;
        var count = EnvironmentStepCount(map.SpecularMipCount);
        if (!_lutBuilt)
        {
            _lutBuilt = true;
            AddLutPass(graph);
        }

        long spent = 0;
        while (load.Step < count)
        {
            var cost = EnvironmentStepCost(map.Size, map.SpecularMipCount, load.Step);
            if (spent > 0 && spent + cost > load.Options.GpuBudget)
            {
                break;
            }

            AddEnvironmentStep(graph, map, load.Set!, load.Step);
            spent += cost;
            load.CostDone += cost;
            load.Step++;
        }

        if (load.Step == count)
        {
            map.Pending = false;
            RetireEnvironmentSet(load.Set);
            load.Set = null;
            load.State = EnvironmentLoadState.Ready;
        }
    }
}
