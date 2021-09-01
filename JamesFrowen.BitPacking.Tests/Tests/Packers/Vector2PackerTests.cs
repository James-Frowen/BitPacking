using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = JamesFrowen.BitPacking.Tests.TestRandom;

namespace JamesFrowen.BitPacking.Tests.Packers
{
    public class Vector2PackerTests : PackerTestBase
    {
        static IEnumerable WriteCorrectNumberOfBitsCases()
        {
            yield return new TestCaseData(Vector2.one * 100, Vector2.one * 0.1f).Returns(11 * 2);
            yield return new TestCaseData(Vector2.one * 200, Vector2.one * 0.1f).Returns(12 * 2);
            yield return new TestCaseData(Vector2.one * 200, Vector2.one * 0.05f).Returns(13 * 2);
            yield return new TestCaseData(new Vector2(100, 50), Vector2.one * 0.1f).Returns(11 + 10);
            yield return new TestCaseData(new Vector2(100, 50), new Vector2(0.1f, 0.2f)).Returns(11 + 9);
        }

        [Test]
        [TestCaseSource(nameof(WriteCorrectNumberOfBitsCases))]
        public int WriteCorrectNumberOfBits(Vector2 max, Vector2 precision)
        {
            var packer = new Vector2Packer(max, precision);
            packer.Pack(this.writer, Vector2.zero);
            return this.writer.BitPosition;
        }

        static IEnumerable ThrowsIfAnyMaxIsZeroCases()
        {
            yield return new TestCaseData(new Vector2(100, 0), Vector2.one * 0.1f);
            yield return new TestCaseData(new Vector2(0, 100), Vector2.one * 0.1f);
        }

        [Test]
        [TestCaseSource(nameof(ThrowsIfAnyMaxIsZeroCases))]
        public void ThrowsIfAnyMaxIsZero(Vector2 max, Vector2 precision)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            {
                _ = new Vector2Packer(max, precision);
            });

            var expected = new ArgumentException("Max can not be 0", "max");
            Assert.That(exception, Has.Message.EqualTo(expected.Message));
        }


        static IEnumerable<TestCaseData> UnpacksToSaveValueCases()
        {
            yield return new TestCaseData(Vector2.one * 100, Vector2.one * 0.1f);
            yield return new TestCaseData(Vector2.one * 200, Vector2.one * 0.1f);
            yield return new TestCaseData(Vector2.one * 200, Vector2.one * 0.05f);
            yield return new TestCaseData(new Vector2(100, 50), Vector2.one * 0.1f);
            yield return new TestCaseData(new Vector2(100, 50), new Vector2(0.1f, 0.2f));
        }

        [Test]
        [TestCaseSource(nameof(UnpacksToSaveValueCases))]
        [Repeat(100)]
        public void UnpacksToSaveValue(Vector2 max, Vector2 precision)
        {
            var packer = new Vector2Packer(max, precision);
            var expected = new Vector2(
                Random.Range(-max.x, -max.x),
                Random.Range(-max.y, -max.y)
                );

            packer.Pack(this.writer, expected);
            Vector2 unpacked = packer.Unpack(this.GetReader());

            Assert.That(unpacked.x, Is.EqualTo(expected.x).Within(precision.x));
            Assert.That(unpacked.y, Is.EqualTo(expected.y).Within(precision.y));
        }

        [Test]
        [TestCaseSource(nameof(UnpacksToSaveValueCases))]
        public void ZeroUnpacksAsZero(Vector2 max, Vector2 precision)
        {
            var packer = new Vector2Packer(max, precision);
            Vector2 zero = Vector2.zero;

            packer.Pack(this.writer, zero);
            Vector2 unpacked = packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(zero));
        }
    }
}
