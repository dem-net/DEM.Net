using AssetGenerator.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.glTF
{
    public static class MeshTransform
    {
        public static void RotateX(this MeshPrimitive mesh, float angle)
        {
            var matrix = Matrix4x4.CreateRotationX(angle);
            Transform(mesh, matrix);
        }
        public static void RotateY(this MeshPrimitive mesh, float angle)
        {
            var matrix = Matrix4x4.CreateRotationY(angle);
            Transform(mesh, matrix);
        }
        public static void RotateZ(this MeshPrimitive mesh, float angle)
        {
            var matrix = Matrix4x4.CreateRotationZ(angle);
            Transform(mesh, matrix);
        }
        public static void Transform(this MeshPrimitive mesh, Matrix4x4 matrix)
        {
            mesh.Positions = mesh.Positions.Select(v => Vector3.Transform(v, matrix));
            mesh.Normals = mesh.Normals.Select(v => Vector3.Transform(v, matrix));
        }
    }
}
