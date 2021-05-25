using System;

namespace JamesFrowen.BitPacking
{
    /// <summary>
    /// Packs Uint to different sizes based on value
    /// <para>uses 1 bit for size</para>
    /// </summary>
    public class UIntVariablePacker2
    {
        readonly int smallBitCount;
        readonly int largeBitCount;
        // exclusive max
        readonly ulong smallMax;
        readonly ulong largeMax;

        public readonly ulong MaxValue;

        // for debugging
        public readonly int minBitCount;
        public readonly int maxBitCount;

        public UIntVariablePacker2(int smallBitCount, int largeBitCount)
        {
            this.smallBitCount = smallBitCount;
            this.largeBitCount = largeBitCount;

            smallMax = 1u << smallBitCount;
            largeMax = 1u << largeBitCount;

            MaxValue = largeMax - 1;

            minBitCount = smallBitCount + 1;
            maxBitCount = largeBitCount + 2;
        }

        public void Pack(BitWriter writer, uint value)
        {
            if (value < smallMax)
            {
                writer.Write(0, 1);
                writer.Write(value, smallBitCount);
            }
            else if (value < largeMax)
            {
                writer.Write(1, 1);
                writer.Write(value, largeBitCount);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be less than large limit");
            }
        }

        public ulong Unpack(BitReader reader)
        {
            ulong a = reader.Read(1);
            if (a == 0)
            {
                return reader.Read(smallBitCount);
            }
            else
            {
                return reader.Read(largeBitCount);
            }
        }

        public void PackNullable(BitWriter writer, uint? value)
        {
            bool hasValue = value.HasValue;
            writer.WriteBool(hasValue);
            if (hasValue)
            {
                Pack(writer, value.Value);
            }
        }

        public ulong? UnpackNullable(BitReader reader)
        {
            bool hasValue = reader.ReadBool();
            if (hasValue)
            {
                return Unpack(reader);
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Packs Uint to different sizes based on value
    /// </summary>
    public class UIntVariablePacker
    {
        readonly int smallBitCount;
        readonly int mediumBitCount;
        readonly int largeBitCount;
        // exclusive max
        readonly ulong smallMax;
        readonly ulong mediumMax;
        readonly ulong largeMax;

        public readonly ulong MaxValue;

        // for debugging
        public readonly int minBitCount;
        public readonly int maxBitCount;

        public UIntVariablePacker(int smallBitCount, int mediumBitCount, int largeBitCount)
        {
            this.smallBitCount = smallBitCount;
            this.mediumBitCount = mediumBitCount;
            this.largeBitCount = largeBitCount;

            smallMax = 1u << smallBitCount;
            mediumMax = 1u << mediumBitCount;
            largeMax = 1u << largeBitCount;

            MaxValue = largeMax - 1;

            minBitCount = smallBitCount + 1;
            maxBitCount = largeBitCount + 2;
        }

        public void Pack(BitWriter writer, uint value)
        {
            if (value < smallMax)
            {
                writer.Write(0, 1);
                writer.Write(value, smallBitCount);
            }
            else if (value < mediumMax)
            {
                writer.Write(1, 1);
                writer.Write(0, 1);
                writer.Write(value, mediumBitCount);
            }
            else if (value < largeMax)
            {
                writer.Write(1, 1);
                writer.Write(1, 1);
                writer.Write(value, largeBitCount);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be less than large limit");
            }
        }

        public ulong Unpack(BitReader reader)
        {
            ulong a = reader.Read(1);
            if (a == 0)
            {
                return reader.Read(smallBitCount);
            }
            else
            {
                ulong b = reader.Read(1);
                if (b == 0)
                {
                    return reader.Read(mediumBitCount);
                }
                else
                {
                    return reader.Read(largeBitCount);
                }
            }
        }

        public void PackNullable(BitWriter writer, uint? value)
        {
            bool hasValue = value.HasValue;
            writer.WriteBool(hasValue);
            if (hasValue)
            {
                Pack(writer, value.Value);
            }
        }

        public ulong? UnpackNullable(BitReader reader)
        {
            bool hasValue = reader.ReadBool();
            if (hasValue)
            {
                return Unpack(reader);
            }
            else
            {
                return null;
            }
        }
    }
}
