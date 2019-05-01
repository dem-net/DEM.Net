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

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public class MeshService : IMeshService
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

        public IEnumerableWithCount<Vector3> ComputeNormals(List<Vector3> positions, List<int> indices)
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
                //Accumulate it to norm array for i1, i2, i3
                norm[i1] += dir;
                norm[i2] += dir;
                norm[i3] += dir;
            }

            return new EnumerableWithCount<Vector3>(nV, norm.Select(v => Vector3.Normalize(v)));
        }

        /// <summary>
        /// Calculate normals for a given height map
        /// </summary>
        /// <param name="heightMap">Height map (gridded data)</param>
        /// <returns>Normals for each point of the height map</returns>
        public IEnumerableWithCount<Vector3> ComputeNormals(HeightMap heightMap)
        {
            var triangulation = TriangulateHeightMap(heightMap);
            var normals = ComputeNormals(
                    heightMap.Coordinates.Select(c =>
                                                            new Vector3((
                                                                float)c.Longitude,
                                                                (float)c.Latitude,
                                                                (float)c.Elevation)
                                                            ).ToList(),
            triangulation.Indices.ToList());

            return normals;
        }

    }
}
