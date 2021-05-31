using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JamesFrowen.BitPacking
{
    public class QuaternionPacker
    {
        /// <summary>
        /// 1 / sqrt(2)
        /// </summary>
        const float MaxValue = 1f / 1.414214f;

        readonly int BitLength;

        /// <summary>
        /// Mathf.Pow(1, targetBitLength) - 1
        /// <para>
        /// Can also be used as mask
        /// </para>
        /// </summary>
        readonly uint UintMax;

        /// <summary>
        /// bit count per element writen
        /// </summary>
        public readonly int bitCountPerElement;

        /// <summary>
        /// total bit count for Quaternion
        /// <para>
        /// count = 3 * perElement + 2;
        /// </para>
        /// </summary>
        public readonly int bitCount;

        /// <param name="quaternionBitLength">10 per "smallest 3" is good enough for most people</param>
        public QuaternionPacker(int quaternionBitLength = 10)
        {
            this.BitLength = quaternionBitLength;
            // (this.BitLength - 1) because pack sign by itself
            this.UintMax = (1u << (this.BitLength - 1)) - 1u;
            this.bitCountPerElement = quaternionBitLength;
            this.bitCount = 2 + (quaternionBitLength * 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pack(NetworkWriter writer, Quaternion _value)
        {
            // make sure value is normalized (dont trust user given value, and math here assumes normalized)
            var x = _value.x;
            var y = _value.y;
            var z = _value.z;
            var w = _value.w;

            quickNormalize(ref x, ref y, ref z, ref w);

            FindLargestIndex(x, y, z, w, out var index, out var largest);

            GetSmallerDimensions(index, x, y, z, w, out var a, out var b, out var c);

            // largest needs to be positive to be calculated by reader 
            // if largest is negative flip sign of others because Q = -Q
            if (largest < 0)
            {
                a = -a;
                b = -b;
                c = -c;
            }

            writer.Write((uint)index, 2);
            writer.WriteFloatSigned(a, MaxValue, this.UintMax, this.BitLength);
            writer.WriteFloatSigned(b, MaxValue, this.UintMax, this.BitLength);
            writer.WriteFloatSigned(c, MaxValue, this.UintMax, this.BitLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void quickNormalize(ref float x, ref float y, ref float z, ref float w)
        {
            var dot =
                (x * x) +
                (y * y) +
                (z * z) +
                (w * w);
            const float allowedEpsilon = 1E-5f;
            const float minAllowed = 1 - allowedEpsilon;
            const float maxAllowed = 1 + allowedEpsilon;
            if (minAllowed > dot || maxAllowed < dot)
            {
                var dotSqrt = (float)Math.Sqrt(dot);
                // rotation is 0
                if (dotSqrt < allowedEpsilon)
                {
                    // identity
                    x = 0;
                    y = 0;
                    z = 0;
                    w = 1;
                }
                else
                {
                    x /= dotSqrt;
                    y /= dotSqrt;
                    z /= dotSqrt;
                    w /= dotSqrt;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FindLargestIndex(float x, float y, float z, float w, out int index, out float largest)
        {
            var x2 = x * x;
            var y2 = y * y;
            var z2 = z * z;
            var w2 = w * w;

            index = 0;
            var current = x2;
            largest = x;
            // check vs sq to avoid doing mathf.abs
            if (y2 > current)
            {
                index = 1;
                largest = y;
                current = y2;
            }
            if (z2 > current)
            {
                index = 2;
                largest = z;
                current = z2;
            }
            if (w2 > current)
            {
                index = 3;
                largest = w;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GetSmallerDimensions(int largestIndex, float x, float y, float z, float w, out float a, out float b, out float c)
        {
            switch (largestIndex)
            {
                case 0:
                    a = y;
                    b = z;
                    c = w;
                    return;
                case 1:
                    a = x;
                    b = z;
                    c = w;
                    return;
                case 2:
                    a = x;
                    b = y;
                    c = w;
                    return;
                case 3:
                    a = x;
                    b = y;
                    c = z;
                    return;
                default:
                    throw new IndexOutOfRangeException("Invalid Quaternion index!");
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion Unpack(NetworkReader reader)
        {
            Quaternion result;

            var index = reader.Read(2);

            var a = reader.ReadFloatSigned(MaxValue, this.UintMax, this.BitLength);
            var b = reader.ReadFloatSigned(MaxValue, this.UintMax, this.BitLength);
            var c = reader.ReadFloatSigned(MaxValue, this.UintMax, this.BitLength);

            result = FromSmallerDimensions(index, a, b, c);

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Quaternion FromSmallerDimensions(ulong largestIndex, float a, float b, float c)
        {
            var l2 = 1 - ((a * a) + (b * b) + (c * c));
            var largest = (float)Math.Sqrt(l2);
            // this Quaternion should already be normallized because of the way that largest is calculated
            // todo create test to validate that result is normalized
            switch (largestIndex)
            {
                case 0:
                    return new Quaternion(largest, a, b, c);
                case 1:
                    return new Quaternion(a, largest, b, c);
                case 2:
                    return new Quaternion(a, b, largest, c);
                case 3:
                    return new Quaternion(a, b, c, largest);
                default:
                    throw new IndexOutOfRangeException("Invalid Quaternion index!");

            }
        }
    }
}
