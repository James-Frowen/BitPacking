using NUnit.Framework;
using System;
using Random = System.Random;

namespace JamesFrowen.BitPacking.Tests.Packers
{
    [TestFixture(4)]
    [TestFixture(7)]
    [TestFixture(8)]
    [TestFixture(12)]
    [TestFixture(16)]
    public class UintBlockPackerTests : PackerTestBase
    {
        readonly Random random = new Random();
        readonly int blockSize;

        public UintBlockPackerTests(int blockSize)
        {
            this.blockSize = blockSize;
        }


        ulong GetRandonUlongBias()
        {
            return (ulong)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * ulong.MaxValue);
        }

        uint GetRandonUintBias()
        {
            return (uint)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * uint.MaxValue);
        }

        ushort GetRandonUshortBias()
        {
            return (ushort)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * ushort.MaxValue);
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUlongValue()
        {
            ulong start = this.GetRandonUlongBias();
            VariableBlockPacker.Pack(this.writer, start, this.blockSize);
            ulong unpacked = VariableBlockPacker.Unpack(this.GetReader(), this.blockSize);

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUintValue()
        {
            uint start = this.GetRandonUintBias();
            VariableBlockPacker.Pack(this.writer, start, this.blockSize);
            ulong unpacked = VariableBlockPacker.Unpack(this.GetReader(), this.blockSize);

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUshortValue()
        {
            ushort start = this.GetRandonUshortBias();
            VariableBlockPacker.Pack(this.writer, start, this.blockSize);
            ulong unpacked = VariableBlockPacker.Unpack(this.GetReader(), this.blockSize);

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        public void WritesNplus1BitsPerBlock()
        {
            uint zero = 0u;
            VariableBlockPacker.Pack(this.writer, zero, this.blockSize);
            Assert.That(this.writer.BitPosition, Is.EqualTo(this.blockSize + 1));

            ulong unpacked = VariableBlockPacker.Unpack(this.GetReader(), this.blockSize);
            Assert.That(unpacked, Is.EqualTo(zero));
        }

        [Test]
        public void WritesNplus1BitsPerBlock_bigger()
        {
            uint aboveBlockSize = (1u << this.blockSize) + 1u;
            VariableBlockPacker.Pack(this.writer, aboveBlockSize, this.blockSize);
            Assert.That(this.writer.BitPosition, Is.EqualTo(2 * (this.blockSize + 1)));

            ulong unpacked = VariableBlockPacker.Unpack(this.GetReader(), this.blockSize);
            Assert.That(unpacked, Is.EqualTo(aboveBlockSize));
        }
    }
}
