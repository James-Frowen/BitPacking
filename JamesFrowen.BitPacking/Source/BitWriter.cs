using System;
using System.Runtime.CompilerServices;

namespace JamesFrowen.BitPacking
{
    public sealed unsafe class BitWriter
    {
        const int WORD_SIZE = sizeof(ulong) * 8;
        const int WORD_BITS_MASK = 0b11_1111;
        const int WORD_BITS_SHIFT = 6;


        readonly ulong[] buffer;
        readonly int byteCapacity;

        int bitsInWord;
        int wordCount;


        public BitWriter(int byteCapacity)
        {
            var ulongSize = (int)Math.Ceiling(byteCapacity / (float)sizeof(ulong));
            this.byteCapacity = ulongSize * sizeof(ulong);
            this.buffer = new ulong[ulongSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetByteCount()
        {
            return this.wordCount * sizeof(ulong) + (int)Math.Ceiling(this.bitsInWord / 8f);
        }

        /// <summary>
        /// Resets length
        /// </summary>
        public void Reset()
        {
            this.bitsInWord = 0;
            this.wordCount = 0;
        }


        /// <summary>
        /// Copies internal buffer to new Array
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            var byteLength = this.GetByteCount();
            var data = new byte[byteLength];
            Buffer.BlockCopy(this.buffer, 0, data, 0, byteLength);
            return data;
        }

        public int CopyToBuffer(byte[] outArray, int outOffset)
        {
            var byteLength = this.GetByteCount();
            Buffer.BlockCopy(this.buffer, 0, outArray, outOffset, byteLength);
            return byteLength;
        }

        /// <summary>
        /// Write <paramref name="bits"/> from <paramref name="value"/> to buffer
        /// </summary>
        /// <param name="value">what value to write</param>
        /// <param name="bits">number of bits of <paramref name="value"/> to write</param>
        public void Write(ulong value, int bits)
        {
            // write first part to current word
            this.buffer[this.wordCount] |= value << this.bitsInWord;

            // if too long write to next word
            var nextBits = (this.bitsInWord + bits);
            if (nextBits >= WORD_SIZE)
            {
                this.wordCount++;
                if (nextBits != WORD_SIZE)
                {
                    // if nextBits is exactly 64, then we increment index,
                    // and write to buffer even tho 0 useful bits will be written
                    // todo test performance of write (below) vs if>WORD_SIZE
                    this.buffer[this.wordCount] = value >> (WORD_SIZE - this.bitsInWord);
                }
            }

            // set bits, ensure it is less than 64
            this.bitsInWord = (nextBits & WORD_BITS_MASK);
        }

        /// <summary>
        /// Pads to next byte.
        /// <para>This is useful before writing an array to the buffer</para>
        /// </summary>
        public void PadToNextByte()
        {
            var bits = 8 - (this.bitsInWord & 0b111);
            this.Write(0, bits);
        }

        /// <summary>
        /// Writes bytes from <paramref name="src"/> to internal buffer
        /// </summary>
        /// <param name="src"></param>
        /// <param name="srcOffset"></param>
        /// <param name="length"></param>
        public void WriteBytes(byte[] src, int srcOffset, int length)
        {
            this.PadToNextByte();

            var dstOffset = this.GetByteCount();
            var end = dstOffset + length;
            if (end > this.byteCapacity)
            {
                throw new IndexOutOfRangeException($"No room in buffer to write {length} bytes, Current ByteCount: {dstOffset}");
            }

            Buffer.BlockCopy(src, srcOffset, this.buffer, dstOffset, length);

            var newBits = this.bitsInWord + (length * 8);
            this.wordCount += newBits >> WORD_BITS_SHIFT;
            this.bitsInWord = newBits & WORD_BITS_MASK;
        }
    }
}
