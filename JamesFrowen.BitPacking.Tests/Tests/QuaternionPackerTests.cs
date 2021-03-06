using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JamesFrowen.BitPacking.Tests
{
    public class QuaternionPackerTests : NetworkWriterTestBase
    {
        private const int BufferSize = 1000;

        TestRandom random = new TestRandom();

        static float Precision(int bits)
        {
            // sqrt2 / range * 3
            // * 3 because largest value is caculated from smallest 3, their precision error is additive
            return 1.404f / ((1 << bits) - 1) * 3f;
        }

        static IEnumerable ReturnsCorrectIndexCases()
        {
            var values = new List<float>() { 0.1f, 0.2f, 0.3f, 0.4f };
            // abcd are index
            // testing all permutation, index can only be used once
            for (var a = 0; a < 4; a++)
            {
                for (var b = 0; b < 4; b++)
                {
                    if (b == a) { continue; }

                    for (var c = 0; c < 4; c++)
                    {
                        if (c == a || c == b) { continue; }

                        for (var d = 0; d < 4; d++)
                        {
                            if (d == a || d == b || d == c) { continue; }

                            var largest = 0;
                            // index 3 is the largest, 
                            if (a == 3) { largest = 0; }
                            if (b == 3) { largest = 1; }
                            if (c == 3) { largest = 2; }
                            if (d == 3) { largest = 3; }
                            yield return new TestCaseData(values[a], values[b], values[c], values[d])
                                .Returns(largest);
                        }
                    }
                }
            }
        }

        [Test]
        [TestCaseSource(nameof(ReturnsCorrectIndexCases))]
        public int ReturnsCorrectIndex(float x, float y, float z, float w)
        {
            QuaternionPacker.FindLargestIndex(x, y, z, w, out var index, out var largest);
            return index;
        }


        static IEnumerable CompressesAndDecompressesCases()
        {
            for (var i = 8; i < 12; i++)
            {
                yield return new TestCaseData(i, Quaternion.identity);
                yield return new TestCaseData(i, Quaternion.Euler(25, 30, 0));
                yield return new TestCaseData(i, Quaternion.Euler(-50, 30, 90));
                yield return new TestCaseData(i, Quaternion.Euler(90, 90, 180));
                yield return new TestCaseData(i, Quaternion.Euler(-20, 0, 45));
                yield return new TestCaseData(i, Quaternion.Euler(80, 30, -45));
            }
        }

        [Test]
        [TestCaseSource(nameof(CompressesAndDecompressesCases))]
#if !UNITY_ENGINE
        [Ignore("Quaternion.Euler Requires unity engine to run")]
#endif
        public void PackAndUnpack(int bits, Quaternion inValue)
        {

            var precision = Precision(bits);

            var packer = new QuaternionPacker(bits);

            packer.Pack(this.writer, inValue);

            this.reader.Reset(this.writer.ToArraySegment());
            var outValue = packer.Unpack(this.reader);
            //Debug.Log($"Packed: ({inValue.x:0.000},{inValue.y:0.000},{inValue.z:0.000},{inValue.w:0.000}) " +
            //          $"UnPacked: ({outValue.x:0.000},{outValue.y:0.000},{outValue.z:0.000},{outValue.w:0.000})");

            Assert.That(outValue.x, Is.Not.NaN, "x was NaN");
            Assert.That(outValue.y, Is.Not.NaN, "y was NaN");
            Assert.That(outValue.z, Is.Not.NaN, "z was NaN");
            Assert.That(outValue.w, Is.Not.NaN, "w was NaN");

            var assertSign = getAssertSign(inValue, outValue);

            Assert.That(outValue.x, IsUnSignedEqualWithIn(inValue.x), $"x off by {Mathf.Abs(assertSign * inValue.x - outValue.x)}");
            Assert.That(outValue.y, IsUnSignedEqualWithIn(inValue.y), $"y off by {Mathf.Abs(assertSign * inValue.y - outValue.y)}");
            Assert.That(outValue.z, IsUnSignedEqualWithIn(inValue.z), $"z off by {Mathf.Abs(assertSign * inValue.z - outValue.z)}");
            Assert.That(outValue.w, IsUnSignedEqualWithIn(inValue.w), $"w off by {Mathf.Abs(assertSign * inValue.w - outValue.w)}");

            var inVec = inValue * Vector3.forward;
            var outVec = outValue * Vector3.forward;

            // allow for extra precision when rotating vector
            Assert.AreEqual(inVec.x, outVec.x, precision * 2, $"vx off by {Mathf.Abs(inVec.x - outVec.x)}");
            Assert.AreEqual(inVec.y, outVec.y, precision * 2, $"vy off by {Mathf.Abs(inVec.y - outVec.y)}");
            Assert.AreEqual(inVec.z, outVec.z, precision * 2, $"vz off by {Mathf.Abs(inVec.z - outVec.z)}");


            EqualConstraint IsUnSignedEqualWithIn(float v)
            {
                return Is.EqualTo(v).Within(precision).Or.EqualTo(assertSign * v).Within(precision);
            }
        }

        /// <summary>
        /// sign used to validate values (in/out are different, then flip values
        /// </summary>
        /// <param name="inValue"></param>
        /// <param name="outValue"></param>
        /// <returns></returns>
        private static float getAssertSign(Quaternion inValue, Quaternion outValue)
        {
            QuaternionPacker.FindLargestIndex(inValue.x, inValue.y, inValue.z, inValue.w, out var _, out var inLargest);
            QuaternionPacker.FindLargestIndex(outValue.x, outValue.y, outValue.z, outValue.w, out var _, out var outLargest);
            // flip sign of A if largest is is negative
            // Q == (-Q)
            var inSign = Mathf.Sign(inLargest);
            var outSign = Mathf.Sign(outLargest);

            float assertSign = inSign == outSign ? 1 : -1;
            return assertSign;
        }

        private Quaternion PackUnpack(Quaternion inValue, QuaternionPacker packer)
        {
            packer.Pack(this.writer, inValue);

            this.reader.Reset(this.writer.ToArraySegment());
            return packer.Unpack(this.reader);
        }


        public static bool QuaternionAlmostEqual(Quaternion actual, Quaternion expected, float precision)
        {
            return FloatAlmostEqual(actual.x, expected.x, precision)
                && FloatAlmostEqual(actual.y, expected.y, precision)
                && FloatAlmostEqual(actual.z, expected.z, precision)
                && FloatAlmostEqual(actual.w, expected.w, precision);
        }

        public static bool FloatAlmostEqual(float actual, float expected, float precision)
        {
            var minAllowed = expected - precision;
            var maxnAllowed = expected + precision;

            return minAllowed < actual && actual < maxnAllowed;
        }
    }
}
