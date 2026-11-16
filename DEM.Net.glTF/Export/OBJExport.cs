using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;

namespace DEM.Net.glTF.Export;

public class OBJExportService(ILogger<OBJExportService>? logger = null)
{
    /// <summary>
    /// Writes the model as a Wavefront OBJ file in <paramref name="directory"/>, with its material file and textures
    /// </summary>
    /// <param name="overwrite">Deletes the directory first (and the zip file) when it exists, an error otherwise</param>
    /// <param name="zip">Zips the directory next to it (directory name + .zip) and deletes the directory</param>
    /// <returns>OBJ file path, or the zip file path</returns>
    public async Task<string> ExportGlTFModelToWaveFrontObjAsync(ModelRoot model, string directory, string fileName, bool overwrite = false, bool zip = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
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

        try
        {
            await WriteObjAsync(model, directory, fileName, materialFile, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger?.LogError(ex, "Error while exporting OBJ model");
            throw;
        }

        if (!zip)
            return fileName;

        string outputFileName = Path.Combine(new DirectoryInfo(directory).Parent!.FullName, fileTitle + ".zip");
        if (overwrite && File.Exists(outputFileName))
            File.Delete(outputFileName);

        await ZipFile.CreateFromDirectoryAsync(directory, outputFileName, CompressionLevel.Fastest, includeBaseDirectory: false, cancellationToken).ConfigureAwait(false);
        Directory.Delete(directory, recursive: true);
        return outputFileName;
    }

    private static async Task WriteObjAsync(ModelRoot model, string directory, string fileName, string materialFile, CancellationToken cancellationToken)
    {
        string fileTitle = Path.GetFileNameWithoutExtension(fileName);
        await using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 1 << 16, useAsync: true);
        await using var fsMtl = new FileStream(materialFile, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true);
        await using var sw = new TextChunkWriter(fs);
        await using var swMtl = new TextChunkWriter(fsMtl);

        await WriteHeaderAsync(sw, $"{fileTitle}.mtl", cancellationToken).ConfigureAwait(false);

        int materialIndex = 0;
        int indexOffsetVertices = 1;
        int indexOffsetTexCoords = 1;
        int indexOffsetNormals = 1;
        int nbVertices = 0;
        int nbTexCoords = 0;
        int nbNormals = 0;
        foreach (var logicalMesh in model.LogicalMeshes)
        {
            await sw.WriteLineAsync($"o {logicalMesh.Name}", cancellationToken).ConfigureAwait(false);

            foreach (var meshPrimitive in logicalMesh.Primitives)
            {
                var accessors = meshPrimitive.VertexAccessors;

                var vertices = meshPrimitive.GetVertices("POSITION").AsVector3Array();
                await WriteVectorsAsync(sw, vertices, "v", cancellationToken).ConfigureAwait(false);
                nbVertices = vertices.Count;

                bool hasTexture = accessors.ContainsKey("TEXCOORD_0");
                if (hasTexture)
                {
                    var texCoords = meshPrimitive.GetVertices("TEXCOORD_0").AsVector2Array();
                    await WriteTexCoordsAsync(sw, texCoords, "vt", cancellationToken).ConfigureAwait(false);
                    nbTexCoords = texCoords.Count;
                }
                bool hasNormals = accessors.ContainsKey("NORMAL");
                if (hasNormals)
                {
                    var normals = meshPrimitive.GetVertices("NORMAL").AsVector3Array();
                    await WriteVectorsAsync(sw, normals, "vn", cancellationToken).ConfigureAwait(false);
                    nbNormals = normals.Count;
                }

                string materialName = $"Material_{materialIndex:000}";
                if (hasTexture)
                {
                    await sw.WriteLineAsync($"usemtl {materialName}", cancellationToken).ConfigureAwait(false);
                    await sw.WriteLineAsync("s 1", cancellationToken).ConfigureAwait(false); // smooth shading
                }

                var indices = meshPrimitive.GetIndices();
                await WriteFacesAsync(sw, indices, "f", indexOffsetVertices, indexOffsetTexCoords, indexOffsetNormals, hasTexture, cancellationToken).ConfigureAwait(false);
                indexOffsetVertices += nbVertices;
                indexOffsetNormals += nbNormals;
                indexOffsetTexCoords += nbTexCoords;

                var matChannel = meshPrimitive.Material.FindChannel("BaseColor")!.Value;
                if (matChannel.Texture != null)
                {
                    var imgContent = matChannel.Texture.PrimaryImage.Content;
                    var textureFileName = $"texture_{materialIndex}.{imgContent.FileExtension}";
                    await File.WriteAllBytesAsync(Path.Combine(directory, textureFileName), imgContent.Content, cancellationToken).ConfigureAwait(false);

                    await WriteTextureAsync(swMtl, materialName, textureFileName, cancellationToken).ConfigureAwait(false);
                }

                materialIndex++;
            }
        }
        await sw.FlushAsync(cancellationToken).ConfigureAwait(false);
        await swMtl.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteTextureAsync(TextChunkWriter swMtl, string materialName, string textureFileName, CancellationToken cancellationToken)
    {
        await swMtl.WriteLineAsync($"newmtl {materialName}", cancellationToken).ConfigureAwait(false);
        await swMtl.WriteLineAsync("Ns 96.078431", cancellationToken).ConfigureAwait(false);
        await swMtl.WriteLineAsync("Ka 0.000000 0.000000 0.000000", cancellationToken).ConfigureAwait(false);
        await swMtl.WriteLineAsync("Kd 0.640000 0.640000 0.640000", cancellationToken).ConfigureAwait(false);
        await swMtl.WriteLineAsync("Ks 0.000000 0.000000 0.000000", cancellationToken).ConfigureAwait(false);
        await swMtl.WriteLineAsync("Ni 1.000000", cancellationToken).ConfigureAwait(false);
        await swMtl.WriteLineAsync("d 1.000000", cancellationToken).ConfigureAwait(false);
        await swMtl.WriteLineAsync("illum 1", cancellationToken).ConfigureAwait(false);
        await swMtl.WriteLineAsync($"map_Kd {textureFileName}", cancellationToken).ConfigureAwait(false);
        await swMtl.WriteLineAsync("", cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteFacesAsync(TextChunkWriter sw, IList<uint> indices, string prefix, int indexOffsetVertices, int indexOffsetTexCoords, int indexOffsetNormals, bool hasTexture, CancellationToken cancellationToken)
    {
        for (var i = 0; i < indices.Count; i += 3)
        {
            await sw.ReserveAsync(cancellationToken).ConfigureAwait(false);
            if (hasTexture)
            {
                sw.AppendLine($"{prefix} {indices[i] + indexOffsetVertices}/{indices[i] + indexOffsetTexCoords}/{indices[i] + indexOffsetNormals} {indices[i + 1] + indexOffsetVertices}/{indices[i + 1] + indexOffsetTexCoords}/{indices[i + 1] + indexOffsetNormals} {indices[i + 2] + indexOffsetVertices}/{indices[i + 2] + indexOffsetTexCoords}/{indices[i + 2] + indexOffsetNormals}");
            }
            else
            {
                sw.AppendLine($"{prefix} {indices[i] + indexOffsetVertices}//{indices[i] + indexOffsetNormals} {indices[i + 1] + indexOffsetVertices}//{indices[i + 1] + indexOffsetNormals} {indices[i + 2] + indexOffsetVertices}//{indices[i + 2] + indexOffsetNormals}");
            }
        }
        await sw.WriteLineAsync("", cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteTexCoordsAsync(TextChunkWriter sw, SharpGLTF.Memory.Vector2Array texCoords, string prefix, CancellationToken cancellationToken)
    {
        foreach (var v in texCoords)
        {
            await sw.ReserveAsync(cancellationToken).ConfigureAwait(false);
            sw.AppendLine($"{prefix} {v.X} {1f - v.Y}");
        }
        await sw.WriteLineAsync("", cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteVectorsAsync(TextChunkWriter sw, SharpGLTF.Memory.Vector3Array vertices, string prefix, CancellationToken cancellationToken)
    {
        foreach (var v in vertices)
        {
            await sw.ReserveAsync(cancellationToken).ConfigureAwait(false);
            sw.AppendLine($"{prefix} {v.X} {v.Y} {v.Z}");
        }
        await sw.WriteLineAsync("", cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteHeaderAsync(TextChunkWriter sw, string materialLibFile, CancellationToken cancellationToken)
    {
        await sw.WriteLineAsync("# DEM.Net Elevation API", cancellationToken).ConfigureAwait(false);
        await sw.WriteLineAsync("# https://elevationapi.com", cancellationToken).ConfigureAwait(false);
        await sw.WriteLineAsync("", cancellationToken).ConfigureAwait(false);
        await sw.WriteLineAsync($"mtllib {materialLibFile}", cancellationToken).ConfigureAwait(false);
        await sw.WriteLineAsync("", cancellationToken).ConfigureAwait(false);
    }
}
