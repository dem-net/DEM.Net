//
// MeshTransform.cs
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
        //public static void RotateX(this MeshPrimitive mesh, float angle)
        //{
        //    var matrix = Matrix4x4.CreateRotationX(angle);
        //    Transform(mesh, matrix);
        //}
        //public static void RotateY(this MeshPrimitive mesh, float angle)
        //{
        //    var matrix = Matrix4x4.CreateRotationY(angle);
        //    Transform(mesh, matrix);
        //}
        //public static void RotateZ(this MeshPrimitive mesh, float angle)
        //{
        //    var matrix = Matrix4x4.CreateRotationZ(angle);
        //    Transform(mesh, matrix);
        //}
        //public static void Translate(this MeshPrimitive mesh, float x,float y, float z)
        //{
        //    var matrix = Matrix4x4.CreateTranslation(x,y,z);
        //    Transform(mesh, matrix);
        //}
        //public static void Transform(this MeshPrimitive mesh, Matrix4x4 matrix)
        //{
        //    mesh.Positions = mesh.Positions.Select(v => Vector3.Transform(v, matrix));
        //    mesh.Normals = mesh.Normals.Select(v => Vector3.Transform(v, matrix));
        //}
    }
}
