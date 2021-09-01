using NUnit.Framework;
using System;
using Random = System.Random;

namespace JamesFrowen.BitPacking.Tests.Packers
{
    [TestFixture(50ul, 1_000ul, null)]
    [TestFixture(250ul, 10_000ul, null)]
    [TestFixture(500ul, 100_000ul, null)]
    [TestFixture(50ul, 1_000ul, 10_000_000ul)]
    [TestFixture(250ul, 10_000ul, 10_000_000ul)]
    [TestFixture(500ul, 100_000ul, 10_000_000ul)]
    public class UintPackerTests : PackerTestBase
    {
        readonly Random random = new Random();
        readonly VariableIntPacker packer;
        readonly ulong max;

        public UintPackerTests(ulong smallValue, ulong mediumValue, ulong? largeValue)
        {
            if (largeValue.HasValue)
            {
                this.packer = new VariableIntPacker(smallValue, mediumValue, largeValue.Value, false);
                this.max = largeValue.Value;
            }
            else
            {
                this.packer = new VariableIntPacker(smallValue, mediumValue);
                this.max = ulong.MaxValue;
            }
        }


        ulong GetRandonUlongBias()
        {
            return (ulong)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * this.max);
        }

        uint GetRandonUintBias()
        {
            return (uint)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * Math.Min(this.max, uint.MaxValue));
        }

        ushort GetRandonUshortBias()
        {
            return (ushort)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * Math.Min(this.max, ushort.MaxValue));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUlongValue()
        {
            ulong start = this.GetRandonUlongBias();
            this.packer.PackUlong(this.writer, start);
            ulong unpacked = this.packer.UnpackUlong(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUintValue()
        {
            uint start = this.GetRandonUintBias();
            this.packer.PackUint(this.writer, start);
            uint unpacked = this.packer.UnpackUint(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUshortValue()
        {
            ushort start = this.GetRandonUshortBias();
            this.packer.PackUshort(this.writer, start);
            ushort unpacked = this.packer.UnpackUshort(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start));
        }
    }
}
