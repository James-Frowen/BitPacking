using NUnit.Framework;
using System.Linq;

namespace JamesFrowen.BitPacking.Tests
{
    public abstract class NetworkWriterTestBase
    {
        private const int BufferSize = 1200;
        protected NetworkWriter writer;
        protected NetworkReader reader;

        [SetUp]
        public void SetUp()
        {
            this.writer = new NetworkWriter(BufferSize);
            this.reader = new NetworkReader();
        }


        [TearDown]
        public void TearDown()
        {
            this.writer = null;
            this.reader = null;
        }
    }

    public class NetworkWriterTest : NetworkWriterTestBase
    {
        const int max = (1 << 10) - 1;

        TestRandom random = new TestRandom();

        [Test]
        public void WritesCorrectBits()
        {
            uint bits1 = 0b1011;
            uint bits2 = 0b10_0111_1001;
            var expected1 = (byte)bits1;
            byte expected2_1 = 0b1001_1011;
            byte expected2_2 = 0b10_0111;

            {
                Assert.That(this.writer.ByteLength, Is.Zero, "Should start at length 0");
                this.writer.Write(bits1, 4);

                Assert.That(this.writer.ByteLength, Is.EqualTo(1), "should have length 1 after writing 4 bits");

                var array = this.writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(1), "segment length should by 1 after writing 4 bits");
                var first = array[0];

                Assert.That(first, Is.EqualTo(expected1));
            }

            {
                this.writer.Write(bits2, 10);
                Assert.That(this.writer.ByteLength, Is.EqualTo(2), "should have length 2 after writing 14 bits");

                var array = this.writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(2), "segment length should by 2 after writing 14 bits");
                var first = array[0];
                var second = array[1];

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

            this.reader.Reset(buffers, 0, buffers.Length);

            {
                var value = this.reader.Read(count1);
                Assert.That(this.reader.BitPosition, Is.EqualTo(count1));

                Assert.That(value, Is.EqualTo(expected1));
            }

            {
                var value = this.reader.Read(count2);
                Assert.That(this.reader.BitPosition, Is.EqualTo(count1 + count2));

                Assert.That(value, Is.EqualTo(expected2));
            }
        }


        [Test]
        public void WritesCorrectBitsLong()
        {
            // 30 bits
            uint bits1 = 0b01_0010__0011_0100__0101_0110__0111_1000;
            uint bits2 = 0b01_1010__1011_1100__1101_1110__1111_0000;
            uint bits3 = 0b11_0101__0011_0001__0101_1001__1101_1010;


            var expected1 = new byte[] {
                0b01111000,
                0b01010110,
                0b00110100,
                0b00010010,
            };

            // reverse because array is left to right, but bits above are right to left.
            var expected2 = new byte[] {
                0b00000110,
                0b10101111,
                0b00110111,
                0b10111100,

                0b00010010,
                0b00110100,
                0b01010110,
                0b01111000
            }.Reverse().ToArray();

            var expected3 = new byte[] {
                0b00000011,
                0b01010011,
                0b00010101,
                0b10011101,

                0b10100110,
                0b10101111,
                0b00110111,
                0b10111100,

                0b00010010,
                0b00110100,
                0b01010110,
                0b01111000
            }.Reverse().ToArray();



            {
                Assert.That(this.writer.ByteLength, Is.Zero, "Should start at length 0");
                this.writer.Write(bits1, 30);

                Assert.That(this.writer.ByteLength, Is.EqualTo(4), "should have length 4 after writing 30 bits");

                var array = this.writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(4), "segment length should by 4 after writing 30 bits");
                for (var i = 0; i < 4; i++)
                {
                    Assert.That(array[i], Is.EqualTo(expected1[i]));
                }
            }

