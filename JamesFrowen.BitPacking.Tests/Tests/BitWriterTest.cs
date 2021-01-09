using NUnit.Framework;
using System;

namespace JamesFrowen.BitPacking.Tests
{
    public class BitWriterTest
    {
        private const int BufferSize = 1000;
        const int max = (1 << 10) - 1;

        Random random = new Random();

        uint randomUint(int min, int max)
        {
            return (uint)this.random.Next(min, max);
        }

        [Test]
        public void WritesCorrectBits()
        {
            uint bits1 = 0b1011;
            uint bits2 = 0b10_0111_1001;
            var expected1 = (byte)bits1;
            byte expected2_1 = 0b1001_1011;
            byte expected2_2 = 0b10_0111;


            var writer = new BitWriter(BufferSize);


            {
                Assert.That(writer.Length, Is.Zero, "Should start at length 0");
                writer.Write(bits1, 4);

                Assert.That(writer.Length, Is.EqualTo(1), "should have length 1 after writing 4 bits");

                var segment = writer.ToArraySegment();
                Assert.That(segment.Offset, Is.Zero, "segment sould be at start of array");
                Assert.That(segment.Count, Is.EqualTo(1), "segment length should by 1 after writing 4 bits");
                var first = segment.Array[0];

                Assert.That(first, Is.EqualTo(expected1));
            }

            {
                writer.Write(bits2, 10);
                Assert.That(writer.Length, Is.EqualTo(2), "should have length 2 after writing 14 bits");

                var segment = writer.ToArraySegment();
                Assert.That(segment.Offset, Is.Zero, "segment should be at start of array");
                Assert.That(segment.Count, Is.EqualTo(2), "segment length should by 2 after writing 14 bits");
                var first = segment.Array[0];
                var second = segment.Array[1];

                Assert.That(first, Is.EqualTo(expected2_1));
                Assert.That(second, Is.EqualTo(expected2_2));
            }
        }

        [Test]
        public void ReadsCorrectBits()
        {
            // numbers from WritesCorrectBits
            var buffers = new byte[10];
            buffers[0] = 0b1001_1011;
            buffers[1] = 0b10_0111;

            uint expected1 = 0b1011;
            var count1 = 4;
            uint expected2 = 0b10_0111_1001;
            var count2 = 10;

            var reader = new BitReader(buffers, 0, 2);

            {
                var value = reader.Read(count1);
                Assert.That(reader.Position, Is.Zero, "position should be 0 untill whole of first byte has been read");
                Assert.That(reader.BitPosition, Is.EqualTo(count1));

                Assert.That(value, Is.EqualTo(expected1));
            }

            {
                var value = reader.Read(count2);
                Assert.That(reader.Position, Is.EqualTo(1), "position should be 1 after reading first whole byte");
                Assert.That(reader.BitPosition, Is.EqualTo(count1 + count2));

                Assert.That(value, Is.EqualTo(expected2));
            }
        }

        [Test]
        [Repeat(1000)]
        public void CanWrite32BitsRepeat()
        {
            var inValue = this.randomUint(0, int.MaxValue);

            var writer = new BitWriter(BufferSize);

            writer.Write(inValue, 32);

            var reader = new BitReader(writer.ToArraySegment());

            var outValue = reader.Read(32);

            Assert.That(outValue, Is.EqualTo(inValue));
        }

        [Test]
        [Repeat(1000)]
        public void CanWrite3MultipleValuesRepeat()
        {
            var inValue1 = this.randomUint(0, max);
            var inValue2 = this.randomUint(0, max);
            var inValue3 = this.randomUint(0, max);

            var writer = new BitWriter(BufferSize);

            writer.Write(inValue1, 10);
            writer.Write(inValue2, 10);
            writer.Write(inValue3, 10);

            var reader = new BitReader(writer.ToArraySegment());

            var outValue1 = reader.Read(10);
            var outValue2 = reader.Read(10);
            var outValue3 = reader.Read(10);

            Assert.That(outValue1, Is.EqualTo(inValue1), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue2, Is.EqualTo(inValue2), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue3, Is.EqualTo(inValue3), $"Failed [{inValue1},{inValue2},{inValue3}]");
        }

