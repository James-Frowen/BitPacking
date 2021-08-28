using Mirage.Serialization;
using NUnit.Framework;
using Random = JamesFrowen.BitPacking.Tests.TestRandom;

namespace JamesFrowen.BitPacking.Tests.Packers
{
    [TestFixture(100, 0.1f)]
    [TestFixture(500, 0.02f)]
    [TestFixture(2000, 0.05f)]
    [TestFixture(1.5f, 0.01f)]
    [TestFixture(100_000, 30)]
    public class FloatPackerTests : PackerTestBase
    {
        readonly FloatPacker packer;
        readonly float max;
        readonly float precsion;

        public FloatPackerTests(float max, float precsion)
        {
            this.max = max;
            this.precsion = precsion;
            this.packer = new FloatPacker(max, precsion);
        }


        float GetRandomFloat()
        {
            return Random.Range(-this.max, this.max);
        }


        [Test]
        // takes about 1 seconds per 1000 values (including all fixtures)
        [Repeat(1000)]
        public void UnpackedValueIsWithinPrecision()
        {
            float start = this.GetRandomFloat();
            uint packed = this.packer.Pack(start);
            float unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(start).Within(this.precsion));
        }

        [Test]
        public void ValueOverMaxWillBeUnpackedAsMax()
        {
            float start = this.max * 1.2f;
            uint packed = this.packer.Pack(start);
            float unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(this.max).Within(this.precsion));
        }

        [Test]
        public void ValueUnderNegativeMaxWillBeUnpackedAsNegativeMax()
        {
            float start = this.max * -1.2f;
            uint packed = this.packer.Pack(start);
            float unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(-this.max).Within(this.precsion));
        }

        [Test]
        public void ZeroUnpackToExactlyZero()
        {
            const float zero = 0;
            uint packed = this.packer.Pack(zero);
            float unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(zero));
        }


        [Test]
        [Repeat(100)]
        public void UnpackedValueIsWithinPrecisionUsingWriter()
        {
            float start = this.GetRandomFloat();
            this.packer.Pack(this.writer, start);
            float unpacked = this.packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start).Within(this.precsion));
        }

        [Test]
        public void ValueOverMaxWillBeUnpackedUsingWriterAsMax()
        {
            float start = this.max * 1.2f;
            this.packer.Pack(this.writer, start);
            float unpacked = this.packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(this.max).Within(this.precsion));
        }

        [Test]
        public void ValueUnderNegativeMaxWillBeUnpackedUsingWriterAsNegativeMax()
        {
            float start = this.max * -1.2f;
            this.packer.Pack(this.writer, start);
            float unpacked = this.packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(-this.max).Within(this.precsion));
        }


        [Test]
        [Repeat(100)]
        public void UnpackedValueIsWithinPrecisionNoClamp()
        {
            float start = this.GetRandomFloat();
            uint packed = this.packer.PackNoClamp(start);
            float unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(start).Within(this.precsion));
        }

        [Test]
        [Repeat(100)]
        public void UnpackedValueIsWithinPrecisionNoClampUsingWriter()
        {
            float start = this.GetRandomFloat();
            this.packer.PackNoClamp(this.writer, start);
            float unpacked = this.packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start).Within(this.precsion));
        }
    }
}
