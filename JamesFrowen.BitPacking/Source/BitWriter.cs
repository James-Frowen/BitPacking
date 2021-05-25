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
            int ulongSize = (int)Math.Ceiling(byteCapacity / (float)sizeof(ulong));
            this.byteCapacity = ulongSize * sizeof(ulong);
            buffer = new ulong[ulongSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetBitCount()
        {
            return wordCount * sizeof(ulong) * 8 + bitsInWord;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetByteCount()
        {
            return wordCount * sizeof(ulong) + (int)Math.Ceiling(bitsInWord / 8f);
        }

        /// <summary>   
        /// Resets length
        /// </summary>
        public void Reset()
        {
            bitsInWord = 0;
            wordCount = 0;
        }


        /// <summary>
        /// Copies internal buffer to new Array
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            int byteLength = GetByteCount();
            byte[] data = new byte[byteLength];
            Buffer.BlockCopy(buffer, 0, data, 0, byteLength);
            return data;
        }

        public int CopyToBuffer(byte[] outArray, int outOffset)
        {
            int byteLength = GetByteCount();
            Buffer.BlockCopy(buffer, 0, outArray, outOffset, byteLength);
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
            buffer[wordCount] |= value << bitsInWord;

            // if too long write to next word
            int nextBits = (bitsInWord + bits);
            if (nextBits >= WORD_SIZE)
            {
                wordCount++;
                if (nextBits != WORD_SIZE)
                {
                    // if nextBits is exactly 64, then we increment index,
                    // and write to buffer even tho 0 useful bits will be written
                    // todo test performance of write (below) vs if>WORD_SIZE
                    buffer[wordCount] = value >> (WORD_SIZE - bitsInWord);
                }
            }

            // set bits, ensure it is less than 64
            bitsInWord = (nextBits & WORD_BITS_MASK);
        }

        /// <summary>
        /// Pads to next byte.
        /// <para>This is useful before writing an array to the buffer</para>
        /// </summary>
        public void PadToNextByte()
        {
            int bits = 8 - (bitsInWord & 0b111);
            Write(0, bits);
        }

        /// <summary>
        /// Writes bytes from <paramref name="src"/> to internal buffer
        /// </summary>
        /// <param name="src"></param>
        /// <param name="srcOffset"></param>
        /// <param name="length"></param>
        public void WriteBytes(byte[] src, int srcOffset, int length)
        {
            PadToNextByte();

            int dstOffset = GetByteCount();
            int end = dstOffset + length;
            if (end > byteCapacity)
            {
                throw new IndexOutOfRangeException($"No room in buffer to write {length} bytes, Current ByteCount: {dstOffset}");
            }

            Buffer.BlockCopy(src, srcOffset, buffer, dstOffset, length);

            int newBits = bitsInWord + (length * 8);
            wordCount += newBits >> WORD_BITS_SHIFT;
            bitsInWord = newBits & WORD_BITS_MASK;
        }
    }
}
