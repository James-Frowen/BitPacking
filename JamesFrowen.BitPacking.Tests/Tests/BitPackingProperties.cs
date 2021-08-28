using NUnit.Framework;

namespace Mirage.Serialization.Tests
{
    public class BitPackingProperties
    {
        private readonly NetworkWriter writer = new NetworkWriter(1300);
        private readonly NetworkReader reader = new NetworkReader();

        readonly byte[] sampleData = new byte[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        const int BITS_PER_BYTE = 8;

        [TearDown]
        public void TearDown()
        {
            this.writer.Reset();
            this.reader.Dispose();
        }


        [Test]
        public void WriterBitPositionStartsAtZero()
        {
            Assert.That(this.writer.BitPosition, Is.EqualTo(0));
        }

        [Test]
        public void WriterByteLengthStartsAtZero()
        {
            Assert.That(this.writer.ByteLength, Is.EqualTo(0));
        }

        [Test]
        public void ReaderBitPositionStartsStartsAtZero()
        {
            this.reader.Reset(this.sampleData);
            Assert.That(this.reader.BitPosition, Is.EqualTo(0));
        }

        [Test]
        public void ReaderBytePositionStartsStartsAtZero()
        {
            this.reader.Reset(this.sampleData);
            Assert.That(this.reader.BytePosition, Is.EqualTo(0));
        }

        [Test]
        public void ReaderBitLengthStartsStartsAtArrayLength()
        {
            this.reader.Reset(this.sampleData);
            Assert.That(this.reader.BitLength, Is.EqualTo(this.sampleData.Length * BITS_PER_BYTE));
        }



        [Test]
        public void WriterBitPositionIncreasesAfterWriting()
        {
            this.writer.Write(0, 15);
            Assert.That(this.writer.BitPosition, Is.EqualTo(15));

            this.writer.Write(0, 50);
            Assert.That(this.writer.BitPosition, Is.EqualTo(65));
        }

        [Test]
        public void WriterByteLengthIncreasesAfterWriting_ShouldRoundUp()
        {
            this.writer.Write(0, 15);
            Assert.That(this.writer.ByteLength, Is.EqualTo(2));

            this.writer.Write(0, 50);
            Assert.That(this.writer.ByteLength, Is.EqualTo(9));
        }

        [Test]
        public void ReaderBitPositionIncreasesAfterReading()
        {
            this.reader.Reset(this.sampleData);
            _ = this.reader.Read(15);
            Assert.That(this.reader.BitPosition, Is.EqualTo(15));

            _ = this.reader.Read(50);
            Assert.That(this.reader.BitPosition, Is.EqualTo(65));
        }

        [Test]
        public void ReaderBytePositionIncreasesAfterReading_ShouldRoundUp()
        {
            this.reader.Reset(this.sampleData);
            _ = this.reader.Read(15);
            Assert.That(this.reader.BytePosition, Is.EqualTo(2));

            _ = this.reader.Read(50);
            Assert.That(this.reader.BytePosition, Is.EqualTo(9));
        }

        [Test]
        public void ReaderBitLengthDoesnotIncreasesAfterReading()
        {
            this.reader.Reset(this.sampleData);
            _ = this.reader.Read(15);
            Assert.That(this.reader.BitLength, Is.EqualTo(this.sampleData.Length * BITS_PER_BYTE));

            _ = this.reader.Read(50);
            Assert.That(this.reader.BitLength, Is.EqualTo(this.sampleData.Length * BITS_PER_BYTE));
        }
    }
}
