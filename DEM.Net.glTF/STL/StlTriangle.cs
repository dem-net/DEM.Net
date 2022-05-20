namespace IxMilia.Stl
{
    internal class StlTriangle
    {
        public StlNormal Normal { get; set; }

        public StlVertex Vertex1 { get; set; }

        public StlVertex Vertex2 { get; set; }

        public StlVertex Vertex3 { get; set; }

        public StlTriangle(StlNormal normal, StlVertex v1, StlVertex v2, StlVertex v3)
        {
            Normal = normal;
            Vertex1 = v1;
            Vertex2 = v2;
            Vertex3 = v3;
        }

        public StlNormal GetValidNormal()
        {
            if (Normal.IsZero)
            {
                // using counter-clockwise vertex ordering,
                // let u = Vertex2 - Vertex1
                // let v = Vertex3 - Vertex1
                var u1 = Vertex2.X - Vertex1.X;
                var u2 = Vertex2.Y - Vertex1.Y;
                var u3 = Vertex2.Z - Vertex1.Z;
                var v1 = Vertex3.X - Vertex1.X;
                var v2 = Vertex3.Y - Vertex1.Y;
                var v3 = Vertex3.Z - Vertex1.Z;

                var i = u2 * v3 - u3 * v2;
                var j = u3 * v1 - u1 * v3;
                var k = u1 * v2 - u2 * v1;

                return new StlNormal(i, j, k);
            }
            else
            {
                return Normal;
            }
        }
    }
}
