using AssetGenerator.Runtime;
using MeshDecimator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    public static class MeshDecimationWrapper
    {
        private static MeshDecimator.Mesh CreateDecimationMesh(MeshPrimitive mesh)
        {
            MeshDecimator.Mesh outMesh = new MeshDecimator.Mesh(mesh.Positions.ToArray(), mesh.Indices.ToArray())
            { Colors = mesh.Colors.ToArray() };
            return outMesh;
        }

        private static MeshPrimitive CreateFromDecimationMesh(MeshDecimator.Mesh decimMesh)
        {
            MeshPrimitive outMesh = new MeshPrimitive() { Positions = decimMesh.Vertices, Indices = decimMesh.Indices,Colors = decimMesh.Colors };
            return outMesh;
        }

        public static MeshPrimitive Decimate(MeshPrimitive mesh, float quality = 0.5f)
        {
            Stopwatch swConvert = Stopwatch.StartNew();
            
            MeshDecimator.Mesh inputMesh = CreateDecimationMesh(mesh);

            swConvert.Stop();

            int currentTriangleCount = inputMesh.Indices.Length / 3;
            int targetTriangleCount = (int)Math.Ceiling(currentTriangleCount * quality);
            Console.WriteLine("Input: {0} vertices, {1} triangles (target {2})",
                inputMesh.Vertices.Length, currentTriangleCount, targetTriangleCount);

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Reset();
            stopwatch.Start();

            var algorithm = MeshDecimation.CreateAlgorithm(Algorithm.Default);
            algorithm.Verbose = true;
            MeshDecimator.Mesh destMesh = MeshDecimation.DecimateMesh(algorithm, inputMesh, targetTriangleCount);
            stopwatch.Stop();

            swConvert.Start();

            MeshPrimitive outputMesh = CreateFromDecimationMesh(destMesh);

            swConvert.Stop();
            
            int outputTriangleCount = 0;
            var destIndices = destMesh.GetSubMeshIndices();
            for (int i = 0; i < destIndices.Length; i++)
            {
                outputTriangleCount += (destIndices[i].Length / 3);
            }
            float reduction = (float)outputTriangleCount / (float)currentTriangleCount;
            float timeTaken = (float)stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine("Output: {0} vertices, {1} triangles ({2} reduction; {3:0.0000} sec, conversion {4:0.0000})",
                destMesh.Vertices.Length, outputTriangleCount, reduction, timeTaken, swConvert.Elapsed.TotalSeconds);

            return outputMesh;
        }
    }
}