        [Test]
        [Repeat(1000)]
        public void CanWrite8MultipleValuesRepeat()
        {
            var inValue1 = this.randomUint(0, max);
            var inValue2 = this.randomUint(0, max);
            var inValue3 = this.randomUint(0, max);
            var inValue4 = this.randomUint(0, max);
            var inValue5 = this.randomUint(0, max);
            var inValue6 = this.randomUint(0, max);
            var inValue7 = this.randomUint(0, max);
            var inValue8 = this.randomUint(0, max);

            var writer = new BitWriter(BufferSize);

            writer.Write(inValue1, 10);
            writer.Write(inValue2, 10);
            writer.Write(inValue3, 10);
            writer.Write(inValue4, 10);
            writer.Write(inValue5, 10);
            writer.Write(inValue6, 10);
            writer.Write(inValue7, 10);
            writer.Write(inValue8, 10);

            var reader = new BitReader(writer.ToArraySegment());

            var outValue1 = reader.Read(10);
            var outValue2 = reader.Read(10);
            var outValue3 = reader.Read(10);
            var outValue4 = reader.Read(10);
            var outValue5 = reader.Read(10);
            var outValue6 = reader.Read(10);
            var outValue7 = reader.Read(10);
            var outValue8 = reader.Read(10);

            Assert.That(outValue1, Is.EqualTo(inValue1), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
            Assert.That(outValue2, Is.EqualTo(inValue2), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
            Assert.That(outValue3, Is.EqualTo(inValue3), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
            Assert.That(outValue4, Is.EqualTo(inValue4), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
            Assert.That(outValue5, Is.EqualTo(inValue5), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
            Assert.That(outValue6, Is.EqualTo(inValue6), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
            Assert.That(outValue7, Is.EqualTo(inValue7), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
            Assert.That(outValue8, Is.EqualTo(inValue8), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
        }

        [Test]
        [Description("these are failed random cases")]
        [TestCase(859u, 490u, 45u, 583u, 153u, 321u, 147u, 305u)]
        [TestCase(360u, 454u, 105u, 949u, 194u, 312u, 272u, 350u)]
        public void CanWrite8MultipleValues(uint inValue1, uint inValue2, uint inValue3, uint inValue4, uint inValue5, uint inValue6, uint inValue7, uint inValue8)
        {
            var writer = new BitWriter(BufferSize);

            writer.Write(inValue1, 10);
            writer.Write(inValue2, 10);
            writer.Write(inValue3, 10);
            writer.Write(inValue4, 10);
            writer.Write(inValue5, 10);
            writer.Write(inValue6, 10);
            writer.Write(inValue7, 10);
            writer.Write(inValue8, 10);

            var reader = new BitReader(writer.ToArraySegment());

            var outValue1 = reader.Read(10);
            Assert.That(outValue1, Is.EqualTo(inValue1), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue2 = reader.Read(10);
            Assert.That(outValue2, Is.EqualTo(inValue2), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue3 = reader.Read(10);
            Assert.That(outValue3, Is.EqualTo(inValue3), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue4 = reader.Read(10);
            Assert.That(outValue4, Is.EqualTo(inValue4), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue5 = reader.Read(10);
            Assert.That(outValue5, Is.EqualTo(inValue5), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue6 = reader.Read(10);
            Assert.That(outValue6, Is.EqualTo(inValue6), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue7 = reader.Read(10);
            Assert.That(outValue7, Is.EqualTo(inValue7), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue8 = reader.Read(10);
            Assert.That(outValue8, Is.EqualTo(inValue8), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
        }
    }
}
