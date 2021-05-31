using System.Runtime.CompilerServices;

namespace JamesFrowen.BitPacking
{
    /// <summary>
    /// These are common methods for NetworkWriter, 
    /// <para>This class is for simple methods and types that dont need any optimization or packing, like Boolean.</para>
    /// <para>For other types like Vector3 they should have their own packer class to handle packing and unpacking</para>
    /// </summary>
    public static class BitPackingExtensions
    {
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
        public static void WriteFloatSigned(this NetworkWriter writer, float value, float signedMaxFloat, uint maxUint, int bitCount)
        {
            // 1 bit for sign
            var bitValueCount = bitCount - 1;

            if (value < 0)
            {
                writer.WriteBoolean(true);
                value = -value;
            }
            else
            {
                writer.WriteBoolean(false);
            }

            var uValue = Compression.ScaleToUInt(value, 0, signedMaxFloat, 0, maxUint);
            writer.Write(uValue, bitValueCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloatSigned(this NetworkReader reader, float maxFloat, uint maxUint, int bitCount)
        {
            // 1 bit for sign
            var bitValueCount = bitCount - 1;

            var signBit = reader.ReadBoolean();
            var valueBits = reader.Read(bitValueCount);

            var fValue = Compression.ScaleFromUInt(valueBits, 0, maxFloat, 0, maxUint);

            return signBit ? -fValue : fValue;
        }
    }
}
