using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.Lib;
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
        /// Triangulates the height map and build a triangle mesh
        /// </summary>
        /// <param name="heightMap"></param>
        /// <param name="colorFunc">Function delegate for assigning a color to a point (ie: using elevation or other property for instance).
        /// Pass null to use white as default.</param>
        /// <returns></returns>
        MeshPrimitive GenerateTriangleMesh(HeightMap heightMap, Func<GeoPoint, Vector3> colorFunc = null);

        /// <summary>
        /// Generate triangle mesh with supplied triangulation
        /// </summary>
        /// <param name="points">Vertices</param>
        /// <param name="indices">Triplets of vertex index in points list</param>
        /// <param name="color"></param>
        /// <returns></returns>
        MeshPrimitive GenerateTriangleMesh(IEnumerable<GeoPoint> points, List<int> indices, Vector4 color);

        /// <summary>
        /// Build a line mesh from given points
        /// </summary>
        /// <param name="points"></param>
        /// <param name="color">Line color</param>
        /// <param name="width">If width > 1 then line will be a mesh of given width</param>
        /// <returns></returns>
        MeshPrimitive GenerateLine(IEnumerable<GeoPoint> points, Vector4 color, float width = 1f);

        /// <summary>
        /// Build a line mesh from given points
        /// </summary>
        /// <param name="points"></param>
        /// <param name="color">Line color</param>
        /// <returns></returns>
        MeshPrimitive GeneratePointMesh(IEnumerable<GeoPoint> points, Vector4 color);

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
