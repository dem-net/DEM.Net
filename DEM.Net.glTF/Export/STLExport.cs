//
// STLExport.cs
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
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;

namespace DEM.Net.glTF.Export;

public class STLExportService(ILogger<STLExportService>? logger = null) : ISTLExportService
{
    private const int BinaryHeaderSize = 84; // 80 bytes (attributions) + triangle count
    private const int BinaryTriangleSize = 50; // normal and 3 vertices (12 floats) + attribute byte count

    public async Task STLExportAsync(MeshPrimitive mesh, string fileName, bool ascii = true, IEnumerable<Attribution>? attributions = null, CancellationToken cancellationToken = default)
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(fileName));
        if (folder != null)
            Directory.CreateDirectory(folder);

        string tempFile = $"{fileName}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1 << 16, useAsync: true))
            {
                await WriteSTLAsync(mesh, fs, ascii, attributions, cancellationToken).ConfigureAwait(false);
            }
            File.Move(tempFile, fileName, overwrite: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger?.LogError(ex, "Error while exporting STL model");
            throw;
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public Task WriteSTLAsync(MeshPrimitive mesh, Stream stream, bool ascii = true, IEnumerable<Attribution>? attributions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(stream);

        var positions = mesh.GetVertexAccessor("POSITION").AsVector3Array();
        var triangles = mesh.GetTriangleIndices();
        return ascii
            ? WriteAsciiAsync(positions, triangles, GetAttributionExtraText(attributions), stream, cancellationToken)
            : WriteBinaryAsync(positions, triangles, GetBinaryHeader(attributions), stream, cancellationToken);
    }

    /// <summary>
    /// The 80 bytes header of binary STL: the attributions in ASCII, truncated, data first (dataset, imagery),
    /// software credits last. It mustn't start with "solid" (readers would take the file for ASCII STL).
    /// </summary>
    internal static byte[] GetBinaryHeader(IEnumerable<Attribution>? attributions)
    {
        var texts = (attributions ?? []).Where(a => a.Subject != "Generator" && !string.IsNullOrWhiteSpace(a.Text))
            .OrderBy(a => a.Subject == "Mesh reduction") // stable: keeps the order otherwise
            .Select(a => a.Text.Trim());
        string text = string.Join("; ", texts.Prepend("elevationapi.com"));
        var header = new byte[80];
        for (int i = 0; i < Math.Min(text.Length, header.Length); i++)
            header[i] = text[i] is >= ' ' and <= '~' ? (byte)text[i] : (byte)'?';
        return header;
    }

    private static async Task WriteAsciiAsync(IList<Vector3> positions, IEnumerable<(int A, int B, int C)> triangles, string solidName, Stream stream, CancellationToken cancellationToken)
    {
        await using var writer = new TextChunkWriter(stream);
        await writer.WriteLineAsync($"solid {solidName}", cancellationToken).ConfigureAwait(false);
        foreach (var (a, b, c) in triangles)
        {
            Vector3 v1 = positions[a], v2 = positions[b], v3 = positions[c];
            Vector3 n = Normal(v1, v2, v3);

            await writer.ReserveAsync(cancellationToken).ConfigureAwait(false);
            writer.AppendLine($"  facet normal {n.X:e6} {n.Y:e6} {n.Z:e6}");
            writer.AppendLine($"    outer loop");
            writer.AppendLine($"      vertex {v1.X:e6} {v1.Y:e6} {v1.Z:e6}");
            writer.AppendLine($"      vertex {v2.X:e6} {v2.Y:e6} {v2.Z:e6}");
            writer.AppendLine($"      vertex {v3.X:e6} {v3.Y:e6} {v3.Z:e6}");
            writer.AppendLine($"    endloop");
            writer.AppendLine($"  endfacet");
        }
        await writer.WriteLineAsync($"endsolid {solidName}", cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteBinaryAsync(IList<Vector3> positions, IEnumerable<(int A, int B, int C)> triangles, byte[] header, Stream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1 << 16);
        try
        {
            header.AsSpan(0, 80).CopyTo(buffer);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(80), (uint)triangles.Count());
            int length = BinaryHeaderSize;

            foreach (var (a, b, c) in triangles)
            {
                if (length + BinaryTriangleSize > buffer.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await stream.WriteAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                    length = 0;
                }
                WriteTriangle(buffer.AsSpan(length, BinaryTriangleSize), positions[a], positions[b], positions[c]);
                length += BinaryTriangleSize;
            }
            await stream.WriteAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void WriteTriangle(Span<byte> destination, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Write(destination, Normal(v1, v2, v3));
        Write(destination[12..], v1);
        Write(destination[24..], v2);
        Write(destination[36..], v3);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[48..], 0);

        static void Write(Span<byte> destination, Vector3 v)
        {
            BinaryPrimitives.WriteSingleLittleEndian(destination, v.X);
            BinaryPrimitives.WriteSingleLittleEndian(destination[4..], v.Y);
            BinaryPrimitives.WriteSingleLittleEndian(destination[8..], v.Z);
        }
    }

    private static Vector3 Normal(Vector3 v1, Vector3 v2, Vector3 v3) => Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));

    private static string GetAttributionExtraText(IEnumerable<Attribution>? attributions)
    {
        string text = "Generated by elevationapi.com";
        if (attributions != null && attributions.Any())
        {
            var attributionsText = ", " + string.Join(", ", attributions.Select(a => $"{a.Subject}: {a.Text}"));
            text = string.Concat(text, attributionsText);
        }
        return text;
    }
}
