using System.Linq;
using NUnit.Framework;

namespace JamesFrowen.BitPacking.Tests
{
    public abstract class BitWriterTestBase
    {
        private const int BufferSize = 1200;
        protected BitWriter writer;
        protected BitReader reader;

        [SetUp]
        public void SetUp()
        {
            writer = new BitWriter(BufferSize);
            reader = new BitReader(BufferSize);
        }


        [TearDown]
        public void TearDown()
        {
            writer = null;
            reader = null;
        }
    }

    public class BitWriterTest : BitWriterTestBase
    {
        const int max = (1 << 10) - 1;

        TestRandom random = new TestRandom();

        [Test]
        public void WritesCorrectBits()
        {
            uint bits1 = 0b1011;
            uint bits2 = 0b10_0111_1001;
            byte expected1 = (byte)bits1;
            byte expected2_1 = 0b1001_1011;
            byte expected2_2 = 0b10_0111;

            {
                Assert.That(writer.GetByteCount(), Is.Zero, "Should start at length 0");
                writer.Write(bits1, 4);

                Assert.That(writer.GetByteCount(), Is.EqualTo(1), "should have length 1 after writing 4 bits");

                byte[] array = writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(1), "segment length should by 1 after writing 4 bits");
                byte first = array[0];

                Assert.That(first, Is.EqualTo(expected1));
            }

            {
                writer.Write(bits2, 10);
                Assert.That(writer.GetByteCount(), Is.EqualTo(2), "should have length 2 after writing 14 bits");

                byte[] array = writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(2), "segment length should by 2 after writing 14 bits");
                byte first = array[0];
                byte second = array[1];

