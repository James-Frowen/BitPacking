using Mirage.Serialization;
using NUnit.Framework;

namespace JamesFrowen.BitPacking.Tests
{
    public class BitPackingResizeTest
    {
        private NetworkWriter writer;
        private NetworkReader reader;

        [SetUp]
        public void SetUp()
        {
            this.writer = new NetworkWriter(1300, true);
            this.reader = new NetworkReader();
        }

        [TearDown]
        public void TearDown()
        {
            // we have to clear these each time so that capactity doesn't effect other tests
            this.writer.Reset();
            this.writer = null;
            this.reader.Dispose();
            this.reader = null;
        }

        [Test]
#if !UNITY_EDITOR
        [Ignore("Debug.LogWarning Requires unity engine to run")]
#endif
        public void ResizesIfWritingOverCapacity()
        {
            int overCapacity = (1300 / 8) + 10;
            Assert.That(this.writer.ByteCapacity, Is.EqualTo(1304), "is first multiple of 8 over 1300");
            for (int i = 0; i < overCapacity; i++)
            {
                this.writer.WriteUInt64((ulong)i);
            }

            Assert.That(this.writer.ByteCapacity, Is.EqualTo(1304 * 2), "should double in size");
        }


        [Test]
#if !UNITY_EDITOR
        [Ignore("Debug.LogWarning Requires unity engine to run")]
#endif
        public void WillResizeMultipleTimes()
        {
            int overCapacity = ((1300 / 8) + 10) * 10; // 1720 * 8 = 13760 bytes

            Assert.That(this.writer.ByteCapacity, Is.EqualTo(1304), "is first multiple of 8 over 1300");
            for (int i = 0; i < overCapacity; i++)
            {
                this.writer.WriteUInt64((ulong)i);
            }


            Assert.That(this.writer.ByteCapacity, Is.EqualTo(20_864), "should double each time it goes over capacity");
        }

        [Test]
#if !UNITY_EDITOR
        [Ignore("Debug.LogWarning Requires unity engine to run")]
#endif
        public void ResizedArrayContainsAllData()
        {
            int overCapacity = (1300 / 8) + 10;
            for (int i = 0; i < overCapacity; i++)
            {
                this.writer.WriteUInt64((ulong)i);
            }


            var segment = this.writer.ToArraySegment();
            this.reader.Reset(segment);
            for (int i = 0; i < overCapacity; i++)
            {
                Assert.That(this.reader.ReadUInt64(), Is.EqualTo((ulong)i));
            }
        }
    }
    public class BitPackingReusingWriterTests
    {
        private NetworkWriter writer;
        private NetworkReader reader;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            this.writer = new NetworkWriter(1300);
            this.reader = new NetworkReader();
        }

        [TearDown]
        public void TearDown()
        {
            this.writer.Reset();
            this.reader.Dispose();
        }


        [Test]
        public void WriteUShortAfterReset()
        {
            ushort value1 = 0b0101;
            ushort value2 = 0x1000;

            // write first value
            this.writer.WriteUInt16(value1);

            this.reader.Reset(this.writer.ToArray());
            ushort out1 = this.reader.ReadUInt16();
            Assert.That(out1, Is.EqualTo(value1));

            // reset and write 2nd value
            this.writer.Reset();

            this.writer.WriteUInt16(value2);

            this.reader.Reset(this.writer.ToArray());
            ushort out2 = this.reader.ReadUInt16();
            Assert.That(out2, Is.EqualTo(value2), "Value 2 was incorrect");
        }

        [Test]
        [TestCase(0b0101ul, 0x1000ul)]
        [TestCase(0xffff_0000_ffff_fffful, 0x0000_ffff_1111_0000ul)]
        public void WriteULongAfterReset(ulong value1, ulong value2)
        {
            // write first value
            this.writer.WriteUInt64(value1);

            this.reader.Reset(this.writer.ToArray());
            ulong out1 = this.reader.ReadUInt64();
            Assert.That(out1, Is.EqualTo(value1));

            // reset and write 2nd value
            this.writer.Reset();

            this.writer.WriteUInt64(value2);

            this.reader.Reset(this.writer.ToArray());
            ulong out2 = this.reader.ReadUInt64();
            Assert.That(out2, Is.EqualTo(value2), "Value 2 was incorrect");
        }

        [Test]
        [TestCase(0b0101ul, 0x1000ul)]
        [TestCase(0xffff_0000_ffff_fffful, 0x0000_ffff_1111_0000ul)]
        public void WriteULongWriteBitsAfterReset(ulong value1, ulong value2)
        {
            // write first value
            this.writer.Write(value1, 64);

            this.reader.Reset(this.writer.ToArray());
            ulong out1 = this.reader.Read(64);
            Assert.That(out1, Is.EqualTo(value1));

            // reset and write 2nd value
            this.writer.Reset();

            this.writer.Write(value2, 64);

            this.reader.Reset(this.writer.ToArray());
            ulong out2 = this.reader.Read(64);
            Assert.That(out2, Is.EqualTo(value2), "Value 2 was incorrect");
        }
    }
}
