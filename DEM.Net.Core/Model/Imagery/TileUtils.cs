// TileUtils.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Imagery
{
    /// <summary>
    /// Source : https://docs.microsoft.com/en-us/bingmaps/articles/bing-maps-tile-system
    /// </summary>
    public static class TileUtils
    {
        public const double EARTH_RADIUS = 6378137;
        public const double EARTH_CIRCUMFERENCE = 40075016.69;
        public const double MIN_LATITUDE = -85.05112878;
        private const double MAX_LATITUDE = 85.05112878;
        private const double MIN_LONGITUDE = -180;
        private const double MAX_LONGITUDE = 180;


        public static int UsefulDigits(double resolution)
        {
            return 1 + (int)Math.Floor(Math.Abs(Math.Log10(resolution * 360d / EARTH_CIRCUMFERENCE)));
        }

        /// <summary>
        /// Clips a number to the specified minimum and maximum values.
        /// </summary>
        /// <param name="n">The number to clip.</param>
        /// <param name="minValue">Minimum allowable value.</param>
        /// <param name="maxValue">Maximum allowable value.</param>
        /// <returns>The clipped value.</returns>
        private static double Clip(double n, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(n, minValue), maxValue);
        }



        /// <summary>
        /// Determines the map width and height (in pixels) at a specified level
        /// of detail.
        /// </summary>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <returns>The map width and height in pixels.</returns>
        public static uint MapSize(int levelOfDetail)
        {
            return (uint)256 << levelOfDetail;
        }



        /// <summary>
        /// Determines the ground resolution (in meters per pixel) at a specified
        /// latitude and level of detail.
        /// </summary>
        /// <param name="latitude">Latitude (in degrees) at which to measure the
        /// ground resolution.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <returns>The ground resolution, in meters per pixel.</returns>
        public static double GroundResolution(double latitude, int levelOfDetail)
        {
            latitude = Clip(latitude, MIN_LATITUDE, MAX_LATITUDE);
            return Math.Cos(latitude * Math.PI / 180) * 2 * Math.PI * EARTH_RADIUS / MapSize(levelOfDetail);
        }



        /// <summary>
        /// Determines the map scale at a specified latitude, level of detail,
        /// and screen resolution.
        /// </summary>
        /// <param name="latitude">Latitude (in degrees) at which to measure the
        /// map scale.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <param name="screenDpi">Resolution of the screen, in dots per inch.</param>
        /// <returns>The map scale, expressed as the denominator N of the ratio 1 : N.</returns>
        public static double MapScale(double latitude, int levelOfDetail, int screenDpi)
        {
            return GroundResolution(latitude, levelOfDetail) * screenDpi / 0.0254;
        }



        /// <summary>
        /// Converts a point from latitude/longitude WGS-84 coordinates (in degrees)
        /// into pixel XY coordinates at a specified level of detail.
        /// </summary>
        /// <param name="latitude">Latitude of the point, in degrees.</param>
        /// <param name="longitude">Longitude of the point, in degrees.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <param name="pixelX">Output parameter receiving the X coordinate in pixels.</param>
        /// <param name="pixelY">Output parameter receiving the Y coordinate in pixels.</param>
        public static PointInt LatLongToPixelXY(double latitude, double longitude, int levelOfDetail)
        {
            latitude = Clip(latitude, MIN_LATITUDE, MAX_LATITUDE);
            longitude = Clip(longitude, MIN_LONGITUDE, MAX_LONGITUDE);

            double x = (longitude + 180) / 360;
            double sinLatitude = Math.Sin(latitude * Math.PI / 180);
            double y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

            uint mapSize = MapSize(levelOfDetail);
            return new PointInt((int)Clip(x * mapSize + 0.5, 0, mapSize - 1),
                                (int)Clip(y * mapSize + 0.5, 0, mapSize - 1));
        }

        /// <summary>
        /// Converts a pixel from pixel XY coordinates at a specified level of detail
        /// into latitude/longitude WGS-84 coordinates (in degrees).
        /// </summary>
        /// <param name="pixelX">X coordinate of the point, in pixels.</param>
        /// <param name="pixelY">Y coordinates of the point, in pixels.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <param name="latitude">Output parameter receiving the latitude in degrees.</param>
        /// <param name="longitude">Output parameter receiving the longitude in degrees.</param>
        public static LatLong PixelXYToLatLong(int pixelX, int pixelY, int levelOfDetail)
        {
            double mapSize = MapSize(levelOfDetail);
            double x = (Clip(pixelX, 0, mapSize - 1) / mapSize) - 0.5;
            double y = 0.5 - (Clip(pixelY, 0, mapSize - 1) / mapSize);


            double latitude = 90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI;
            double longitude = 360 * x;
            return new LatLong(latitude, longitude);
        }



        /// <summary>
        /// Converts pixel XY coordinates into tile XY coordinates of the tile containing
        /// the specified pixel.
        /// </summary>
        /// <param name="pixelX">Pixel X coordinate.</param>
        /// <param name="pixelY">Pixel Y coordinate.</param>
        /// <returns>PointInt receiving the tile X and Y coordinates.</param>
        public static PointInt PixelXYToTileXY(int pixelX, int pixelY)
        {
            int tileX = pixelX / 256;
            int tileY = pixelY / 256;
            return new PointInt(tileX, tileY);
        }



        /// <summary>
        /// Converts tile XY coordinates into pixel XY coordinates of the upper-left pixel
        /// of the specified tile.
        /// </summary>
        /// <param name="tileX">Tile X coordinate.</param>
        /// <param name="tileY">Tile Y coordinate.</param>
        /// <returns>PointInt receiving the pixel X and Y coordinates.</param>
        public static PointInt TileXYToPixelXY(int tileX, int tileY)
        {
            int pixelX = tileX * 256;
            int pixelY = tileY * 256;
            return new PointInt(pixelX, pixelY);
        }



        /// <summary>
        /// Converts tile XY coordinates into a QuadKey at a specified level of detail.
        /// </summary>
        /// <param name="tileX">Tile X coordinate.</param>
        /// <param name="tileY">Tile Y coordinate.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <returns>A string containing the QuadKey.</returns>
        public static string TileXYToQuadKey(int tileX, int tileY, int levelOfDetail)
        {
            StringBuilder quadKey = new StringBuilder();
            for (int i = levelOfDetail; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((tileX & mask) != 0)
                {
                    digit++;
                }
                if ((tileY & mask) != 0)
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }



        /// <summary>
        /// Converts a QuadKey into tile XY coordinates.
        /// </summary>
        /// <param name="quadKey">QuadKey of the tile.</param>
        /// <param name="tileX">Output parameter receiving the tile X coordinate.</param>
        /// <param name="tileY">Output parameter receiving the tile Y coordinate.</param>
        /// <param name="levelOfDetail">Output parameter receiving the level of detail.</param>
        public static void QuadKeyToTileXY(string quadKey, out int tileX, out int tileY, out int levelOfDetail)
        {
            tileX = tileY = 0;
            levelOfDetail = quadKey.Length;
            for (int i = levelOfDetail; i > 0; i--)
            {
                int mask = 1 << (i - 1);
                switch (quadKey[levelOfDetail - i])
                {
                    case '0':
                        break;

                    case '1':
                        tileX |= mask;
                        break;

                    case '2':
                        tileY |= mask;
                        break;

                    case '3':
                        tileX |= mask;
                        tileY |= mask;
                        break;

                    default:
                        throw new ArgumentException("Invalid QuadKey digit sequence.");
                }
            }
        }
    }
}
