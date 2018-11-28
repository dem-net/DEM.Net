using DEM.Net.Lib;
using DEM.Net.Lib.Gpx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    public static class GpxImport
    {
        public static void ReadGPX(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                using (GpxReader reader = new GpxReader(fs))
                {
                    while (reader.Read())
                    {
                        switch (reader.ObjectType)
                        {
                            case GpxObjectType.Metadata:
                                GpxMetadata metadata = reader.Metadata;
                                Logger.Info("Metadata");
                                break;
                            case GpxObjectType.WayPoint:
                                GpxWayPoint waypoint = reader.WayPoint;
                                Logger.Info("WayPoint");
                                break;
                            case GpxObjectType.Route:
                                GpxRoute route = reader.Route;
                                Logger.Info("Route");
                                break;
                            case GpxObjectType.Track:
                                GpxTrack track = reader.Track;
                                Logger.Info("Track");
                                break;
                        }
                    }
                }
            }
        }

        public static IEnumerable<IEnumerable<GeoPoint>> ReadGPX_Segments(string fileName)
        {
            IEnumerable<IEnumerable<GeoPoint>> segments = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                using (GpxReader reader = new GpxReader(fs))
                {
                    while (reader.Read())
                    {
                        switch (reader.ObjectType)
                        {
                            case GpxObjectType.Track:
                                GpxTrack track = reader.Track;
                                segments = ConvertTrack(track);
                                Logger.Info("Track");
                                break;
                        }
                    }
                }
            }

            return segments;
        }

        private static IEnumerable<IEnumerable<GeoPoint>> ConvertTrack(GpxTrack track)
        {
            IEnumerable<IEnumerable<GeoPoint>> segments = null;

            if (track == null || track.Segments == null)
                throw new ArgumentNullException("track", "Track is empty.");

            try
            {
                segments = track.Segments.Select(seg => seg.TrackPoints.Select(pt => ConvertGpsPoint(pt)));
            }
            catch (Exception)
            {

                throw;
            }
            return segments;
        }

        private static GeoPoint ConvertGpsPoint(GpxTrackPoint pt)
        {
            return new GeoPoint(pt.Latitude, pt.Longitude, (float)pt.Elevation.GetValueOrDefault(0), 0, 0);
        }
    }
}
