using System;
using System.Runtime.CompilerServices;

namespace JamesFrowen.BitPacking
{
    public class BitCountHelper
    {
        /// <summary>
        /// returns how many bits are required for inclusive range of min to max
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BitCountFromRange(ulong min, ulong max)
        {
            if (min >= max) { throw new ArgumentException($"Min:{min} is greater or equal to than Max:{max}"); }

            return BitCountFromRange(max - min);
        }

        /// <summary>
        /// returns how many bits are required for inclusive range
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BitCountFromRange(ulong range)
        {
            if (range == 0u) { throw new ArgumentException($"Range is zero"); }

            // plus 1 because range is inclusive
            range++;

            double logBase2 = Math.Log(range, 2);

            return (int)Math.Ceiling(logBase2);
        }
    }
}
