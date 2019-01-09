using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public class ImageryService
    {
        private int _serverCycle = 0;

        public List<MapTile> DownloadTiles(BoundingBox bbox, ImageryProvider provider)
        {
            List<MapTile> tiles = new List<MapTile>();
            int zoom = 0;
            BoundingBox mapBbox = null;
            PointInt topLeft = new PointInt();
            PointInt bottomRight = new PointInt();
            do
            {
                zoom++;
                topLeft = TileUtils.LatLongToPixelXY(bbox.yMax, bbox.xMin, zoom);
                bottomRight = TileUtils.LatLongToPixelXY(bbox.yMin, bbox.xMax, zoom);
                mapBbox = new BoundingBox(topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y);
            }
            while (zoom < provider.MaxZoom && (mapBbox.Width < provider.TileSize || mapBbox.Height < provider.TileSize));

            PointInt tileStart = TileUtils.PixelXYToTileXY(topLeft.X, topLeft.Y);
            PointInt tileEnd = TileUtils.PixelXYToTileXY(bottomRight.X, bottomRight.Y);

            using (HttpClient _httpClient = new HttpClient())
            {
                for (int x = tileStart.X; x <= tileEnd.X; x++)
                    for (int y = tileStart.Y; y <= tileEnd.Y; y++)
                    {
                        Uri tileUri = BuildUri(provider, x, y, zoom);
                        var imgBytes = _httpClient.GetByteArrayAsync(tileUri).ConfigureAwait(false).GetAwaiter().GetResult();

                        MapTile tile = new MapTile() { Image = imgBytes, PixelSize = provider.TileSize, Uri = tileUri, TileInfo = new MapTileInfo(x, y, zoom) };
                        tiles.Add(tile);
                    }
            }

            return tiles;
        }

        private Uri BuildUri(ImageryProvider provider, int x, int y, int zoom)
        {
            string[] serverNodes = provider.UrlModel.Servers;
            string server = string.Empty;
            if (serverNodes != null && serverNodes.Length > 0)
            {
                _serverCycle = (_serverCycle + 1) % serverNodes.Length;
                server = serverNodes[_serverCycle];
            }

            string url = provider.UrlModel.UrlFormat;
            url = url.Replace("{s}", server);
            url = url.Replace("{x}", x.ToString());
            url = url.Replace("{y}", y.ToString());
            url = url.Replace("{z}", zoom.ToString());

            return new Uri(url, UriKind.Absolute);
        }
    }
}
