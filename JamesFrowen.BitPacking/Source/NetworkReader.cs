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
    /// Binary stream Reader. Supports simple types, buffers, arrays, structs, and nested types
    /// <para>Use <see cref="NetworkReaderPool.GetReader">NetworkReaderPool.GetReader</see> to reduce memory allocation</para>
    /// </summary>
    public unsafe class NetworkReader : IDisposable
    {
        byte[] managedBuffer;
        GCHandle handle;
        ulong* longPtr;
        bool disposed;


        int bitPosition;
        int bitLength;

        public int BitLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.bitLength;
        }

        public int BitPosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.bitPosition;
        }

        /// <summary>
        /// Position to the nearest byte
        /// </summary>
        public int BytePosition
        {
            // rounds up to nearest 8
            // add to 3 last bits,
            //   if any are 1 then it will roll over 4th bit.
            //   if all are 0, then nothing happens 
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (this.bitPosition + 0b111) >> 3;
        }

        public NetworkReader()
        {
            // start disposed as there is no handle until first reset
            this.disposed = true;
        }
        ~NetworkReader()
        {
            this.Dispose(false);
        }
        /// <param name="disposing">true if called from IDisposable</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed) return;

            this.handle.Free();
            this.longPtr = null;
            this.disposed = true;

            if (disposing)
            {
                // clear manged stuff here because we no longer want reader to keep reference to buffer
                this.bitLength = 0;
                this.managedBuffer = null;
            }
        }
        public void Dispose()
        {
            this.Dispose(true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(ArraySegment<byte> segment)
        {
            this.Reset(segment.Array, segment.Offset, segment.Count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(byte[] array) => this.Reset(array, 0, array.Length);
        public void Reset(byte[] array, int position, int length)
        {
            if (!this.disposed)
            {
                // dispose old handler first
                this.Dispose();

                Debug.LogWarning("Calling reset on reader before disposing old handler");
            }

            // reset disposed bool, as it can be disposed again after reset
            this.disposed = false;

            this.bitPosition = position * 8;
            this.bitLength = bitPosition + (length * 8);
            this.managedBuffer = array;
            this.handle = GCHandle.Alloc(this.managedBuffer, GCHandleType.Pinned);
            this.longPtr = (ulong*)this.handle.AddrOfPinnedObject();
        }




        public bool CanRead()
        {
            return this.bitPosition < this.bitLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckNewLength(int newPosition)
        {
            if (newPosition > this.bitLength)
            {
                throw new IndexOutOfRangeException($"NewPosition:{newPosition} reader length:{bitLength}");
            }
        }

        private void PadToByte()
        {
            // todo do we need to clear skipped bits?
            this.bitPosition += this.bitPosition & 0b111;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            return this.ReadBooleanAsUlong() == 1UL;
        }
        /// <summary>
        /// Writes first bit of <paramref name="value"/> to buffer
        /// </summary>
        /// <param name="value"></param>
        public ulong ReadBooleanAsUlong()
        {
            var newPosition = this.bitPosition + 1;
            this.CheckNewLength(newPosition);

            var ptr = (this.longPtr + (this.bitPosition >> 6));
            var result = ((*ptr) >> this.bitPosition) & 0b1;

            this.bitPosition = newPosition;
            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            return (short)this.ReadUInt16();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            return (ushort)this.ReadUnmasked(16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            return (int)this.ReadUInt32();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            return (uint)this.ReadUnmasked(32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            return (long)this.ReadUInt64();
        }
        public ulong ReadUInt64()
        {
            var newPosition = this.bitPosition + 64;
            this.CheckNewLength(newPosition);

            var bitsInLong = this.bitPosition & 0b11_1111;
            var bitsLeft = 64 - bitsInLong;

            var ptr1 = (this.longPtr + (this.bitPosition >> 6));
            var ptr2 = (ptr1 + 1);

            // eg use byte, read 6  =>bitPosition=5, bitsLeft=3, newPos=1
            // r1 = aaab_bbbb => 0000_0aaa
            // r2 = cccc_caaa => ccaa_a000
            // r = r1|r2 => ccaa_aaaa
            // we mask this result later

            var r1 = (*ptr1) >> this.bitPosition;
            var r2 = (*ptr2) << bitsLeft;
            var result = r1 | r2;

            this.bitPosition = newPosition;

            // dont need to mask this result because should be reading all 64 bits
            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            var uValue = this.ReadUInt32();
            return *(float*)&uValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            var uValue = this.ReadUInt64();
            return *(double*)&uValue;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Read(int bits)
        {
            // mask so we dont returns extra bits
            return this.ReadUnmasked(bits) & (ulong.MaxValue >> (64 - bits));
        }

        private ulong ReadUnmasked(int bits)
        {
            var newPosition = this.bitPosition + bits;
            this.CheckNewLength(newPosition);

            var bitsInLong = this.bitPosition & 0b11_1111;
            var bitsLeft = 64 - bitsInLong;

            ulong result;
            if (bitsLeft >= bits)
            {
                var ptr = (this.longPtr + (this.bitPosition >> 6));
                result = (*ptr) >> this.bitPosition;
            }
            else
            {
                var ptr1 = (this.longPtr + (this.bitPosition >> 6));
                var ptr2 = (ptr1 + 1);

                // eg use byte, read 6  =>bitPosition=5, bitsLeft=3, newPos=1
                // r1 = aaab_bbbb => 0000_0aaa
                // r2 = cccc_caaa => ccaa_a000
                // r = r1|r2 => ccaa_aaaa
                // we mask this result later

                var r1 = (*ptr1) >> this.bitPosition;
                var r2 = (*ptr2) << bitsLeft;
                result = r1 | r2;
            }
            this.bitPosition = newPosition;

            return result;
        }

        public void UnsafeCopy(ulong* targetPtr, int count)
        {
            // todo Implemented this, see NetworkWriter.UnsafeCopy
            throw new NotImplementedException();
        }


        /// <summary>
        /// <para>
        ///    Moves poition to nearest byte then copies struct from that position
        /// </para>
        /// See <see href="https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.UnsafeUtility.CopyPtrToStructure.html">UnsafeUtility.CopyPtrToStructure</see>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="byteSize"></param>
        public void PadAndCopy<T>(int byteSize, out T value) where T : struct
        {
            this.PadToByte();
            var newPosition = this.bitPosition + 64 * byteSize;
            this.CheckNewLength(newPosition);

            var startPtr = ((byte*)this.longPtr) + (this.bitPosition >> 3);

            UnsafeUtility.CopyPtrToStructure(startPtr, out value);
            this.bitPosition = newPosition;
        }

        /// <summary>
        /// <para>
        ///    Moves poition to nearest byte then copies bytes from that position
        /// </para>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void ReadBytes(byte[] array, int offset, int length)
        {
            this.PadToByte();
            var newPosition = this.bitPosition + 8 * length;
            this.CheckNewLength(newPosition);

            // todo benchmark this vs Marshal.Copy or for loop
            Buffer.BlockCopy(array, offset, this.managedBuffer, this.BytePosition, length);
            this.bitPosition = newPosition;
        }
    }
}
