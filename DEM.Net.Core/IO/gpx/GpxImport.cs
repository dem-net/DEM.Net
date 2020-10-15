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
        public static Func<GpxTrackPoint, GeoPoint> defaultConversionFunc = ToGeoPoint;
        /// <summary>
        /// Read gpx file segments as enumerable GeoPoints
        /// </summary>
        /// <param name="fileName">Path to GPX file</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> ReadGPX_Segments<T>(string fileName
            , Func<GpxTrackPoint, T> conversionFunc
            , Func<GpxRoutePoint, T> routeConversionFunc)
        {
            IEnumerable<IEnumerable<T>> segments = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                segments = ReadGPX_Segments(fs, conversionFunc, routeConversionFunc);
            }

            return segments;
        }
        public static IEnumerable<IEnumerable<GeoPoint>> ReadGPX_Segments(string fileName)
        {
            return ReadGPX_Segments(fileName, ToGeoPoint, ToGeoPoint);
        }
        /// <summary>
        /// Read gpx file segments as enumerable GeoPoints
        /// </summary>
        /// <param name="gpxFileStream">GPX file stream</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> ReadGPX_Segments<T>(Stream gpxFileStream
            , Func<GpxTrackPoint, T> conversionFunc
            , Func<GpxRoutePoint, T> routeConversionFunc)
        {
            IEnumerable<IEnumerable<T>> segments = Enumerable.Empty<IEnumerable<T>>();
            using (GpxReader reader = new GpxReader(gpxFileStream))
            {
                while (reader.Read())
                {
                    switch (reader.ObjectType)
                    {
                        case GpxObjectType.Track:
                            GpxTrack track = reader.Track;
                            segments = segments.Concat(ConvertTrack(track, conversionFunc));
                            break;
                        case GpxObjectType.Route:
                            GpxRoute route = reader.Route;
                            segments = segments.Concat(ConvertRoute(route, routeConversionFunc));
                            break;
                    }
                }
            }
            return segments;
        }
        public static IEnumerable<IEnumerable<GeoPoint>> ReadGPX_Segments(Stream gpxFileStream)
        {
            return ReadGPX_Segments(gpxFileStream, ToGeoPoint, ToGeoPoint);
        }



        private static IEnumerable<IEnumerable<T>> ConvertTrack<T>(GpxTrack track, Func<GpxTrackPoint, T> conversionFunc)
        {
            IEnumerable<IEnumerable<T>> segments = null;

            if (track == null || track.Segments == null)
                throw new ArgumentNullException("track", "Track is empty.");

            try
            {
                segments = track.Segments.Select(seg => seg.TrackPoints.Select(pt => conversionFunc(pt)));
            }
            catch (Exception)
            {

                throw;
            }
            return segments;
        }
        private static IEnumerable<IEnumerable<T>> ConvertRoute<T>(GpxRoute route, Func<GpxRoutePoint, T> conversionFunc)
        {
            IEnumerable<IEnumerable<T>> segments = null;

            if (route == null || route.RoutePoints == null)
                throw new ArgumentNullException("route", "Route is empty.");

            try
            {
                segments = Enumerable.Range(0,1).Select(_=> route.RoutePoints.Select(pt => conversionFunc(pt)));
            }
            catch (Exception)
            {

                throw;
            }
            return segments;
        }

        public static GeoPoint ToGeoPoint(this GpxTrackPoint pt)
        {
            return new GeoPoint(pt.Latitude, pt.Longitude, pt.Elevation);
        }
        public static GeoPoint ToGeoPoint(this GpxRoutePoint pt)
        {
            return new GeoPoint(pt.Latitude, pt.Longitude, pt.Elevation);
        }
        public static IEnumerable<GeoPoint> ToGeoPoints(this IEnumerable<GpxTrackPoint> points)
        {
            return points.Select(ToGeoPoint);
        }


    }
}
