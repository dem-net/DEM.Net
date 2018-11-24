// ==========================================================================
// Copyright (c) 2011-2016, dlg.krakow.pl
// All Rights Reserved
//
// NOTICE: dlg.krakow.pl permits you to use, modify, and distribute this file
// in accordance with the terms of the license agreement accompanying it.
// ==========================================================================

using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Gpx
{
    public class GpxWriter : IDisposable
    {
        private const string GPX_VERSION = "1.1";
        private const string GPX_CREATOR = "http://dlg.krakow.pl/gpx";
        private const string GARMIN_EXTENSIONS_PREFIX = "gpxx";
        private const string GARMIN_WAYPOINT_EXTENSIONS_PREFIX = "gpxwpx";
        private const string GARMIN_TRACKPOINT_EXTENSIONS_V2_PREFIX = "gpxtpx";
        private const string DLG_EXTENSIONS_PREFIX = "dlg";

        private XmlWriter Writer_;

        public GpxWriter(Stream stream)
        {
            Writer_ = XmlWriter.Create(stream, new XmlWriterSettings { CloseOutput = true, Indent = true });
            Writer_.WriteStartDocument(false);
            Writer_.WriteStartElement("gpx", GpxNamespaces.GPX_NAMESPACE);
            Writer_.WriteAttributeString("version", GPX_VERSION);
            Writer_.WriteAttributeString("creator", GPX_CREATOR);
            Writer_.WriteAttributeString("xmlns", GARMIN_EXTENSIONS_PREFIX, null, GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE);
            Writer_.WriteAttributeString("xmlns", GARMIN_WAYPOINT_EXTENSIONS_PREFIX, null, GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE);
            Writer_.WriteAttributeString("xmlns", GARMIN_TRACKPOINT_EXTENSIONS_V2_PREFIX, null, GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE);
            Writer_.WriteAttributeString("xmlns", DLG_EXTENSIONS_PREFIX, null, GpxNamespaces.DLG_EXTENSIONS_NAMESPACE);
        }

        public void Dispose()
        {
            Writer_.WriteEndElement();
            Writer_.Close();
        }

        public void WriteMetadata(GpxMetadata metadata)
        {
            Writer_.WriteStartElement("metadata");

            if (metadata.Name != null) Writer_.WriteElementString("name", metadata.Name);
            if (metadata.Description != null) Writer_.WriteElementString("desc", metadata.Description);
            if (metadata.Author != null) WritePerson("author", metadata.Author);
            if (metadata.Copyright != null) WriteCopyright("copyright", metadata.Copyright);
            if (metadata.Link != null) WriteLink("link", metadata.Link);
            if (metadata.Time != null) Writer_.WriteElementString("time", ToGpxDateString(metadata.Time.Value));
            if (metadata.Keywords != null) Writer_.WriteElementString("keywords", metadata.Keywords);
            if (metadata.Bounds != null) WriteBounds("bounds", metadata.Bounds);

            Writer_.WriteEndElement();
        }

        public void WriteWayPoint(GpxWayPoint wayPoint)
        {
            Writer_.WriteStartElement("wpt");
            WritePoint(wayPoint);

            if (wayPoint.HasExtensions)
            {
                Writer_.WriteStartElement("extensions");

                if (wayPoint.HasGarminExtensions || wayPoint.HasGarminWaypointExtensions)
                {
                    Writer_.WriteStartElement("WaypointExtension", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE);

                    if (wayPoint.Proximity != null) Writer_.WriteElementString("Proximity", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, wayPoint.Proximity.Value.ToString());
                    if (wayPoint.Temperature != null) Writer_.WriteElementString("Temperature", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, wayPoint.Temperature.Value.ToString());
                    if (wayPoint.Depth != null) Writer_.WriteElementString("Depth", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, wayPoint.Depth.Value.ToString());
                    if (wayPoint.DisplayMode != null) Writer_.WriteElementString("DisplayMode", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, wayPoint.DisplayMode);

                    if (wayPoint.Categories.Count != 0)
                    {
                        Writer_.WriteStartElement("Categories", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE);

                        foreach (string category in wayPoint.Categories)
                        {
                            Writer_.WriteElementString("Category", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, category);
                        }

                        Writer_.WriteEndElement();
                    }

                    if (wayPoint.Address != null) WriteAddress("Address", wayPoint.Address);

                    foreach (GpxPhone phone in wayPoint.Phones)
                    {
                        WritePhone("PhoneNumber", phone);
                    }

                    if (wayPoint.Samples != null) Writer_.WriteElementString("Samples", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, wayPoint.Samples.Value.ToString());
                    if (wayPoint.Expiration != null) Writer_.WriteElementString("Expiration", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, ToGpxDateString(wayPoint.Expiration.Value));

                    Writer_.WriteEndElement();
                }

                if (wayPoint.Aliases.Count != 0)
                {
                    Writer_.WriteStartElement("aliases", GpxNamespaces.DLG_EXTENSIONS_NAMESPACE);

                    foreach (string alias in wayPoint.Aliases)
                    {
                        Writer_.WriteElementString("alias", GpxNamespaces.DLG_EXTENSIONS_NAMESPACE, alias);
                    }

                    Writer_.WriteEndElement();
                }

                if (wayPoint.Level != null)
                {
                    Writer_.WriteElementString("level", GpxNamespaces.DLG_EXTENSIONS_NAMESPACE, wayPoint.Level.Value.ToString());
                }

                Writer_.WriteEndElement();
            }

            Writer_.WriteEndElement();
        }

        public void WriteRoute(GpxRoute route)
        {
            Writer_.WriteStartElement("rte");
            WriteTrackOrRoute(route);

            foreach (GpxRoutePoint routePoint in route.RoutePoints)
            {
                WriteRoutePoint("rtept", routePoint);
            }

            Writer_.WriteEndElement();
        }

        public void WriteTrack(GpxTrack track)
        {
            Writer_.WriteStartElement("trk");
            WriteTrackOrRoute(track);

            foreach (GpxTrackSegment segment in track.Segments)
            {
                WriteTrackSegment("trkseg", segment);
            }

            Writer_.WriteEndElement();
        }

        private void WriteTrackOrRoute(GpxTrackOrRoute trackOrRoute)
        {
            if (trackOrRoute.Name != null) Writer_.WriteElementString("name", trackOrRoute.Name);
            if (trackOrRoute.Comment != null) Writer_.WriteElementString("cmt", trackOrRoute.Comment);
            if (trackOrRoute.Description != null) Writer_.WriteElementString("desc", trackOrRoute.Description);
            if (trackOrRoute.Source != null) Writer_.WriteElementString("src", trackOrRoute.Source);

            foreach (GpxLink link in trackOrRoute.Links)
            {
                WriteLink("link", link);
            }

            if (trackOrRoute.Number != null) Writer_.WriteElementString("number", trackOrRoute.Number.Value.ToString(CultureInfo.InvariantCulture));
            if (trackOrRoute.Type != null) Writer_.WriteElementString("type", trackOrRoute.Type);

            if (trackOrRoute.HasExtensions)
            {
                Writer_.WriteStartElement("extensions");
                Writer_.WriteStartElement(trackOrRoute is GpxTrack ? "TrackExtension" : "RouteExtension", GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE);

                Writer_.WriteElementString("DisplayColor", GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE, trackOrRoute.DisplayColor.ToString());

                Writer_.WriteEndElement();
                Writer_.WriteEndElement();
            }
        }

        private void WriteTrackSegment(string elementName, GpxTrackSegment segment)
        {
            Writer_.WriteStartElement(elementName);

            foreach (GpxTrackPoint trackPoint in segment.TrackPoints)
            {
                WriteTrackPoint(trackPoint);
            }

            Writer_.WriteEndElement();
        }

        private void WriteTrackPoint(GpxTrackPoint trackPoint)
        {
            Writer_.WriteStartElement("trkpt");
            WritePoint(trackPoint);

            if (trackPoint.HasExtensions)
            {
                Writer_.WriteStartElement("extensions");
                Writer_.WriteStartElement("TrackPointExtension", GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE);

                if (trackPoint.Temperature != null) Writer_.WriteElementString("atemp", GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE, trackPoint.Temperature.Value.ToString());
                if (trackPoint.WaterTemperature != null) Writer_.WriteElementString("wtemp", GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE, trackPoint.WaterTemperature.Value.ToString());
                if (trackPoint.Depth != null) Writer_.WriteElementString("depth", GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE, trackPoint.Depth.Value.ToString());
                if (trackPoint.HeartRate != null) Writer_.WriteElementString("hr", GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE, trackPoint.HeartRate.Value.ToString());
                if (trackPoint.Cadence != null) Writer_.WriteElementString("cad", GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE, trackPoint.Cadence.Value.ToString());
                if (trackPoint.Speed != null) Writer_.WriteElementString("speed", GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE, trackPoint.Speed.Value.ToString());
                if (trackPoint.Course != null) Writer_.WriteElementString("course", GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE, trackPoint.Course.Value.ToString());
                if (trackPoint.Bearing != null) Writer_.WriteElementString("bearing", GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE, trackPoint.Bearing.Value.ToString());

                Writer_.WriteEndElement();
                Writer_.WriteEndElement();
            }

            Writer_.WriteEndElement();
        }

        private void WriteRoutePoint(string elementName, GpxRoutePoint routePoint)
        {
            Writer_.WriteStartElement(elementName);
            WritePoint(routePoint);

            if (routePoint.HasExtensions)
            {
                Writer_.WriteStartElement("extensions");
                Writer_.WriteStartElement("RoutePointExtension", GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE);

                foreach (GpxPoint point in routePoint.RoutePoints)
                {
                    WriteSubPoint(point);
                }

                Writer_.WriteEndElement();
                Writer_.WriteEndElement();
            }

            Writer_.WriteEndElement();
        }

        private void WritePoint(GpxPoint point)
        {
            Writer_.WriteAttributeString("lat", point.Latitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("lon", point.Longitude.ToString(CultureInfo.InvariantCulture));
            if (point.Elevation != null) Writer_.WriteElementString("ele", point.Elevation.Value.ToString(CultureInfo.InvariantCulture));
            if (point.Time != null) Writer_.WriteElementString("time", ToGpxDateString(point.Time.Value));
            if (point.MagneticVar != null) Writer_.WriteElementString("magvar", point.MagneticVar.Value.ToString(CultureInfo.InvariantCulture));
            if (point.GeoidHeight != null) Writer_.WriteElementString("geoidheight", point.GeoidHeight.Value.ToString(CultureInfo.InvariantCulture));
            if (point.Name != null) Writer_.WriteElementString("name", point.Name);
            if (point.Comment != null) Writer_.WriteElementString("cmt", point.Comment);
            if (point.Description != null) Writer_.WriteElementString("desc", point.Description);
            if (point.Source != null) Writer_.WriteElementString("src", point.Source);

            foreach (GpxLink link in point.Links)
            {
                WriteLink("link", link);
            }

            if (point.Symbol != null) Writer_.WriteElementString("sym", point.Symbol);
            if (point.Type != null) Writer_.WriteElementString("type", point.Type);
            if (point.FixType != null) Writer_.WriteElementString("fix", point.FixType);
            if (point.Satelites != null) Writer_.WriteElementString("sat", point.Satelites.Value.ToString(CultureInfo.InvariantCulture));
            if (point.Hdop != null) Writer_.WriteElementString("hdop", point.Hdop.Value.ToString(CultureInfo.InvariantCulture));
            if (point.Vdop != null) Writer_.WriteElementString("vdop", point.Vdop.Value.ToString(CultureInfo.InvariantCulture));
            if (point.Pdop != null) Writer_.WriteElementString("pdop", point.Pdop.Value.ToString(CultureInfo.InvariantCulture));
            if (point.AgeOfData != null) Writer_.WriteElementString("ageofdgpsdata", point.AgeOfData.Value.ToString(CultureInfo.InvariantCulture));
            if (point.DgpsId != null) Writer_.WriteElementString("dgpsid", point.DgpsId.Value.ToString(CultureInfo.InvariantCulture));
        }

        private void WriteSubPoint(GpxPoint point)
        {
            Writer_.WriteStartElement("rpt", GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE);

            Writer_.WriteAttributeString("lat", point.Latitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("lon", point.Longitude.ToString(CultureInfo.InvariantCulture));

            Writer_.WriteEndElement();
        }

        private void WritePerson(string elementName, GpxPerson person)
        {
            Writer_.WriteStartElement(elementName);

            if (person.Name != null) Writer_.WriteElementString("name", person.Name);
            if (person.Email != null) WriteEmail("email", person.Email);

            Writer_.WriteEndElement();
        }

        private void WriteEmail(string elementName, GpxEmail email)
        {
            Writer_.WriteStartElement(elementName);
            Writer_.WriteAttributeString("id", email.Id);
            Writer_.WriteAttributeString("domain", email.Domain);
            Writer_.WriteEndElement();
        }

        private void WriteLink(string elementName, GpxLink link)
        {
            Writer_.WriteStartElement(elementName);
            Writer_.WriteAttributeString("href", link.Href);
            if (link.Text != null) Writer_.WriteElementString("text", link.Text);
            if (link.MimeType != null) Writer_.WriteElementString("type", link.MimeType);
            Writer_.WriteEndElement();
        }

        private void WriteCopyright(string elementName, GpxCopyright copyright)
        {
            Writer_.WriteStartElement(elementName);
            Writer_.WriteAttributeString("author", copyright.Author);
            if (copyright.Year != null) Writer_.WriteElementString("year", copyright.Year.Value.ToString());
            if (copyright.Licence != null) Writer_.WriteElementString("licence", copyright.Licence.ToString());
            Writer_.WriteEndElement();
        }

        private void WriteAddress(string elementName, GpxAddress address)
        {
            Writer_.WriteStartElement(elementName, GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE);
            if (address.StreetAddress != null) Writer_.WriteElementString("StreetAddress", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, address.StreetAddress);
            if (address.City != null) Writer_.WriteElementString("City", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, address.City);
            if (address.State != null) Writer_.WriteElementString("State", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, address.State);
            if (address.Country != null) Writer_.WriteElementString("Country", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, address.Country);
            if (address.PostalCode != null) Writer_.WriteElementString("PostalCode", GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE, address.PostalCode);
            Writer_.WriteEndElement();
        }

        private void WritePhone(string elementName, GpxPhone phone)
        {
            Writer_.WriteStartElement(elementName, GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE);
            if (phone.Category != null) Writer_.WriteAttributeString("Category", phone.Category);
            Writer_.WriteString(phone.Number);
            Writer_.WriteEndElement();
        }

        private void WriteBounds(string elementName, GpxBounds bounds)
        {
            Writer_.WriteStartElement(elementName);
            Writer_.WriteAttributeString("minlat", bounds.MinLatitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("minlon", bounds.MinLongitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("maxlat", bounds.MaxLatitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("maxlon", bounds.MaxLongitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteEndElement();
        }

        private static string ToGpxDateString(DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH':'mm':'ss.FFFZ");
            //return string.Format("{0:D4}-{1:D2}-{2:D2}T{3:D2}:{4:D2}:{5:D2}", date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
        }
    }
}