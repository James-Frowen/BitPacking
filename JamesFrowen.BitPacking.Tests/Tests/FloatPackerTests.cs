using NUnit.Framework;
using System.Collections;
using UnityEngine;

namespace Mirage.Serialization.Tests
{
    public abstract class NetworkWriterTestBase
    {
        private const int BufferSize = 1300;
        protected readonly NetworkWriter writer = new NetworkWriter(BufferSize);
        protected readonly NetworkReader reader = new NetworkReader();

        [TearDown]
        public void TearDown()
        {
            this.writer.Reset();
            this.reader.Dispose();
        }
    }
    public class FloatPackerTests : NetworkWriterTestBase
    {
        private const int BufferSize = 1000;

        TestRandom random = new TestRandom();

        static IEnumerable CompressesAndDecompressesCases()
        {
            yield return new TestCaseData(1269679f, 0.1005143f, 558430.4f);
        }

        [Test]
        [TestCaseSource(nameof(CompressesAndDecompressesCases))]
        public void PackAndUnpack(float max, float percision, float inValue)
        {
            var packer = new FloatPacker(0, max, percision);

            packer.Pack(this.writer, inValue);

            this.reader.Reset(this.writer.ToArraySegment());
            float outValue = packer.Unpack(this.reader);


            Assert.That(outValue, Is.Not.NaN, "x was NaN");

            Assert.That(outValue, Is.EqualTo(inValue).Within(percision * 2), $"value off by {Mathf.Abs(inValue - outValue)}");
        }
    }
}
