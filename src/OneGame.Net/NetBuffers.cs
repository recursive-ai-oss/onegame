using System.Numerics;

namespace OneGame.Net;

/// <summary>
/// A small size-classed buffer pool. The shared array pool trims itself when the garbage collector runs and hands out arrays from
/// per-core stacks, so a steady stream of messages still allocates now and then; this one keeps what it is given, up to a cap.
/// Arrays it hands out are powers of two in length, so a returned array always finds its class again.
/// </summary>
internal static class NetBuffers
{
    private const int MinShift = 6; // 64 bytes
    private const int MaxShift = 27; // 128 MiB, above the largest message the options allow
    private static readonly Stack<byte[]>[] Classes = CreateClasses();

    /// <summary>Returns an array of at least <paramref name="size"/> bytes, with unspecified contents.</summary>
    public static byte[] Rent(int size)
    {
        var shift = Math.Max(MinShift, BitOperations.Log2((uint)Math.Max(1, size) - 1) + 1);
        if (shift > MaxShift)
        {
            return new byte[size];
        }

        var stack = Classes[shift - MinShift];
        lock (stack)
        {
            if (stack.TryPop(out var buf))
            {
                return buf;
            }
        }

        return new byte[1 << shift];
    }

    /// <summary>Gives an array back. Arrays that did not come from <see cref="Rent"/> are simply dropped.</summary>
    public static void Return(byte[] buffer)
    {
        if (buffer.Length == 0 || !BitOperations.IsPow2(buffer.Length))
        {
            return;
        }

        var shift = BitOperations.Log2((uint)buffer.Length);
        if (shift is < MinShift or > MaxShift)
        {
            return;
        }

        var stack = Classes[shift - MinShift];
        var cap = shift >= 16 ? 16 : 256;
        lock (stack)
        {
            if (stack.Count < cap)
            {
                stack.Push(buffer);
            }
        }
    }

    private static Stack<byte[]>[] CreateClasses()
    {
        var all = new Stack<byte[]>[MaxShift - MinShift + 1];
        for (var i = 0; i < all.Length; i++)
        {
            all[i] = new Stack<byte[]>();
        }

        return all;
    }
}
