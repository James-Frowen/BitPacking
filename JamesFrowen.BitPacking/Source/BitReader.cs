using System;

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

        int readBit;

        public int Position => this.readBit / 8;

        public int BitPosition => this.readBit;


        public BitReader(byte[] buffer, int offset, int byteLength)
        {
            this.buffer = buffer;
            this.startOffset = offset;
            this.endLength = byteLength;
        }
        public BitReader(byte[] buffer) : this(buffer, 0, buffer.Length) { }
        public BitReader(ArraySegment<byte> arraySegment) : this(arraySegment.Array, arraySegment.Offset, arraySegment.Count) { }

        public unsafe uint Read(int inBits)
        {
            // reading 0 is ok, but do nothing
            if (inBits == 0)
            {
                return 0;
            }

            if (inBits > MaxReadSize)
            {
                throw new ArgumentException($"bits must be less than {MaxReadSize}");
            }

            fixed (byte* ptr = &this.buffer[this.readBit / 8])
            {
                var longPtr = (ulong*)ptr;
                // get bufferValue
                var v = *longPtr;

                var shiftBits = this.readBit % 8;
                v >>= shiftBits;

                var mask = ((1ul << inBits) - 1);
                v &= mask;

                this.readBit += inBits;
                return (uint)v;
            }
        }
    }
}
