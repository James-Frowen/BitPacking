using JamesFrowen.BitPacking;
using Mirror.Benchmark;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JamesFrowen.BitPacking.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var performance = new Performance1();
            performance.Start();
        }
    }
}


namespace Mirror.Benchmark
{
    public static class DoStuffWithData
    {
        public struct ProcessedData
        {
            public WriterType writer;
            public int bufferSize;
            public int writeSize;
            public double elapsed;
            public double delta;
            public double percent;
            public double stddev;
        }
        public static ProcessedData[] analysisData(WriterType[] writerTypes, int[] bufferSizes, int[] writeSizes, Dictionary<WriterType, Dictionary<int, Dictionary<int, double>>> data)
        {
            return findBest(writerTypes, bufferSizes, writeSizes, data);
            //return findBestFromKnown(bufferSizes, writeSizes, data);
        }

        private static ProcessedData[] findBest(WriterType[] writerTypes, int[] bufferSizes, int[] writeSizes, Dictionary<WriterType, Dictionary<int, Dictionary<int, double>>> data)
        {
            var list = new List<ProcessedData>();
            foreach (var bufferSize in bufferSizes)
            {
                foreach (var writeSize in writeSizes)
                {
                    var lowest = double.MaxValue;
                    double avg = 0;
                    WriterType lowestType = default;
                    var values = new List<double>();
                    foreach (var type in writerTypes)
                    {
                        var value = data[type][bufferSize][writeSize];
                        values.Add(value);
                        avg += value / writerTypes.Length;
                        if (value < lowest)
                        {
                            lowest = value;
                            lowestType = type;
                        }
                    }

                    list.Add(new ProcessedData
                    {
                        bufferSize = bufferSize,
                        writeSize = writeSize,
                        elapsed = lowest,
                        delta = avg - lowest,
                        percent = (avg - lowest) / lowest,
                        stddev = stddev(values),
                        writer = lowestType
                    });
                }
            }
            return list.ToArray();
        }

        private static ProcessedData[] findBestFromKnown(int[] bufferSizes, int[] writeSizes, Dictionary<WriterType, Dictionary<int, Dictionary<int, double>>> data)
        {
            var list = new List<ProcessedData>();
            Console.WriteLine($"{"type",-20}{"bufferSize",20}{"writeSize",20}{"elapsed",20}" +
                $"{"percentVsN11",20}{"percentVsN5",20}{"percentVsS2",20}");

            foreach (var bufferSize in bufferSizes)
            {
                foreach (var writeSize in writeSizes)
                {
                    var n11 = data[WriterType.NoScratch11][bufferSize][writeSize];
                    var n5 = data[WriterType.NoScratch5][bufferSize][writeSize];
                    var s2 = data[WriterType.Scratch2][bufferSize][writeSize];

                    var best = WriterType.NoScratch11;
                    var bestTime = n11;
                    if (n5 < bestTime)
                    {
                        best = WriterType.NoScratch5;
                        bestTime = n5;
                    }
                    if (s2 < bestTime)
                    {
                        best = WriterType.Scratch2;
                        bestTime = s2;
                    }

                    var bestVsN11 = n11 - bestTime;
                    var bestVsN5 = n5 - bestTime;
                    var bestVsS2 = s2 - bestTime;

                    var percentVsN11 = bestVsN11 / bestTime;
                    var percentVsN5 = bestVsN5 / bestTime;
                    var percentVsS2 = bestVsS2 / bestTime;

                    Console.WriteLine($"{best,-20}{bufferSize,20}{writeSize,20}{bestTime * 1000,20:0.0}" +
                        $"{percentVsN11 * 100,20:0.0}{percentVsN5 * 100,20:0.0}{percentVsS2 * 100,20:0.0}");


                    //list.Add(new ProcessedData
                    //{
                    //    bufferSize = bufferSize,
                    //    writeSize = writeSize,
                    //    elapsed = lowest,
                    //    delta = avg - lowest,
                    //    percent = (avg - lowest) / lowest,
                    //    stddev = stddev(values),
                    //    writer = lowestType
                    //});
                }
            }
            return list.ToArray();
        }


