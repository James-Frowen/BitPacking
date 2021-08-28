using UnityEngine;

namespace Mirage.Serialization.Tests
{
    public class TestRandom
    {
        System.Random random = new System.Random();

        public uint Uint(int min, int max)
        {
            return (uint)this.random.Next(min, max);
        }

        public float Float(float min, float max)
        {
            return (float)(this.random.NextDouble() * (max - min) + min);
        }

        public Vector3 Vector3(Vector3 min, Vector3 max)
        {
            return new Vector3(
                this.Float(min.x, max.x),
                this.Float(min.y, max.y),
                this.Float(min.z, max.z)
            );
        }

        public Quaternion Quaternion()
        {
            return new Quaternion(
                this.Float(-1, 1),
                this.Float(-1, 1),
                this.Float(-1, 1),
                this.Float(-1, 1)
            ).normalized;
        }
    }
}
