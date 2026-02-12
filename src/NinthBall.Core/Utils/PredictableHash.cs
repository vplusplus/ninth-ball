
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace NinthBall.Utils
{
    /// <summary>
    /// Deterministic, cross-process and cross-platform hash code generation.
    /// </summary>
    static class PredictableHashCode
    {
        public static int GetPredictableHashCode(this string something)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(something ?? string.Empty));

            var uint1 = BinaryPrimitives.ReadUInt32LittleEndian(hashBytes.AsSpan(0));
            var uint2 = BinaryPrimitives.ReadUInt32LittleEndian(hashBytes.AsSpan(4));
            var uint3 = BinaryPrimitives.ReadUInt32LittleEndian(hashBytes.AsSpan(8));
            var uint4 = BinaryPrimitives.ReadUInt32LittleEndian(hashBytes.AsSpan(12));

            return (int)(uint1 ^ uint2 ^ uint3 ^ uint4);
        }

        public static int Combine(int num1, int num2)
        {
            unchecked
            {
                uint h1 = (uint)num1;
                uint h2 = (uint)num2;

                // Mix h1
                h1 ^= h1 >> 16;                 // Mix upper 16 bits into lower 16 bits
                h1 *= 0x85ebca6b;               // Multiply by: 2,244,714,091 (a large prime-like constant)
                h1 ^= h1 >> 13;                 // Further mix bits, different offset than before (13 vs 16)
                h1 *= 0xc2b2ae35;               // Multiply by: 3,266,489,917 (another mixing constant)
                h1 ^= h1 >> 16;                 // Final avalanche pass    

                // Combine with h2
                h1 ^= h2;                       // Incorporate num2 into the hash
                h1 *= 0x1b873593;               // Multiply by: 461,845,907 (MurmurHash3's c1 constant). Spread the combined bits
                h1 = (h1 << 13) | (h1 >> 19);   // Non-linear transformation (rotation is not the same as shift). Bits wrap around, creating more mixing
                h1 = h1 * 5 + 0xe6546b64;       // Final scrambling with a simple operation

                return (int)h1;
            }
        }

        public static int Combine(int num1, int num2, int num3) => Combine(Combine(num1, num2), num3);
    }
}
