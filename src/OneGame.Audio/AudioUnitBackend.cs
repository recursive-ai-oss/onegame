using System.Runtime.InteropServices;
using System.Threading;

namespace OneGame.Audio;

/// <summary>How an iOS app's audio relates to other apps' audio and the ring/silent switch (AVAudioSession categories).</summary>
public enum AudioSessionCategory
{
    /// <summary>Mixes with other apps' audio and follows the silent switch. The default for games.</summary>
    Ambient,

    /// <summary>Silences other apps' audio and follows the silent switch.</summary>
    SoloAmbient,

    /// <summary>Plays even with the silent switch on and silences other apps' audio.</summary>
    Playback,
}

/// <summary>The Audio Toolbox calls the Apple backend uses (Audio Unit Hosting Guide; Audio Unit Properties Reference).</summary>
internal static unsafe partial class AudioToolbox
{
    private const string Lib = "/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox";

    public const uint TypeOutput = 0x61756F75;         // 'auou'
    public const uint SubTypeRemoteIo = 0x72696F63;    // 'rioc'
    public const uint SubTypeDefaultOutput = 0x64656620; // 'def '
    public const uint ManufacturerApple = 0x6170706C;  // 'appl'
    public const uint FormatLinearPcm = 0x6C70636D;    // 'lpcm'
    public const uint FlagIsFloat = 1;
    public const uint FlagIsPacked = 8;
    public const uint PropertyStreamFormat = 8;
    public const uint PropertySetRenderCallback = 23;
    public const uint ScopeInput = 1;
    public const uint ActionOutputIsSilence = 1u << 4;

    [StructLayout(LayoutKind.Sequential)]
    public struct ComponentDescription
    {
        public uint Type;
        public uint SubType;
        public uint Manufacturer;
        public uint Flags;
        public uint FlagsMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StreamDescription
    {
        public double SampleRate;
        public uint FormatId;
        public uint FormatFlags;
        public uint BytesPerPacket;
        public uint FramesPerPacket;
        public uint BytesPerFrame;
        public uint ChannelsPerFrame;
        public uint BitsPerChannel;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderCallback
    {
        public nint Proc;
        public nint RefCon;
    }

    // AudioBufferList with its first (and, for interleaved audio, only) buffer inline. The buffer holds a pointer, so it starts on an
    // 8-byte boundary: four bytes of padding follow the count.
    [StructLayout(LayoutKind.Sequential)]
    public struct BufferList
    {
        public uint BufferCount;
        public uint Padding;
        public uint Channels;
        public uint ByteSize;
        public nint Data;
    }

    [LibraryImport(Lib, EntryPoint = "AudioComponentFindNext")]
    public static partial nint FindNext(nint after, ComponentDescription* description);

    [LibraryImport(Lib, EntryPoint = "AudioComponentInstanceNew")]
    public static partial int InstanceNew(nint component, out nint unit);

    [LibraryImport(Lib, EntryPoint = "AudioComponentInstanceDispose")]
    public static partial int InstanceDispose(nint unit);

    [LibraryImport(Lib, EntryPoint = "AudioUnitSetProperty")]
    public static partial int SetProperty(nint unit, uint property, uint scope, uint element, void* data, uint size);

    [LibraryImport(Lib, EntryPoint = "AudioUnitInitialize")]
    public static partial int Initialize(nint unit);

    [LibraryImport(Lib, EntryPoint = "AudioUnitUninitialize")]
    public static partial int Uninitialize(nint unit);

    [LibraryImport(Lib, EntryPoint = "AudioOutputUnitStart")]
    public static partial int Start(nint unit);

    [LibraryImport(Lib, EntryPoint = "AudioOutputUnitStop")]
    public static partial int Stop(nint unit);
}

/// <summary>
/// Apple output through an Audio Unit: RemoteIO on iOS (after setting up the AVAudioSession), the default output unit on macOS. The unit
/// pulls samples in a render callback that only copies from a ring the audio thread fills, so the callback allocates and locks nothing.
/// On iOS the backend reacts to the session's interruption notifications (calls, Siri, alarms): the unit stops, the mixer's clock holds
/// still, and sound resumes where it was when the interruption ends. The app lifecycle stops and starts the unit the same way.
/// </summary>
public sealed class AudioUnitBackend : IAudioBackend
{
    /// <summary>Creates the backend.</summary>
    /// <param name="category">The iOS session category; ignored on macOS.</param>
    public AudioUnitBackend(AudioSessionCategory category = AudioSessionCategory.Ambient) => Category = category;

    /// <summary>The iOS session category this backend sets.</summary>
    public AudioSessionCategory Category { get; }

