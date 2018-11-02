using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotSpatial.Projections;
using Microsoft.SqlServer.Types;

namespace DEM.Net.Lib
{
    public static class HeightMapReprojection
    {

        public static HeightMap ReprojectTo(this HeightMap heightMap, int sourceEpsgCode, int destinationEpsgCode)
        {
            HeightMap outHeightMap = heightMap.Clone();

            if (sourceEpsgCode != destinationEpsgCode)
            {
                // Defines the starting coordiante system
                ProjectionInfo pSource = ProjectionInfo.FromEpsgCode(sourceEpsgCode);
                // Defines the starting coordiante system
                ProjectionInfo pTarget = ProjectionInfo.FromEpsgCode(destinationEpsgCode);

                outHeightMap.Coordinates = outHeightMap.Coordinates.Select(pt => ReprojectPoint(pt, pSource, pTarget)).ToList();
    
            }

            return outHeightMap;
        }

       
        private static GeoPoint ReprojectPoint(GeoPoint sourcePoint, ProjectionInfo sourceProj, ProjectionInfo destProj)
        {

            double[] coords = new double[] { sourcePoint.Longitude, sourcePoint.Latitude };
            // Calls the reproject function that will transform the input location to the output locaiton
            Reproject.ReprojectPoints(coords, new double[] { sourcePoint.Elevation.GetValueOrDefault(0) }, sourceProj, destProj, 0, 1);

            return new GeoPoint(coords[1], coords[0], (float)sourcePoint.Elevation, sourcePoint.XIndex.GetValueOrDefault() , sourcePoint.YIndex.GetValueOrDefault());
        }

        

    }


}
