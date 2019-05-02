﻿using System.Collections.Generic;
using System.Numerics;

namespace DEM.Net.Core
{
    public interface IMeshService
    {
        IEnumerableWithCount<Vector3> ComputeNormals(HeightMap heightMap);
        IEnumerableWithCount<Vector3> ComputeNormals(List<Vector3> positions, List<int> indices);
        Triangulation GenerateTriangleMesh_Boxed(HeightMap heightMap, BoxBaseThickness thickness, float zValue);
        Triangulation TriangulateHeightMap(HeightMap heightMap, bool regularTriangulation = true);
    }
}