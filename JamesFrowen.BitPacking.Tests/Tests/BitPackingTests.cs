using NUnit.Framework;
using System;

namespace JamesFrowen.BitPacking.Tests
{
    public class BitPackingTests
    {
        private NetworkWriter writer;
        private NetworkReader reader;

        [SetUp]
        public void Setup()
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
        [TestCase(0ul)]
        [TestCase(1ul)]
        [TestCase(0x_FFFF_FFFF_12Ul)]
        public void WritesCorrectUlongValue(ulong value)
        {
            this.writer.Write(value, 64);
            this.reader.Reset(this.writer.ToArray());

            ulong result = this.reader.Read(64);
            Assert.That(result, Is.EqualTo(value));
        }

        [Test]
        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(0x_FFFF_FF12U)]
        public void WritesCorrectUIntValue(uint value)
        {
            this.writer.Write(value, 32);
            this.reader.Reset(this.writer.ToArray());

            ulong result = this.reader.Read(32);
            Assert.That(result, Is.EqualTo(value));
        }

        [Test]
        [TestCase(0u, 10, 2u, 5)]
        [TestCase(10u, 10, 36u, 15)]
        [TestCase(1u, 1, 250u, 8)]
        public void WritesCorrectValues(uint value1, int bits1, uint value2, int bits2)
        {
            this.writer.Write(value1, bits1);
            this.writer.Write(value2, bits2);
            this.reader.Reset(this.writer.ToArray());

            ulong result1 = this.reader.Read(bits1);
            ulong result2 = this.reader.Read(bits2);
            Assert.That(result1, Is.EqualTo(value1));
            Assert.That(result2, Is.EqualTo(value2));
        }


        [Test]
        public void CanWriteToBufferLimit()
        {
            for (int i = 0; i < 208; i++)
            {
                this.writer.Write((ulong)i, 50);
            }

            byte[] result = this.writer.ToArray();

            // written bits/8
            int expectedLength = (208 * 50) / 8;
            Assert.That(result, Has.Length.EqualTo(expectedLength));
        }

        [Test, Description("Real buffer size is 1304 because 1300 rounds up")]
        public void WriterThrowIfWritesTooMuch()
        {
            // write 1296 up to last word
            for (int i = 0; i < 162; i++)
            {
                this.writer.Write((ulong)i, 64);
            }

            this.writer.Write(0, 63);

            Assert.DoesNotThrow(() =>
            {
                this.writer.Write(0, 1);
            });

            IndexOutOfRangeException exception = Assert.Throws<IndexOutOfRangeException>(() =>
            {
                this.writer.Write(0, 1);
            });
            Assert.That(exception, Has.Message.EqualTo("Index was outside the bounds of the array."));
        }

        [Test]
        [Repeat(10)]
        public void WritesAllValueSizesCorrectly([Range(0, 63)] int writerBits, [Range(0, 64)] int valueBits)
        {
            ulong randomValue = ULongRandom.Next();
            this.writer.Write(0, writerBits);

            ulong maskedValue = randomValue & BitMask.Mask(valueBits);

            this.writer.Write(randomValue, valueBits);
            this.reader.Reset(this.writer.ToArray());

            _ = this.reader.Read(writerBits);
            ulong result = this.reader.Read(valueBits);
            Assert.That(result, Is.EqualTo(maskedValue));
        }
    }

    public static class ULongRandom
    {
        static Random rand;
        static byte[] bytes;

        public static unsafe ulong Next()
        {
            if (rand == null)
            {
                rand = new System.Random();
                bytes = new byte[8];
            }

            rand.NextBytes(bytes);
            fixed (byte* ptr = &bytes[0])
            {
                return *(ulong*)ptr;
            }
        }
    }
}
