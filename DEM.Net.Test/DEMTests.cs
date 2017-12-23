using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Types;
using DEM.Net.Lib.Services;
using DEM.Net.Lib;
using System.Windows.Media.Imaging;
using System.IO;

namespace DEM.Net.Test
{
	[TestClass]
	public class DEMTests
	{

		[TestInitialize()]
		public void Initialize()
		{
			SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
		}

		[TestMethod]
        [TestCategory("Geometry")]
		public void LineLineIntersectionTest()
		{
			string wkt1 = "LINESTRING(-5.888671875 47.90161354142077,3.4716796875 44.11914151643737)";
			string wkt2 = "LINESTRING(-2.8564453125 44.30812668488613,5.625 48.166085419012525)";
			SqlGeometry geom1 = GeometryService.ParseWKTAsGeometry(wkt1);
			SqlGeometry geom2 = GeometryService.ParseWKTAsGeometry(wkt2);
			SqlGeometry intersection = geom1.STIntersection(geom2);

			GeoSegment seg1 = new GeoSegment(new GeoPoint(geom1.STStartPoint().STY.Value, geom1.STStartPoint().STX.Value), new GeoPoint(geom1.STEndPoint().STY.Value, geom1.STEndPoint().STX.Value));
			GeoSegment seg2 = new GeoSegment(new GeoPoint(geom2.STStartPoint().STY.Value, geom2.STStartPoint().STX.Value), new GeoPoint(geom2.STEndPoint().STY.Value, geom2.STEndPoint().STX.Value));
			GeoPoint intersectionResult = GeoPoint.Zero;

			bool intersects = GeometryService.LineLineIntersection(out intersectionResult, seg1, seg2);


            SqlGeography geog1 = null;
            intersection.TryToGeography(out geog1);
            SqlGeography geog2 = SqlGeography.Point(intersectionResult.Latitude, intersectionResult.Longitude, 4326);
			double dist = geog1.STDistance(geog2).Value;

			Assert.IsTrue(dist < 0.05d, "Problem in intersection calculation.");
		}

        [TestMethod]
        [TestCategory("Raster")]
        public void GeoTiffTags()
        {
            string geoTiffFileName = @"C:\Users\ext.dev.xfi\AppData\Roaming\DEM.Net\AW3D30\AW3D30_alos\North\North_0_45\N043E004_AVE_DSM.tif";
            using (Stream tiffStream = new FileStream(geoTiffFileName, FileMode.Open))
            {
                var tiffDecoder = new TiffBitmapDecoder(tiffStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                if (tiffDecoder.Frames.Count == 1)
                {
                    var tiffFrameCopy = tiffDecoder.Frames[0];
                    var pixels = new Int16[tiffFrameCopy.PixelWidth * tiffFrameCopy.PixelHeight];
                   // tiffFrameCopy.CopyPixels(pixels, tiffFrameCopy.PixelWidth * 2, 0);
                   // cache.Add(filename, pixels);
                }
            }
        }
	}
}
