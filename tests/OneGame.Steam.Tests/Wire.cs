using System.Buffers.Binary;
using System.Text;

namespace OneGame.Steam.Tests;

/// <summary>Builds callback bytes the way the C++ library lays them out, from the field list and packing, so decode tests do not
/// rely on the code under test to compute offsets.</summary>
internal static class Wire
{
    /// <summary>Writes values (byte, int, uint, ulong or string for the 128-byte name array) with C struct rules for the packing.</summary>
    public static byte[] Pack(int pack, params object[] values)
    {
        var bytes = new List<byte>();
        var structAlign = 1;
        foreach (var v in values)
        {
            var (size, natural) = v switch
            {
                byte => (1, 1),
                int or uint => (4, 4),
                ulong => (8, 8),
                string => (128, 1),
                _ => throw new ArgumentException("unsupported field"),
            };
            var align = Math.Min(natural, pack);
            structAlign = Math.Max(structAlign, align);
            while (bytes.Count % align != 0)
            {
                bytes.Add(0xAA); // padding is not zeroed in real structs; the decoder must not care
            }

            var field = new byte[size];
            switch (v)
            {
                case byte b: field[0] = b; break;
                case int i: BinaryPrimitives.WriteInt32LittleEndian(field, i); break;
                case uint u: BinaryPrimitives.WriteUInt32LittleEndian(field, u); break;
                case ulong l: BinaryPrimitives.WriteUInt64LittleEndian(field, l); break;
                case string s: Encoding.UTF8.GetBytes(s).CopyTo(field, 0); break;
            }

            bytes.AddRange(field);
        }

        while (bytes.Count % structAlign != 0)
        {
            bytes.Add(0xAA);
        }

        return [.. bytes];
    }
}
