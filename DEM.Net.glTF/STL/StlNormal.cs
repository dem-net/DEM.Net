using System;

namespace IxMilia.Stl
{
    internal struct StlNormal
    {
        public float X;
        public float Y;
        public float Z;

        public bool IsZero => X == 0.0f && Y == 0.0f && Z == 0.0f;

        public StlNormal(float x, float y, float z)
            : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public StlNormal Normalize()
        {
            var length = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            if (length == 0.0f)
                return new StlNormal();
            return new StlNormal(X / length, Y / length, Z / length);
        }
    }
}
