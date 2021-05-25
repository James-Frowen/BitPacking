using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace JamesFrowen.BitPacking
{
    public class BitReader
    {
        const int WORD_SIZE = sizeof(ulong) * 8;
        const int WORD_BITS_MASK = 0b11_1111;
        const int WORD_BITS_SHIFT = 6;

        readonly ulong[] buffer;
        public readonly int byteCapacity;

        /// <summary>
        /// Number of bytes in buffer
        /// </summary>
        int byteCount;
        /// <summary>Number of words to read from buffer</summary>
        int wordCount;

        int bitsInWord;
        int wordIndex;


        internal int Debug_BitsInWord => this.bitsInWord;
        internal int Debug_WorldIndex => this.wordIndex;

        public BitReader(int byteCapacity)
        {
            var ulongSize = (int)Math.Ceiling(byteCapacity / (float)sizeof(ulong));
            this.byteCapacity = ulongSize * sizeof(ulong);
            this.buffer = new ulong[ulongSize];
        }

        /// <summary>
        /// Copies bytes from <paramref name="segment"/> to internal buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToBuffer(ArraySegment<byte> segment) => this.CopyToBuffer(segment.Array, segment.Offset, segment.Count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToBuffer(byte[] arry) => this.CopyToBuffer(arry, 0, arry.Length);
        /// <summary>
        /// Copies bytes from <paramref name="array"/> to internal buffer
        /// </summary>
        public void CopyToBuffer(byte[] array, int offset, int length)
        {
            if (length > this.byteCapacity) throw new ArgumentException($"Length is over capacity of NetworkReader buffer, Length {length}, Capacity: {this.byteCapacity}", nameof(length));
            this.byteCount = length;
            this.wordCount = (int)Math.Ceiling(length / (float)sizeof(ulong));
            Buffer.BlockCopy(array, offset, this.buffer, 0, length);

            this.bitsInWord = 0;
            this.wordIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ulong BufferRead(int index)
        {
            if (index > this.wordCount) throw new EndOfStreamException();
            return this.buffer[index];
        }

        public ulong Read(int bits)
        {
            var value = this.BufferRead(this.wordIndex) >> this.bitsInWord;

            var nextBits = (this.bitsInWord + bits);
            if (nextBits >= WORD_SIZE)
            {
                this.wordIndex++;

                // if no more bits to read, then dont read incase of indexoutofbounds
                if (nextBits != WORD_SIZE)
                {

                    value |= this.BufferRead(this.wordIndex) << (WORD_SIZE - this.bitsInWord);
                }
            }

            // set bits, ensure it is less than 64
            this.bitsInWord = (nextBits & WORD_BITS_MASK);

            // enure value isn't over expected size
            var mask = ulong.MaxValue >> (WORD_SIZE - bits);
            return value & mask;
        }

        /// <summary>
        /// Pads to next byte.
        /// <para>This is useful before writing an array to the buffer</para>
        /// </summary>
        public void PadToNextByte()
        {
            var bits = 8 - (this.bitsInWord & 0b111);
            _ = this.Read(bits);
        }

        /// <summary>
        /// Reads bytes from internal buffer to <paramref name="dst"/>
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="dstOffset"></param>
        /// <param name="length"></param>
        public void ReadBytes(byte[] dst, int dstOffset, int length)
        {
            this.PadToNextByte();

            var srcOffset = this.wordIndex;
            var end = srcOffset + length;
            if (end > this.byteCount)
            {
                throw new EndOfStreamException($"Not enough bytes in buffer to read {length} bytes, Current ByteCount: {srcOffset}");
            }

            Buffer.BlockCopy(this.buffer, srcOffset, dst, dstOffset, length);

            var newBits = this.bitsInWord + (length * 8);
            this.wordCount += newBits >> WORD_BITS_SHIFT;
            this.bitsInWord = newBits & WORD_BITS_MASK;
        }
    }
}