        static double stddev(List<double> values)
        {
            var mean = values.Sum() / values.Count();

            // Get the sum of the squares of the differences
            // between the values and the mean.
            var squares_query =
                from double value in values
                select (value - mean) * (value - mean);
            var sum_of_squares = squares_query.Sum();

            return Math.Sqrt(sum_of_squares / values.Count());
        }
    }
    public enum WriterType
    {
        Scratch1,
        Scratch2,
        NoScratch5,
        NoScratch10,
        NoScratch11,
        NoScratch12,
        //Blittable,
    }
    public class Performance1
    {
        public void Start()
        {
            try
            {
                var iterations = 100_000;

                Console.WriteLine($"{"type",-20},{"bufferSize",20},{"writeSize",20},{"elapsed",20},");
                var bufferSizes = new int[] {
                     //50,
                     //200,
                     //500,
                     //1000,
                     1200,
                     //2000,
                     //10_000,
                     //100_000
                };
                //int[] writeSizes = new int[]
                //{
                //    1, 3, 5, 8, 10, 15, 23, 24, 30, 32
                //};
                var writeSizes = Enumerable.Range(1, 32).ToArray();
                var writerTypes = new WriterType[] {
                    //WriterType.Scratch1,
                    WriterType.Scratch2,
                    WriterType.NoScratch5,
                    //WriterType.NoScratch10,
                    WriterType.NoScratch11,
                    WriterType.NoScratch12,
                };
                foreach (var bufferSize in bufferSizes)
                {
                    foreach (var writeSize in writeSizes)
                    {
                        foreach (var writerType in writerTypes)
                        {
                            this.testBitWriter(writerType, bufferSize, writeSize, iterations, bufferSizes);
                        }
                        Console.WriteLine("");
                    }
                }

                Console.WriteLine($"\n\n-------------\n\n");

                Console.WriteLine($"\n-------------\nBest\n\n");
                var processed = DoStuffWithData.analysisData(writerTypes, bufferSizes, writeSizes, this.data);
                if (processed.Length > 0)
                    Console.WriteLine($"{"type",-20}{"bufferSize",20}{"writeSize",20}{"elapsed",20}{"delta",20}{"percent",20}{"stddev",20}");
                foreach (var item in processed)
                {
                    Console.WriteLine($"{item.writer,-20}{item.bufferSize,20}{item.writeSize,20}{item.elapsed * 1000,20:0.0}{item.delta * 1000,20:0.0}{item.percent * 100,20:0.0}{item.stddev,20:0.000}");
                }


                Console.WriteLine($"\n\n-------------\n\n");
            }
            finally
            {
                //Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }

        Dictionary<WriterType, Dictionary<int, Dictionary<int, double>>> data = new Dictionary<WriterType, Dictionary<int, Dictionary<int, double>>>();


        static int writeSize;
        static int writeCount;
        private void testBitWriter(WriterType writerType, int bufferSize, int writeSize, int iterations, int[] bufferSizes)
        {
            Performance1.writeSize = writeSize;
            // 50 is min buffer size
            writeCount = Mathf.Min(300, bufferSizes.Min() * 8 / writeSize);
            Func<long> writeSome;
            switch (writerType)
            {
                default:
                    throw new InvalidEnumArgumentException();
                case WriterType.Scratch1:
                    {
                        var writer = new BitWriter_scratch1(bufferSize);
                        writer.Reset();
                        writeSome = () =>
                        {
                            var start = Stopwatch.GetTimestamp();
                            this.WriteSome(writer);
                            writer.Flush();
                            var end = Stopwatch.GetTimestamp();
                            writer.Reset();
                            return end - start;
                        };
                        break;
                    }
                case WriterType.Scratch2:
                    {
                        var writer = new BitWriter_scratch2(bufferSize);
                        writer.Reset();
                        writeSome = () =>
                        {
                            var start = Stopwatch.GetTimestamp();
                            this.WriteSome(writer);
                            writer.Flush();
                            var end = Stopwatch.GetTimestamp();
                            writer.Reset();
                            return end - start;
                        };
                        break;
                    }
                case WriterType.NoScratch5:
                    {
                        var writer = new BitWriter_NoScratch5(bufferSize);
                        writer.Reset();
                        writeSome = () =>
                        {
                            var start = Stopwatch.GetTimestamp();
                            this.WriteSome(writer);
                            var end = Stopwatch.GetTimestamp();
                            writer.Reset();
                            return end - start;
                        };
                        break;
                    }
                case WriterType.NoScratch10:
                    {
                        var writer = new BitWriter_NoScratch10(bufferSize);
                        writer.Reset();
                        writeSome = () =>
                        {
                            var start = Stopwatch.GetTimestamp();
                            this.WriteSome(writer);
                            var end = Stopwatch.GetTimestamp();
                            writer.Reset();
                            return end - start;
                        };
                        break;
                    }
                case WriterType.NoScratch11:
                    {
                        var writer = new BitWriter_NoScratch11(bufferSize);
                        writer.Reset();
                        writeSome = () =>
                        {
                            var start = Stopwatch.GetTimestamp();
                            this.WriteSome(writer);
                            var end = Stopwatch.GetTimestamp();
                            writer.Reset();
                            return end - start;
                        };
                        break;
                    }
                case WriterType.NoScratch12:
                    {
                        var writer = new BitWriter_NoScratch12(bufferSize);
                        writer.Reset();
                        writeSome = () =>
                        {
                            var start = Stopwatch.GetTimestamp();
                            this.WriteSome(writer);
                            var end = Stopwatch.GetTimestamp();
                            writer.Reset();
                            return end - start;
                        };
                        break;
                    }
                    //case WriterType.Blittable:
                    //    {
                    //        PooledNetworkWriter writer = new PooledNetworkWriter();
                    //        writer.Reset();
                    //        writeSome = () =>
                    //        {
                    //            long start = Stopwatch.GetTimestamp();
                    //            WriteSome(writer);
                    //            long end = Stopwatch.GetTimestamp();
                    //            writer.Reset();
                    //            return end - start;
                    //        };
                    //        break;
                    //    }
            }

            // warmup
            for (var i = 0; i < iterations / 100; i++)
            {
                writeSome.Invoke();
            }

            long total = 0;
            for (var i = 0; i < iterations; i++)
            {
                total += writeSome.Invoke();
            }
            var elapsed = total / (double)Stopwatch.Frequency;
            Console.WriteLine($"{writerType,-20},{bufferSize,20},{writeSize,20},{elapsed * 1000,20:0.0},");

            if (!this.data.ContainsKey(writerType))
            {
                this.data[writerType] = new Dictionary<int, Dictionary<int, double>>();
            }
            if (!this.data[writerType].ContainsKey(bufferSize))
            {
                this.data[writerType][bufferSize] = new Dictionary<int, double>();
            }
            this.data[writerType][bufferSize][writeSize] = elapsed;
        }


        void WriteSome(BitWriter_NoScratch5 writer)
        {
            for (var i = 0; i < writeCount; i++)
            {
                writer.Write((uint)i, writeSize);
            }
        }

        void WriteSome(BitWriter_scratch1 writer)
        {
            for (var i = 0; i < writeCount; i++)
            {
                writer.Write((uint)i, writeSize);
            }
        }
        void WriteSome(BitWriter_scratch2 writer)
        {
            for (var i = 0; i < writeCount; i++)
            {
                writer.Write((uint)i, writeSize);
            }
        }
        void WriteSome(BitWriter_NoScratch10 writer)
        {
            for (var i = 0; i < writeCount; i++)
            {
                writer.Write((uint)i, writeSize);
            }
        }
        void WriteSome(BitWriter_NoScratch11 writer)
        {
            for (var i = 0; i < writeCount; i++)
            {
                writer.Write((uint)i, writeSize);
            }
        }
        void WriteSome(BitWriter_NoScratch12 writer)
        {
            for (var i = 0; i < writeCount; i++)
            {
                writer.Write((uint)i, writeSize);
            }
        }
    }
}
namespace JamesFrowen.BitPacking
{
    public unsafe class BitWriter_NoScratch5
    {
        // todo allow this to work with pooling

