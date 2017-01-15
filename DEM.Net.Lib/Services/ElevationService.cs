using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services
{
    public static class ElevationService
    {
        /// <summary>
        /// Add elevation data to line
        /// </summary>
        /// <param name="geometry">Line string</param>
        /// <param name="strategy">Method to use to get elevation data</param>
        /// <param name="geoTiffService">GeoTiff service</param>
        /// <returns></returns>
        public static SqlGeometry GetElevation(SqlGeometry geometry, IGeoTiffRepositoryService geoTiffService, enElevationStrategy strategy = enElevationStrategy.MaximumDetail)
        {
            if (geometry == null || geometry.IsNull || geometry.STIsEmpty())
                throw new ArgumentNullException("Geometry is null or empty.");
            if (geometry.STIsValid() == false)
                throw new ArgumentNullException("Geometry is invalid: " + geometry.IsValidDetailed());
            if (geometry.STGeometryType().Value != OpenGisGeometryType.LineString.ToString())
                throw new NotSupportedException("Only line strings are supported at this time.");

            /*  Algo explanation
            *   Get GeoTiff catalog for line bbox (C)
            *   For each line segment S from point A to B
            *       Get geotiff metadata for A-B bbox
            *       Get elevation for A and B
            *       Check for all intersections with DEM Grid -> I(x,y)
            *       Get elevation for all I
            *       Construct new line with A, all Is, B
            */
            SqlGeometry ret = null;
            List<FileMetadata> fullLineMetadata = geoTiffService.GetCoveringFiles(geometry.GetBoundingBox());
            foreach (var segment in geometry.Segments())
            {
                // Get geotiff metadata for A-B bbox
                List<FileMetadata> segmentMetadata = geoTiffService.GetCoveringFiles(geometry.GetBoundingBox(), fullLineMetadata);

                // Get elevation for A and B
               // using (GeoTiff tiff = new GeoTiff())
            }
            return ret;
        }
    }
}
