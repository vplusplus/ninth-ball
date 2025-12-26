
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace NinthBall.Core
{
    /// <summary>
    /// Deterministic, cross-process and cross-platform hash code generation.
    /// </summary>
    internal static class PredictableHashCode
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
                int hash = 17;
                hash = hash * 31 + num1;
                hash = hash * 31 + num2;
                return hash;
            }
        }

        public static int Combine(int num1, int num2, int num3) => Combine(Combine(num1, num2), num3);
    }
}
