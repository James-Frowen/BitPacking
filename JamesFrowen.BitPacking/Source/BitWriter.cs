using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JamesFrowen.BitPacking
{
    public class BitWriter
    {
        // todo allow this to work with pooling

        // todo do we need this max write size?
        private const int MaxWriteSize = 32;

        readonly byte[] buffer;

        /// <summary>
        /// next bit to write to inside writeByte
        /// </summary>
        int writeBit;

        public int Length => Mathf.CeilToInt(this.writeBit / 8f);

        public BitWriter(int bufferSize) : this(new byte[bufferSize]) { }
        public BitWriter(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public void Reset()
        {
            this.writeBit = 0;
            // +1 because last might not be full word
            Array.Clear(this.buffer, 0, Math.Min(this.Length, this.buffer.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(uint inValue, int inBits)
        {
            if (inBits > MaxWriteSize)
            {
                throw new ArgumentException($"bits must be less than {MaxWriteSize}");
            }

            fixed (byte* ptr = &this.buffer[this.writeBit / 8])
            {
                var longPtr = (ulong*)ptr;
                // get bufferValue
                var v = *longPtr;
                // add inValue
                var shiftBits = this.writeBit % 8;
                v |= inValue << shiftBits;
                // set bufferValue
                *longPtr = v;

                this.writeBit += inBits;
            }
        }

        public ArraySegment<byte> ToArraySegment()
        {
            return new ArraySegment<byte>(this.buffer, 0, this.Length);
        }
    }
}
