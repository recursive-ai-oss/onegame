using System.Buffers.Binary;

namespace OneGame.Gltf;

/// <summary>Splits a binary glTF (GLB) into its JSON and binary chunks. Every length is checked against the data before a slice is made.</summary>
internal static class GlbContainer
{
    private const uint Magic = 0x46546C67; // "glTF"
    private const uint JsonChunk = 0x4E4F534A; // "JSON"
    private const uint BinChunk = 0x004E4942; // "BIN\0"

    internal static bool IsGlb(ReadOnlySpan<byte> data) => data.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(data) == Magic;

    /// <summary>Reads the container (glTF 2.0 section "Binary glTF Layout").</summary>
    internal static void Split(ReadOnlyMemory<byte> data, GltfLimits limits, out ReadOnlyMemory<byte> json, out ReadOnlyMemory<byte> bin)
    {
        var span = data.Span;
        json = default;
        bin = default;
        if (span.Length < 12 + 8)
        {
            throw new GltfException("glb_invalid", "The file is too short to be a GLB.");
        }

        var version = BinaryPrimitives.ReadUInt32LittleEndian(span[4..]);
        if (version != 2)
        {
            throw new GltfException("unsupported_version", $"GLB container version {version} is not supported; only 2.");
        }

        var total = BinaryPrimitives.ReadUInt32LittleEndian(span[8..]);
        if (total > span.Length)
        {
            throw new GltfException("glb_invalid", $"The header says {total} bytes but the file has {span.Length} (truncated).");
        }

        if (total < 12 + 8)
        {
            throw new GltfException("glb_invalid", "The header length is smaller than one chunk.");
        }

        // Anything after the declared length is ignored, like trailing padding.
        long pos = 12;
        var first = true;
        var haveJson = false;
        var haveBin = false;
        while (pos + 8 <= total)
        {
            var length = BinaryPrimitives.ReadUInt32LittleEndian(span[(int)pos..]);
            var type = BinaryPrimitives.ReadUInt32LittleEndian(span[(int)(pos + 4)..]);
            pos += 8;
            if (length > total - pos)
            {
                throw new GltfException("glb_invalid", $"A chunk declares {length} bytes but only {total - pos} remain.");
            }

            if ((length & 3) != 0)
            {
                throw new GltfException("glb_invalid", "A chunk length is not a multiple of 4.");
            }

            if (first && type != JsonChunk)
            {
                throw new GltfException("glb_invalid", "The first chunk must be JSON.");
            }

            first = false;
            var slice = data.Slice((int)pos, (int)length);
            if (type == JsonChunk)
            {
                if (haveJson)
                {
                    throw new GltfException("glb_invalid", "There is more than one JSON chunk.");
                }

                if (length > limits.MaxJsonBytes)
                {
                    throw new GltfException("limit_exceeded", $"The JSON chunk is {length} bytes; the limit is {limits.MaxJsonBytes}.");
                }

                json = slice;
                haveJson = true;
            }
            else if (type == BinChunk && !haveBin)
            {
                bin = slice;
                haveBin = true;
            }

            // Other chunk types are skipped, as the specification asks.
            pos += length;
        }

        if (!haveJson)
        {
            throw new GltfException("glb_invalid", "There is no JSON chunk.");
        }
    }
}
