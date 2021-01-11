using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JamesFrowen.BitPacking
{
    public unsafe class BitWriter
    {
        // todo allow this to work with pooling

        byte[] managedBuffer;
        IntPtr intPtr;
        ulong* ulongPtr;
        readonly int bufferSize;
        readonly int bufferSizeBits;

        /// <summary>
        /// next bit to write to inside writeByte
        /// </summary>
        int writeBit;

        public int ByteLength => Mathf.CeilToInt(this.writeBit / 8f);

        public BitWriter(int bufferSize)
        {
            // round to up multiple of 8
            bufferSize = ((bufferSize / 8) + 1) * 8;
            this.bufferSize = bufferSize;
            this.bufferSizeBits = bufferSize * 8;

            this.intPtr = Marshal.AllocHGlobal(bufferSize);
            var voidPtr = this.intPtr.ToPointer();
            this.ulongPtr = (ulong*)voidPtr;

            this.managedBuffer = new byte[bufferSize];

            ClearUnmanged(this.ulongPtr, bufferSize / 8);
        }

        static void ClearUnmanged(ulong* longPtr, int count)
        {
            for (var i = 0; i < count; i++)
                *(longPtr + i) = 0;
        }

        public void Reset()
        {
            Array.Clear(this.managedBuffer, 0, this.ByteLength);
            ClearUnmanged(this.ulongPtr, (this.ByteLength / 8) + 1);
            this.writeBit = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint inValue, int inBits)
        {
            if (inBits == 0) { throw new ArgumentException("inBits should not be zero", nameof(inBits)); }

            const int MaxWriteSize = 32;
            if (inBits > MaxWriteSize) { throw new ArgumentException($"inBits should not be greater than {MaxWriteSize}", nameof(inBits)); }

            var endCount = this.writeBit + inBits;
            if (endCount > this.bufferSizeBits) { throw new EndOfStreamException(); }

            var mask = (1ul << inBits) - 1;
            var maskedValue = mask & inValue;
            // writeBit= 188
            // remainder = 60
            var remainder = this.writeBit & 0b11_1111;
            // true
            var isOver32 = (remainder >> 5) == 1;

            // shifted 60, only writes first 4 bits
            var value = maskedValue << remainder;
            // write first 4 to first ulong
            *(this.ulongPtr + (this.writeBit >> 6)) |= value;

            if (isOver32)
            {
                // shift to remove first 4
                var v2 = maskedValue >> (64 - remainder);
                // write rest to second ulong
                *(this.ulongPtr + (this.writeBit >> 6) + 1) |= v2;
            }

            this.writeBit = endCount;
        }

        public ArraySegment<byte> ToArraySegment()
        {
            fixed (byte* mPtr = &this.managedBuffer[0])
            {
                for (var i = 0; i < ((this.writeBit >> 6) + 1); i++)
                {
                    *(((ulong*)mPtr) + i) = *(this.ulongPtr + i);
                }
            }

            var length = this.ByteLength;
            return new ArraySegment<byte>(this.managedBuffer, 0, length);
        }
    }
}
