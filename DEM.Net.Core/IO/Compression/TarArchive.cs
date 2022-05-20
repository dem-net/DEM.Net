using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
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
            // A GZipStream is not seekable, so copy it first to a MemoryStream
            using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
            {
                const int chunk = 4096;
                using (var ms = new MemoryStream())
                {
                    int read;
                    var buffer = new byte[chunk];
                    do
                    {
                        read = gzip.Read(buffer, 0, chunk);
                        ms.Write(buffer, 0, read);
                    }
                    while (read == chunk);

                    ms.Seek(0, SeekOrigin.Begin);
                    ExtractTar(ms, outputDir);
                }
            }
        }

        /// <summary>
        /// Extracts a <c>tar</c> archive to the specified directory.
        /// </summary>
        /// <param name="filename">The <i>.tar</i> to extract.</param>
        /// <param name="outputDir">Output directory to write the files.</param>
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
        public static void ExtractTar(Stream stream, string outputDir, string onlyExtensions = null)
        {
            var buffer = new byte[100];
            while (true)
            {
                stream.Read(buffer, 0, 100);

                var name = Encoding.ASCII.GetString(buffer).Trim('\0', ' ');
                if (string.IsNullOrWhiteSpace(name))
                    break;

                stream.Seek(24, SeekOrigin.Current);
                stream.Read(buffer, 0, 12);

                long size;

                string hex = Encoding.ASCII.GetString(buffer, 0, 12).Trim('\0', ' ');
                try
                {
                    size = Convert.ToInt64(hex, 8);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not parse hex: " + hex, ex);
                }

                stream.Seek(376L, SeekOrigin.Current);

                var output = Path.Combine(outputDir, name);

                if (size > 0) // ignores directory entries
                {
                    if (onlyExtensions != null && !output.ToLower().EndsWith(onlyExtensions.ToLower()))
                    {
                        stream.Seek(size, SeekOrigin.Current);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(output));

                        using (var fs = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            var blob = new byte[size];
                            stream.Read(blob, 0, blob.Length);
                            fs.Write(blob, 0, blob.Length);
                        }
                    }
                }

                var pos = stream.Position;

                var offset = 512 - (pos  % 512);
                if (offset == 512)
                    offset = 0;

                stream.Seek(offset, SeekOrigin.Current);
            }
        }
    }
}
