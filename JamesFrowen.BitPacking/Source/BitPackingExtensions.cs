using System.Runtime.CompilerServices;

namespace JamesFrowen.BitPacking
{
    /// <summary>
    /// These are common methods for BitWriter, 
    /// <para>This class is for simple methods and types that dont need any optimization or packing, like Boolean.</para>
    /// <para>For other types like Vector3 they should have their own packer class to handle packing and unpacking</para>
    /// </summary>
    public static class BitPackingExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(this BitWriter writer, bool value)
        {
            writer.Write(value ? 1u : 0u, 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBool(this BitReader reader)
        {
            return reader.Read(1) == 1ul;
        }

        /// <summary>
        /// Writes float in range -max to +max, uses first bit to say the sign of value
        /// <para>sign bit: false is positive, true is negative</para>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="signedMaxFloat"></param>
        /// <param name="maxUint"></param>
        /// <param name="bitCount"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloatSigned(this BitWriter writer, float value, float signedMaxFloat, uint maxUint, int bitCount)
        {
            // 1 bit for sign
            var bitValueCount = bitCount - 1;

            if (value < 0)
            {
                writer.WriteBool(true);
                value = -value;
            }
            else
            {
                writer.WriteBool(false);
            }

            var uValue = Compression.ScaleToUInt(value, 0, signedMaxFloat, 0, maxUint);
            writer.Write(uValue, bitValueCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloatSigned(this BitReader reader, float maxFloat, uint maxUint, int bitCount)
        {
            // 1 bit for sign
            var bitValueCount = bitCount - 1;

            var signBit = reader.ReadBool();
            var valueBits = reader.Read(bitValueCount);

            var fValue = Compression.ScaleFromUInt(valueBits, 0, maxFloat, 0, maxUint);

            return signBit ? -fValue : fValue;
        }
    }
}