    /// <inheritdoc/>
    public string Name => "audiounit";

    /// <inheritdoc/>
    public bool IsManual => false;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() => [new AudioDeviceInfo(string.Empty, "System default output", true)];

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
    {
        sink = null;
        try
        {
            var opened = AudioUnitSink.TryOpen(options, Category, out reason);
            sink = opened;
            return opened is not null;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            reason = "Audio Toolbox is not available on this system: " + e.Message;
            return false;
        }
    }

    /// <summary>Posts the same notification the system sends when another app or a call takes the audio session (<c>AVAudioSessionInterruptionNotification</c>), so the whole interruption path runs without a phone call. iOS only.</summary>
    /// <param name="began"><see langword="true"/> for the start of an interruption, <see langword="false"/> for its end.</param>
    /// <returns>Whether the notification was posted.</returns>
    public static bool SimulateInterruption(bool began) => AudioSession.PostInterruption(began);
}

internal sealed unsafe class AudioUnitSink : CallbackSink
{
    private static AudioUnitSink? s_current;

    private readonly nint _unit;
    private readonly GCHandle _self;
    private readonly AudioSessionCategory _category;
    private long _callbacks;
    private long _requested;
    private uint _lastFrames;
    private int _callbackPeakBits;
    private long _callbackAllocBase;
    private long _callbackAlloc;
    private int _callbackThread;
    private long _interruptions;
    private long _routeChanges;
    private int _interrupted;

    private AudioUnitSink(nint unit, AudioDeviceFormat format, int period, int buffer, AudioSessionCategory category)
        : base(format, period, buffer)
    {
        _unit = unit;
        _category = category;
        _self = GCHandle.Alloc(this);
    }

    protected override string DeviceState => Volatile.Read(ref _interrupted) != 0 ? "interrupted" : IsActive ? "started" : "stopped";

    protected override string? DeviceDetail
    {
        get
        {
            var route = OperatingSystem.IsIOS() ? AudioSession.Describe(_category) : "default output";
            return $"{route}; interruptions={Interlocked.Read(ref _interruptions)}; routeChanges={Interlocked.Read(ref _routeChanges)}; callbacks={Interlocked.Read(ref _callbacks)}; requestedFrames={Interlocked.Read(ref _requested)}; lastCallbackFrames={_lastFrames}; callbackPeak={BitConverter.Int32BitsToSingle(Volatile.Read(ref _callbackPeakBits)):0.####}; callbackAllocatedBytes={Interlocked.Read(ref _callbackAlloc)}";
        }
    }

    public static AudioUnitSink? TryOpen(AudioDeviceOptions options, AudioSessionCategory category, out string reason)
    {
        if (OperatingSystem.IsIOS() && !AudioSession.TryActivate(category, out reason))
        {
            return null;
        }

        var description = new AudioToolbox.ComponentDescription
        {
            Type = AudioToolbox.TypeOutput,
            SubType = OperatingSystem.IsIOS() ? AudioToolbox.SubTypeRemoteIo : AudioToolbox.SubTypeDefaultOutput,
            Manufacturer = AudioToolbox.ManufacturerApple,
        };
        var component = AudioToolbox.FindNext(0, &description);
        if (component == 0)
        {
            reason = "no output audio unit on this system";
            return null;
        }

        var status = AudioToolbox.InstanceNew(component, out var unit);
        if (status != 0)
        {
            reason = $"AudioComponentInstanceNew failed ({status})";
            return null;
        }

        // The unit converts from the format we hand it to whatever the hardware runs at, so the mixer keeps its rate across route changes.
        var channels = (uint)options.Channels;
        var asbd = new AudioToolbox.StreamDescription
        {
            SampleRate = options.SampleRate,
            FormatId = AudioToolbox.FormatLinearPcm,
            FormatFlags = AudioToolbox.FlagIsFloat | AudioToolbox.FlagIsPacked,
            BytesPerPacket = 4 * channels,
            FramesPerPacket = 1,
            BytesPerFrame = 4 * channels,
            ChannelsPerFrame = channels,
            BitsPerChannel = 32,
        };
        status = AudioToolbox.SetProperty(unit, AudioToolbox.PropertyStreamFormat, AudioToolbox.ScopeInput, 0, &asbd, (uint)sizeof(AudioToolbox.StreamDescription));
        if (status != 0)
        {
            _ = AudioToolbox.InstanceDispose(unit);
            reason = $"the output unit refused {options.SampleRate} Hz x {options.Channels} float ({status})";
            return null;
        }

        var period = OperatingSystem.IsIOS() ? Math.Max(64, (int)(AudioSession.IoBufferSeconds() * options.SampleRate)) : 512;
        var buffer = Math.Max(period * 2, options.LatencyMilliseconds * options.SampleRate / 1000);
        var sink = new AudioUnitSink(unit, new AudioDeviceFormat(options.SampleRate, options.Channels), period, buffer, category);
        var callback = new AudioToolbox.RenderCallback { Proc = (nint)(delegate* unmanaged<nint, nint, nint, uint, uint, AudioToolbox.BufferList*, int>)&Render, RefCon = GCHandle.ToIntPtr(sink._self) };
        status = AudioToolbox.SetProperty(unit, AudioToolbox.PropertySetRenderCallback, AudioToolbox.ScopeInput, 0, &callback, (uint)sizeof(AudioToolbox.RenderCallback));
        if (status == 0)
        {
            status = AudioToolbox.Initialize(unit);
        }

        if (status != 0)
        {
            sink.Dispose();
            reason = $"the output unit could not be set up ({status})";
            return null;
        }

        s_current = sink;
        if (OperatingSystem.IsIOS())
        {
            AudioSession.Attach(sink.OnInterruption, sink.OnRouteChange, sink.MarkLost);
        }

        if (!sink.Begin())
        {
            sink.Dispose();
            reason = "the output unit would not start";
            return null;
        }

        reason = string.Empty;
        return sink;
    }

