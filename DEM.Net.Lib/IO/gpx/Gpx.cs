// ==========================================================================
// Copyright (c) 2011-2016, dlg.krakow.pl
// All Rights Reserved
//
// NOTICE: dlg.krakow.pl permits you to use, modify, and distribute this file
// in accordance with the terms of the license agreement accompanying it.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace Gpx
{
    public static class GpxNamespaces
    {
        public const string GPX_NAMESPACE = "http://www.topografix.com/GPX/1/1";
        public const string GARMIN_EXTENSIONS_NAMESPACE = "http://www.garmin.com/xmlschemas/GpxExtensions/v3";
        public const string GARMIN_TRACKPOINT_EXTENSIONS_V1_NAMESPACE = "http://www.garmin.com/xmlschemas/TrackPointExtension/v1";
        public const string GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE = "http://www.garmin.com/xmlschemas/TrackPointExtension/v2";
        public const string GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE = "http://www.garmin.com/xmlschemas/WaypointExtension/v1";
        public const string DLG_EXTENSIONS_NAMESPACE = "http://dlg.krakow.pl/gpx/extensions/v1";
    }

    public class GpxAttributes
    {
        public string Version { get; set; }
        public string Creator { get; set; }
    }

    public class GpxMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public GpxPerson Author { get; set; }
        public GpxCopyright Copyright { get; set; }
        public GpxLink Link { get; set; }
        public DateTime? Time { get; set; }
        public string Keywords { get; set; }
        public GpxBounds Bounds { get; set; }
    }

    public class GpxPoint
    {
        private const double EARTH_RADIUS = 6371; // [km]
        private const double RADIAN = Math.PI / 180;

        protected GpxProperties Properties_ = new GpxProperties();

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Elevation { get; set; }
        public DateTime? Time { get; set; }

        public double? MagneticVar
        {
            get { return Properties_.GetValueProperty<double>("MagneticVar"); }
            set { Properties_.SetValueProperty<double>("MagneticVar", value); }
        }

        public double? GeoidHeight
        {
            get { return Properties_.GetValueProperty<double>("GeoidHeight"); }
            set { Properties_.SetValueProperty<double>("GeoidHeight", value); }
        }

        public string Name
        {
            get { return Properties_.GetObjectProperty<string>("Name"); }
            set { Properties_.SetObjectProperty<string>("Name", value); }
        }

        public string Comment
        {
            get { return Properties_.GetObjectProperty<string>("Comment"); }
            set { Properties_.SetObjectProperty<string>("Comment", value); }
        }

        public string Description
        {
            get { return Properties_.GetObjectProperty<string>("Description"); }
            set { Properties_.SetObjectProperty<string>("Description", value); }
        }

        public string Source
        {
            get { return Properties_.GetObjectProperty<string>("Source"); }
            set { Properties_.SetObjectProperty<string>("Source", value); }
        }

        public IList<GpxLink> Links
        {
            get { return Properties_.GetListProperty<GpxLink>("Links"); }
        }

        public string Symbol
        {
            get { return Properties_.GetObjectProperty<string>("Symbol"); }
            set { Properties_.SetObjectProperty<string>("Symbol", value); }
        }

        public string Type
        {
            get { return Properties_.GetObjectProperty<string>("Type"); }
            set { Properties_.SetObjectProperty<string>("Type", value); }
        }

        public string FixType
        {
            get { return Properties_.GetObjectProperty<string>("FixType"); }
            set { Properties_.SetObjectProperty<string>("FixType", value); }
        }

        public int? Satelites
        {
            get { return Properties_.GetValueProperty<int>("Satelites"); }
            set { Properties_.SetValueProperty<int>("Satelites", value); }
        }

        public double? Hdop
        {
            get { return Properties_.GetValueProperty<double>("Hdop"); }
            set { Properties_.SetValueProperty<double>("Hdop", value); }
        }

        public double? Vdop
        {
            get { return Properties_.GetValueProperty<double>("Vdop"); }
            set { Properties_.SetValueProperty<double>("Vdop", value); }
        }

        public double? Pdop
        {
            get { return Properties_.GetValueProperty<double>("Pdop"); }
            set { Properties_.SetValueProperty<double>("Pdop", value); }
        }

        public double? AgeOfData
        {
            get { return Properties_.GetValueProperty<double>("AgeOfData"); }
            set { Properties_.SetValueProperty<double>("AgeOfData", value); }
        }

        public int? DgpsId
        {
            get { return Properties_.GetValueProperty<int>("DgpsId"); }
            set { Properties_.SetValueProperty<int>("DgpsId", value); }
        }

        public GpxLink HttpLink
        {
            get
            {
                return Links.Where(l => l != null && l.Uri != null && l.Uri.Scheme == Uri.UriSchemeHttp).FirstOrDefault();
            }
        }

        public GpxLink EmailLink
        {
            get
            {
                return Links.Where(l => l != null && l.Uri != null && l.Uri.Scheme == Uri.UriSchemeMailto).FirstOrDefault();
            }
        }

        public double GetDistanceFrom(GpxPoint other)
        {
            double thisLatitude = this.Latitude;
            double otherLatitude = other.Latitude;
            double thisLongitude = this.Longitude;
            double otherLongitude = other.Longitude;

            double deltaLatitude = Math.Abs(this.Latitude - other.Latitude);
            double deltaLongitude = Math.Abs(this.Longitude - other.Longitude);

            thisLatitude *= RADIAN;
            otherLatitude *= RADIAN;
            deltaLongitude *= RADIAN;

            double cos = Math.Cos(deltaLongitude) * Math.Cos(thisLatitude) * Math.Cos(otherLatitude) +
                Math.Sin(thisLatitude) * Math.Sin(otherLatitude);

            return EARTH_RADIUS * Math.Acos(cos);
        }
    }

    public class GpxWayPoint : GpxPoint
    {
        // GARMIN_EXTENSIONS, GARMIN_WAYPOINT_EXTENSIONS

        public double? Proximity
        {
            get { return Properties_.GetValueProperty<double>("Proximity"); }
            set { Properties_.SetValueProperty<double>("Proximity", value); }
        }

        public double? Temperature
        {
            get { return Properties_.GetValueProperty<double>("Temperature"); }
            set { Properties_.SetValueProperty<double>("Temperature", value); }
        }

        public double? Depth
        {
            get { return Properties_.GetValueProperty<double>("Depth"); }
            set { Properties_.SetValueProperty<double>("Depth", value); }
        }

        public string DisplayMode
        {
            get { return Properties_.GetObjectProperty<string>("DisplayMode"); }
            set { Properties_.SetObjectProperty<string>("DisplayMode", value); }
        }

        public IList<string> Categories
        {
            get { return Properties_.GetListProperty<string>("Categories"); }
        }

        public GpxAddress Address
        {
            get { return Properties_.GetObjectProperty<GpxAddress>("Address"); }
            set { Properties_.SetObjectProperty<GpxAddress>("Address", value); }
        }

        public IList<GpxPhone> Phones
        {
            get { return Properties_.GetListProperty<GpxPhone>("Phones"); }
        }

        // GARMIN_WAYPOINT_EXTENSIONS

        public int? Samples
        {
            get { return Properties_.GetValueProperty<int>("Samples"); }
            set { Properties_.SetValueProperty<int>("Samples", value); }
        }

        public DateTime? Expiration
        {
            get { return Properties_.GetValueProperty<DateTime>("Expiration"); }
            set { Properties_.SetValueProperty<DateTime>("Expiration", value); }
        }

        // DLG_EXTENSIONS

        public int? Level
        {
            get { return Properties_.GetValueProperty<int>("Level"); }
            set { Properties_.SetValueProperty<int>("Level", value); }
        }

        public IList<string> Aliases
        {
            get { return Properties_.GetListProperty<string>("Aliases"); }
        }

        public bool HasGarminExtensions
        {
            get
            {
                return Proximity != null || Temperature != null || Depth != null ||
                    DisplayMode != null || Address != null ||
                    Categories.Count != 0 || Phones.Count != 0;
            }
        }

        public bool HasGarminWaypointExtensions
        {
            get { return Samples != null || Expiration != null; }
        }

        public bool HasDlgExtensions
        {
            get { return Level != null || Aliases.Count != 0; }
        }

        public bool HasExtensions
        {
            get { return HasGarminExtensions || HasGarminWaypointExtensions || HasDlgExtensions; }
        }
    }

    public class GpxTrackPoint : GpxPoint
    {
        // GARMIN_EXTENSIONS, GARMIN_TRACKPOINT_EXTENSIONS_V1, GARMIN_TRACKPOINT_EXTENSIONS_V2

        public double? Temperature
        {
            get { return Properties_.GetValueProperty<double>("Temperature"); }
            set { Properties_.SetValueProperty<double>("Temperature", value); }
        }

        public double? Depth
        {
            get { return Properties_.GetValueProperty<double>("Depth"); }
            set { Properties_.SetValueProperty<double>("Depth", value); }
        }

        // GARMIN_TRACKPOINT_EXTENSIONS_V1, GARMIN_TRACKPOINT_EXTENSIONS_V2

        public double? WaterTemperature
        {
            get { return Properties_.GetValueProperty<double>("WaterTemperature"); }
            set { Properties_.SetValueProperty<double>("WaterTemperature", value); }
        }

        public int? HeartRate
        {
            get { return Properties_.GetValueProperty<int>("HeartRate"); }
            set { Properties_.SetValueProperty<int>("HeartRate", value); }
        }

        public int? Cadence
        {
            get { return Properties_.GetValueProperty<int>("Cadence"); }
            set { Properties_.SetValueProperty<int>("Cadence", value); }
        }

        // GARMIN_TRACKPOINT_EXTENSIONS_V2

        public double? Speed
        {
            get { return Properties_.GetValueProperty<double>("Speed"); }
            set { Properties_.SetValueProperty<double>("Speed", value); }
        }

        public double? Course
        {
            get { return Properties_.GetValueProperty<double>("Course"); }
            set { Properties_.SetValueProperty<double>("Course", value); }
        }

        public double? Bearing
        {
            get { return Properties_.GetValueProperty<double>("Bearing"); }
            set { Properties_.SetValueProperty<double>("Bearing", value); }
        }

        public bool HasGarminExtensions
        {
            get { return Temperature != null || Depth != null; }
        }

        public bool HasGarminTrackpointExtensionsV1
        {
            get { return WaterTemperature != null || HeartRate != null || Cadence != null; }
        }

        public bool HasGarminTrackpointExtensionsV2
        {
            get { return Speed != null || Course != null || Bearing != null; }
        }

        public bool HasExtensions
        {
            get { return HasGarminExtensions || HasGarminTrackpointExtensionsV1 || HasGarminTrackpointExtensionsV2; }
        }
    }

    public class GpxRoutePoint : GpxPoint
    {
        // GARMIN_EXTENSIONS

        public IList<GpxPoint> RoutePoints
        {
            get { return Properties_.GetListProperty<GpxPoint>("RoutePoints"); }
        }

        public bool HasExtensions
        {
            get { return RoutePoints.Count != 0; }
        }
    }

    public class GpxPointCollection<T> : IList<T> where T : GpxPoint
    {
        private List<T> Points_ = new List<T>();

        public GpxPoint AddPoint(T point)
        {
            Points_.Add(point);
            return point;
        }

        public T StartPoint
        {
            get { return (Points_.Count == 0) ? null : Points_[0]; }
        }

        public T EndPoint
        {
            get { return (Points_.Count == 0) ? null : Points_[Points_.Count - 1]; }
        }

        public double GetLength()
        {
            double result = 0;

            for (int i = 1; i < Points_.Count; i++)
            {
                double dist = Points_[i].GetDistanceFrom(Points_[i - 1]);
                result += dist;
            }

            return result;
        }

        public double? GetMinElevation()
        {
            return Points_.Select(p => p.Elevation).Min();
        }

        public double? GetMaxElevation()
        {
            return Points_.Select(p => p.Elevation).Max();
        }

        public GpxPointCollection<GpxPoint> ToGpxPoints()
        {
            GpxPointCollection<GpxPoint> points = new GpxPointCollection<GpxPoint>();

            foreach (T gpxPoint in Points_)
            {
                GpxPoint point = new GpxPoint
                {
                    Longitude = gpxPoint.Longitude,
                    Latitude = gpxPoint.Latitude,
                    Elevation = gpxPoint.Elevation,
                    Time = gpxPoint.Time
                };

                points.Add(point);
            }

            return points;
        }

        public int Count
        {
            get { return Points_.Count; }
        }

        public int IndexOf(T item)
        {
            return Points_.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Points_.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Points_.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return Points_[index]; }
            set { Points_[index] = value; }
        }

        public void Add(T item)
        {
            Points_.Add(item);
        }

        public void Clear()
        {
            Points_.Clear();
        }

        public bool Contains(T item)
        {
            return Points_.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Points_.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return Points_.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Points_.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public abstract class GpxTrackOrRoute
    {
        private List<GpxLink> Links_ = new List<GpxLink>(0);

        public string Name { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public int? Number { get; set; }
        public string Type { get; set; }

        public IList<GpxLink> Links
        {
            get { return Links_; }
        }

        // GARMIN_EXTENSIONS

        public GpxColor? DisplayColor { get; set; }

        public bool HasExtensions
        {
            get { return DisplayColor != null; }
        }

        public abstract GpxPointCollection<GpxPoint> ToGpxPoints();
    }

    public class GpxRoute : GpxTrackOrRoute
    {
        private GpxPointCollection<GpxRoutePoint> RoutePoints_ = new GpxPointCollection<GpxRoutePoint>();

        public GpxPointCollection<GpxRoutePoint> RoutePoints
        {
            get { return RoutePoints_; }
        }

        public override GpxPointCollection<GpxPoint> ToGpxPoints()
        {
            GpxPointCollection<GpxPoint> points = new GpxPointCollection<GpxPoint>();

            foreach (GpxRoutePoint routePoint in RoutePoints_)
            {
                points.Add(routePoint);

                foreach (GpxPoint gpxPoint in routePoint.RoutePoints)
                {
                    points.Add(gpxPoint);
                }
            }

            return points;
        }
    }

    public class GpxTrack : GpxTrackOrRoute
    {
        private List<GpxTrackSegment> Segments_ = new List<GpxTrackSegment>(1);

        public IList<GpxTrackSegment> Segments
        {
            get { return Segments_; }
        }

        public override GpxPointCollection<GpxPoint> ToGpxPoints()
        {
            GpxPointCollection<GpxPoint> points = new GpxPointCollection<GpxPoint>();

            foreach (GpxTrackSegment segment in Segments_)
            {
                GpxPointCollection<GpxPoint> segmentPoints = segment.TrackPoints.ToGpxPoints();

                foreach (GpxPoint point in segmentPoints)
                {
                    points.Add(point);
                }
            }

            return points;
        }
    }

    public class GpxTrackSegment
    {
        GpxPointCollection<GpxTrackPoint> TrackPoints_ = new GpxPointCollection<GpxTrackPoint>();

        public GpxPointCollection<GpxTrackPoint> TrackPoints
        {
            get { return TrackPoints_; }
        }
    }

    public class GpxLink
    {
        public string Href { get; set; }
        public string Text { get; set; }
        public string MimeType { get; set; }

        public Uri Uri
        {
            get
            {
                Uri result;
                return Uri.TryCreate(Href, UriKind.Absolute, out result) ? result : null;
            }
        }
    }

    public class GpxEmail
    {
        public string Id { get; set; }
        public string Domain { get; set; }
    }

    public class GpxAddress
    {
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
    }

    public class GpxPhone
    {
        public string Number { get; set; }
        public string Category { get; set; }
    }

    public class GpxPerson
    {
        public string Name { get; set; }
        public GpxEmail Email { get; set; }
        public GpxLink Link { get; set; }
    }

    public class GpxCopyright
    {
        public string Author { get; set; }
        public int? Year { get; set; }
        public string Licence { get; set; }
    }

    public class GpxBounds
    {
        public double MinLatitude { get; set; }
        public double MinLongitude { get; set; }
        public double MaxLatitude { get; set; }
        public double MaxLongitude { get; set; }
    }

    public enum GpxColor : uint
    {
        Black = 0xff000000,
        DarkRed = 0xff8b0000,
        DarkGreen = 0xff008b00,
        DarkYellow = 0x8b8b0000,
        DarkBlue = 0Xff00008b,
        DarkMagenta = 0xff8b008b,
        DarkCyan = 0xff008b8b,
        LightGray = 0xffd3d3d3,
        DarkGray = 0xffa9a9a9,
        Red = 0xffff0000,
        Green = 0xff00b000,
        Yellow = 0xffffff00,
        Blue = 0xff0000ff,
        Magenta = 0xffff00ff,
        Cyan = 0xff00ffff,
        White = 0xffffffff,
        Transparent = 0x00ffffff
    }
}