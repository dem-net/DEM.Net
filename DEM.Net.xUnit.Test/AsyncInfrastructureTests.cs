using System;
using System.Formats.Tar;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BitMiracle.LibTiff.Classic;
using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test;

/// <summary>Offline tests of the v11 async infrastructure and of the bugs fixed with it.</summary>
public class AsyncInfrastructureTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "DEM.Net.Tests", Guid.NewGuid().ToString("N"));

    public AsyncInfrastructureTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }


    [Fact]
    public async Task SingleFlightRunsConcurrentCallsOnce()
    {
        var flight = new SingleFlight(keepCompleted: false);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int runs = 0;
        Func<Task> operation = async () => { Interlocked.Increment(ref runs); await release.Task; };

        var calls = Enumerable.Range(0, 10).Select(_ => flight.RunAsync("key", operation)).ToList();
        release.SetResult();
        await Task.WhenAll(calls);
        Assert.Equal(1, runs);

        // released once completed: a later call runs again
        await flight.RunAsync("key", operation);
        Assert.Equal(2, runs);
    }

    [Fact]
    public async Task SingleFlightKeepsCompletedOperations()
    {
        var flight = new SingleFlight(keepCompleted: true);
        int runs = 0;
        await flight.RunAsync("setup", () => { runs++; return Task.CompletedTask; });
        await flight.RunAsync("setup", () => { runs++; return Task.CompletedTask; });
        Assert.Equal(1, runs);
        Assert.True(flight.IsCompleted("setup"));
    }

    [Fact]
    public async Task SingleFlightForgetsFailures()
    {
        var flight = new SingleFlight(keepCompleted: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => flight.RunAsync("setup", () => throw new InvalidOperationException("network down")));
        Assert.False(flight.IsCompleted("setup"));

        int runs = 0;
        await flight.RunAsync("setup", () => { runs++; return Task.CompletedTask; });
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task SingleFlightCallerCancellationDoesNotCancelOthers()
    {
        var flight = new SingleFlight(keepCompleted: false);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool completed = false;
        Func<Task> operation = async () => { await release.Task; completed = true; };

        using var cts = new CancellationTokenSource();
        var cancelled = flight.RunAsync("key", operation, cts.Token);
        var other = flight.RunAsync("key", operation);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        release.SetResult();
        await other;
        Assert.True(completed);
    }

    [Fact]
    public async Task InterruptedDownloadLeavesNoFile()
    {
        string target = Path.Combine(_dir, "tiles", "tile.tif");
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new FailingStream(bytesBeforeFailure: 100_000))
        }));

        await Assert.ThrowsAsync<IOException>(() => HttpDownload.DownloadToFileAsync(client, "http://test/tile.tif", target, CancellationToken.None));

        Assert.False(File.Exists(target));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(target)));
    }

    [Fact]
    public async Task DownloadWritesTheWholeFile()
    {
        string target = Path.Combine(_dir, "tile.bin");
        var content = Enumerable.Range(0, 300_000).Select(i => (byte)i).ToArray();
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) }));

        Assert.True(await HttpDownload.DownloadToFileAsync(client, "http://test/tile.bin", target, CancellationToken.None));

        Assert.Equal(content, File.ReadAllBytes(target));
        Assert.Single(Directory.GetFiles(_dir));
    }

    [Fact]
    public async Task MissingRemoteFileIsReported()
    {
        string target = Path.Combine(_dir, "missing.bin");
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        Assert.False(await HttpDownload.DownloadToFileAsync(client, "http://test/missing.bin", target, CancellationToken.None, notFoundIsMissing: true));
        Assert.False(File.Exists(target));
        await Assert.ThrowsAsync<HttpRequestException>(() => HttpDownload.DownloadToFileAsync(client, "http://test/missing.bin", target, CancellationToken.None));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }

    /// <summary>A response body cut by the network after some bytes.</summary>
    private sealed class FailingStream(int bytesBeforeFailure) : Stream
    {
        private int _position;
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= bytesBeforeFailure)
                throw new IOException("Connection reset");
            int n = Math.Min(count, bytesBeforeFailure - _position);
            _position += n;
            return n;
        }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _position; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public void TarGzArchiveIsFullyExtracted()
    {
        // larger than the 4 KB chunks the former implementation stopped at, after a short gzip read
        var big = Enumerable.Range(0, 1_000_000).Select(i => (byte)(i * 31)).ToArray();
        string archive = CreateTarGz(("tiles/big.tif", big), ("readme.txt", Encoding.UTF8.GetBytes("hello")));

        string output = Path.Combine(_dir, "out");
        TarArchive.ExtractTarGz(archive, output);

        Assert.Equal(big, File.ReadAllBytes(Path.Combine(output, "tiles", "big.tif")));
        Assert.Equal("hello", File.ReadAllText(Path.Combine(output, "readme.txt")));
    }

    [Fact]
    public void TarExtractionCanFilterExtensions()
    {
        string tar = Path.ChangeExtension(CreateTarGz(("a.TIF", new byte[] { 1 }), ("b.txt", new byte[] { 2 })), ".tar");
        string output = Path.Combine(_dir, "filtered");
        TarArchive.ExtractTar(tar, output, onlyExtensions: ".tif");

        Assert.True(File.Exists(Path.Combine(output, "a.TIF")));
        Assert.False(File.Exists(Path.Combine(output, "b.txt")));
    }

    [Fact]
    public void TarEntriesCannotEscapeTheOutputDirectory()
    {
        string archive = CreateTarGz(("../evil.txt", new byte[] { 1 }));
        Assert.Throws<InvalidDataException>(() => TarArchive.ExtractTarGz(archive, Path.Combine(_dir, "out")));
        Assert.False(File.Exists(Path.Combine(_dir, "evil.txt")));
    }

    /// <summary>Writes a .tar.gz, and the same entries as a plain .tar next to it.</summary>
    private string CreateTarGz(params (string name, byte[] content)[] entries)
    {
        string tarGz = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".tar.gz");
        using (var tarStream = new MemoryStream())
        {
            using (var writer = new TarWriter(tarStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                foreach (var (name, content) in entries)
                    writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(content) });
            }
            File.WriteAllBytes(Path.ChangeExtension(tarGz, ".tar"), tarStream.ToArray());
            using var fs = File.Create(tarGz);
            using var gzip = new GZipStream(fs, CompressionLevel.Fastest);
            tarStream.Position = 0;
            tarStream.CopyTo(gzip);
        }
        return tarGz;
    }

    [Fact]
    public void NoDataValueIsCultureInvariant()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal(-9999.5f, new FileMetadata("a.tif", null) { NoDataValue = "-9999.5" }.NoDataValueFloat);
            // manifests written by former versions on a French machine
            Assert.Equal(-9999.5f, new FileMetadata("a.tif", null) { NoDataValue = "-9999,5" }.NoDataValueFloat);
            Assert.Equal(-32768f, new FileMetadata("a.tif", null) { NoDataValue = "-32768" }.NoDataValueFloat);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void TiledGeoTiffWithNonSquareTilesIsReadAtTheRightPixels()
    {
        const int width = 64, height = 48, tileWidth = 32, tileHeight = 16;
        static short Value(int x, int y) => (short)(x + y * 100);

        string path = Path.Combine(_dir, "tiled.tif");
        using (var tiff = Tiff.Open(path, "w"))
        {
            tiff.SetField(TiffTag.IMAGEWIDTH, width);
            tiff.SetField(TiffTag.IMAGELENGTH, height);
            tiff.SetField(TiffTag.SAMPLESPERPIXEL, 1);
            tiff.SetField(TiffTag.BITSPERSAMPLE, 16);
            tiff.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.INT);
            tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
            tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
            tiff.SetField(TiffTag.TILEWIDTH, tileWidth);
            tiff.SetField(TiffTag.TILELENGTH, tileHeight);

            var tile = new byte[tileWidth * tileHeight * 2];
            for (int ty = 0; ty < height; ty += tileHeight)
                for (int tx = 0; tx < width; tx += tileWidth)
                {
                    for (int j = 0; j < tileHeight; j++)
                        for (int i = 0; i < tileWidth; i++)
                            BitConverter.TryWriteBytes(tile.AsSpan((j * tileWidth + i) * 2), Value(tx + i, ty + j));
                    tiff.WriteTile(tile, tx, ty, 0, 0);
                }
        }

        var metadata = new FileMetadata(path, null) { Width = width, Height = height, BitsPerSample = 16, SampleFormat = RasterSampleFormat.INTEGER, NoDataValue = "-32768" };
        using var geoTiff = new GeoTiff(path);
        foreach (var (x, y) in new[] { (0, 0), (5, 3), (31, 15), (32, 15), (40, 17), (63, 47), (10, 40) })
            Assert.Equal(Value(x, y), geoTiff.GetElevationAtPoint(metadata, x, y));
    }

}
