// MeshService.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
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

using DEM.Net.Core.Tesselation;
using Microsoft.Extensions.Logging;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public class MeshService
    {
        private readonly ILogger<MeshService> _logger;

        public MeshService(ILogger<MeshService> logger = null)
        {
            _logger = logger;
        }
        /// <summary>
        /// Triangulate a regular set of points
        /// </summary>
        /// <param name="heightMap">Gridded set of points. Corrdinates can differ, 
        /// but height map should be organized a set of rows and columns</param>
        /// <param name="regularTriangulation">(optional) Determines which diagnal is choosen</param>
        /// <returns>List of indexes (triplets) in height map coordinates, indicating each of the triangles vertices</returns>
        public Triangulation TriangulateHeightMap(HeightMap heightMap, bool regularTriangulation = true)
        {

            int capacity = ((heightMap.Width - 1) * 6) * (heightMap.Height - 1);

            Triangulation triangulationResult = new Triangulation();
            triangulationResult.NumIndices = capacity;
            triangulationResult.Indices = TriangulateHeightMap_Internal(heightMap, regularTriangulation);
            triangulationResult.Positions = heightMap.Coordinates;
            triangulationResult.NumPositions = heightMap.Count;

            return triangulationResult;
        }
        private IEnumerable<int> TriangulateHeightMap_Internal(HeightMap heightMap, bool regularTriangulation = true)
        {
            for (int y = 0; y < heightMap.Height; y++)
            {
                for (int x = 0; x < heightMap.Width; x++)
                {

                    if (x < (heightMap.Width - 1) && y < (heightMap.Height - 1))
                    {
                        if (regularTriangulation)
                        {
                            // Triangulation 1
                            yield return (x + 0) + (y + 0) * heightMap.Width;
                            yield return (x + 0) + (y + 1) * heightMap.Width;
                            yield return (x + 1) + (y + 0) * heightMap.Width;

                            yield return (x + 1) + (y + 0) * heightMap.Width;
                            yield return (x + 0) + (y + 1) * heightMap.Width;
                            yield return (x + 1) + (y + 1) * heightMap.Width;
                        }
                        else
                        {

                            // Triangulation 2
                            yield return (x + 0) + (y + 0) * heightMap.Width;
                            yield return (x + 1) + (y + 1) * heightMap.Width;
                            yield return (x + 0) + (y + 1) * heightMap.Width;

                            yield return (x + 0) + (y + 0) * heightMap.Width;
                            yield return (x + 1) + (y + 0) * heightMap.Width;
                            yield return (x + 1) + (y + 1) * heightMap.Width;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Triangulate an height map and generates a base of given height
        /// </summary>
        /// <param name="heightMap">Gridded set of points. Corrdinates can differ, 
        /// but height map should be organized a set of rows and columns</param>
        /// <returns>TriangulationResult</returns>
        public Triangulation GenerateTriangleMesh_Boxed(HeightMap heightMap
                    , BoxBaseThickness thickness, float zValue)
        {

            Triangulation triangulationResult = new Triangulation();

            triangulationResult.NumIndices = ((heightMap.Width - 1) * 6) * (heightMap.Height - 1); // height map plane
            triangulationResult.NumIndices += ((heightMap.Width - 1) * 6) * 2; // two sides 
            triangulationResult.NumIndices += ((heightMap.Height - 1) * 6) * 2; // two sizes
            triangulationResult.NumIndices += 6; // bottom (two big triangles)

            // Regular triangulation
            triangulationResult.Positions = heightMap.Coordinates;
            triangulationResult.Indices = TriangulateHeightMap_Internal(heightMap, true);

            // Generate box vertices and trianglation
            AddHeightMapBase(triangulationResult, heightMap, thickness, zValue);

            return triangulationResult;
        }

        private void AddHeightMapBase(Triangulation triangulation, HeightMap heightMap, BoxBaseThickness thickness, float zValue)
        {
            // bake coordinates to avoid executing the coords transfrom pipeline
            var coords = heightMap.Coordinates.ToList();
            int capacity = heightMap.Width * 2 + (heightMap.Height - 2) * 2;
            var basePoints = new List<GeoPoint>(capacity);
            var baseIndexes = new List<int>(capacity);

            float baseElevation = 0;
            switch (thickness)
            {
                case BoxBaseThickness.FromMinimumPoint:
                    baseElevation = (float)coords.Min(p => p.Elevation).GetValueOrDefault(0) - zValue;
                    break;

                default:
                case BoxBaseThickness.FixedElevation:
                    baseElevation = zValue;
                    break;

            }

            // x : 0 => width // y : 0
            int baseIndex0 = coords.Count;
            int baseIndex = baseIndex0;
            for (int x = 0; x < heightMap.Width - 1; x++)
            {
                var pBase = coords[x].Clone();
                pBase.Elevation = baseElevation;
                basePoints.Add(pBase);

                baseIndexes.Add(x);
                baseIndexes.Add(baseIndex + 1);
                baseIndexes.Add(baseIndex);

                baseIndexes.Add(x + 1);
                baseIndexes.Add(baseIndex + 1);
                baseIndexes.Add(x);

                baseIndex++;
            }
            // x : width // y : 0 => height
            // 
            for (int y = 0; y < heightMap.Height - 1; y++)
            {
                int x = heightMap.Width - 1;
                int index = x + y * heightMap.Width;

                var pBase = coords[index].Clone();
                pBase.Elevation = baseElevation;
                basePoints.Add(pBase);

                baseIndexes.Add(x + y * heightMap.Width);
                baseIndexes.Add(baseIndex + 1);
                baseIndexes.Add(baseIndex);

                baseIndexes.Add(x + y * heightMap.Width);
                baseIndexes.Add(x + (y + 1) * heightMap.Width);
                baseIndexes.Add(baseIndex + 1);

                baseIndex++;
            }
            //// x : width => 0 // y : height
            for (int x = heightMap.Width - 1; x > 0; x--)
            {
                int index = x + (heightMap.Height - 1) * heightMap.Width;
                var pBase = coords[index].Clone();
                pBase.Elevation = baseElevation;
                basePoints.Add(pBase);

                baseIndexes.Add(index);
                baseIndexes.Add(index - 1);
                baseIndexes.Add(baseIndex);

                baseIndexes.Add(index - 1);
                baseIndexes.Add(baseIndex + 1);
                baseIndexes.Add(baseIndex);

                baseIndex++;
            }
            //// x : 0 // y : height => 0
            for (int y = heightMap.Height - 1; y > 0; y--)
            {
                int x = 0;
                int index = x + y * heightMap.Width;

                var pBase = coords[index].Clone();
                pBase.Elevation = baseElevation;
                basePoints.Add(pBase);

                // last base position is the first base generated
                int nextBaseIndex = baseIndex + 1 > baseIndex0 + capacity - 1 ? baseIndex0 : baseIndex + 1;

                baseIndexes.Add(x + y * heightMap.Width);
                baseIndexes.Add(nextBaseIndex);
                baseIndexes.Add(baseIndex);

                baseIndexes.Add(x + y * heightMap.Width);
                baseIndexes.Add(x + (y - 1) * heightMap.Width);
                baseIndexes.Add(nextBaseIndex);


                baseIndex++;
            }


            // base (2 big triangles)
            baseIndexes.Add(baseIndex0);
            baseIndexes.Add(baseIndex0 + heightMap.Width - 1);
            baseIndexes.Add(baseIndex0 + heightMap.Width - 1 + heightMap.Height - 1);

            baseIndexes.Add(baseIndex0);
            baseIndexes.Add(baseIndex0 + heightMap.Width - 1 + heightMap.Height - 1);
            baseIndexes.Add(baseIndex0 + 2 * (heightMap.Width - 1) + heightMap.Height - 1);




            triangulation.Positions = triangulation.Positions.Concat(basePoints);
            triangulation.NumPositions += basePoints.Count;
            triangulation.Indices = triangulation.Indices.Concat(baseIndexes);



        }

        /// <summary>
        /// Computes normals for a given mesh. All positions must form a continuous mesh.
        /// For multi meshes, perform triangulation on every mesh and aggregate normals on a final pass
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="indices"></param>
        /// <returns></returns>
        public IEnumerable<Vector3> ComputeMeshNormals(IList<Vector3> positions, IList<int> indices)
        {
            //The number of the vertices
            int nV = positions.Count;
            //The number of the triangles
            int nT = indices.Count / 3;

            Vector3[] norm = new Vector3[nV]; //Array for the normals
                                              //Scan all the triangles. For each triangle add its
                                              //normal to norm's vectors of triangle's vertices
            for (int t = 0; t < nT; t++)
            {
                //Get indices of the triangle t
                int i1 = indices[3 * t];
                int i2 = indices[3 * t + 1];
                int i3 = indices[3 * t + 2];
                //Get vertices of the triangle
                Vector3 v1 = positions[i1];
                Vector3 v2 = positions[i2];
                Vector3 v3 = positions[i3];

                //Compute the triangle's normal
                Vector3 dir = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));

                if (float.IsNaN(dir.X) || float.IsInfinity(dir.X))
                {
                    dir = Vector3.UnitY;
                }
                //Debug.Assert(!float.IsNaN(dir.X) && !float.IsInfinity(dir.X));
                //Accumulate it to norm array for i1, i2, i3
                norm[i1] += dir;
                norm[i2] += dir;
                norm[i3] += dir;
            }
#if DEBUG
            for (int i = 0; i < norm.Length; i++)
            {
                var vec = norm[i];
                if (vec == Vector3.Zero)
                {
                    //_logger.LogWarning("Invalid normal detected for position {0}", i);
                    yield return Vector3.UnitY;
                }
                else
                {
                    yield return Vector3.Normalize(vec);
                }
            }
#else
            foreach (var vec in norm)
            {
                yield return vec == Vector3.Zero ? Vector3.UnitY : Vector3.Normalize(vec);
            }
#endif
        }


        /// <summary>
        /// Calculate normals for a given height map
        /// </summary>
        /// <param name="heightMap">Height map (gridded data)</param>
        /// <returns>Normals for each point of the height map</returns>
        public IEnumerable<Vector3> ComputeNormals(HeightMap heightMap)
        {
            var triangulation = TriangulateHeightMap(heightMap);
            var normals = ComputeMeshNormals(
                    heightMap.Coordinates.Select(c =>
                                                            new Vector3((
                                                                float)c.Longitude,
                                                                (float)c.Latitude,
                                                                (float)c.Elevation)
                                                            ).ToList(),
            triangulation.Indices.ToList());

            return normals;
        }

        public TriangulationList<Vector3> GenerateTriangleMesh_Line(IEnumerable<GeoPoint> points, float width, Matrix4x4 transform)
        {
            try
            {
                if (points == null)
                {
                    _logger?.LogWarning("Points are empty.");
                }
                else
                {

                    if (width <= 0)
                    {
                        throw new Exception("Line width of 0 is not supported. Please provide a with > 0.");
                    }
                    else
                    {
                        TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();

                        // https://gist.github.com/gszauer/5718441
                        // Line triangle mesh
                        transform = transform == default ? Matrix4x4.Identity : transform;
                        List<Vector3> sections = points.Select(pt => Vector3.Transform(pt.ToVector3GlTFSpace(), transform))
                            .FilterConsecutiveSame()
                            .ToList();

                        for (int i = 0; i < sections.Count - 1; i++)
                        {
                            Vector3 current = sections[i];
                            Vector3 next = sections[i + 1];
                            Vector3 dir = Vector3.Normalize(next - current);


                            // translate the vector to the left along its way
                            Vector3 side;
                            if (dir.Equals(Vector3.UnitY))
                            {
                                side = Vector3.UnitX * width;
                            }
                            else
                            {
                                side = Vector3.Cross(dir, Vector3.UnitY) * width;
                            }


                            Vector3 v0 = current - side; // 0
                            Vector3 v1 = current + side; // 1

                            triangulation.Positions.Add(v0);
                            triangulation.Positions.Add(v1);

                            if (i == sections.Count - 2) // add last vertices
                            {
                                v0 = next - side; // 0
                                v1 = next + side; // 1
                                triangulation.Positions.Add(v0);
                                triangulation.Positions.Add(v1);
                            }
                        }
                        // add last vertices


                        for (int i = 0; i < sections.Count - 1; i++)
                        {
                            int i0 = i * 2;
                            triangulation.Indices.Add(i0);
                            triangulation.Indices.Add(i0 + 1);
                            triangulation.Indices.Add(i0 + 3);

                            triangulation.Indices.Add(i0 + 0);
                            triangulation.Indices.Add(i0 + 3);
                            triangulation.Indices.Add(i0 + 2);
                        }

                        return triangulation;

                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, ex.ToString());
                throw;
            }
            return default;
        }

        /// <summary>
        /// With a polygon input ring, returns the same number of points, with indices triplets forming a triangulation
        /// </summary>
        /// <param name="outerRingPoints"></param>
        /// <returns></returns>
        public TriangulationList<GeoPoint> Tesselate(IEnumerable<GeoPoint> outerRingPoints, IEnumerable<IEnumerable<GeoPoint>> innerRingsPoints)
        {
            TriangulationList<GeoPoint> triangulation = new TriangulationList<GeoPoint>();

            var data = new List<double>();
            var holeIndices = new List<int>();

            foreach (var p in outerRingPoints)
            {
                triangulation.Positions.Add(p);

                data.Add(p.Longitude);
                data.Add(p.Latitude);

            }
            if (innerRingsPoints != null)
            {
                foreach (var ring in innerRingsPoints)
                {
                    holeIndices.Add(triangulation.Positions.Count);
                    foreach (var p in ring)
                    {
                        triangulation.Positions.Add(p);

                        data.Add(p.Longitude);
                        data.Add(p.Latitude);

                    }
                }
            }
            var trianglesIndices = Earcut.Tessellate(data, holeIndices);

            triangulation.Indices = trianglesIndices;

            return triangulation;
        }
        public TriangulationList<Vector3> Tesselate(IEnumerable<Vector3> outerRingPoints, IEnumerable<IEnumerable<Vector3>> innerRingsPoints)
        {
            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();

            var data = new List<double>();
            var holeIndices = new List<int>();

            foreach (var p in outerRingPoints)
            {
                triangulation.Positions.Add(p);

                data.Add(p.X);
                data.Add(p.Y);

            }
            if (innerRingsPoints != null)
            {
                foreach (var ring in innerRingsPoints)
                {
                    holeIndices.Add(triangulation.Positions.Count);
                    foreach (var p in ring)
                    {
                        triangulation.Positions.Add(p);

                        data.Add(p.X);
                        data.Add(p.Y);

                    }
                }
            }
            var trianglesIndices = Earcut.Tessellate(data, holeIndices);

            triangulation.Indices = trianglesIndices;

            return triangulation;
        }
        public TriangulationList<Vector3> Tesselate(Polygon<Vector3> polygon)
        {
            return Tesselate(polygon.ExteriorRing, polygon.InteriorRings);
        }
        public TriangulationList<Vector3> Extrude(List<Polygon<Vector3>> polygons)
        {
            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();
            foreach (var polygon in polygons)
            {
                triangulation += Extrude(polygon);
            }
            return triangulation;
        }
        public TriangulationList<Vector3> Extrude(Polygon<Vector3> polygon, float depth = 10f)
        {
            
            //==========================
            // Footprint triangulation
            //
            var footPrintOutline = polygon.ExteriorRing; 
            var footPrintInnerRingsFlattened = polygon.InteriorRings == null ? null : polygon.InteriorRings;

            TriangulationList<Vector3> triangulation = this.Tesselate(footPrintOutline, footPrintInnerRingsFlattened);
            int numFootPrintIndices = triangulation.Indices.Count;
            /////

            // Now extrude it (build the sides)
            // Algo
            // First triangulate the foot print (with inner rings if existing)
            // This triangulation is the roof top if polygon is flat
            int totalPoints = polygon.NumPoints;

            // Triangulate wall for each ring
            // (We add floor indices before copying the vertices, they will be duplicated and z shifted later on)
            List<int> numVerticesPerRing = new List<int>();
            numVerticesPerRing.Add(polygon.ExteriorRing.Count);
            numVerticesPerRing.AddRange(polygon.InteriorRings.Select(r => r.Count));
            triangulation = this.TriangulateRingsWalls(triangulation, numVerticesPerRing, totalPoints);

            // Roof
            // polygon has real elevations

            // Create floor vertices by copying roof vertices and setting their z
            var floorVertices = triangulation.Positions.Select(pt => new Vector3(pt.X, pt.Y, pt.Z + depth)).ToList();
            triangulation.Positions.AddRange(floorVertices);

            // duplicate tesselation for extruded back face
            triangulation.Indices.AddRange(triangulation.Indices.Take(numFootPrintIndices).Select(i => i + totalPoints).ToList());

            //==========================
            // Colors: assume all vertices have the same color, otherwise look at code done for buildings            
            Vector4 DefaultColor = Vector4.One;
            // assign wall or default color to all vertices
            triangulation.Colors = triangulation.Positions.Select(p => DefaultColor).ToList();

            Debug.Assert(triangulation.Colors.Count == 0 || triangulation.Colors.Count == triangulation.Positions.Count);

            return triangulation;
        }
        private TriangulationList<Vector3> TriangulateRingsWalls(TriangulationList<Vector3> triangulation, List<int> numVerticesPerRing, int totalPoints)
        {
            int offset = numVerticesPerRing.Sum();

            Debug.Assert(totalPoints == offset);

            int ringOffset = 0;
            foreach (var numRingVertices in numVerticesPerRing)
            {
                int i = 0;
                do
                {
                    triangulation.Indices.Add(ringOffset + i);
                    triangulation.Indices.Add(ringOffset + i + offset);
                    triangulation.Indices.Add(ringOffset + i + 1);

                    triangulation.Indices.Add(ringOffset + i + offset);
                    triangulation.Indices.Add(ringOffset + i + offset + 1);
                    triangulation.Indices.Add(ringOffset + i + 1);

                    i++;
                }
                while (i < numRingVertices - 1);

                // Connect last vertices to start vertices
                triangulation.Indices.Add(ringOffset + i);
                triangulation.Indices.Add(ringOffset + i + offset);
                triangulation.Indices.Add(ringOffset + 0);

                triangulation.Indices.Add(ringOffset + i + offset);
                triangulation.Indices.Add(ringOffset + 0 + offset);
                triangulation.Indices.Add(ringOffset + 0);

                ringOffset += numRingVertices;

            }
            return triangulation;
        }

        public TriangulationList<Vector3> CreateWaterSurface(Vector3 bottomLeft, Vector3 topRight, Vector3 topLeft, Vector3 bottomRight, float minZ, Vector4 color)
        {
            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();

            // base
            triangulation.Positions.Add(bottomLeft);
            triangulation.Positions.Add(topLeft);
            triangulation.Positions.Add(topRight);
            triangulation.Positions.Add(bottomRight);

            triangulation.Positions.Add(bottomLeft.AtZ(minZ));
            triangulation.Positions.Add(topLeft.AtZ(minZ));
            triangulation.Positions.Add(topRight.AtZ(minZ));
            triangulation.Positions.Add(bottomRight.AtZ(minZ));

            // base at min Z
            triangulation.Indices.AddRange(new[] { 0, 2, 1 });
            triangulation.Indices.AddRange(new[] { 0, 3, 2 });

            // sides
            triangulation.Indices.AddRange(new[] { 0, 1, 5 });
            triangulation.Indices.AddRange(new[] { 0, 5, 4 });

            triangulation.Indices.AddRange(new[] { 1, 2, 6 });
            triangulation.Indices.AddRange(new[] { 1, 6, 5 });

            triangulation.Indices.AddRange(new[] { 2, 3, 7 });
            triangulation.Indices.AddRange(new[] { 2, 7, 6 });

            triangulation.Indices.AddRange(new[] { 3, 0, 4 });
            triangulation.Indices.AddRange(new[] { 3, 4, 7 });

            //triangulation.Colors = new List<Vector4>();
            //triangulation.Colors.Add(VectorsExtensions.CreateColor(255, 0, 0));
            //triangulation.Colors.Add(VectorsExtensions.CreateColor(0, 255, 0));
            //triangulation.Colors.Add(VectorsExtensions.CreateColor(0, 0, 255));
            //triangulation.Colors.Add(VectorsExtensions.CreateColor(255,255, 255));

            // add color for each position
            triangulation.Colors = triangulation.Positions.Select(p => color).ToList();
            return triangulation;
        }
        public TriangulationList<Vector3> CreatePlane(Vector3 bottomLeft, Vector3 topRight, Vector3 topLeft, Vector3 bottomRight, Vector4 color)
        {
            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();

            // base
            triangulation.Positions.Add(bottomLeft);
            triangulation.Positions.Add(topLeft);
            triangulation.Positions.Add(topRight);
            triangulation.Positions.Add(bottomRight);


            triangulation.Indices.Add(0);
            triangulation.Indices.Add(2);
            triangulation.Indices.Add(1);
            triangulation.Indices.Add(0);
            triangulation.Indices.Add(3);
            triangulation.Indices.Add(2);

            //triangulation.Colors = new List<Vector4>();
            //triangulation.Colors.Add(VectorsExtensions.CreateColor(255, 0, 0));
            //triangulation.Colors.Add(VectorsExtensions.CreateColor(0, 255, 0));
            //triangulation.Colors.Add(VectorsExtensions.CreateColor(0, 0, 255));
            //triangulation.Colors.Add(VectorsExtensions.CreateColor(255,255, 255));

            // add color for each position
            triangulation.Colors = triangulation.Positions.Select(p => color).ToList();
            return triangulation;
        }

        /// <summary>
        /// Create a cylinder where the base center is at position (height is in Z direction)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <param name="height"></param>
        /// <param name="segmentCount">Base is not a circle, it's a polygon with this segment count</param>
        /// <returns></returns>
        public TriangulationList<Vector3> CreateCylinder(Vector3 position, float radius, float height, Vector4 color, int segmentCount = 12)
        {
            if (segmentCount < 3) throw new ArgumentOutOfRangeException(nameof(segmentCount), segmentCount, "There must be a least 3 segments to form a polygonal cylinder base");


            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();

            // base
            triangulation.Positions.Add(position);
            var angleStep = 2d * Math.PI / segmentCount;
            double angle = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                var x = (float)Math.Cos(angle) * radius;
                var y = (float)Math.Sin(angle) * radius;

                triangulation.Positions.Add(new Vector3(position.X + x, position.Y + y, position.Z));

                angle += angleStep;
            }

            // top (copy the base vertices with Z tranlation)
            var topPositions = triangulation.Positions.Select(p => new Vector3(p.X, p.Y, p.Z + height)).ToList();
            triangulation.Positions.AddRange(topPositions);

            // indices 
            // base (clockwise winding)
            for (int i = 0; i < segmentCount; i++)
            {
                triangulation.Indices.Add(0);
                triangulation.Indices.Add(i + 1);
                triangulation.Indices.Add(i == 0 ? segmentCount : i);
            }
            // top (anticlockwise winding)
            var offset = segmentCount + 1;
            for (int i = 0; i < segmentCount; i++)
            {
                triangulation.Indices.Add(0 + offset);
                triangulation.Indices.Add((i == 0 ? segmentCount : i) + offset);
                triangulation.Indices.Add(i + 1 + offset);
            }

            // sides
            for (int i = 0; i < segmentCount; i++)
            {
                triangulation.Indices.Add(i + 1);
                triangulation.Indices.Add(i + 1 + offset);
                triangulation.Indices.Add((i == 0 ? segmentCount : i) + offset);

                triangulation.Indices.Add(i + 1);
                triangulation.Indices.Add((i == 0 ? segmentCount : i) + offset);
                triangulation.Indices.Add((i == 0 ? segmentCount : i));
            }

            // add color for each position
            triangulation.Colors = triangulation.Positions.Select(p => color).ToList();
            return triangulation;
        }

        /// <summary>
        /// Create a cylinder where the base center is at position (orientation base->tip in Z axis)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <param name="height"></param>
        /// <param name="segmentCount">Base is not a circle, it's a polygon with this segment count</param>
        /// <returns></returns>
        public TriangulationList<Vector3> CreateCone(Vector3 position, float radius, float height, Vector4 color, int segmentCount = 12)
        {
            if (segmentCount < 3) throw new ArgumentOutOfRangeException(nameof(segmentCount), segmentCount, "There must be a least 3 segments to form a polygonal cylinder base");


            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();

            // base
            triangulation.Positions.Add(position);
            var angleStep = 2d * Math.PI / segmentCount;
            double angle = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                var x = (float)Math.Cos(angle) * radius;
                var y = (float)Math.Sin(angle) * radius;

                triangulation.Positions.Add(new Vector3(position.X + x, position.Y + y, position.Z));

                angle += angleStep;
            }

            // top
            triangulation.Positions.Add(new Vector3(position.X, position.Y, position.Z + height));

            // indices 
            // base (clockwise winding)
            for (int i = 0; i < segmentCount; i++)
            {
                triangulation.Indices.Add(0);
                triangulation.Indices.Add(i + 1);
                triangulation.Indices.Add(i == 0 ? segmentCount : i);
            }
            // sides
            for (int i = 0; i < segmentCount; i++)
            {
                triangulation.Indices.Add(triangulation.Positions.Count - 1);
                triangulation.Indices.Add(i == 0 ? segmentCount : i);
                triangulation.Indices.Add(i + 1);
            }

            // add color for each position
            triangulation.Colors = triangulation.Positions.Select(p => color).ToList();

            return triangulation;
        }


        public TriangulationList<Vector3> CreateAxis(float radius = 2f, float length = 50f, float tipRadius = 2.5f, float tipLength = 10f, int segmentCount = 10)
        {
            TriangulationList<Vector3> axis = new TriangulationList<Vector3>();

            float halfPi = (float)Math.PI / 2f;

            var red = VectorsExtensions.CreateColor(255, 0, 0, 255);
            var green = VectorsExtensions.CreateColor(0, 255, 0, 255);
            var blue = VectorsExtensions.CreateColor(0, 0, 255, 255);

            // Z axis
            axis += CreateCylinder(Vector3.Zero, radius, length, blue, segmentCount);
            axis += CreateCone(Vector3.UnitZ * length, tipRadius, tipLength, blue, segmentCount);

            // X axis (90* rotation around Y)
            axis += CreateCylinder(Vector3.Zero, radius, length, red, segmentCount)
                        .Transform(Matrix4x4.CreateRotationY(halfPi) * Matrix4x4.CreateRotationZ(halfPi));
            axis += CreateCone(Vector3.UnitZ * length, tipRadius, tipLength, red, segmentCount)
                        .Transform(Matrix4x4.CreateRotationY(halfPi) * Matrix4x4.CreateRotationZ(halfPi));

            // Y axis (90* rotation around X)
            axis += CreateCylinder(Vector3.Zero, radius, length, green, segmentCount)
                       .Transform(Matrix4x4.CreateRotationX(halfPi) * Matrix4x4.CreateRotationZ(halfPi));
            axis += CreateCone(Vector3.UnitZ * length, tipRadius, tipLength, green, segmentCount)
                        .Transform(Matrix4x4.CreateRotationX(halfPi) * Matrix4x4.CreateRotationZ(halfPi));

            return axis;
        }

        public TriangulationList<Vector3> CreateArrow(float radius = 0.05f, float length = 0.75f, float tipRadius = 0.1f, float tipLength = 0.25f, int segmentCount = 30)
        {
            TriangulationList<Vector3> arrow = new TriangulationList<Vector3>();

            var white = VectorsExtensions.CreateColor(255, 255, 255, 255);

            // Z axis
            arrow += CreateCylinder(Vector3.Zero, radius, length, white, segmentCount);
            arrow += CreateCone(Vector3.UnitZ * length, tipRadius, tipLength, white, segmentCount);

            return arrow;
        }


    }
}
