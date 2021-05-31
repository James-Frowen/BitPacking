/*
MIT License

Copyright (c) 2021 James Frowen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace JamesFrowen.BitPacking
{
    /// <summary>
    /// Binary stream Writer. Supports simple types, buffers, arrays, structs, and nested types
    /// <para>Use <see cref="NetworkWriterPool.GetWriter">NetworkWriter.GetWriter</see> to reduce memory allocation</para>
    /// </summary>
    public unsafe class NetworkWriter
    {
        byte[] managedBuffer;
        GCHandle handle;
        ulong* longPtr;
        int bitCapacity;
        bool disposed;

        int bitPosition;


        public int ByteLength
        {
            // rounds up to nearest 8
            // add to 3 last bits,
            //   if any are 1 then it will roll over 4th bit.
            //   if all are 0, then nothing happens 
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (this.bitPosition + 0b111) >> 3;
        }

        public int BitPosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.bitPosition;
        }

        public NetworkWriter(int minByteCapacity)
        {
            var ulongCapacity = Mathf.CeilToInt(minByteCapacity / (float)sizeof(ulong));
            var byteCapacity = ulongCapacity * sizeof(ulong);
            this.bitCapacity = byteCapacity * 8;
            this.managedBuffer = new byte[byteCapacity];
            this.handle = GCHandle.Alloc(this.managedBuffer, GCHandleType.Pinned);
            this.longPtr = (ulong*)this.handle.AddrOfPinnedObject();
        }
        ~NetworkWriter()
        {
            this.FreeHandle();
        }
        /// <summary>
        /// Frees the handle for the buffer
        /// <para>In order for <see cref="PooledNetworkWriter"/> to work This class can not have <see cref="IDisposable"/>. Instead we call this method from Finalze</para>
        /// </summary>
        void FreeHandle()
        {
            if (this.disposed) return;

            this.handle.Free();
            this.longPtr = null;
            this.disposed = true;
        }

        public void Reset()
        {
            this.bitPosition = 0;
        }

        /// <summary>
        /// Copies internal buffer to new Array
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            var data = new byte[this.ByteLength];
            // todo benchmark and optimize (can we copy from ptr faster
            Buffer.BlockCopy(this.managedBuffer, 0, data, 0, this.ByteLength);
            return data;
        }
        public ArraySegment<byte> ToArraySegment()
        {
            // todo clear extra bits in byte (dont want last byte to have useless data)
            return new ArraySegment<byte>(this.managedBuffer, 0, this.ByteLength);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckNewLength(int newLength)
        {
            if (newLength > this.bitCapacity)
            {
                throw new IndexOutOfRangeException();
            }
        }

        private void PadToByte()
        {
            // todo do we need to clear skipped bits?
            this.bitPosition += this.bitPosition & 0b111;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBoolean(bool value)
        {
            this.WriteBoolean(value ? 1UL : 0UL);
        }
        /// <summary>
        /// Writes first bit of <paramref name="value"/> to buffer
        /// </summary>
        /// <param name="value"></param>
        public void WriteBoolean(ulong value)
        {
            var newPosition = this.bitPosition + 1;
            this.CheckNewLength(newPosition);

            var bitsInLong = this.bitPosition & 0b11_1111;

            var ptr = (this.longPtr + (this.bitPosition >> 6));
            *ptr = (
                *ptr & (
                    // start with 0000_0001
                    // shift by number in bit, eg 5 => 0010_0000
                    // then not 1101_1111
                    ~(1UL << bitsInLong)
                )
            ) | ((value & 0b1) << bitsInLong);

            this.bitPosition = newPosition;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short value)
        {
            this.WriteUInt16((ushort)value);
        }
        public void WriteUInt16(ushort value)
        {
            var newPosition = this.bitPosition + 32;
            this.CheckNewLength(newPosition);

            ulong longValue = value;

            var bitsInLong = this.bitPosition & 0b11_1111;
            var bitsLeft = 64 - bitsInLong;

            if (bitsLeft >= 16)
            {
                var ptr = (this.longPtr + (this.bitPosition >> 6));
                *ptr = (
                    *ptr & (
                        (ulong.MaxValue >> bitsLeft) | (ulong.MaxValue << newPosition)
                    )
                ) | (longValue << bitsInLong);
            }
            else
            {
                var ptr1 = (this.longPtr + (this.bitPosition >> 6));
                var ptr2 = (ptr1 + 1);

                *ptr1 = ((*ptr1 & (ulong.MaxValue >> bitsLeft)) | (longValue << bitsInLong));
                *ptr2 = ((*ptr2 & (ulong.MaxValue << newPosition)) | (longValue >> bitsLeft));
            }
            this.bitPosition = newPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value)
        {
            this.WriteUInt32((uint)value);
        }
        public void WriteUInt32(uint value)
        {
            var newPosition = this.bitPosition + 32;
            this.CheckNewLength(newPosition);

            ulong longValue = value;

            var bitsInLong = this.bitPosition & 0b11_1111;
            var bitsLeft = 64 - bitsInLong;

            if (bitsLeft >= 32)
            {
                var ptr = (this.longPtr + (this.bitPosition >> 6));
                *ptr = (
                    *ptr & (
                        (ulong.MaxValue >> bitsLeft) | (ulong.MaxValue << newPosition)
                    )
                ) | (longValue << bitsInLong);
            }
            else
            {
                var ptr1 = (this.longPtr + (this.bitPosition >> 6));
                var ptr2 = (ptr1 + 1);

                *ptr1 = ((*ptr1 & (ulong.MaxValue >> bitsLeft)) | (longValue << bitsInLong));
                *ptr2 = ((*ptr2 & (ulong.MaxValue << newPosition)) | (longValue >> bitsLeft));
            }
            this.bitPosition = newPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(long value)
        {
            this.WriteUInt64((ulong)value);
        }
        public void WriteUInt64(ulong value)
        {
            var newPosition = this.bitPosition + 32;
            this.CheckNewLength(newPosition);

            var bitsInLong = this.bitPosition & 0b11_1111;
            var bitsLeft = 64 - bitsInLong;

            var ptr1 = (this.longPtr + (this.bitPosition >> 6));
            var ptr2 = (ptr1 + 1);

            *ptr1 = ((*ptr1 & (ulong.MaxValue >> bitsLeft)) | (value << bitsInLong));
            *ptr2 = ((*ptr2 & (ulong.MaxValue << newPosition)) | (value >> bitsLeft));

            this.bitPosition = newPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSingle(float value)
        {
            this.WriteUInt32(*(uint*)&value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double value)
        {
            this.WriteUInt64(*(ulong*)&value);
        }

        public void Write(ulong value, int bits)
        {
            var newPosition = this.bitPosition + bits;
            this.CheckNewLength(newPosition);

            // mask so we dont overwrite
            value = value & (ulong.MaxValue >> (64 - bits));

            var bitsInLong = this.bitPosition & 0b11_1111;
            var bitsLeft = 64 - bitsInLong;
            if (bitsLeft >= bits)
            {
                var ptr = (this.longPtr + (this.bitPosition >> 6));
                *ptr = (
                    *ptr & (
                        (ulong.MaxValue >> bitsLeft) | (ulong.MaxValue << (newPosition /*we can use full position here as c# will mask it to just 6 bits*/))
                    )
                ) | (value << bitsInLong);
            }
            else
            {
                var ptr1 = (this.longPtr + (this.bitPosition >> 6));
                var ptr2 = (ptr1 + 1);

                *ptr1 = ((*ptr1 & (ulong.MaxValue >> bitsLeft)) | (value << bitsInLong));
                *ptr2 = ((*ptr2 & (ulong.MaxValue << newPosition)) | (value >> bitsLeft));
            }
            this.bitPosition = newPosition;
        }

        public void WriteAtPosition(ulong value, int bits, int position)
        {
            // careful with this method, dont set bitPosition

            var newPosition = position + bits;
            this.CheckNewLength(newPosition);

            // mask so we dont overwrite
            value = value & (ulong.MaxValue >> (64 - bits));

            var bitsInLong = position & 0b11_1111;
            var bitsLeft = 64 - bitsInLong;
            if (bitsLeft >= bits)
            {
                var ptr = (this.longPtr + (position >> 6));
                *ptr = (
                    *ptr & (
                        (ulong.MaxValue >> bitsLeft) | (ulong.MaxValue << (newPosition /*we can use full position here as c# will mask it to just 6 bits*/))
                    )
                ) | (value << bitsInLong);
            }
            else
            {
                var ptr1 = (this.longPtr + (position >> 6));
                var ptr2 = (ptr1 + 1);

                *ptr1 = ((*ptr1 & (ulong.MaxValue >> bitsLeft)) | (value << bitsInLong));
                *ptr2 = ((*ptr2 & (ulong.MaxValue << newPosition)) | (value >> bitsLeft));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valuePtr"></param>
        /// <param name="count">How many ulongs to copy, eg 64 bits</param>
        public void UnsafeCopy(ulong* valuePtr, int count)
        {
            if (count == 0) { return; }

            var newBit = this.bitPosition + 64 * count;
            this.CheckNewLength(newBit);

            var startPtr = this.longPtr + (this.bitPosition >> 6);

            var bitsInLong = this.bitPosition & 0b11_1111;
            var bitsLeft = 64 - bitsInLong;

            // write first part to end of current ulong
            *startPtr = ((*startPtr & (ulong.MaxValue >> bitsLeft)) | (*(valuePtr) << bitsInLong));

            // write middle parts to single ulong
            for (var i = 1; i < count; i++)
            {
                *(startPtr + i) = (*(valuePtr + i - 1) >> (64 - bitsInLong)) | (*(valuePtr + i) << bitsInLong);
            }

            // write end part to start of next ulong
            *(startPtr + count) = ((*(startPtr + count) & (ulong.MaxValue << this.bitPosition)) | (*(valuePtr + count - 1) >> bitsLeft));

            this.bitPosition = newBit;
        }

        /// <summary>
        /// <para>
        ///    Moves poition to nearest byte then copies struct to that position
        /// </para>
        /// See <see href="https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.UnsafeUtility.CopyStructureToPtr.html">UnsafeUtility.CopyStructureToPtr</see>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="byteSize">size of stuct, in bytes</param>
        public void PadAndCopy<T>(ref T value, int byteSize) where T : struct
        {
            this.PadToByte();
            var newPosition = this.bitPosition + 8 * byteSize;
            this.CheckNewLength(newPosition);

            var startPtr = ((byte*)this.longPtr) + (this.bitPosition >> 3);

            UnsafeUtility.CopyStructureToPtr(ref value, startPtr);
            this.bitPosition = newPosition;
        }

        /// <summary>
        /// <para>
        ///    Moves poition to nearest byte then writes bytes to that position
        /// </para>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void WriteBytes(byte[] array, int offset, int length)
        {
            this.PadToByte();
            var newPosition = this.bitPosition + 8 * length;
            this.CheckNewLength(newPosition);

            // todo benchmark this vs Marshal.Copy or for loop
            Buffer.BlockCopy(array, offset, this.managedBuffer, this.ByteLength, length);
            this.bitPosition = newPosition;
        }



        public void CopyFromWriter(NetworkWriter other, int otherBitPosition, int bitLength)
        {
            var newBit = this.bitPosition + 64 * bitLength;
            this.CheckNewLength(newBit);


            var bitsToCopyFromOtherLong = Math.Min(64 - (otherBitPosition & 0b11_1111), bitLength);
            var otherLongPosition = otherBitPosition >> 6;
            var first = other.longPtr[otherLongPosition];
            this.Write(first >> (64 - bitsToCopyFromOtherLong), bitsToCopyFromOtherLong);
            // written all bits
            if (bitsToCopyFromOtherLong == bitLength) { return; }


            bitLength -= bitsToCopyFromOtherLong;
            otherBitPosition += bitsToCopyFromOtherLong;
            otherLongPosition++;
            // other should now be aligned to ulong;

            var ulongCount = bitLength >> 6;
            this.UnsafeCopy(other.longPtr + otherBitPosition, ulongCount);

            var leftOver = bitLength - (ulongCount * 64);
            var last = other.longPtr[otherBitPosition + ulongCount];
            this.Write(last, leftOver);

            this.bitPosition = newBit;
        }
    }
}