    // Runs on the system's audio thread. It only copies from the ring and counts; an exception there would be fatal, so none can escape.
    [UnmanagedCallersOnly]
    private static int Render(nint refCon, nint flags, nint timestamp, uint bus, uint frames, AudioToolbox.BufferList* io)
    {
        try
        {
            var sink = (AudioUnitSink?)GCHandle.FromIntPtr(refCon).Target;
            if (sink is null || io->BufferCount == 0)
            {
                return 0;
            }

            var channels = sink.Format.Channels;
            var capacityFrames = io->ByteSize / (uint)(4 * channels);
            var samples = new Span<float>((void*)io->Data, (int)(Math.Min(frames, capacityFrames) * channels));
            if (sink.IsActive)
            {
                sink.Ring.Read(samples);
            }
            else
            {
                samples.Clear();
                *(uint*)flags |= AudioToolbox.ActionOutputIsSilence;
            }

            sink.CountCallback(frames, samples);
        }
        catch (Exception)
        {
            return 0;
        }

        return 0;
    }

    // The callback thread's own allocation counter, after a warm-up: the figure the harness shows for "allocates nothing".
    private void CountCallback(uint frames, Span<float> delivered)
    {
        _requested += frames;
        _lastFrames = frames;
        var peak = BitConverter.Int32BitsToSingle(_callbackPeakBits);
        foreach (var v in delivered)
        {
            if (MathF.Abs(v) > peak)
            {
                peak = MathF.Abs(v);
            }
        }

        Volatile.Write(ref _callbackPeakBits, BitConverter.SingleToInt32Bits(peak));
        var n = Interlocked.Increment(ref _callbacks);
        var thread = Environment.CurrentManagedThreadId;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        if (n <= 64 || thread != _callbackThread)
        {
            _callbackThread = thread;
            _callbackAllocBase = allocated;
            _callbackAlloc = 0;
            return;
        }

        Volatile.Write(ref _callbackAlloc, allocated - _callbackAllocBase);
    }

    private void OnInterruption(bool began)
    {
        if (began)
        {
            Interlocked.Increment(ref _interruptions);
            Volatile.Write(ref _interrupted, 1);
            NotifySystemSuspended(true);
        }
        else
        {
            // The session must be active again before the unit can run.
            AudioSession.TryActivate(_category, out _);
            Volatile.Write(ref _interrupted, 0);
            NotifySystemSuspended(false);
        }
    }

    private void OnRouteChange() => Interlocked.Increment(ref _routeChanges);

    protected override bool StartDevice()
    {
        if (OperatingSystem.IsIOS())
        {
            AudioSession.TryActivate(_category, out _);
        }

        return AudioToolbox.Start(_unit) == 0;
    }

    protected override void StopDevice() => _ = AudioToolbox.Stop(_unit);

    protected override void CloseDevice()
    {
        if (ReferenceEquals(s_current, this))
        {
            s_current = null;
            if (OperatingSystem.IsIOS())
            {
                AudioSession.Detach();
            }
        }

        _ = AudioToolbox.Uninitialize(_unit);
        _ = AudioToolbox.InstanceDispose(_unit);
        if (_self.IsAllocated)
        {
            _self.Free();
        }
    }
}
