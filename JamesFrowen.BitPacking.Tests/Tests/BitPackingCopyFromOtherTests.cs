using NUnit.Framework;

namespace Mirage.Serialization.Tests
{
    public class BitPackingCopyFromOtherTests
    {
        private NetworkWriter writer;
        private NetworkWriter otherWriter;
        private NetworkReader reader;

        [SetUp]
        public void Setup()
        {
            this.writer = new NetworkWriter(1300);
            this.otherWriter = new NetworkWriter(1300);
            this.reader = new NetworkReader();
        }

        [TearDown]
        public void TearDown()
        {
            this.writer.Reset();
            this.otherWriter.Reset();
            this.reader.Dispose();
        }

        [Test]
        public void CopyFromOtherWriterAligned()
        {
            this.otherWriter.Write(1, 8);
            this.otherWriter.Write(2, 8);
            this.otherWriter.Write(3, 8);
            this.otherWriter.Write(4, 8);
            this.otherWriter.Write(5, 8);


            this.writer.CopyFromWriter(this.otherWriter, 0, 5 * 8);

            var segment = this.writer.ToArraySegment();
            this.reader.Reset(segment);

            Assert.That(this.reader.Read(8), Is.EqualTo(1));
            Assert.That(this.reader.Read(8), Is.EqualTo(2));
            Assert.That(this.reader.Read(8), Is.EqualTo(3));
            Assert.That(this.reader.Read(8), Is.EqualTo(4));
            Assert.That(this.reader.Read(8), Is.EqualTo(5));
        }

        [Test]
        public void CopyFromOtherWriterUnAligned()
        {
            this.otherWriter.Write(1, 6);
            this.otherWriter.Write(2, 7);
            this.otherWriter.Write(3, 8);
            this.otherWriter.Write(4, 9);
            this.otherWriter.Write(5, 10);

            this.writer.Write(1, 3);

            this.writer.CopyFromWriter(this.otherWriter, 0, 40);

            var segment = this.writer.ToArraySegment();
            this.reader.Reset(segment);

            Assert.That(this.reader.Read(3), Is.EqualTo(1));
            Assert.That(this.reader.Read(6), Is.EqualTo(1));
            Assert.That(this.reader.Read(7), Is.EqualTo(2));
            Assert.That(this.reader.Read(8), Is.EqualTo(3));
            Assert.That(this.reader.Read(9), Is.EqualTo(4));
            Assert.That(this.reader.Read(10), Is.EqualTo(5));
        }

        [Test]
        [Repeat(100)]
        public void CopyFromOtherWriterUnAlignedBig()
        {
            ulong value1 = (ulong)UnityEngine.Random.Range(0, 20000);
            ulong value2 = (ulong)UnityEngine.Random.Range(0, 20000);
            ulong value3 = (ulong)UnityEngine.Random.Range(0, 20000);
            ulong value4 = (ulong)UnityEngine.Random.Range(0, 20000);
            ulong value5 = (ulong)UnityEngine.Random.Range(0, 20000);
            this.otherWriter.Write(value1, 46);
            this.otherWriter.Write(value2, 47);
            this.otherWriter.Write(value3, 48);
            this.otherWriter.Write(value4, 49);
            this.otherWriter.Write(value5, 50);

            this.writer.WriteUInt64(5);
            this.writer.Write(1, 3);
            this.writer.WriteByte(171);

            this.writer.CopyFromWriter(this.otherWriter, 0, 240);

            var segment = this.writer.ToArraySegment();
            this.reader.Reset(segment);

            Assert.That(this.reader.ReadUInt64(), Is.EqualTo(5ul));
            Assert.That(this.reader.Read(3), Is.EqualTo(1));
            Assert.That(this.reader.ReadByte(), Is.EqualTo(171));
            Assert.That(this.reader.Read(46), Is.EqualTo(value1), "Random value 1 not correct");
            Assert.That(this.reader.Read(47), Is.EqualTo(value2), "Random value 2 not correct");
            Assert.That(this.reader.Read(48), Is.EqualTo(value3), "Random value 3 not correct");
            Assert.That(this.reader.Read(49), Is.EqualTo(value4), "Random value 4 not correct");
            Assert.That(this.reader.Read(50), Is.EqualTo(value5), "Random value 5 not correct");
        }

        [Test]
        [Repeat(100)]
        public void CopyFromOtherWriterUnAlignedBigOtherUnaligned()
        {
            for (int i = 0; i < 10; i++)
            {
                this.otherWriter.Write(12, 20);
            }


            ulong value1 = (ulong)UnityEngine.Random.Range(0, 20000);
            ulong value2 = (ulong)UnityEngine.Random.Range(0, 20000);
            ulong value3 = (ulong)UnityEngine.Random.Range(0, 20000);
            ulong value4 = (ulong)UnityEngine.Random.Range(0, 20000);
            ulong value5 = (ulong)UnityEngine.Random.Range(0, 20000);
            this.otherWriter.Write(value1, 46);
            this.otherWriter.Write(value2, 47);
            this.otherWriter.Write(value3, 48);
            this.otherWriter.Write(value4, 49);
            this.otherWriter.Write(value5, 50);

            this.writer.WriteUInt64(5);
            this.writer.Write(1, 3);
            this.writer.WriteByte(171);

            this.writer.CopyFromWriter(this.otherWriter, 200, 240);

            var segment = this.writer.ToArraySegment();
            this.reader.Reset(segment);

            Assert.That(this.reader.ReadUInt64(), Is.EqualTo(5ul));
            Assert.That(this.reader.Read(3), Is.EqualTo(1));
            Assert.That(this.reader.ReadByte(), Is.EqualTo(171));
            Assert.That(this.reader.Read(46), Is.EqualTo(value1), "Random value 1 not correct");
            Assert.That(this.reader.Read(47), Is.EqualTo(value2), "Random value 2 not correct");
            Assert.That(this.reader.Read(48), Is.EqualTo(value3), "Random value 3 not correct");
            Assert.That(this.reader.Read(49), Is.EqualTo(value4), "Random value 4 not correct");
            Assert.That(this.reader.Read(50), Is.EqualTo(value5), "Random value 5 not correct");
        }
    }
}
