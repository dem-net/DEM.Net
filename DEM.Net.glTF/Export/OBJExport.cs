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
        public string ExportGlTFModelToWaveFrontObj(ModelRoot model, string directory, string fileName, bool overwrite = false, bool zip = false)
        {
            if (Directory.Exists(directory))
            {
                if (!overwrite)
                    throw new ArgumentException("Directory is not empty", nameof(directory));

                Directory.Delete(directory, recursive: true);
            }
            Directory.CreateDirectory(directory);

            fileName = Path.ChangeExtension(Path.Combine(directory, Path.GetFileName(fileName)), ".obj");
            string fileTitle = Path.GetFileNameWithoutExtension(fileName);

            string materialFile = Path.Combine(directory, $"{fileTitle}.mtl");
            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            using (FileStream fsMtl = new FileStream(materialFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            using (StreamWriter sw = new StreamWriter(fs))
            using (StreamWriter swMtl = new StreamWriter(fsMtl))
            {
                WriteHeader(model, sw, $"{fileTitle}.mtl");

                int materialIndex = 0;
                int indexOffsetVertices = 1;
                int indexOffsetTexCoords = 1;
                int indexOffsetNormals = 1;
                int nbVertices = 0;
                int nbTexCoords = 0;
                int nbNormals = 0;
                foreach (var logicalMesh in model.LogicalMeshes)
                {
                    sw.WriteLine($"o {logicalMesh.Name}");

                    foreach (var meshPrimitive in logicalMesh.Primitives)
                    {
                        var accessors = meshPrimitive.VertexAccessors;

                        var vertices = meshPrimitive.GetVertices("POSITION").AsVector3Array();
                        WriteVectors(sw, vertices, "v");
                        nbVertices = vertices.Count;

                        bool hasTexture = accessors.ContainsKey("TEXCOORD_0");
                        if (hasTexture)
                        {
                            var texCoords = meshPrimitive.GetVertices("TEXCOORD_0").AsVector2Array();
                            WriteVectors(sw, texCoords, "vt");
                            nbTexCoords = texCoords.Count;
                        }
                        bool hasNormals = accessors.ContainsKey("NORMAL");
                        if (hasNormals)
                        {
                            var normals = meshPrimitive.GetVertices("NORMAL").AsVector3Array();
                            WriteVectors(sw, normals, "vn");
                            nbNormals = normals.Count;
                        }

                        string materialName = $"Material_{materialIndex:000}";
                        if (hasTexture)
                        {
                            sw.WriteLine($"usemtl {materialName}");
                            sw.WriteLine($"s 1"); // smooth shading
                        }

                        var indices = meshPrimitive.GetIndices();
                        WriteFaces(sw, indices, "f", indexOffsetVertices, indexOffsetTexCoords, indexOffsetNormals, hasTexture);
                        indexOffsetVertices += nbVertices;
                        indexOffsetNormals += nbNormals;
                        indexOffsetTexCoords += nbTexCoords;

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

                string outputFileName = Path.Combine(new DirectoryInfo(directory).Parent.FullName, fileTitle + ".zip");
                if (File.Exists(outputFileName))
                {
                    if (overwrite)
                        File.Delete(outputFileName);
                }
                ZipFile.CreateFromDirectory(directory, outputFileName, compressionLevel: CompressionLevel.Fastest, false);
                Directory.Delete(directory, recursive: true);
                return outputFileName;
            }
            else
            {
                return fileName;
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

        void WriteFaces(StreamWriter sw, IList<uint> indices, string prefix, int indexOffsetVertices, int indexOffsetTexCoords, int indexOffsetNormals, bool hasTexture)
        {
            if (hasTexture)
            {
                for (var i = 0; i < indices.Count; i += 3)
                {
                    sw.WriteLine($"{prefix} {indices[i] + indexOffsetVertices}/{indices[i] + indexOffsetTexCoords}/{indices[i] + indexOffsetNormals} " +
                                            $"{indices[i + 1] + indexOffsetVertices}/{indices[i + 1] + indexOffsetTexCoords}/{indices[i + 1] + indexOffsetNormals} " +
                                            $"{indices[i + 2] + indexOffsetVertices}/{indices[i + 2] + indexOffsetTexCoords}/{indices[i + 2] + indexOffsetNormals}");
                }
            }
            else
            {
                for (var i = 0; i < indices.Count; i += 3)
                {
                    sw.WriteLine($"{prefix} {indices[i] + indexOffsetVertices}//{indices[i] + indexOffsetNormals} " +
                                            $"{indices[i + 1] + indexOffsetVertices}//{indices[i + 1] + indexOffsetNormals} " +
                                            $"{indices[i + 2] + indexOffsetVertices}//{indices[i + 2] + indexOffsetNormals}");
                }
            }
            sw.WriteLine();
        }

        void WriteVectors(StreamWriter sw, SharpGLTF.Memory.Vector2Array texCoords, string prefix)
        {
            var c = CultureInfo.InvariantCulture;

            foreach (var v in texCoords)
            {
                sw.WriteLine($"{prefix} {v.X.ToString(c)} {(1f - v.Y).ToString(c)}");
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