        // todo do we need this max write size?
        private const int MaxWriteSize = 32;

        readonly byte[] buffer;

        /// <summary>
        /// next bit to write to inside writeByte
        /// </summary>
        int writeBit;

        public int Length => Mathf.CeilToInt(this.writeBit / 8f);

        public BitWriter_NoScratch5(int bufferSize) : this(new byte[bufferSize]) { }
        public BitWriter_NoScratch5(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public void Reset()
        {
            this.writeBit = 0;
            // +1 because last might not be full word
            Array.Clear(this.buffer, 0, Math.Min(this.Length, this.buffer.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(uint inValue, int inBits)
        {
            if (inBits > MaxWriteSize)
            {
                throw new ArgumentException($"bits must be less than {MaxWriteSize}");
            }
            fixed (byte* ptr = &this.buffer[this.writeBit >> 3])
            {
                *(ulong*)ptr |= inValue << (this.writeBit & 0b111);

                this.writeBit += inBits;
            }
        }

        public ArraySegment<byte> ToArraySegment()
        {
            return new ArraySegment<byte>(this.buffer, 0, this.Length);
        }
    }

    public unsafe class BitWriter_NoScratch10
    {
        // todo allow this to work with pooling

        // todo do we need this max write size?
        private const int MaxWriteSize = 32;

        byte[] managedBuffer;
        byte* ptr;
        ulong* longPtr;
        readonly int bufferSize;

        /// <summary>
        /// next bit to write to inside writeByte
        /// </summary>
        int writeBit;

        public int Length => Mathf.CeilToInt(this.writeBit / 8f);

        public BitWriter_NoScratch10(int bufferSize)
        {
            this.bufferSize = bufferSize;
            var voidPtr = NewUnmanaged(bufferSize);
            this.ptr = (byte*)voidPtr;
            this.longPtr = (ulong*)voidPtr;
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
            // todo do safely checks on args
            // todo mask incase invalue is more than inBits

            var value = ((ulong)inValue) << (this.writeBit & 0b111);

            *(this.longPtr + (this.writeBit >> 6)) |= value;

            this.writeBit += inBits;
        }

        public ArraySegment<byte> ToArraySegment()
        {
            var length = this.Length;
            for (var i = 0; i < length; i++)
                this.managedBuffer[i] = *(this.ptr + i);
            return new ArraySegment<byte>(this.managedBuffer, 0, length);
        }
    }
    public unsafe class BitWriter_NoScratch11
    {
        // todo allow this to work with pooling

        byte[] managedBuffer;
        byte* ptr;
        ulong* longPtr;
        readonly int bufferSize;
        readonly int bufferSizeBits;

        /// <summary>
        /// next bit to write to inside writeByte
        /// </summary>
        int writeBit;

        public int Length => Mathf.CeilToInt(this.writeBit / 8f);

        public BitWriter_NoScratch11(int bufferSize)
        {
            this.bufferSize = bufferSize;
            this.bufferSizeBits = bufferSize * 8;
            var voidPtr = NewUnmanaged(bufferSize);
            this.ptr = (byte*)voidPtr;
            this.longPtr = (ulong*)voidPtr;
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
            var value = (mask & inValue) << (this.writeBit & 0b111);

            *(this.longPtr + (this.writeBit >> 6)) |= value;

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

    public unsafe class BitWriter_NoScratch12
    {

        // todo allow this to work with pooling

        byte[] managedBuffer;
        byte* ptr;
        uint* uintPtr;
        readonly int bufferSize;
        readonly int bufferSizeBits;

        /// <summary>
        /// next bit to write to inside writeByte
        /// </summary>
        int writeBit;

        public int Length => Mathf.CeilToInt(this.writeBit / 8f);

        public BitWriter_NoScratch12(int bufferSize)
        {
            this.bufferSize = bufferSize;
            this.bufferSizeBits = bufferSize * 8;
            var voidPtr = NewUnmanaged(bufferSize);
            this.ptr = (byte*)voidPtr;
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


namespace JamesFrowen.BitPacking
{
    public class BitWriter_scratch1
    {
        // todo allow this to work with pooling
        // todo try writing to buffer directly instead of using scratch

        private const int WriteSize = 32;

        byte[] buffer;
        int writeCount;

        ulong scratch;
        int bitsInScratch;

        public int Length => this.writeCount;

        public BitWriter_scratch1(int bufferSize) : this(new byte[bufferSize]) { }
        public BitWriter_scratch1(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public void Reset()
        {
            this.scratch = 0;
            this.bitsInScratch = 0;
            // +1 because last might not be full word
            Array.Clear(this.buffer, 0, this.writeCount);
            this.writeCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value, int bits)
        {
            //Console.WriteLine($"{value},{bits}");
            if (bits > WriteSize)
            {
                throw new ArgumentException($"bits must be less than {WriteSize}");
            }

            var mask = (1ul << bits) - 1;
            var longValue = value & mask;

            this.scratch |= (longValue << this.bitsInScratch);

            this.bitsInScratch += bits;


            if (this.bitsInScratch >= WriteSize)
            {
                var toWrite = (uint)this.scratch;
                this.write32bitToBuffer(toWrite);

                this.scratch >>= WriteSize;
                this.bitsInScratch -= WriteSize;
            }
        }

        public void Flush()
        {
            var toWrite = (uint)this.scratch;
            if (this.bitsInScratch > 24)
            {
                this.write32bitToBuffer(toWrite);
            }
            else if (this.bitsInScratch > 16)
            {
                this.write24bitToBuffer(toWrite);
            }
            else if (this.bitsInScratch > 8)
            {
                this.write16bitToBuffer(toWrite);
            }
            else if (this.bitsInScratch > 0)
            {
                this.write8bitToBuffer(toWrite);
            }

            // set to 0 incase flush is called twice
            this.bitsInScratch = 0;
        }
        public ArraySegment<byte> ToArraySegment()
        {
            this.Flush();
            return new ArraySegment<byte>(this.buffer, 0, this.writeCount);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void write32bitToBuffer(uint toWrite)
        {
            this.buffer[this.writeCount] = (byte)(toWrite);
            this.buffer[this.writeCount + 1] = (byte)(toWrite >> 8);
            this.buffer[this.writeCount + 2] = (byte)(toWrite >> 16);
            this.buffer[this.writeCount + 3] = (byte)(toWrite >> 24);
            this.writeCount += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void write24bitToBuffer(uint toWrite)
        {
            this.buffer[this.writeCount] = (byte)(toWrite);
            this.buffer[this.writeCount + 1] = (byte)(toWrite >> 8);
            this.buffer[this.writeCount + 2] = (byte)(toWrite >> 16);
            this.writeCount += 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void write16bitToBuffer(uint toWrite)
        {
            this.buffer[this.writeCount] = (byte)(toWrite);
            this.buffer[this.writeCount + 1] = (byte)(toWrite >> 8);
            this.writeCount += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void write8bitToBuffer(uint toWrite)
        {
            this.buffer[this.writeCount] = (byte)(toWrite);
            this.writeCount += 1;
        }
    }
    public unsafe class BitWriter_scratch2
    {
        // todo allow this to work with pooling
        // todo try writing to buffer directly instead of using scratch

        private const int WriteSize = 32;

        byte[] buffer;
        int writeCount;

        ulong scratch;
        int bitsInScratch;

        public int Length => this.writeCount;

        public BitWriter_scratch2(int bufferSize) : this(new byte[bufferSize]) { }
        public BitWriter_scratch2(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public void Reset()
        {
            this.scratch = 0;
            this.bitsInScratch = 0;
            // +1 because last might not be full word
            Array.Clear(this.buffer, 0, this.writeCount);
            this.writeCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value, int bits)
        {
            //Console.WriteLine($"{value},{bits}");
            if (bits > WriteSize)
            {
                throw new ArgumentException($"bits must be less than {WriteSize}");
            }

            var mask = (1ul << bits) - 1;
            var longValue = value & mask;

            this.scratch |= (longValue << this.bitsInScratch);

            this.bitsInScratch += bits;

            if (this.bitsInScratch >= WriteSize)
            {
                var toWrite = (uint)this.scratch;
                this.write32BitsToBuffer(toWrite);

                this.scratch >>= WriteSize;
                this.bitsInScratch -= WriteSize;
            }
        }

        public void Flush()
        {
            var toWrite = (uint)this.scratch;
            if (this.bitsInScratch > 24)
            {
                this.unsafeWriteToBuffer(toWrite);
                this.writeCount += 4;
            }
            else if (this.bitsInScratch > 16)
            {
                this.unsafeWriteToBuffer(toWrite);
                this.writeCount += 3;
            }
            else if (this.bitsInScratch > 8)
            {
                this.unsafeWriteToBuffer(toWrite);
                this.writeCount += 2;
            }
            else if (this.bitsInScratch > 0)
            {
                this.unsafeWriteToBuffer(toWrite);
                this.writeCount += 1;
            }

            // set to 0 incase flush is called twice
            this.bitsInScratch = 0;
        }
        public ArraySegment<byte> ToArraySegment()
        {
            this.Flush();
            return new ArraySegment<byte>(this.buffer, 0, this.writeCount);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void write32BitsToBuffer(uint toWrite)
        {
            this.unsafeWriteToBuffer(toWrite);
            this.writeCount += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void unsafeWriteToBuffer(uint toWrite)
        {
            fixed (byte* ptr = &this.buffer[this.writeCount])
            {
                *(uint*)ptr = toWrite;
            }
        }
    }
}
