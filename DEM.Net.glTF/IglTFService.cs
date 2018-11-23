using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <returns></returns>
        MeshPrimitive GenerateTriangleMesh(HeightMap heightMap);

        /// <summary>
        /// Generate a full glTF model from a mesh
        /// </summary>
        /// <param name="meshPrimitive"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Model GenerateModel(MeshPrimitive meshPrimitive, string name);


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

    }
}
