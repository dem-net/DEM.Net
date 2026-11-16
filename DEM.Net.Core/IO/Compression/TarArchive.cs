using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;

namespace DEM.Net.Core;

public class TarArchive
{
    /// <summary>
    /// Extracts a <i>.tar.gz</i> archive to the specified directory.
    /// </summary>
    /// <param name="filename">The <i>.tar.gz</i> to decompress and extract.</param>
    /// <param name="outputDir">Output directory to write the files.</param>
    public static void ExtractTarGz(string filename, string outputDir)
    {
        using (var stream = File.OpenRead(filename))
            ExtractTarGz(stream, outputDir);
    }

    /// <summary>
    /// Extracts a <i>.tar.gz</i> archive stream to the specified directory.
    /// </summary>
    /// <param name="stream">The <i>.tar.gz</i> to decompress and extract.</param>
    /// <param name="outputDir">Output directory to write the files.</param>
    public static void ExtractTarGz(Stream stream, string outputDir)
    {
        // TarReader reads forward only: the archive is streamed, never buffered in memory
        using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
            ExtractTar(gzip, outputDir);
    }

    /// <summary>
    /// Extracts a <c>tar</c> archive to the specified directory.
    /// </summary>
    /// <param name="filename">The <i>.tar</i> to extract.</param>
    /// <param name="outputDir">Output directory to write the files.</param>
    /// <param name="onlyExtensions">When set, only files ending with this extension are extracted.</param>
    public static void ExtractTar(string filename, string outputDir, string onlyExtensions = null)
    {
        using (var stream = File.OpenRead(filename))
            ExtractTar(stream, outputDir, onlyExtensions);
    }

    /// <summary>
    /// Extracts a <c>tar</c> archive to the specified directory.
    /// </summary>
    /// <param name="stream">The <i>.tar</i> to extract.</param>
    /// <param name="outputDir">Output directory to write the files.</param>
    /// <param name="onlyExtensions">When set, only files ending with this extension are extracted.</param>
    public static void ExtractTar(Stream stream, string outputDir, string onlyExtensions = null)
    {
        var outputRoot = Path.GetFullPath(outputDir);
        if (!outputRoot.EndsWith(Path.DirectorySeparatorChar))
            outputRoot += Path.DirectorySeparatorChar;

        using var reader = new TarReader(stream);
        while (reader.GetNextEntry() is TarEntry entry)
        {
            // directories are created with their files; links and other special entries are not extracted
            if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile) || entry.DataStream == null)
                continue;

            var output = Path.GetFullPath(Path.Combine(outputRoot, entry.Name));
            if (!output.StartsWith(outputRoot, StringComparison.Ordinal))
                throw new InvalidDataException($"Tar entry '{entry.Name}' would be extracted outside of the output directory.");

            if (onlyExtensions != null && !output.EndsWith(onlyExtensions, StringComparison.OrdinalIgnoreCase))
                continue;

            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var fs = new FileStream(output, FileMode.Create, FileAccess.Write))
            {
                entry.DataStream.CopyTo(fs);
            }
        }
    }
}
