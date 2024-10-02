using DEM.Net.Core.Gpx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    /// <summary>
    /// GpxImport is a thin wrapper above gpx by dlg.krakow.pl
    /// allowing conversion from gpx to DEM.Net types (GeoPoints)
    /// </summary>
    public static class GpxImport
    {

        /// <summary>
        /// Read gpx file segments as enumerable GeoPoints
        /// </summary>
        /// <param name="fileName">Path to GPX file</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<GeoPoint>> ReadGPX_Segments(string fileName)
        {
            IEnumerable<IEnumerable<GeoPoint>> segments = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                segments = ReadGPX_Segments(fs);
            }

            return segments;
        }

        /// <summary>
        /// Read gpx file segments as enumerable GeoPoints
        /// </summary>
        /// <param name="gpxFileStream">GPX file stream</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<GeoPoint>> ReadGPX_Segments(Stream gpxFileStream)
        {
            var trackPointConvert = (GpxTrackPoint pt, int index) => new GeoPoint(pt.Latitude, pt.Longitude, pt.Elevation) { Id=index };
            var routePointConvert = (GpxPoint pt, int index) => new GeoPoint(pt.Latitude, pt.Longitude, pt.Elevation) { Id=index };

            IEnumerable<IEnumerable<GeoPoint>> segments = Enumerable.Empty<IEnumerable<GeoPoint>>();

            int i = 0;
            using (GpxReader reader = new GpxReader(gpxFileStream))
            {
                while (reader.Read())
                {
                    switch (reader.ObjectType)
                    {
                        case GpxObjectType.Track:
                            GpxTrack track = reader.Track;
                            var currentSegment = track.Segments.Select(seg => seg.TrackPoints.Select(pt => trackPointConvert(pt, i++)));
                            segments = segments.Concat(currentSegment);
                            break;
                        case GpxObjectType.Route:
                            GpxRoute route = reader.Route;
                            var currentRouteSegment = route.RoutePoints.Select(seg => seg.RoutePoints.Select(pt => routePointConvert(pt, i++)));
                            segments = segments.Concat(currentRouteSegment);
                            break;
                    }
                }
            }
            return segments;
        }
    }
}
