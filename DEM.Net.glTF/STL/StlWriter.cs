using System.Globalization;
using System.IO;

namespace IxMilia.Stl
{
    internal class StlWriter
    {
        private const string FloatFormat = "e6";

        public void Write(StlFile file, Stream stream, bool asAscii)
        {
            if (asAscii)
                WriteAscii(file, stream);
            else
                WriteBinary(file, stream);
        }

        private void WriteAscii(StlFile file, Stream stream)
        {
            var writer = new StreamWriter(stream);
            writer.WriteLine(string.Format("solid {0}", file.SolidName));
            foreach (var triangle in file.Triangles)
            {
                writer.WriteLine(string.Format("  facet normal {0}", NormalToString(triangle.Normal)));
                writer.WriteLine("    outer loop");
                writer.WriteLine(string.Format("      vertex {0}", VertexToString(triangle.Vertex1)));
                writer.WriteLine(string.Format("      vertex {0}", VertexToString(triangle.Vertex2)));
                writer.WriteLine(string.Format("      vertex {0}", VertexToString(triangle.Vertex3)));
                writer.WriteLine("    endloop");
                writer.WriteLine("  endfacet");
            }

            writer.WriteLine(string.Format("endsolid {0}", file.SolidName));
            writer.Flush();
        }

        private void WriteBinary(StlFile file, Stream stream)
        {
            var writer = new BinaryWriter(stream);

            // write header
            var header = new byte[80]; // can be a garbage value
            writer.Write(header);

            // write vertex count
            writer.Write((uint)file.Triangles.Count);

            // write triangles
            foreach (var triangle in file.Triangles)
            {
                writer.Write(triangle.Normal.X);
                writer.Write(triangle.Normal.Y);
                writer.Write(triangle.Normal.Z);

                writer.Write(triangle.Vertex1.X);
                writer.Write(triangle.Vertex1.Y);
                writer.Write(triangle.Vertex1.Z);

                writer.Write(triangle.Vertex2.X);
                writer.Write(triangle.Vertex2.Y);
                writer.Write(triangle.Vertex2.Z);

                writer.Write(triangle.Vertex3.X);
                writer.Write(triangle.Vertex3.Y);
                writer.Write(triangle.Vertex3.Z);

                writer.Write((ushort)0); // garbage value
            }

            writer.Flush();
        }

        private static string NormalToString(StlNormal normal)
        {
            return string.Format("{0} {1} {2}",
                normal.X.ToString(FloatFormat, CultureInfo.InvariantCulture),
                normal.Y.ToString(FloatFormat, CultureInfo.InvariantCulture),
                normal.Z.ToString(FloatFormat, CultureInfo.InvariantCulture));
        }

        private static string VertexToString(StlVertex vertex)
        {
            return string.Format("{0} {1} {2}",
                vertex.X.ToString(FloatFormat, CultureInfo.InvariantCulture),
                vertex.Y.ToString(FloatFormat, CultureInfo.InvariantCulture),
                vertex.Z.ToString(FloatFormat, CultureInfo.InvariantCulture));
        }
    }
}
