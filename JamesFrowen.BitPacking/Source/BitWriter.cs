using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JamesFrowen.BitPacking
{
    public class BitWriter
    {
        // todo allow this to work with pooling
        // todo try writing to buffer directly instead of using scratch

        // todo do we need this max write size?
        private const int MaxWriteSize = 32;

        readonly byte[] buffer;

        /// <summary>
        /// next byte to write to
        /// <para>index in buffer</para>
        /// </summary>
        int writeByte;
        /// <summary>
        /// next bit to write to inside writeByte
        /// </summary>
        int writeBit;

        public int Length => this.writeByte + Mathf.CeilToInt(this.writeBit / 8f);

        public BitWriter(int bufferSize) : this(new byte[bufferSize]) { }
        public BitWriter(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public void Reset()
        {
            this.writeByte = 0;
            this.writeBit = 0;
            // +1 because last might not be full word
            Array.Clear(this.buffer, 0, this.writeByte + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint inValue, int inBits)
        {
            //Console.WriteLine($"{value},{bits}");
            if (inBits > MaxWriteSize)
            {
                throw new ArgumentException($"bits must be less than {MaxWriteSize}");
            }

            do
            {
                // shift value and add to array
                this.buffer[this.writeByte] |= (byte)(inValue << this.writeBit);

                // caclulate how many bits were written
                var written = Math.Min(inBits, 8 - this.writeBit);
                // keep track of bits in buffer/incoming
                this.writeBit += written;
                inBits -= written;

                // shift incoming
                inValue >>= written;

                // if writeBit is at end of byte, then increment byte
                if (this.writeBit >= 8)
                {
                    Debug.Assert(this.writeBit > 8, "WriteBits should never be more than 8");
                    this.writeBit = 0;
                    this.writeByte++;
                }

                // keep going if there are still inbits to write
            }
            while (inBits > 0);
        }

        public ArraySegment<byte> ToArraySegment()
        {
            return new ArraySegment<byte>(this.buffer, 0, this.Length);
        }
    }
}
