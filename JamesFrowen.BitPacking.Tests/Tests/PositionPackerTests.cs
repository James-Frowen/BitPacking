using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;

namespace Mirage.Serialization.Tests
{
    public class PositionPackerTests : NetworkWriterTestBase
    {
        private const int BufferSize = 1000;

        TestRandom random = new TestRandom();

        static IEnumerable PackAndUnpackCases
        {
            get
            {
                yield return new TestCaseData(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 0.01f, new Vector3(0, 0, 0));
                yield return new TestCaseData(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 0.01f, new Vector3(20, 20, 20));
                yield return new TestCaseData(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 0.01f, new Vector3(50, 50, 50));
                yield return new TestCaseData(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 0.01f, new Vector3(100, 100, 100));
            }
        }

        [Test]
        [TestCaseSource(nameof(PackAndUnpackCases))]
        public void PackAndUnpack(Vector3 min, Vector3 max, float precision, Vector3 inValue)
        {
            var packer = new PositionPacker(min, max, precision);

            packer.Pack(this.writer, inValue);

            this.reader.Reset(this.writer.ToArray());
            var outValue = packer.Unpack(this.reader);

            var debugMessage = $"in{inValue} out{outValue}";
            Assert.That(outValue.x, Is.EqualTo(inValue.x).Within(precision), debugMessage);
            Assert.That(outValue.y, Is.EqualTo(inValue.y).Within(precision), debugMessage);
            Assert.That(outValue.z, Is.EqualTo(inValue.z).Within(precision), debugMessage);
        }


        [Test]
        [TestCaseSource(nameof(PackAndUnpackCases))]
        public void PackHasCorrectLength(Vector3 min, Vector3 max, float precision, Vector3 inValue)
        {
            var packer = new PositionPacker(min, max, precision);

            packer.Pack(this.writer, inValue);

            Assert.That(this.writer.BitPosition, Is.EqualTo(packer.bitCount));
        }

        [Test]
        [TestCaseSource(nameof(PackAndUnpackCases))]
        public void UnpackHasCorrectLength(Vector3 min, Vector3 max, float precision, Vector3 inValue)
        {
            var packer = new PositionPacker(min, max, precision);

            packer.Pack(this.writer, inValue);

            this.reader.Reset(this.writer.ToArraySegment());
            var _ = packer.Unpack(this.reader);

            Assert.That(this.reader.BitPosition, Is.EqualTo(packer.bitCount));
        }


        static IEnumerable CompressesAndDecompressesCasesRepeat()
        {
            yield return new TestCaseData(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 0.01f);
            yield return new TestCaseData(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 0.05f);
            yield return new TestCaseData(new Vector3(-100, 0, -100), new Vector3(100, 100, 100), 0.01f);
            yield return new TestCaseData(new Vector3(-100, 0, -100), new Vector3(100, 100, 100), 0.05f);
            yield return new TestCaseData(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 0.1f);
            yield return new TestCaseData(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 0.5f);
            yield return new TestCaseData(new Vector3(-500, -100, -500), new Vector3(500, 100, 500), 0.5f);
            yield return new TestCaseData(new Vector3(0, -20, 0), new Vector3(500, 40, 500), 0.031f);
        }


        [Test]
        [TestCase(0u, 1u, ExpectedResult = 1)]
        [TestCase(0u, 1024u, ExpectedResult = 11)]
        [TestCase(0u, 1000u, ExpectedResult = 10)]
        [TestCase(0u, (uint)(int.MaxValue - 1), ExpectedResult = 31)]
        [TestCase(1000u, 2000u, ExpectedResult = 10)]
        public int BitCountFromRangeGivesCorrectValues(uint min, uint max)
        {
            return BitCountHelper.BitCountFromRange(min, max);
        }
        [Test]
        [TestCase(0u, 0u)]
        [TestCase(10u, 0u)]
        public void BitCountFromRangeThrowsForBadInputs(uint min, uint max)
        {
            var execption = Assert.Throws<ArgumentException>(() =>
            {
                BitCountHelper.BitCountFromRange(min, max);
            });

            Assert.That(execption, Has.Message.EqualTo($"Min:{min} is greater or equal to than Max:{max}"));
        }
    }
}
