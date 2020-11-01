// VectorsExtensions.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using DEM.Net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public static class VectorsExtensions
    {
        public static IEnumerable<Vector3> ToVector3GlTFSpace(this IEnumerable<GeoPoint> geoPoint)
        {
            return geoPoint.Select(p => p.ToVector3GlTFSpace());
        }
        public static IEnumerable<Vector3> FilterConsecutiveSame(this IEnumerable<Vector3> vectors)
        {
            Vector3 last = Vector3.Zero;
            bool firstPoint = true;
            foreach (var pt in vectors)
            {
                if (firstPoint)
                {
                    firstPoint = false;
                    last = pt;
                    yield return pt;
                }
                else
                {
                    if (!pt.Equals(last))
                    {
                        last = pt;
                        yield return pt;
                    }
                }
            }
        }
        public static Vector3 ToVector3GlTFSpace(this GeoPoint geoPoint)
        {
            return ToGlTFSpace(new Vector3((float)geoPoint.Longitude, (float)geoPoint.Latitude, (float)(geoPoint.Elevation ?? 0D)));
        }
        public static Vector3 AsVector3(this GeoPoint geoPoint)
        {
            return new Vector3((float)geoPoint.Longitude, (float)geoPoint.Latitude, (float)(geoPoint.Elevation ?? 0D));
        }
        public static Vector3 ToGlTFSpace(this Vector3 vector3)
        {
            return new Vector3(vector3.X, vector3.Z, -vector3.Y);
        }
        public static void ToGlTFSpace(this List<Vector3> vector3)
        {
            for (int i = 0; i < vector3.Count; i++)
            {
                vector3[i] = new Vector3(vector3[i].X, vector3[i].Z, -vector3[i].Y);
            }
        }
        public static Vector4 ToVector4(this Vector3 vector3, float w = 0f)
        {
            return new Vector4(vector3, w);
        }
        public static string ToRgbString(this Vector4 color)
        {
            return $"{color.X * 255f:N0},{color.Y * 255f:N0},{color.Z * 255f:N0}";
        }
        public static Vector4 CreateColor(byte r, byte g, byte b, byte a = 255)
        {
            return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
        }
        public static Vector3 AtZ(this Vector3 vector3, float newZ)
        {
            vector3.Z = newZ;
            return vector3;
        }
        public static IEnumerable<Vector3> ToQuadPoints(this Vector3 vertex, float pointSize)
        {
            float halfSize = pointSize / 2f;
            //List<Vector3> outList = new List<Vector3>(4);

            //outList.Add(new Vector3(vertex.X - halfSize, vertex.Y, vertex.Z - halfSize));
            //outList.Add(new Vector3(vertex.X + halfSize, vertex.Y, vertex.Z - halfSize));
            //outList.Add(new Vector3(vertex.X + halfSize, vertex.Y, vertex.Z + halfSize));
            //outList.Add(new Vector3(vertex.X - halfSize, vertex.Y, vertex.Z + halfSize));
            //return outList;
            yield return new Vector3(vertex.X - halfSize, vertex.Y, vertex.Z - halfSize);
            yield return new Vector3(vertex.X + halfSize, vertex.Y, vertex.Z - halfSize);
            yield return new Vector3(vertex.X + halfSize, vertex.Y, vertex.Z + halfSize);
            yield return new Vector3(vertex.X - halfSize, vertex.Y, vertex.Z + halfSize);
        }

        public static IEnumerable<int> TriangulateQuadIndices(int startindex)
        {
            yield return startindex + 0;
            yield return startindex + 2;
            yield return startindex + 1;
            yield return startindex + 0;
            yield return startindex + 3;
            yield return startindex + 2;
        }

        // Not optimized but useful
        public static void GetStats(this IEnumerable<Vector3> vectors, out Vector3 average, out Vector3 min, out Vector3 max)
        {
            average = new Vector3(
                    vectors.Average(v => v.X),
                    vectors.Average(v => v.Y),
                    vectors.Average(v => v.Z));
            min = new Vector3(
                    vectors.Min(v => v.X),
                    vectors.Min(v => v.Y),
                    vectors.Min(v => v.Z));
            max = new Vector3(
                    vectors.Max(v => v.X),
                    vectors.Max(v => v.Y),
                    vectors.Max(v => v.Z));
        }

        public static TriangulationList<Vector3> Translate(this TriangulationList<Vector3> triangulation, Vector3 vector)
        {
            Matrix4x4 translate = Matrix4x4.CreateTranslation(vector);
            return Transform(triangulation, translate);
        }
        public static TriangulationList<Vector3> RotateX(this TriangulationList<Vector3> triangulation, float radians) => Transform(triangulation, Matrix4x4.CreateRotationX(radians));
        public static TriangulationList<Vector3> RotateY(this TriangulationList<Vector3> triangulation, float radians) => Transform(triangulation, Matrix4x4.CreateRotationY(radians));
        public static TriangulationList<Vector3> RotateZ(this TriangulationList<Vector3> triangulation, float radians) => Transform(triangulation, Matrix4x4.CreateRotationZ(radians));
        public static TriangulationList<Vector3> Transform(this TriangulationList<Vector3> triangulation, Matrix4x4 matrix4x4)
        {
            for (int i = 0; i < triangulation.NumPositions; i++)
            {
                triangulation.Positions[i] = Vector3.Transform(triangulation.Positions[i], matrix4x4);
            }
            return triangulation;
        }
        public static TriangulationList<Vector3> CenterOnOrigin(this TriangulationList<Vector3> triangulation, BoundingBox bbox, bool centerOnZ = false)
        {
            //Logger.Info("CenterOnOrigin...");
            double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
            double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;

            Matrix4x4 translate = Matrix4x4.CreateTranslation((float)-xOriginOffset, (float)-yOriginOffset, centerOnZ ? (float)-bbox.zMin : 0);
            for (int i = 0; i < triangulation.NumPositions; i++)
            {
                triangulation.Positions[i] = Vector3.Transform(triangulation.Positions[i], translate);
            }

            return triangulation;
        }
        public static TriangulationList<Vector3> CenterOnOrigin(this TriangulationList<Vector3> triangulation, Vector3 origin)
        {
           Matrix4x4 translate = Matrix4x4.CreateTranslation((float)-origin.X, (float)-origin.Y, -origin.Z);
            for (int i = 0; i < triangulation.NumPositions; i++)
            {
                triangulation.Positions[i] = Vector3.Transform(triangulation.Positions[i], translate);
            }

            return triangulation;
        }
        public static TriangulationList<Vector3> CenterOnOrigin(this TriangulationList<Vector3> triangulation)
        {
            return triangulation.CenterOnOrigin(triangulation.GetBoundingBox(), true);
        }

        public static BoundingBox GetBoundingBox(this TriangulationList<Vector3> triangulation)
        {
            var bbox = new BoundingBox() { zMin = double.MaxValue, zMax = double.MinValue };
            for (int i = 0; i < triangulation.NumPositions; i++)
            {
                bbox.xMin = Math.Min(bbox.xMin, triangulation.Positions[i].X);
                bbox.xMax = Math.Max(bbox.xMax, triangulation.Positions[i].X);
                bbox.yMin = Math.Min(bbox.yMin, triangulation.Positions[i].Y);
                bbox.yMax = Math.Max(bbox.yMax, triangulation.Positions[i].Y);
                bbox.zMin = Math.Min(bbox.zMin, triangulation.Positions[i].Z);
                bbox.zMax = Math.Max(bbox.zMax, triangulation.Positions[i].Z);
            }

            return bbox;
        }

    }
}
