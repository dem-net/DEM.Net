using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using DEM.Net.glTF.SharpglTF;
using g3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DEM.Net.glTF
{
    public class MeshReducer
    {
        private readonly ILogger<MeshReducer> _logger;
        private readonly DEMNetOptions _options;

        public MeshReducer(ILogger<MeshReducer> logger, IOptions<DEMNetOptions> options)
        {
            _logger = logger;
            _options = options?.Value;
        }

        public Triangulation Decimate(ModelRoot inputModel, float quality = 0.5F)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            DMesh3 originalMesh = LoadGLB(inputModel);
            int targetCount = (int)(quality * originalMesh.TriangleCount);
            PrintMeshInfo("Input", originalMesh.VertexCount, originalMesh.TriangleCount);
            float t1 = stopwatch.ElapsedMilliseconds;

            originalMesh = Decimate(originalMesh, targetCount);
            PrintMeshInfo("Input", originalMesh.VertexCount, originalMesh.TriangleCount);
            float t2 = stopwatch.ElapsedMilliseconds - t1;

            Triangulation outTriangulation = SaveTriangulation(originalMesh);
            //var outModel = CloneGlbWithMesh(inputModel, outTriangulation);
            float t3 = stopwatch.ElapsedMilliseconds - t2;

            _logger.LogInformation($"QEM Execution details [ms]: Overall time: {t1 + t2 + t3}, Loading: {t1}, QEM Algorithm: {t2}, Saving: {t3}");

            return outTriangulation;
        }
        public Triangulation Decimate(Triangulation triangulation, float quality = 0.5F)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var originalMesh = LoadTriangulation(triangulation);
            int targetCount = (int)(quality * originalMesh.TriangleCount);

            PrintMeshInfo("Input", originalMesh.VertexCount, originalMesh.TriangleCount);
            float t1 = stopwatch.ElapsedMilliseconds;

            originalMesh = Decimate(originalMesh, targetCount);
            PrintMeshInfo("Input", originalMesh.VertexCount, originalMesh.TriangleCount);
            float t2 = stopwatch.ElapsedMilliseconds - t1;

            Triangulation outTriangulation = SaveTriangulation(originalMesh);
            float t3 = stopwatch.ElapsedMilliseconds - t2;


            _logger.LogInformation($"QEM Execution details [ms]: Overall time: {t1 + t2 + t3}, Loading: {t1}, QEM Algorithm: {t2}, Saving: {t3}");

            return outTriangulation;
        }
        private void PrintMeshInfo(string header, int verts, int currentTrisCount)
        {
            _logger.LogInformation($"{header} | Vertices: {verts}, Tris: {currentTrisCount}");
        }
       
        private DMesh3 LoadTriangulation(Triangulation triangulation)
        {
            var mesh = DMesh3Builder.Build<Vector3d, int, Vector3d>(triangulation.Positions.Select(p => p.AsVector3()).Select(v => new Vector3d(v.X, v.Y, v.Z)), triangulation.Indices);

            return mesh;
        }

        private Triangulation SaveTriangulation(DMesh3 mesh)
        {
            mesh.CompactInPlace();           

            return new Triangulation(GetMeshPositions(mesh), GetMeshIndices(mesh));
        }

        private IEnumerable<GeoPoint> GetMeshPositions(DMesh3 mesh)
        {
            return mesh.Vertices().Select(v => new GeoPoint(v.y, v.x, v.z));
        }

        private IEnumerable<int> GetMeshIndices(DMesh3 mesh)
        {
            foreach (var index in mesh.Triangles())
            {
                yield return index.a;
                yield return index.b;
                yield return index.c;
            }
        }
       
        private DMesh3 Decimate(DMesh3 mesh, int targetCount)
        {
            Reducer r = new Reducer(mesh);

            if (_options.ReduceMeshPreserveEdges)
            {
                r.SetExternalConstraints(new MeshConstraints());
                MeshConstraintUtil.FixAllBoundaryEdges(r.Constraints, mesh);
            }
            r.ReduceToTriangleCount(targetCount);

            return mesh;
        }

        private DMesh3 LoadGLB(ModelRoot inputModel)
        {
            var glbMesh = inputModel.LogicalMeshes.First().Primitives.First();

            var vertices = glbMesh.GetVertices("POSITION").AsVector3Array();
            var indices = glbMesh.GetIndices();
            var normals = glbMesh.GetVertices("NORMAL").AsVector3Array();

            var mesh = DMesh3Builder.Build(vertices, indices, normals);
            return mesh;
        }
    }
}
