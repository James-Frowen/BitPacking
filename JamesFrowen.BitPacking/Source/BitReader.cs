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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBitPosition()
        {
            return wordIndex * 64 + bitsInWord;
        }

        public BitReader(int byteCapacity)
        {
            int ulongSize = (int)Math.Ceiling(byteCapacity / (float)sizeof(ulong));
            this.byteCapacity = ulongSize * sizeof(ulong);
            buffer = new ulong[ulongSize];
        }

        /// <summary>
        /// Copies bytes from <paramref name="segment"/> to internal buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToBuffer(ArraySegment<byte> segment) => CopyToBuffer(segment.Array, segment.Offset, segment.Count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToBuffer(byte[] arry) => CopyToBuffer(arry, 0, arry.Length);
        /// <summary>
        /// Copies bytes from <paramref name="array"/> to internal buffer
        /// </summary>
        public void CopyToBuffer(byte[] array, int offset, int length)
        {
            if (length > byteCapacity) throw new ArgumentException($"Length is over capacity of NetworkReader buffer, Length {length}, Capacity: {byteCapacity}", nameof(length));
            byteCount = length;
            wordCount = (int)Math.Ceiling(length / (float)sizeof(ulong));
            Buffer.BlockCopy(array, offset, buffer, 0, length);

            bitsInWord = 0;
            wordIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ulong BufferRead(int index)
        {
            if (index > wordCount) throw new EndOfStreamException();
            return buffer[index];
        }

        public ulong Read(int bits)
        {
            ulong value = BufferRead(wordIndex) >> bitsInWord;

            int nextBits = (bitsInWord + bits);
            if (nextBits >= WORD_SIZE)
            {
                wordIndex++;

                // if no more bits to read, then dont read incase of indexoutofbounds
                if (nextBits != WORD_SIZE)
                {

                    value |= BufferRead(wordIndex) << (WORD_SIZE - bitsInWord);
                }
            }

            // set bits, ensure it is less than 64
            bitsInWord = (nextBits & WORD_BITS_MASK);

            // enure value isn't over expected size
            ulong mask = ulong.MaxValue >> (WORD_SIZE - bits);
            return value & mask;
        }

        /// <summary>
        /// Pads to next byte.
        /// <para>This is useful before writing an array to the buffer</para>
        /// </summary>
        public void PadToNextByte()
        {
            int bits = 8 - (bitsInWord & 0b111);
            _ = Read(bits);
        }

        /// <summary>
        /// Reads bytes from internal buffer to <paramref name="dst"/>
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="dstOffset"></param>
        /// <param name="length"></param>
        public void ReadBytes(byte[] dst, int dstOffset, int length)
        {
            PadToNextByte();

            int srcOffset = wordIndex;
            int end = srcOffset + length;
            if (end > byteCount)
            {
                throw new EndOfStreamException($"Not enough bytes in buffer to read {length} bytes, Current ByteCount: {srcOffset}");
            }

            Buffer.BlockCopy(buffer, srcOffset, dst, dstOffset, length);

            int newBits = bitsInWord + (length * 8);
            wordCount += newBits >> WORD_BITS_SHIFT;
            bitsInWord = newBits & WORD_BITS_MASK;
        }
    }
}
