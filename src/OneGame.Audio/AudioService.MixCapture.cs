namespace OneGame.Audio;

public sealed partial class AudioService
{
    private readonly Lock _mixGate = new();
    private readonly Lock _pumpGate = new();
    private MixCapture? _mixCapture;

    /// <summary>
    /// Takes exclusive control of the mixer timeline until disposed. The device receives silence while the caller
    /// renders exact sample counts, including in a live game. Existing voices continue from their current positions.
    /// </summary>
    /// <returns>A manual recording of the mixed output, after output limiting.</returns>
    /// <exception cref="InvalidOperationException">A recording is already active.</exception>
    public MixCapture BeginMixCapture()
    {
        // Wait for an in-flight stream fill separately: normal device rendering must never wait for stream I/O.
        lock (_pumpGate)
        lock (_mixGate)
        {
            if (_mixCapture is not null)
            {
                throw new InvalidOperationException("A mix capture is already active.");
            }

            var capture = new MixCapture(this, _options.Limiter);
            Volatile.Write(ref _mixCapture, capture);
            return capture;
        }
    }

    private void PumpCore()
    {
        lock (_pumpGate)
        {
            if (Volatile.Read(ref _mixCapture) is null)
            {
                Core.Pump();
            }
        }
    }

    // The gate waits for any in-flight device render before handing the timeline to a capture.
    private sealed class CaptureAwareSource(AudioService service) : IAudioRenderSource
    {
        public int SampleRate => service.Core.SampleRate;
        public int Channels => service.Core.Channels;

        public void Render(Span<float> interleaved)
        {
            if (Volatile.Read(ref service._mixCapture) is not null)
            {
                interleaved.Clear();
                return;
            }

            lock (service._mixGate)
            {
                if (service._mixCapture is null)
                {
                    service.Core.Render(interleaved);
                }
                else
                {
                    interleaved.Clear();
                }
            }
        }
    }

    /// <summary>An exclusive, manually stepped recording of an audio service. Dispose to return the timeline to its device.</summary>
    public sealed class MixCapture : IDisposable
    {
        private readonly AudioService _service;
        private readonly CaptureAudioBackend _backend;
        private readonly AudioOutput _output;
        private bool _disposed;

        internal MixCapture(AudioService service, OutputLimiterMode limiter)
        {
            _service = service;
            _backend = new CaptureAudioBackend();
            _output = AudioOutput.Create(_backend, new AudioDeviceOptions
            {
                SampleRate = service.Format.SampleRate,
                Channels = service.Format.Channels,
            }, limiter);
            _output.Start(service.Core);
            Recording = _backend.Sink!;
        }

        /// <summary>The samples rendered so far. Silence is retained too.</summary>
        public CaptureSink Recording { get; }

        /// <summary>Renders exactly this many sample frames, feeding streams synchronously before each slice.</summary>
        /// <param name="frames">Nonnegative sample frame count, within the recording's remaining capacity.</param>
        public void Advance(int frames)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(frames);
            ObjectDisposedException.ThrowIf(_disposed, this);
            if ((long)Recording.FramesCaptured + frames > CaptureSink.MaxFrames)
            {
                throw new ArgumentOutOfRangeException(nameof(frames), "The recording would exceed its sample limit.");
            }

            lock (_service._mixGate)
            {
                _output.Paused = _service.Output.Paused;
                for (var left = frames; left > 0; left -= 2048)
                {
                    _service.Core.Pump();
                    _output.Advance(Math.Min(left, 2048));
                }
            }
        }

        /// <summary>Returns timeline ownership to the service's device. The recording remains readable.</summary>
        public void Dispose()
        {
            lock (_service._mixGate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _output.Dispose();
                _backend.Dispose();
                Volatile.Write(ref _service._mixCapture, null);
            }
        }
    }
}
