using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace DEM.Net.Core.Imagery
{
    /// <summary>
    /// TEMPORARY mitigation while DEM.Net is pinned to ImageSharp 3.1.x (no Six Labors license key yet).
    /// ImageSharp 3.1.11 has known advisories fixed in 3.2.0:
    ///  - GHSA-v76p-62qx-wwq2, GHSA-wmxv-xphr-5c9g (TIFF decoder), GHSA-j9gm-c75j-xc9q, GHSA-jjfr-hcj7-qf5w (TIFF encoder)
    ///  - GHSA-j3p4-wp97-rph4 (HistogramEqualization, not used by DEM.Net)
    ///  - GHSA-gwg2-r3hj-4w44 (ICC profile parsing)
    /// Decoding is restricted to PNG/JPEG/WebP (no TIFF decoder registered) and metadata (ICC/EXIF/XMP) is skipped.
    /// Every ImageSharp decode in DEM.Net must use <see cref="Options"/>.
    /// Remove this and go back to ImageSharp >= 3.2 once a license key is available.
    /// </summary>
    internal static class SafeImageDecoding
    {
        public static readonly SixLabors.ImageSharp.Configuration Configuration = new SixLabors.ImageSharp.Configuration(
            new PngConfigurationModule(),
            new JpegConfigurationModule(),
            new WebpConfigurationModule());

        public static readonly DecoderOptions Options = new DecoderOptions
        {
            Configuration = Configuration,
            SkipMetadata = true,
        };
    }
}
