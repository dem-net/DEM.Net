using System.Collections.Generic;
using System.Numerics;

namespace DEM.Net.Core
{
    public interface IMeshService
    {
        IEnumerable<Vector3> ComputeNormals(HeightMap heightMap);
        IEnumerable<Vector3> ComputeMeshNormals(IList<Vector3> positions, IList<int> indices);
        Triangulation GenerateTriangleMesh_Boxed(HeightMap heightMap, BoxBaseThickness thickness, float zValue);
        Triangulation TriangulateHeightMap(HeightMap heightMap, bool regularTriangulation = true);
        (IEnumerable<Vector3> positions, IEnumerable<int> indexes) GenerateTriangleMesh_Line(IEnumerable<GeoPoint> gpxPointsElevated, float trailWidthMeters);

        TriangulationList<GeoPoint> Tesselate(IEnumerable<GeoPoint> outerRingPoints, IEnumerable<IEnumerable<GeoPoint>> innerRingsPoints);
    }
}