// ImageryProviders.cs
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


namespace DEM.Net.Core.Imagery;

public partial class ImageryProvider
{
    public string Name { get; set; } = null!;
    public UrlModel UrlModel { get; set; } = null!;
    public Attribution Attribution { get; set; } = null!;
    public string? TokenUserSecretsKey { get; set; }
    public int TileSize { get; set; } = 256;
    /// <summary>Tiles downloaded at once by one request.</summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;
    /// <summary>
    /// Tiles downloaded at once by the whole process, from this provider's host (all requests together).
    /// Providers on the same host share one limit, set by the first one used.
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 8;
    public int MaxZoom { get; set; } = 19;
    public bool PrivateUseOnly { get; set; }
    /// <summary>
    /// Provider used for a tile this one doesn't have (HTTP 404, or 400 for a tile outside the server's grid),
    /// e.g. outside a national coverage.
    /// Same tile grid (z/x/y), any tile size: tiles are resized when the texture is built.
    /// A predefined fallback must be declared above the provider using it (static fields are initialized in order).
    /// </summary>
    public ImageryProvider? FallbackProvider { get; set; }

    public override string ToString()
    {
        return Name;
    }        

}
