using DEM.Net.Core;
using DEM.Net.glTF.SharpglTF;
using g3;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DEM.Net.glTF
{
    public class MeshReducer
    {
        private readonly ILogger<MeshReducer> _logger;
        private readonly SharpGltfService _sharpGltfService;
        public MeshReducer(ILogger<MeshReducer> logger, SharpGltfService sharpGltfService)
        {
            _logger = logger;
            _sharpGltfService = sharpGltfService;
        }
        public ModelRoot Decimate(ModelRoot inputModel, float quality = 0.5F)
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
            var outModel = CloneGlbWithMesh(inputModel, outTriangulation);
            float t3 = stopwatch.ElapsedMilliseconds - t2;

            _logger.LogInformation($"QEM Execution details [ms]." +
                $"\nOverall time: {t1 + t2 + t3}" +
                $"\n GLB Loading: {t1}" +
                $"\n QEM Algorithm: {t2}" +
                $"\n GLB Saving: {t3}");

            return outModel;
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


            _logger.LogInformation($"QEM Execution details [ms]." +
                $"\nOverall time: {t1 + t2 + t3}" +
                $"\n STL Loading: {t1}" +
                $"\n QEM Algorithm: {t2}" +
                $"\n STL Saving: {t3}");

            return outTriangulation;
        }
        private void PrintMeshInfo(string header, int verts, int currentTrisCount)
        {
            _logger.LogInformation($"{header} | Vertices: {verts}, Tris: {currentTrisCount}");
        }

        private ModelRoot CloneGlbWithMesh(SharpGLTF.Schema2.ModelRoot inputModel, Triangulation outTriangulation)
        {
            ModelRoot outputModel = _sharpGltfService.CloneWithMesh(inputModel, new IndexedTriangulation(outTriangulation));
            return outputModel;
        }
        private DMesh3 LoadTriangulation(Triangulation triangulation)
        {
            var mesh = DMesh3Builder.Build<Vector3d, int, Vector3d>(triangulation.Positions.Select(p => p.AsVector3()).Select(v => new Vector3d(v.X, v.Y, v.Z)), triangulation.Indices);

            return mesh;
        }

        private Triangulation SaveTriangulation(DMesh3 mesh)
        {
            mesh.CompactInPlace();
            List<int> indices = new List<int>(mesh.TriangleCount * 3);
            foreach (var index in mesh.Triangles())
            {
                indices.Add(index.a); indices.Add(index.b); indices.Add(index.c);
            }
            List<GeoPoint> vectors = mesh.Vertices().Select(v => new GeoPoint(v.y, v.x, v.z))
                                    .ToList();

            return new Triangulation(vectors, indices);
        }

        private DMesh3 Decimate(DMesh3 mesh, int targetCount)
        {
            Reducer r = new Reducer(mesh);

            //DMeshAABBTree3 tree = new DMeshAABBTree3(new DMesh3(mesh));
            //tree.Build();
            r.SetExternalConstraints(new MeshConstraints());
            MeshConstraintUtil.FixAllBoundaryEdges(r.Constraints, mesh);
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