                Assert.That(first, Is.EqualTo(expected2_1));
                Assert.That(second, Is.EqualTo(expected2_2));
            }
        }

        [Test]
        public void ReadsCorrectBits()
        {
            // numbers from WritesCorrectBits
            byte[] buffers = new byte[10];
            buffers[0] = 0b1001_1011;
            buffers[1] = 0b10_0111;

            uint expected1 = 0b1011;
            int count1 = 4;
            uint expected2 = 0b10_0111_1001;
            int count2 = 10;

            reader.CopyToBuffer(buffers, 0, buffers.Length);

            {
                ulong value = reader.Read(count1);
                Assert.That(reader.GetBitPosition(), Is.EqualTo(count1));

                Assert.That(value, Is.EqualTo(expected1));
            }

            {
                ulong value = reader.Read(count2);
                Assert.That(reader.GetBitPosition(), Is.EqualTo(count1 + count2));

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


            byte[] expected1 = new byte[] {
                0b01111000,
                0b01010110,
                0b00110100,
                0b00010010,
            };

            // reverse because array is left to right, but bits above are right to left.
            byte[] expected2 = new byte[] {
                0b00000110,
                0b10101111,
                0b00110111,
                0b10111100,

                0b00010010,
                0b00110100,
                0b01010110,
                0b01111000
            }.Reverse().ToArray();

            byte[] expected3 = new byte[] {
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
                Assert.That(writer.GetByteCount(), Is.Zero, "Should start at length 0");
                writer.Write(bits1, 30);

                Assert.That(writer.GetByteCount(), Is.EqualTo(4), "should have length 4 after writing 30 bits");

                byte[] array = writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(4), "segment length should by 4 after writing 30 bits");
                for (int i = 0; i < 4; i++)
                {
                    Assert.That(array[i], Is.EqualTo(expected1[i]));
                }
            }

            {
                writer.Write(bits2, 30);

                Assert.That(writer.GetByteCount(), Is.EqualTo(8), "should have length 8 after writing 60 bits");

                byte[] array = writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(8), "segment length should by 8 after writing 60 bits");
                for (int i = 0; i < 8; i++)
                {
                    Assert.That(array[i], Is.EqualTo(expected2[i]));
                }
            }

            {
                writer.Write(bits3, 30);

                Assert.That(writer.GetByteCount(), Is.EqualTo(12), "should have length 12 after writing 90 bits");

                byte[] array = writer.ToArray();
                Assert.That(array.Count, Is.EqualTo(12), "segment length should by 12 after writing 90 bits");
                for (int i = 0; i < 12; i++)
                {
                    Assert.That(array[i], Is.EqualTo(expected3[i]));
                }
            }
        }

        [Test]
        public void ReadsCorrectBitsLong()
        {
            // values from WritesCorrectBitsLong
            byte[] buffers = new byte[] {
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


            reader.CopyToBuffer(buffers, 0, buffers.Length);

            const int readLength = 30;

            {
                ulong value = reader.Read(readLength);
                Assert.That(reader.GetBitPosition(), Is.EqualTo(readLength));

                Assert.That(value, Is.EqualTo(expected1));
            }

            {
                ulong value = reader.Read(readLength);
                Assert.That(reader.GetBitPosition(), Is.EqualTo(readLength * 2));

                Assert.That(value, Is.EqualTo(expected2));
            }

            {
                ulong value = reader.Read(readLength);
                Assert.That(reader.GetBitPosition(), Is.EqualTo(readLength * 3));

                Assert.That(value, Is.EqualTo(expected3));
            }
        }


        [Test]
        [Repeat(1000)]
        public void CanWrite32BitsRepeat()
        {
            uint inValue = random.Uint(0, int.MaxValue);

            writer.Write(inValue, 32);

            reader.CopyToBuffer(writer.ToArray());

            ulong outValue = reader.Read(32);

            Assert.That(outValue, Is.EqualTo(inValue));
        }

        [Test]
        [Repeat(1000)]
        public void CanWrite3MultipleValuesRepeat()
        {
            uint inValue1 = random.Uint(0, max);
            uint inValue2 = random.Uint(0, max);
            uint inValue3 = random.Uint(0, max);

            writer.Write(inValue1, 10);
            writer.Write(inValue2, 10);
            writer.Write(inValue3, 10);

            reader.CopyToBuffer(writer.ToArray());

            ulong outValue1 = reader.Read(10);
            ulong outValue2 = reader.Read(10);
            ulong outValue3 = reader.Read(10);

            Assert.That(outValue1, Is.EqualTo(inValue1), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue2, Is.EqualTo(inValue2), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue3, Is.EqualTo(inValue3), $"Failed [{inValue1},{inValue2},{inValue3}]");
        }

        [Test]
        // failing values from repeat
        [TestCase(696u, 617u, 902u)]
        public void CanWrite3MultipleValues_FailingValues(uint inValue1, uint inValue2, uint inValue3)
        {
            writer.Write(inValue1, 10);
            writer.Write(inValue2, 10);
            writer.Write(inValue3, 10);

            reader.CopyToBuffer(writer.ToArray());

            ulong outValue1 = reader.Read(10);
            ulong outValue2 = reader.Read(10);
            ulong outValue3 = reader.Read(10);

            Assert.That(outValue1, Is.EqualTo(inValue1), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue2, Is.EqualTo(inValue2), $"Failed [{inValue1},{inValue2},{inValue3}]");
            Assert.That(outValue3, Is.EqualTo(inValue3), $"Failed [{inValue1},{inValue2},{inValue3}]");
        }

        [Test]
        [Repeat(1000)]
        public void CanWrite8MultipleValuesRepeat()
        {
            uint inValue1 = random.Uint(0, max);
            uint inValue2 = random.Uint(0, max);
            uint inValue3 = random.Uint(0, max);
            uint inValue4 = random.Uint(0, max);
            uint inValue5 = random.Uint(0, max);
            uint inValue6 = random.Uint(0, max);
            uint inValue7 = random.Uint(0, max);
            uint inValue8 = random.Uint(0, max);

            writer.Write(inValue1, 10);
            writer.Write(inValue2, 10);
            writer.Write(inValue3, 10);
            writer.Write(inValue4, 10);
            writer.Write(inValue5, 10);
            writer.Write(inValue6, 10);
            writer.Write(inValue7, 10);
            writer.Write(inValue8, 10);

            reader.CopyToBuffer(writer.ToArray());

            ulong outValue1 = reader.Read(10);
            ulong outValue2 = reader.Read(10);
            ulong outValue3 = reader.Read(10);
            ulong outValue4 = reader.Read(10);
            ulong outValue5 = reader.Read(10);
            ulong outValue6 = reader.Read(10);
            ulong outValue7 = reader.Read(10);
            ulong outValue8 = reader.Read(10);

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
            writer.Write(inValue1, 10);
            writer.Write(inValue2, 10);
            writer.Write(inValue3, 10);
            writer.Write(inValue4, 10);
            writer.Write(inValue5, 10);
            writer.Write(inValue6, 10);
            writer.Write(inValue7, 10);
            writer.Write(inValue8, 10);

            reader.CopyToBuffer(writer.ToArray());

            ulong outValue1 = reader.Read(10);
            Assert.That(outValue1, Is.EqualTo(inValue1), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            ulong outValue2 = reader.Read(10);
            Assert.That(outValue2, Is.EqualTo(inValue2), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            ulong outValue3 = reader.Read(10);
            Assert.That(outValue3, Is.EqualTo(inValue3), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            ulong outValue4 = reader.Read(10);
            Assert.That(outValue4, Is.EqualTo(inValue4), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            ulong outValue5 = reader.Read(10);
            Assert.That(outValue5, Is.EqualTo(inValue5), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            ulong outValue6 = reader.Read(10);
            Assert.That(outValue6, Is.EqualTo(inValue6), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            ulong outValue7 = reader.Read(10);
            Assert.That(outValue7, Is.EqualTo(inValue7), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");

            ulong outValue8 = reader.Read(10);
            Assert.That(outValue8, Is.EqualTo(inValue8), $"Failed [{inValue1},{inValue2},{inValue3},{inValue4},{inValue5},{inValue6},{inValue7},{inValue8}]");
        }


        [Test]
        public void CopiesValuesToSegment()
        {
            const int Count = 100;
            byte[] bytes = Enumerable.Range(0, Count).Select(x => (byte)x).ToArray();
            foreach (byte b in bytes)
            {
                writer.Write(b, 8);
            }
            Assert.That(writer.GetByteCount(), Is.EqualTo(Count), $"Should have written {Count} bytes");

            byte[] array = writer.ToArray();
            for (int i = 0; i < Count; i++)
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
            for (int i = 0; i < numberOf20Writes; i++)
            {
                writer.Write(0, 20);
            }
            Assert.That(writer.GetByteCount(), Is.EqualTo(expectedSegmentSize), $"Should have written {expectedSegmentSize} bytes");

            byte[] segment = writer.ToArray();
            Assert.That(segment.Count, Is.EqualTo(expectedSegmentSize), $"Segment should be {expectedSegmentSize} bytes");
        }
    }
}
