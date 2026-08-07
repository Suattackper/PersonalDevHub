using System.Buffers.Binary;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Managed MD5 (RFC 1321).
/// <para>
/// <see cref="System.Security.Cryptography.MD5"/> is unsupported on the browser runtime — WebAssembly
/// delegates hashing to SubtleCrypto, which deliberately does not offer MD5. DevHub still needs it
/// for checksums and legacy interop, so the algorithm is implemented here.
/// </para>
/// <para>
/// MD5 is broken for any security purpose: collisions are trivially constructible. Use it to match
/// a legacy system's output, never to protect anything.
/// </para>
/// </summary>
public static class Md5Digest
{
    /// <summary>Per-round shift amounts, as specified by RFC 1321.</summary>
    private static readonly int[] Shifts =
    [
        7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
        5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20,
        4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
        6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
    ];

    /// <summary>Sine-derived constants: floor(abs(sin(i + 1)) * 2^32).</summary>
    private static readonly uint[] SineTable = BuildSineTable();

    /// <summary>Computes the 16-byte MD5 digest of <paramref name="input"/>.</summary>
    public static byte[] HashData(ReadOnlySpan<byte> input)
    {
        Span<uint> state = [0x67452301u, 0xEFCDAB89u, 0x98BADCFEu, 0x10325476u];

        var padded = Pad(input);
        Span<uint> block = stackalloc uint[16];

        for (var offset = 0; offset < padded.Length; offset += 64)
        {
            for (var word = 0; word < 16; word++)
            {
                block[word] = BinaryPrimitives.ReadUInt32LittleEndian(padded.AsSpan(offset + (word * 4), 4));
            }

            Transform(state, block);
        }

        var digest = new byte[16];
        for (var index = 0; index < 4; index++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(index * 4, 4), state[index]);
        }

        return digest;
    }

    /// <summary>Appends the 0x80 marker, zero padding and the 64-bit little-endian bit length.</summary>
    private static byte[] Pad(ReadOnlySpan<byte> input)
    {
        var paddedLength = ((input.Length + 8) / 64 * 64) + 64;
        var padded = new byte[paddedLength];

        input.CopyTo(padded);
        padded[input.Length] = 0x80;

        BinaryPrimitives.WriteUInt64LittleEndian(padded.AsSpan(paddedLength - 8, 8), (ulong)input.Length * 8);

        return padded;
    }

    private static void Transform(Span<uint> state, ReadOnlySpan<uint> block)
    {
        var (a, b, c, d) = (state[0], state[1], state[2], state[3]);

        for (var step = 0; step < 64; step++)
        {
            uint f;
            int wordIndex;

            switch (step / 16)
            {
                case 0:
                    f = (b & c) | (~b & d);
                    wordIndex = step;
                    break;

                case 1:
                    f = (d & b) | (~d & c);
                    wordIndex = ((5 * step) + 1) % 16;
                    break;

                case 2:
                    f = b ^ c ^ d;
                    wordIndex = ((3 * step) + 5) % 16;
                    break;

                default:
                    f = c ^ (b | ~d);
                    wordIndex = 7 * step % 16;
                    break;
            }

            var temp = d;
            d = c;
            c = b;
            b = unchecked(b + RotateLeft(unchecked(a + f + SineTable[step] + block[wordIndex]), Shifts[step]));
            a = temp;
        }

        state[0] = unchecked(state[0] + a);
        state[1] = unchecked(state[1] + b);
        state[2] = unchecked(state[2] + c);
        state[3] = unchecked(state[3] + d);
    }

    private static uint RotateLeft(uint value, int count) => (value << count) | (value >> (32 - count));

    private static uint[] BuildSineTable()
    {
        var table = new uint[64];

        for (var index = 0; index < table.Length; index++)
        {
            table[index] = (uint)(Math.Abs(Math.Sin(index + 1)) * 4294967296d);
        }

        return table;
    }
}
