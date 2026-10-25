using System.Runtime.InteropServices;

namespace OneGame.Platform;

/// <summary>The few C library calls the Linux backends share.</summary>
internal static unsafe partial class Libc
{
    public const int O_RDONLY = 0;
    public const int O_RDWR = 2;
    public const int O_NONBLOCK = 0x800;
    public const int O_CLOEXEC = 0x80000;
    public const int EAGAIN = 11;
    public const int PROT_READ = 1;
    public const int PROT_WRITE = 2;
    public const int MAP_SHARED = 1;
    public const int MAP_PRIVATE = 2;
    public const int POLLIN = 1;
    public const int POLLOUT = 4;

    [StructLayout(LayoutKind.Sequential)]
    public struct PollFd
    {
        public int Fd;
        public short Events;
        public short Revents;
    }

    [LibraryImport("libc", EntryPoint = "open", SetLastError = true)]
    public static partial int Open(byte* path, int flags);

    [LibraryImport("libc", EntryPoint = "close")]
    public static partial int Close(int fd);

    [LibraryImport("libc", EntryPoint = "read", SetLastError = true)]
    public static partial nint Read(int fd, void* buffer, nuint count);

    [LibraryImport("libc", EntryPoint = "write", SetLastError = true)]
    public static partial nint Write(int fd, void* buffer, nuint count);

    // ioctl is variadic in C, but the calling convention for one pointer-sized extra argument matches a fixed signature on Linux x64 and arm64.
    [LibraryImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    public static partial int Ioctl(int fd, nuint request, void* argument);

    [LibraryImport("libc", EntryPoint = "pipe2", SetLastError = true)]
    public static partial int Pipe2(int* fds, int flags);

    [LibraryImport("libc", EntryPoint = "poll", SetLastError = true)]
    public static partial int Poll(PollFd* fds, nuint count, int timeoutMilliseconds);

    [LibraryImport("libc", EntryPoint = "mmap", SetLastError = true)]
    public static partial void* Mmap(void* address, nuint length, int protection, int flags, int fd, nint offset);

    [LibraryImport("libc", EntryPoint = "munmap")]
    public static partial int Munmap(void* address, nuint length);

    [LibraryImport("libc", EntryPoint = "memfd_create", SetLastError = true)]
    public static partial int MemfdCreate(byte* name, uint flags);

    [LibraryImport("libc", EntryPoint = "ftruncate", SetLastError = true)]
    public static partial int Ftruncate(int fd, nint length);

    /// <summary>Opens a path read-only and non-blocking; returns -1 on failure.</summary>
    public static int OpenNonBlocking(string path, bool write = false)
    {
        Span<byte> bytes = stackalloc byte[512];
        var n = System.Text.Encoding.UTF8.GetBytes(path, bytes[..^1]);
        bytes[n] = 0;
        fixed (byte* p = bytes)
        {
            return Open(p, (write ? O_RDWR : O_RDONLY) | O_NONBLOCK | O_CLOEXEC);
        }
    }

    /// <summary>Builds a Linux ioctl request number (asm-generic/ioctl.h).</summary>
    public static nuint IoC(uint direction, uint type, uint number, uint size) =>
        (direction << 30) | (size << 16) | (type << 8) | number;
}
