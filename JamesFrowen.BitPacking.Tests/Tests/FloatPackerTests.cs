using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace JamesFrowen.BitPacking.Tests
{
    public class FloatPackerTests : BitWriterTestBase
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
            FloatPacker packer = new FloatPacker(0, max, percision);

            packer.Pack(writer, inValue);

            reader.CopyToBuffer(writer.ToArray());
            float outValue = packer.Unpack(reader);


            Assert.That(outValue, Is.Not.NaN, "x was NaN");

            Assert.That(outValue, Is.EqualTo(inValue).Within(percision * 2), $"value off by {Mathf.Abs(inValue - outValue)}");
        }
    }
}
