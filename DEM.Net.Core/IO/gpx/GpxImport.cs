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
        public static IEnumerable<IEnumerable<T>> ReadGPX_Segments<T>(string fileName, Func<GpxTrackPoint, T> conversionFunc)
        {
            IEnumerable<IEnumerable<T>> segments = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                segments = ReadGPX_Segments(fs, conversionFunc);
            }

            return segments;
        }
        public static IEnumerable<IEnumerable<GeoPoint>> ReadGPX_Segments(string fileName)
        {
            return ReadGPX_Segments(fileName, ToGeoPoint);
        }
        /// <summary>
        /// Read gpx file segments as enumerable GeoPoints
        /// </summary>
        /// <param name="gpxFileStream">GPX file stream</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> ReadGPX_Segments<T>(Stream gpxFileStream, Func<GpxTrackPoint, T> conversionFunc)
        {
            IEnumerable<IEnumerable<T>> segments = null;
            using (GpxReader reader = new GpxReader(gpxFileStream))
            {
                while (reader.Read())
                {
                    switch (reader.ObjectType)
                    {
                        case GpxObjectType.Track:
                            GpxTrack track = reader.Track;
                            segments = ConvertTrack(track, conversionFunc);
                            break;
                    }
                }
            }
            return segments;
        }
        public static IEnumerable<IEnumerable<GeoPoint>> ReadGPX_Segments(Stream gpxFileStream)
        {
            return ReadGPX_Segments(gpxFileStream, ToGeoPoint);
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

        public static GeoPoint ToGeoPoint(this GpxTrackPoint pt)
        {
            return new GeoPoint(pt.Latitude, pt.Longitude, pt.Elevation);
        }
        public static IEnumerable<GeoPoint> ToGeoPoints(this IEnumerable<GpxTrackPoint> points)
        {
            return points.Select(ToGeoPoint);
        }


    }
}
