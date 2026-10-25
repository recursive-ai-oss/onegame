using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;

namespace OneGame.Core;

/// <summary>A source of unpredictable bytes, for platforms that supply their own (the browser's <c>crypto.getRandomValues</c>, say).</summary>
public interface IEntropySource
{
    /// <summary>Fills <paramref name="buffer"/> with unpredictable bytes. May be called from any thread.</summary>
    void Fill(Span<byte> buffer);
}

/// <summary>
/// Unpredictable bytes for connection ids, handshake secrets and keys. Never use it in simulation code: sim randomness comes from
/// named streams so replays match.
/// <para>
/// The default source avoids the runtime's crypto provider, which needs a system library that Android apps lack: Windows asks the
/// OS through the runtime (its provider is built in), every other system reads <c>/dev/urandom</c> (the kernel's generator on
/// Linux and Android, <c>arc4random</c> on Apple systems). A platform without that device installs its own source with
/// <see cref="Use"/> before the first network connection.
/// </para>
/// </summary>
public static class Entropy
{
    private static readonly object Gate = new();
    private static IEntropySource? _source;

    /// <summary>Replaces the source for the whole process.</summary>
    public static void Use(IEntropySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Volatile.Write(ref _source, source);
    }

    /// <summary>Fills <paramref name="buffer"/> with unpredictable bytes.</summary>
    /// <exception cref="PlatformNotSupportedException">No source is available on this platform and none was installed with <see cref="Use"/>.</exception>
    public static void Fill(Span<byte> buffer)
    {
        var source = Volatile.Read(ref _source);
        if (source is null)
        {
            lock (Gate)
            {
                source = _source ??= CreateDefault();
            }
        }

        source.Fill(buffer);
    }

    private static IEntropySource CreateDefault()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsSource();
        }

        if (File.Exists(UrandomSource.Path))
        {
            return new UrandomSource();
        }

        throw new PlatformNotSupportedException("No source of random bytes on this platform: call Entropy.Use with one.");
    }

    private sealed class WindowsSource : IEntropySource
    {
        // Windows' provider is part of the OS (BCrypt), so the runtime wrapper is safe there.
        public void Fill(Span<byte> buffer) => System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
    }

    private sealed class UrandomSource : IEntropySource, IDisposable
    {
        public const string Path = "/dev/urandom";
        private readonly FileStream _file = new(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.None);

        public void Fill(Span<byte> buffer)
        {
            lock (_file)
            {
                _file.ReadExactly(buffer);
            }
        }

        public void Dispose() => _file.Dispose();
    }
}
