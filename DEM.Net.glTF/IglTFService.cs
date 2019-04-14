//
// IglTFService.cs
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

using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Services.Mesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.glTF
{
    public interface IglTFService
    {
        /// <summary>
        /// Generates the triangle with a base, and sides connecting the base.
        /// This is a STL compliant mesh which is water tight
        /// </summary>
        /// <returns>The triangle mesh boxed.</returns>
        /// <param name="heightMap">Height map.</param>
        /// <param name="colors">Colors.</param>
        /// <param name="texture">Texture.</param>
        MeshPrimitive GenerateTriangleMesh_Boxed(HeightMap heightMap, IEnumerable<Vector4> colors = null, PBRTexture texture = null
            , BoxBaseThickness thickness = BoxBaseThickness.FixedElevation, float zValue = 0f);

        /// <summary>
        /// Triangulates the height map and build a triangle mesh
        /// </summary>
        /// <param name="heightMap"></param>
        /// <param name="colors">Colors. Pass null to use white as default.</param>
        /// <param name="texture">Texture path relative from the model</param>
        /// <returns></returns>
        MeshPrimitive GenerateTriangleMesh(HeightMap heightMap, IEnumerable<Vector4> colors = null, PBRTexture texture = null);

        /// <summary>
        /// Generate triangle mesh with supplied triangulation
        /// </summary>
        /// <param name="points">Vertices</param>
        /// <param name="indices">Triplets of vertex index in points list</param>
        /// <param name="color"></param>
        /// <param name="texture">Texture path relative from the model</param>
        /// <returns></returns>
        MeshPrimitive GenerateTriangleMesh(IEnumerable<GeoPoint> points, List<int> indices, IEnumerable<Vector4> colors = null, PBRTexture texture = null);

        /// <summary>
        /// Build a line mesh from given points
        /// </summary>
        /// <param name="points"></param>
        /// <param name="color">Line color</param>
        /// <param name="width">If width > 1 then line will be a mesh of given width</param>
        /// <returns></returns>
        MeshPrimitive GenerateLine(IEnumerable<GeoPoint> points, Vector4 color, float width = 1f);

        /// <summary>
        /// Low level method generating triangle mesh using supplied indices.
        /// However, normals are stil computed within this method
        /// </summary>
        /// <param name="points"></param>
        /// <param name="indices"></param>
        /// <param name="colors"></param>
        /// <param name="texture">Texture path relative from the model</param>
        /// <returns></returns>
        MeshPrimitive GenerateTriangleMesh(IEnumerable<Vector3> points, List<int> indices, IEnumerable<Vector4> colors = null, PBRTexture texture = null);

        /// <summary>
        /// Build a line mesh from given points
        /// </summary>
        /// <param name="points"></param>
        /// <param name="color">Line color</param>
        /// <param name="pointSize">If >0 quads of specified size will be generated (and the ouput mesh will be TRIANGLES)</param>
        /// <returns></returns>
        MeshPrimitive GeneratePointMesh(IEnumerable<GeoPoint> points, Vector4 color, float pointSize);

        /// <summary>
        /// Generate a full glTF model from a mesh
        /// </summary>
        /// <param name="meshPrimitive"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Model GenerateModel(MeshPrimitive meshPrimitive, string name);

        /// <summary>
        /// Generate a full glTF model from a mesh
        /// </summary>
        /// <param name="meshPrimitive"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Model GenerateModel(IEnumerable<MeshPrimitive> meshPrimitives, string name);


        /// <summary>
        /// Export model to disk
        /// </summary>
        /// <param name="model"></param>
        /// <param name="directoryName"></param>
        /// <param name="modelName"></param>
        /// <param name="exportglTF">If true then glTF file and all associated .bin will be exported.</param>
        /// <param name="exportGLB">If true then a single packed GLB file will be exported.</param>
		void Export(Model model, string outputFolder, string modelName, bool exportglTF = true, bool exportGLB = true);

        glTFLoader.Schema.Gltf Import(string path);

       

        #region Helpers
        IEnumerable<MeshPrimitive> GetHeightPlanes();
        #endregion

    }
}