            {
                this.writer.Write(bits2, 30);

                Assert.That(this.writer.ByteLength, Is.EqualTo(8), "should have length 8 after writing 60 bits");

                var array = this.writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(8), "segment length should by 8 after writing 60 bits");
                for (var i = 0; i < 8; i++)
                {
                    Assert.That(array[i], Is.EqualTo(expected2[i]));
                }
            }

            {
                this.writer.Write(bits3, 30);

                Assert.That(this.writer.ByteLength, Is.EqualTo(12), "should have length 12 after writing 90 bits");

                var array = this.writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(12), "segment length should by 12 after writing 90 bits");
                for (var i = 0; i < 12; i++)
                {
                    Assert.That(array[i], Is.EqualTo(expected3[i]));
                }
            }
        }

        [Test]
        public void ReadsCorrectBitsLong()
        {
            // values from WritesCorrectBitsLong
            var buffers = new byte[] {
                0b00000011,
                0b01010011,
                0b00010101,
                0b10011101,

                0b10100110,
                0b10101111,
                0b00110111,
                0b10111100,

                0b00010010,
                0b00110100,
                0b01010110,
                0b01111000
            }.Reverse().ToArray();

            uint expected1 = 0b01_0010__0011_0100__0101_0110__0111_1000;
            uint expected2 = 0b01_1010__1011_1100__1101_1110__1111_0000;
            uint expected3 = 0b11_0101__0011_0001__0101_1001__1101_1010;


            this.reader.Reset(buffers);

            const int readLength = 30;

            {
                var value = this.reader.Read(readLength);
                Assert.That(this.reader.BitPosition, Is.EqualTo(readLength));

                Assert.That(value, Is.EqualTo(expected1));
            }

            {
                var value = this.reader.Read(readLength);
                Assert.That(this.reader.BitPosition, Is.EqualTo(readLength * 2));

                Assert.That(value, Is.EqualTo(expected2));
            }

            {
                var value = this.reader.Read(readLength);
                Assert.That(this.reader.BitPosition, Is.EqualTo(readLength * 3));

                Assert.That(value, Is.EqualTo(expected3));
            }
        }


        [Test]
        [Repeat(1000)]
        public void CanWrite32BitsRepeat()
        {
            var inValue = this.random.Uint(0, int.MaxValue);

            this.writer.Write(inValue, 32);

            this.reader.Reset(this.writer.ToArraySegment());

            var outValue = this.reader.Read(32);

            Assert.That(outValue, Is.EqualTo(inValue));
        }

        [Test]
        [Repeat(1000)]
        public void CanWrite3MultipleValuesRepeat()
        {
            var inValue1 = this.random.Uint(0, max);
            var inValue2 = this.random.Uint(0, max);
            var inValue3 = this.random.Uint(0, max);

            this.writer.Write(inValue1, 10);
            this.writer.Write(inValue2, 10);
            this.writer.Write(inValue3, 10);

            this.reader.Reset(this.writer.ToArraySegment());

            var outValue1 = this.reader.Read(10);
            var outValue2 = this.reader.Read(10);
            var outValue3 = this.reader.Read(10);

            Assert.That(outValue1, Is.EqualTo(inValue1), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue2, Is.EqualTo(inValue2), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue3, Is.EqualTo(inValue3), $"Failed [{inValue1},{inValue2},{inValue3}]");
        }

        [Test]
        // failing values from repeat
        [TestCase(696u, 617u, 902u)]
        public void CanWrite3MultipleValues_FailingValues(uint inValue1, uint inValue2, uint inValue3)
        {
            this.writer.Write(inValue1, 10);
            this.writer.Write(inValue2, 10);
            this.writer.Write(inValue3, 10);

            this.reader.Reset(this.writer.ToArraySegment());

            var outValue1 = this.reader.Read(10);
            var outValue2 = this.reader.Read(10);
            var outValue3 = this.reader.Read(10);

            Assert.That(outValue1, Is.EqualTo(inValue1), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue2, Is.EqualTo(inValue2), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue3, Is.EqualTo(inValue3), $"Failed [{inValue1},{inValue2},{inValue3}]");
        }

        [Test]
        [Repeat(1000)]
        public void CanWrite8MultipleValuesRepeat()
        {
            var inValue1 = this.random.Uint(0, max);
            var inValue2 = this.random.Uint(0, max);
            var inValue3 = this.random.Uint(0, max);
            var inValue4 = this.random.Uint(0, max);
            var inValue5 = this.random.Uint(0, max);
            var inValue6 = this.random.Uint(0, max);
            var inValue7 = this.random.Uint(0, max);
            var inValue8 = this.random.Uint(0, max);

            this.writer.Write(inValue1, 10);
            this.writer.Write(inValue2, 10);
            this.writer.Write(inValue3, 10);
            this.writer.Write(inValue4, 10);
            this.writer.Write(inValue5, 10);
            this.writer.Write(inValue6, 10);
            this.writer.Write(inValue7, 10);
            this.writer.Write(inValue8, 10);

            this.reader.Reset(this.writer.ToArraySegment());

            var outValue1 = this.reader.Read(10);
            var outValue2 = this.reader.Read(10);
            var outValue3 = this.reader.Read(10);
            var outValue4 = this.reader.Read(10);
            var outValue5 = this.reader.Read(10);
            var outValue6 = this.reader.Read(10);
            var outValue7 = this.reader.Read(10);
            var outValue8 = this.reader.Read(10);

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
        [TestCase(660u, 590u, 670u, 1014u, 121u, 743u, 228u, 126u)]
        public void CanWrite8MultipleValues_FailingValues(uint inValue1, uint inValue2, uint inValue3, uint inValue4, uint inValue5, uint inValue6, uint inValue7, uint inValue8)
        {
            this.writer.Write(inValue1, 10);
            this.writer.Write(inValue2, 10);
            this.writer.Write(inValue3, 10);
            this.writer.Write(inValue4, 10);
            this.writer.Write(inValue5, 10);
            this.writer.Write(inValue6, 10);
            this.writer.Write(inValue7, 10);
            this.writer.Write(inValue8, 10);

            this.reader.Reset(this.writer.ToArraySegment());

            var outValue1 = this.reader.Read(10);
            Assert.That(outValue1, Is.EqualTo(inValue1), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue2 = this.reader.Read(10);
            Assert.That(outValue2, Is.EqualTo(inValue2), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue3 = this.reader.Read(10);
            Assert.That(outValue3, Is.EqualTo(inValue3), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue4 = this.reader.Read(10);
            Assert.That(outValue4, Is.EqualTo(inValue4), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue5 = this.reader.Read(10);
            Assert.That(outValue5, Is.EqualTo(inValue5), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue6 = this.reader.Read(10);
            Assert.That(outValue6, Is.EqualTo(inValue6), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue7 = this.reader.Read(10);
            Assert.That(outValue7, Is.EqualTo(inValue7), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            var outValue8 = this.reader.Read(10);
            Assert.That(outValue8, Is.EqualTo(inValue8), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
        }


        [Test]
        public void CopiesValuesToSegment()
        {
            const int Count = 100;
            var bytes = Enumerable.Range(0, Count).Select(x => (byte)x).ToArray();
            foreach (var b in bytes)
            {
                this.writer.Write(b, 8);
            }
            Assert.That(this.writer.ByteLength, Is.EqualTo(Count), $"Should have written {Count} bytes");

            var array = this.writer.ToArray();
            for (var i = 0; i < Count; i++)
            {
                Assert.That(array[i], Is.EqualTo(bytes[i]), "values should be equal");
            }
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 3)]
        [TestCase(2, 5)]
        [TestCase(3, 8)]
        [TestCase(4, 10)]
        [TestCase(5, 13)]
        [TestCase(6, 15)]
        [TestCase(7, 18)]
        [TestCase(8, 20)]
        [TestCase(9, 23)]
        public void SegmentHasCorrectLength(int numberOf20Writes, int expectedSegmentSize)
        {
            for (var i = 0; i < numberOf20Writes; i++)
            {
                this.writer.Write(0, 20);
            }
            Assert.That(this.writer.ByteLength, Is.EqualTo(expectedSegmentSize), $"Should have written {expectedSegmentSize} bytes");

            var segment = this.writer.ToArray();
            Assert.That(segment.Count, Is.EqualTo(expectedSegmentSize), $"Segment should be {expectedSegmentSize} bytes");
        }
    }
}
