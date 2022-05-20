using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace IxMilia.Stl
{
    internal class StlReader
    {
        private Stream baseStream;
        private BinaryReader binReader;
        private IEnumerator<string> tokenEnumerator;
        private bool isAscii;
        private uint triangleCount;
        private Regex headerReg = new Regex("^solid (.*)$");

        public StlReader(Stream stream)
        {
            baseStream = stream;
            binReader = new BinaryReader(stream);
        }

        public string ReadSolidName()
        {
            int i;
            bool headerComplete = false;
            var sb = new StringBuilder();
            for (i = 0; i < 80 && !headerComplete; i++)
            {
                var b = binReader.ReadByte();
                if (b == '\n')
                {
                    isAscii = true;
                    headerComplete = true;
                }
                else if (b == 0)
                {
                    isAscii = false;
                    headerComplete = true;
                }
                else
                {
                    sb.Append((char)b);
                }
            }

            var header = sb.ToString().Trim();
            var match = headerReg.Match(header);
            if (match.Success)
            {
                header = match.Groups[1].Value;
            }
            else
            {
                header = null;
            }

            if (isAscii)
            {
                tokenEnumerator = new StlTokenStream(baseStream);
                tokenEnumerator.MoveNext();
            }
            else
            {
                // swallow the remainder of the header
                for (; i < 80; i++)
                    binReader.ReadByte();

                // get count
                triangleCount = binReader.ReadUInt32();
            }

            return header;
        }

        public List<StlTriangle> ReadTriangles()
        {
            var triangles = new List<StlTriangle>();
            if (isAscii)
            {
                var t = ReadTriangle();
                while (t != null)
                {
                    EnsureCorrectNormal(t);
                    triangles.Add(t);
                    t = ReadTriangle();
                }
            }
            else
            {
                for (uint i = 0; i < triangleCount; i++)
                {
                    // normal should equal (v3-v2)x(v1-v1)
                    var normal = new StlNormal(ReadFloatBinary(), ReadFloatBinary(), ReadFloatBinary());
                    var v1 = ReadVertexBinary();
                    var v2 = ReadVertexBinary();
                    var v3 = ReadVertexBinary();
                    binReader.ReadUInt16(); // attribute byte count; garbage value
                    var t = new StlTriangle(normal, v1, v2, v3);
                    EnsureCorrectNormal(t);
                    triangles.Add(t);
                }
            }

            return triangles;
        }

        private static void EnsureCorrectNormal(StlTriangle triangle)
        {
            triangle.Normal = triangle.GetValidNormal();
        }

        private float ReadFloatBinary()
        {
            return binReader.ReadSingle();
        }

        private StlVertex ReadVertexBinary()
        {
            return new StlVertex(ReadFloatBinary(), ReadFloatBinary(), ReadFloatBinary());
        }

        private StlTriangle ReadTriangle()
        {
            StlTriangle triangle = null;
            if (isAscii)
            {
                switch (PeekToken())
                {
                    case "facet":
                        AdvanceToken();
                        SwallowToken("normal");
                        var normal = new StlNormal(ConsumeNumberToken(), ConsumeNumberToken(), ConsumeNumberToken());
                        SwallowToken("outer");
                        SwallowToken("loop");
                        SwallowToken("vertex");
                        var v1 = ConsumeVertexToken();
                        SwallowToken("vertex");
                        var v2 = ConsumeVertexToken();
                        SwallowToken("vertex");
                        var v3 = ConsumeVertexToken();
                        SwallowToken("endloop");
                        SwallowToken("endfacet");
                        triangle = new StlTriangle(normal, v1, v2, v3);
                        break;
                    case "endsolid":
                        return null;
                    default:
                        throw new StlReadException("Unexpected token " + PeekToken());
                }
            }

            return triangle;
        }

        private void SwallowToken(string token)
        {
            if (PeekToken() == token)
            {
                AdvanceToken();
            }
            else
            {
                throw new StlReadException("Expected token " + token);
            }
        }

        private float ConsumeNumberToken()
        {
            var text = PeekToken();
            AdvanceToken();
            float value;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                throw new StlReadException("Expected number");
            }

            return value;
        }

        private StlVertex ConsumeVertexToken()
        {
            return new StlVertex(ConsumeNumberToken(), ConsumeNumberToken(), ConsumeNumberToken());
        }

        private string PeekToken()
        {
            return tokenEnumerator.Current;
        }

        private void AdvanceToken()
        {
            tokenEnumerator.MoveNext();
        }
    }
}
