using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Mesh
{
    public static class MeshService
    {
        /// <summary>
        /// Triangulate a regular set of points
        /// </summary>
        /// <param name="heightMap">Gridded set of points. Corrdinates can differ, 
        /// but height map should be organized a set of rows and columns</param>
        /// <param name="regularTriangulation">(optional) Determines which diagnal is choosen</param>
        /// <returns>List of indexes (triplets) in height map coordinates, indicating each of the triangles vertices</returns>
        public static EnumerableWithCount<int> TriangulateHeightMap(HeightMap heightMap, bool regularTriangulation = true)
        {

            int capacity = ((heightMap.Width - 1) * 6) * (heightMap.Height - 1);
            EnumerableWithCount<int> indices = new EnumerableWithCount<int>(capacity,
                TriangulateHeightMap_Internal(heightMap, regularTriangulation));

            return indices;
        }
        private static IEnumerable<int> TriangulateHeightMap_Internal(HeightMap heightMap, bool regularTriangulation = true)
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

        public static IEnumerableWithCount<Vector3> ComputeNormals(List<Vector3> positions, List<int> indices)
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
        public static IEnumerableWithCount<Vector3> ComputeNormals(HeightMap heightMap)
        {
            var indices = TriangulateHeightMap(heightMap);
            var normals = ComputeNormals(
                    heightMap.Coordinates.Select(c =>
                                                            new Vector3((
                                                                float)c.Longitude,
                                                                (float)c.Latitude,
                                                                (float)c.Elevation)
                                                            ).ToList(),
            indices.ToList());

            return normals;
        }

    }
}
