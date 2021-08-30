using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.glTF.Export
{
    public class OBJExportService
    {
        readonly ILogger<OBJExportService> _logger;

        public OBJExportService(ILogger<OBJExportService> logger = null)
        {
            _logger = logger;
        }
        public void ExportGlTFModelToWaveFrontObj(ModelRoot model, string directory, string fileName, bool overwrite = false, bool zip = false)
        {
            if (Directory.Exists(directory))
            {
                if (!overwrite)
                    throw new ArgumentException("Directory is not empty", nameof(directory));

                Directory.Delete(directory, recursive: true);
            }
            Directory.CreateDirectory(directory);

            fileName = Path.Combine(directory, Path.GetFileName(fileName));
            string fileTitle = Path.GetFileNameWithoutExtension(fileName);

            string materialFile = Path.Combine(directory, $"{fileTitle}.mtl");
            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            using (FileStream fsMtl = new FileStream(materialFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            using (StreamWriter sw = new StreamWriter(fs))
            using (StreamWriter swMtl = new StreamWriter(fsMtl))
            {
                WriteHeader(model, sw, $"{fileTitle}.mtl");

                int materialIndex = 0;
                int indexOffset = 1;
                foreach (var logicalMesh in model.LogicalMeshes)
                {
                    sw.WriteLine($"o {logicalMesh.Name}");

                    foreach (var meshPrimitive in logicalMesh.Primitives)
                    {
                        var accessors = meshPrimitive.VertexAccessors;

                        var vertices = meshPrimitive.GetVertices("POSITION").AsVector3Array();
                        WriteVectors(sw, vertices, "v");

                        bool hasTexture = accessors.ContainsKey("TEXCOORD_0");
                        if (hasTexture)
                        {
                            var texCoords = meshPrimitive.GetVertices("TEXCOORD_0").AsVector2Array();
                            WriteVectors(sw, texCoords, "vt");
                        }
                        var normals = meshPrimitive.GetVertices("NORMAL").AsVector3Array();
                        WriteVectors(sw, normals, "vn");

                        string materialName = $"Material_{materialIndex:000}";
                        if (hasTexture)
                        {
                            sw.WriteLine($"usemtl {materialName}");
                            sw.WriteLine($"s 1"); // smooth shading
                        }

                        var indices = meshPrimitive.GetIndices();
                        WriteFaces(sw, indices, "f", indexOffset, hasTexture);
                        indexOffset += vertices.Count;

                        var matChannel = meshPrimitive.Material.FindChannel("BaseColor").Value;
                        if (matChannel.Texture != null)
                        {
                            var imgContent = matChannel.Texture.PrimaryImage.Content;
                            var textureFileName = $"texture_{ materialIndex}.{ imgContent.FileExtension}";
                            imgContent.SaveToFile(Path.Combine(directory, textureFileName));

                            WriteTexture(swMtl, materialName, textureFileName);
                        }

                        materialIndex++;
                    }
                }
            }

            if (zip)
            {
                if (File.Exists(fileTitle + ".zip"))
                    File.Delete(fileTitle + ".zip");

                ZipFile.CreateFromDirectory(directory, fileTitle + ".zip", compressionLevel: CompressionLevel.Fastest, false);
            }
        }

        void WriteTexture(StreamWriter swMtl, string materialName, string textureFileName)
        {
            swMtl.WriteLine($"newmtl {materialName}");
            swMtl.WriteLine("Ns 96.078431");
            swMtl.WriteLine("Ka 0.000000 0.000000 0.000000");
            swMtl.WriteLine("Kd 0.640000 0.640000 0.640000");
            swMtl.WriteLine("Ks 0.000000 0.000000 0.000000");
            swMtl.WriteLine("Ni 1.000000");
            swMtl.WriteLine("d 1.000000");
            swMtl.WriteLine("illum 1");
            swMtl.WriteLine($"map_Kd {textureFileName}");
            swMtl.WriteLine();
        }

        void WriteFaces(StreamWriter sw, IList<uint> indices, string prefix, int indexOffset, bool hasTexture)
        {
            var c = CultureInfo.InvariantCulture;

            if (hasTexture)
            {
                for (var i = 0; i < indices.Count; i += 3)
                {
                    sw.WriteLine($"{prefix} {indices[i] + indexOffset}/{indices[i] + indexOffset}/{indices[i] + indexOffset} {indices[i + 1] + indexOffset}/{indices[i + 1] + indexOffset}/{indices[i + 1] + indexOffset} {indices[i + 2] + indexOffset}/{indices[i + 2] + indexOffset}/{indices[i + 2] + indexOffset}");
                }
            }
            else
            {
                for (var i = 0; i < indices.Count; i += 3)
                {
                    sw.WriteLine($"{prefix} {indices[i] + indexOffset}//{indices[i] + indexOffset} {indices[i + 1] + indexOffset}//{indices[i + 1] + indexOffset} {indices[i + 2] + indexOffset}//{indices[i + 2] + indexOffset}");
                }
            }
            sw.WriteLine();
        }

        void WriteVectors(StreamWriter sw, SharpGLTF.Memory.Vector2Array texCoords, string prefix)
        {
            var c = CultureInfo.InvariantCulture;

            foreach (var v in texCoords)
            {
                sw.WriteLine($"{prefix} {v.X.ToString(c)} {v.Y.ToString(c)}");
            }
            sw.WriteLine();
        }

        void WriteVectors(StreamWriter sw, SharpGLTF.Memory.Vector3Array vertices, string prefix)
        {
            var c = CultureInfo.InvariantCulture;

            foreach (var v in vertices)
            {
                sw.WriteLine($"{prefix} {v.X.ToString(c)} {v.Y.ToString(c)} {v.Z.ToString(c)}");
            }
            sw.WriteLine();
        }

        void WriteHeader(ModelRoot model, StreamWriter sw, string materialLibFile)
        {
            sw.WriteLine("# DEM.Net Elevation API");
            sw.WriteLine("# https://elevationapi.com");
            sw.WriteLine();
            sw.WriteLine($"mtllib {materialLibFile}");
            sw.WriteLine();
        }
    }
}
