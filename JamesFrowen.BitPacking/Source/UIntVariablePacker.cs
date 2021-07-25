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

            this.smallMax = 1u << smallBitCount;
            this.largeMax = 1u << largeBitCount;

            this.MaxValue = this.largeMax - 1;

            this.minBitCount = smallBitCount + 1;
            this.maxBitCount = largeBitCount + 2;
        }

        public void Pack(NetworkWriter writer, uint value)
        {
            if (value < this.smallMax)
            {
                writer.Write(0, 1);
                writer.Write(value, this.smallBitCount);
            }
            else if (value < this.largeMax)
            {
                writer.Write(1, 1);
                writer.Write(value, this.largeBitCount);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be less than large limit");
            }
        }

        public ulong Unpack(NetworkReader reader)
        {
            ulong a = reader.Read(1);
            if (a == 0)
            {
                return reader.Read(this.smallBitCount);
            }
            else
            {
                return reader.Read(this.largeBitCount);
            }
        }

        public void PackNullable(NetworkWriter writer, uint? value)
        {
            bool hasValue = value.HasValue;
            writer.WriteBoolean(hasValue);
            if (hasValue)
            {
                this.Pack(writer, value.Value);
            }
        }

        public ulong? UnpackNullable(NetworkReader reader)
        {
            bool hasValue = reader.ReadBoolean();
            if (hasValue)
            {
                return this.Unpack(reader);
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

            this.smallMax = 1u << smallBitCount;
            this.mediumMax = 1u << mediumBitCount;
            this.largeMax = 1u << largeBitCount;

            this.MaxValue = this.largeMax - 1;

            this.minBitCount = smallBitCount + 1;
            this.maxBitCount = largeBitCount + 2;
        }

        public void Pack(NetworkWriter writer, uint value)
        {
            if (value < this.smallMax)
            {
                writer.Write(0, 1);
                writer.Write(value, this.smallBitCount);
            }
            else if (value < this.mediumMax)
            {
                writer.Write(1, 1);
                writer.Write(0, 1);
                writer.Write(value, this.mediumBitCount);
            }
            else if (value < this.largeMax)
            {
                writer.Write(1, 1);
                writer.Write(1, 1);
                writer.Write(value, this.largeBitCount);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be less than large limit");
            }
        }

        public ulong Unpack(NetworkReader reader)
        {
            ulong a = reader.Read(1);
            if (a == 0)
            {
                return reader.Read(this.smallBitCount);
            }
            else
            {
                ulong b = reader.Read(1);
                if (b == 0)
                {
                    return reader.Read(this.mediumBitCount);
                }
                else
                {
                    return reader.Read(this.largeBitCount);
                }
            }
        }

        public void PackNullable(NetworkWriter writer, uint? value)
        {
            bool hasValue = value.HasValue;
            writer.WriteBoolean(hasValue);
            if (hasValue)
            {
                this.Pack(writer, value.Value);
            }
        }

        public ulong? UnpackNullable(NetworkReader reader)
        {
            bool hasValue = reader.ReadBoolean();
            if (hasValue)
            {
                return this.Unpack(reader);
            }
            else
            {
                return null;
            }
        }
    }
}
