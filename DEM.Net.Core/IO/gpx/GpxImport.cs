using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.Gpx;

namespace DEM.Net.Core;

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
        IEnumerable<IEnumerable<GeoPoint>> segments;
        using (FileStream fs = new FileStream(fileName, FileMode.Open))
        {
            segments = ReadGPX_Segments(fs);
        }

        return segments;
    }

    /// <summary>
    /// Read gpx file segments as enumerable GeoPoints: one list per track segment (<c>trkseg</c>)
    /// and one per route (<c>rte</c>), in file order. Empty segments are skipped.
    /// </summary>
    /// <param name="gpxFileStream">GPX file stream</param>
    /// <returns></returns>
    /// <remarks>A route's points are its <c>rtept</c>, each followed by its Garmin <c>gpxx:rpt</c> shape points when present.</remarks>
    public static IEnumerable<IEnumerable<GeoPoint>> ReadGPX_Segments(Stream gpxFileStream)
    {
        static GeoPoint Convert(GpxPoint pt) => new GeoPoint(pt.Latitude, pt.Longitude, pt.Elevation);

        var segments = new List<List<GeoPoint>>();

        using (GpxReader reader = new GpxReader(gpxFileStream))
        {
            while (reader.Read())
            {
                switch (reader.ObjectType)
                {
                    case GpxObjectType.Track:
                        foreach (var seg in reader.Track!.Segments)
                            segments.Add([.. seg.TrackPoints.Select(Convert)]);
                        break;
                    case GpxObjectType.Route:
                        segments.Add([.. reader.Route!.ToGpxPoints().Select(Convert)]);
                        break;
                }
            }
        }
        return segments.Where(seg => seg.Count > 0).ToList();
    }
}
