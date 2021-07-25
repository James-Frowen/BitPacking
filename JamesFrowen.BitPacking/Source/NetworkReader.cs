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
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace JamesFrowen.BitPacking
{
    /// <summary>
    /// Bit writer, writes values to a buffer on a bit level
    /// <para>Use <see cref="NetworkReaderPool.GetReader"/> to reduce memory allocation</para>
    /// </summary>
    public unsafe class NetworkReader : IDisposable
    {
        byte[] managedBuffer;
        GCHandle handle;
        ulong* longPtr;
        bool needsDisposing;


        int bitPosition;
        int bitLength;

        /// <summary>
        /// Size of buffer that is being read from
        /// </summary>
        public int BitLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.bitLength;
        }

        /// <summary>
        /// Current bit position for reading from buffer
        /// </summary>
        public int BitPosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.bitPosition;
        }
        /// <summary>
        /// Current <see cref="BitPosition"/> rounded up to nearest multiple of 8
        /// </summary>
        public int BytePosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            // rounds up to nearest 8
            // add to 3 last bits,
            //   if any are 1 then it will roll over 4th bit.
            //   if all are 0, then nothing happens 
            get => (this.bitPosition + 0b111) >> 3;
        }


        public NetworkReader() { }

        ~NetworkReader()
        {
            this.Dispose(false);
        }
        /// <param name="disposing">true if called from IDisposable</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.needsDisposing) return;

            this.handle.Free();
            this.longPtr = null;
            this.needsDisposing = false;

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

        public void Reset(ArraySegment<byte> segment)
        {
            this.Reset(segment.Array, segment.Offset, segment.Count);
        }
        public void Reset(byte[] array)
        {
            this.Reset(array, 0, array.Length);
        }
        public void Reset(byte[] array, int position, int length)
        {
            if (this.needsDisposing)
            {
                // dispose old handler first
                this.Dispose();
            }

            // reset disposed bool, as it can be disposed again after reset
            this.needsDisposing = true;

            this.bitPosition = position * 8;
            this.bitLength = this.bitPosition + (length * 8);
            this.managedBuffer = array;
            this.handle = GCHandle.Alloc(this.managedBuffer, GCHandleType.Pinned);
            this.longPtr = (ulong*)this.handle.AddrOfPinnedObject();
        }

        /// <summary>
        /// Can read atleast 1 bit
        /// </summary>
        /// <returns></returns>
        public bool CanRead()
        {
            return this.bitPosition < this.bitLength;
        }

        /// <summary>
        /// Can atleast <paramref name="byteLength"/> bytes
        /// </summary>
        /// <param name="byteLength"></param>
        /// <returns></returns>
        public bool CanReadBytes(int byteLength)
        {
            return (this.bitPosition + (byteLength * 8)) <= this.bitLength;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckNewLength(int newPosition)
        {
            if (newPosition > this.bitLength)
            {
                throw new EndOfStreamException($"Can not read over end of buffer, new position {newPosition}, length {this.bitLength} bits");
            }
        }

        private void PadToByte()
        {
            this.bitPosition = this.BytePosition << 3;
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
            int newPosition = this.bitPosition + 1;
            this.CheckNewLength(newPosition);

            ulong* ptr = (this.longPtr + (this.bitPosition >> 6));
            ulong result = ((*ptr) >> this.bitPosition) & 0b1;

            this.bitPosition = newPosition;
            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte() => (sbyte)this.ReadByte();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() => (byte)this.ReadUnmasked(8);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16() => (short)this.ReadUInt16();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16() => (ushort)this.ReadUnmasked(16);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32() => (int)this.ReadUInt32();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32() => (uint)this.ReadUnmasked(32);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64() => (long)this.ReadUInt64();
        public ulong ReadUInt64()
        {
            int newPosition = this.bitPosition + 64;
            this.CheckNewLength(newPosition);

            int bitsInLong = this.bitPosition & 0b11_1111;
            ulong result;
            if (bitsInLong == 0)
            {
                ulong* ptr1 = (this.longPtr + (this.bitPosition >> 6));
                result = *ptr1;
            }
            else
            {
                int bitsLeft = 64 - bitsInLong;

                ulong* ptr1 = (this.longPtr + (this.bitPosition >> 6));
                ulong* ptr2 = (ptr1 + 1);

                // eg use byte, read 6  =>bitPosition=5, bitsLeft=3, newPos=1
                // r1 = aaab_bbbb => 0000_0aaa
                // r2 = cccc_caaa => ccaa_a000
                // r = r1|r2 => ccaa_aaaa
                // we mask this result later

                ulong r1 = (*ptr1) >> this.bitPosition;
                ulong r2 = (*ptr2) << bitsLeft;
                result = r1 | r2;
            }

            this.bitPosition = newPosition;

            // dont need to mask this result because should be reading all 64 bits
            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            uint uValue = this.ReadUInt32();
            return *(float*)&uValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            ulong uValue = this.ReadUInt64();
            return *(double*)&uValue;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Read(int bits)
        {
            if (bits == 0) return 0;
            // mask so we dont returns extra bits
            return this.ReadUnmasked(bits) & (ulong.MaxValue >> (64 - bits));
        }

        private ulong ReadUnmasked(int bits)
        {
            int newPosition = this.bitPosition + bits;
            this.CheckNewLength(newPosition);

            int bitsInLong = this.bitPosition & 0b11_1111;
            int bitsLeft = 64 - bitsInLong;

            ulong result;
            if (bitsLeft >= bits)
            {
                ulong* ptr = this.longPtr + (this.bitPosition >> 6);
                result = (*ptr) >> bitsInLong;
            }
            else
            {
                ulong* ptr1 = this.longPtr + (this.bitPosition >> 6);
                ulong* ptr2 = ptr1 + 1;

                // eg use byte, read 6  =>bitPosition=5, bitsLeft=3, newPos=1
                // r1 = aaab_bbbb => 0000_0aaa
                // r2 = cccc_caaa => ccaa_a000
                // r = r1|r2 => ccaa_aaaa
                // we mask this result later

                ulong r1 = (*ptr1) >> bitsInLong;
                ulong r2 = (*ptr2) << bitsLeft;
                result = r1 | r2;
            }
            this.bitPosition = newPosition;

            return result;
        }

        /// <summary>
        /// Reads n <paramref name="bits"/> from buffer at <paramref name="bitPosition"/>
        /// </summary>
        /// <param name="bits">number of bits in value to write</param>
        /// <param name="bitPosition">where to write bits</param>
        public ulong ReadAtPosition(int bits, int bitPosition)
        {
            // check length here so this methods throws instead of the read below
            this.CheckNewLength(bitPosition + bits);

            int currentPosition = this.bitPosition;
            this.bitPosition = bitPosition;
            ulong result = this.Read(bits);
            this.bitPosition = currentPosition;

            return result;
        }


        /// <summary>
        /// Moves the internal bit position
        /// <para>For most usecases it is safer to use <see cref="ReadAtPosition"/></para>
        /// <para>WARNING: When reading from earlier position make sure to move position back to end of buffer after reading</para>
        /// </summary>
        /// <param name="newPosition"></param>
        public void MoveBitPosition(int newPosition)
        {
            this.CheckNewLength(newPosition);
            this.bitPosition = newPosition;
        }


        /// <summary>
        /// <para>
        ///    Moves position to nearest byte then copies struct from that position
        /// </para>
        /// See <see href="https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.UnsafeUtility.CopyPtrToStructure.html">UnsafeUtility.CopyPtrToStructure</see>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="byteSize"></param>
        public void PadAndCopy<T>(int byteSize, out T value) where T : struct
        {
            this.PadToByte();
            int newPosition = this.bitPosition + (64 * byteSize);
            this.CheckNewLength(newPosition);

            byte* startPtr = ((byte*)this.longPtr) + (this.bitPosition >> 3);

            UnsafeUtility.CopyPtrToStructure(startPtr, out value);
            this.bitPosition = newPosition;
        }

        /// <summary>
        /// <para>
        ///    Moves position to nearest byte then copies bytes from that position
        /// </para>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void ReadBytes(byte[] array, int offset, int length)
        {
            this.PadToByte();
            int newPosition = this.bitPosition + (8 * length);
            this.CheckNewLength(newPosition);

            // todo benchmark this vs Marshal.Copy or for loop
            Buffer.BlockCopy(this.managedBuffer, this.BytePosition, array, offset, length);
            this.bitPosition = newPosition;
        }

        public ArraySegment<byte> ReadBytesSegment(int count)
        {
            this.PadToByte();
            int newPosition = this.bitPosition + (8 * count);
            this.CheckNewLength(newPosition);

            var result = new ArraySegment<byte>(this.managedBuffer, this.BytePosition, count);
            this.bitPosition = newPosition;
            return result;
        }
    }
}
