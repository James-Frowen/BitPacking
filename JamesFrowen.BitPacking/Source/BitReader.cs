using System;
using UnityEngine;

namespace JamesFrowen.BitPacking
{
    public class BitReader
    {
        // todo allow this to work with pooling

        // todo do we need this max read size?
        private const int MaxReadSize = 32;

        readonly byte[] buffer;
        readonly int startOffset;
        readonly int endLength;

        int readByte;
        int readBit;

        public int Position => this.readByte;

        public int BitPosition => this.readByte * 8 + this.readBit;

        public BitReader(byte[] buffer, int offset, int byteLength)
        {
            this.buffer = buffer;
            this.startOffset = offset;
            this.endLength = byteLength;
        }

        public BitReader(ArraySegment<byte> arraySegment) : this(arraySegment.Array, arraySegment.Offset, arraySegment.Count) { }

        public uint Read(int inBits)
        {
            if (inBits > MaxReadSize)
            {
                throw new ArgumentException($"bits must be less than {MaxReadSize}");
            }

            uint outValue = 0;
            var shiftOut = 0;
            do
            {
                // caclulate how many bits to read
                var toRead = Math.Min(inBits, 8 - this.readBit);

                uint bufferValue = this.buffer[this.startOffset + this.readByte];
                // shift to 0
                bufferValue >>= this.readBit;
                var mask = ((1u << toRead) - 1);
                bufferValue &= mask;

                outValue |= (bufferValue << shiftOut);
                shiftOut += toRead;

                this.readBit += toRead;
                inBits -= toRead;

                // if writeBit is at end of byte, then increment byte
                if (this.readBit >= 8)
                {
                    Debug.Assert(this.readBit > 8, "WriteBits should never be more than 8");
                    this.readBit = 0;
                    this.readByte++;
                }

                // keep going if there are still inbits to write
            }
            while (inBits > 0);

            return outValue;
        }
    }
}
