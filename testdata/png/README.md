# PNG decode fixtures

`inflate-input-boundary.png` is a 449x59 RGBA image (32,949 bytes) written by the OneImage 1.10.0 PNG encoder. It is a crop of a rendered 2D scene (abstract shapes, no text) with its lower rows replaced by a repeated row.

Its single zlib stream (32,892 bytes, 106,023 bytes inflated, two dynamic-Huffman blocks) is valid. Python's `zlib`, .NET's `ZLibStream` and Pillow decode it to identical bytes. OneImage 1.10.0 did not (fixed in 1.10.1: the inflater's fast loop refilled too few bits for a literal followed by a length/distance pair):

- `Image.Load(bytes).ToArray<Rgba32>()` throws, with a message that depends on how the input is split into IDAT chunks: `zlib Adler-32 checksum mismatch`, `Invalid scanline filter type 61`, `Invalid DEFLATE block type.` or `Invalid literal/length code.`. Splitting the IDAT data into two chunks fails at all but 13 of the 32,891 possible split offsets.
- Driving the inflater directly (checking its output against `ZLibStream`), the first wrong byte is always at output offset 85,172, whatever the output buffer size (1000 bytes to 1 MiB). It depends on the size of the input pieces: pieces of 1 to 22 bytes decode correctly, and larger sizes mostly do not (234 of 305 sizes tried fail, including the whole stream in one piece).

Around the failure (second block, bit offsets from the start of the zlib data) the stream holds a match of length 44 at distance 10,742 (32 bits: a 14-bit literal/length code, then the length and distance codes with their extra bits) at bit 242,361, a literal at 242,393, and then a match of length 9 at distance 3,582 at bit 242,402. The first wrong byte is inside that last match. Both blocks use literal/length codes up to 14 bits and distance codes of 9 and 11 bits.

`PngDecodeTests.ValidEncoderOutputDecodesLikeAnIndependentInflater` checks that `Png.Decode` now matches an independent inflater on it.
