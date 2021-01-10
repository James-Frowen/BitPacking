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
        byte* ptr;
        ulong* ulongPtr;
        uint* uintPtr;
        readonly int bufferSize;
        readonly int bufferSizeBits;

        /// <summary>
        /// next bit to write to inside writeByte
        /// </summary>
        int writeBit;

        public int Length => Mathf.CeilToInt(this.writeBit / 8f);

        public BitWriter(int bufferSize)
        {
            this.bufferSize = bufferSize;
            this.bufferSizeBits = bufferSize * 8;
            var voidPtr = NewUnmanaged(bufferSize);
            this.ptr = (byte*)voidPtr;
            this.ulongPtr = (ulong*)voidPtr;
            this.uintPtr = (uint*)voidPtr;
            this.managedBuffer = new byte[bufferSize];
        }

        static void* NewUnmanaged(int elementCount)
        {
            var newSizeInBytes = elementCount;
            var newArrayPointer = (byte*)Marshal.AllocHGlobal(newSizeInBytes).ToPointer();

            ClearUnmanged(newArrayPointer, newSizeInBytes);

            return newArrayPointer;
        }

        static void ClearUnmanged(byte* ptr, int count)
        {
            for (var i = 0; i < count; i++)
                *(ptr + i) = 0;
        }

        public void Reset()
        {
            Array.Clear(this.managedBuffer, 0, this.Length);
            ClearUnmanged(this.ptr, this.Length);
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
            var value = (mask & inValue) << (this.writeBit & 0b1_1111);

            *(ulong*)(this.uintPtr + (this.writeBit >> 5)) |= value;

            this.writeBit = endCount;
        }

        public ArraySegment<byte> ToArraySegment()
        {
            var length = this.Length;
            for (var i = 0; i < length; i++)
                this.managedBuffer[i] = *(this.ptr + i);
            return new ArraySegment<byte>(this.managedBuffer, 0, length);
        }
    }
}
