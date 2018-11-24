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
    public enum GpxObjectType { None, Attributes, Metadata, WayPoint, Route, Track };

    public class GpxReader : IDisposable
    {
        private XmlReader Reader_;

        public GpxObjectType ObjectType { get; private set; }
        public GpxAttributes Attributes { get; private set; }
        public GpxMetadata Metadata { get; private set; }
        public GpxWayPoint WayPoint { get; private set; }
        public GpxRoute Route { get; private set; }
        public GpxTrack Track { get; private set; }

        public GpxReader(Stream stream)
        {
            Reader_ = XmlReader.Create(stream);

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        if (Reader_.Name != "gpx") throw new FormatException(Reader_.Name);
                        Attributes = ReadGpxAttribures();
                        ObjectType = GpxObjectType.Attributes;
                        return;
                }
            }

            throw new FormatException();
        }

        public bool Read()
        {
            if (ObjectType == GpxObjectType.None) return false;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "metadata":
                                Metadata = ReadGpxMetadata();
                                ObjectType = GpxObjectType.Metadata;
                                return true;
                            case "wpt":
                                WayPoint = ReadGpxWayPoint();
                                ObjectType = GpxObjectType.WayPoint;
                                return true;
                            case "rte":
                                Route = ReadGpxRoute();
                                ObjectType = GpxObjectType.Route;
                                return true;
                            case "trk":
                                Track = ReadGpxTrack();
                                ObjectType = GpxObjectType.Track;
                                return true;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != "gpx") throw new FormatException(Reader_.Name);
                        ObjectType = GpxObjectType.None;
                        return false;
                }
            }

            ObjectType = GpxObjectType.None;
            return false;
        }

        public void Dispose()
        {
            Reader_.Close();
        }

        private GpxAttributes ReadGpxAttribures()
        {
            GpxAttributes attributes = new GpxAttributes();

            while (Reader_.MoveToNextAttribute())
            {
                switch (Reader_.Name)
                {
                    case "version":
                        attributes.Version = Reader_.Value;
                        break;
                    case "creator":
                        attributes.Creator = Reader_.Value;
                        break;
                }
            }

            return attributes;
        }

        private GpxMetadata ReadGpxMetadata()
        {
            GpxMetadata metadata = new GpxMetadata();
            if (Reader_.IsEmptyElement) return metadata;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "name":
                                metadata.Name = ReadContentAsString();
                                break;
                            case "desc":
                                metadata.Description = ReadContentAsString();
                                break;
                            case "author":
                                metadata.Author = ReadGpxPerson();
                                break;
                            case "copyright":
                                metadata.Copyright = ReadGpxCopyright();
                                break;
                            case "link":
                                metadata.Link = ReadGpxLink();
                                break;
                            case "time":
                                metadata.Time = ReadContentAsDateTime();
                                break;
                            case "keywords":
                                metadata.Keywords = ReadContentAsString();
                                break;
                            case "bounds":
                                ReadGpxBounds();
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return metadata;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxWayPoint ReadGpxWayPoint()
        {
            string elementName = Reader_.Name;
            bool isEmptyElement = Reader_.IsEmptyElement;

            GpxWayPoint wayPoint = new GpxWayPoint();
            GetPointLocation(wayPoint);
            if (isEmptyElement) return wayPoint;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "extensions":
                                ReadWayPointExtensions(wayPoint);
                                break;
                            default:
                                if (!ProcessPointField(wayPoint)) SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return wayPoint;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxRoute ReadGpxRoute()
        {
            GpxRoute route = new GpxRoute();
            if (Reader_.IsEmptyElement) return route;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "name":
                                route.Name = ReadContentAsString();
                                break;
                            case "cmt":
                                route.Comment = ReadContentAsString();
                                break;
                            case "desc":
                                route.Description = ReadContentAsString();
                                break;
                            case "src":
                                route.Source = ReadContentAsString();
                                break;
                            case "link":
                                route.Links.Add(ReadGpxLink());
                                break;
                            case "number":
                                route.Number = int.Parse(ReadContentAsString());
                                break;
                            case "type":
                                route.Type = ReadContentAsString();
                                break;
                            case "rtept":
                                route.RoutePoints.Add(ReadGpxRoutePoint());
                                break;
                            case "extensions":
                                ReadRouteExtensions(route);
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return route;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxRoutePoint ReadGpxRoutePoint()
        {
            string elementName = Reader_.Name;
            bool isEmptyElement = Reader_.IsEmptyElement;

            GpxRoutePoint routePoint = new GpxRoutePoint();
            GetPointLocation(routePoint);
            if (isEmptyElement) return routePoint;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "extensions":
                                ReadRoutePointExtensions(routePoint);
                                break;
                            default:
                                if (!ProcessPointField(routePoint)) SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return routePoint;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxTrack ReadGpxTrack()
        {
            GpxTrack track = new GpxTrack();
            if (Reader_.IsEmptyElement) return track;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "name":
                                track.Name = ReadContentAsString();
                                break;
                            case "cmt":
                                track.Comment = ReadContentAsString();
                                break;
                            case "desc":
                                track.Description = ReadContentAsString();
                                break;
                            case "src":
                                track.Source = ReadContentAsString();
                                break;
                            case "link":
                                track.Links.Add(ReadGpxLink());
                                break;
                            case "number":
                                track.Number = int.Parse(ReadContentAsString());
                                break;
                            case "type":
                                track.Type = ReadContentAsString();
                                break;
                            case "trkseg":
                                track.Segments.Add(ReadGpxTrackSegment());
                                break;
                            case "extensions":
                                ReadTrackExtensions(track);
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return track;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxTrackSegment ReadGpxTrackSegment()
        {
            GpxTrackSegment segment = new GpxTrackSegment();
            if (Reader_.IsEmptyElement) return segment;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "trkpt":
                                segment.TrackPoints.Add(ReadGpxTrackPoint());
                                break;
                            case "extensions":
                                ReadTrackSegmentExtensions();
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return segment;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxTrackPoint ReadGpxTrackPoint()
        {
            string elementName = Reader_.Name;
            bool isEmptyElement = Reader_.IsEmptyElement;

            GpxTrackPoint trackPoint = new GpxTrackPoint();
            GetPointLocation(trackPoint);
            if (isEmptyElement) return trackPoint;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "extensions":
                                ReadTrackPointExtensions(trackPoint);
                                break;
                            default:
                                if (!ProcessPointField(trackPoint)) SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return trackPoint;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxPerson ReadGpxPerson()
        {
            GpxPerson person = new GpxPerson();
            if (Reader_.IsEmptyElement) return person;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "name":
                                person.Name = ReadContentAsString();
                                break;
                            case "email":
                                person.Email = ReadGpxEmail();
                                break;
                            case "link":
                                person.Link = ReadGpxLink();
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return person;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxEmail ReadGpxEmail()
        {
            GpxEmail email = new GpxEmail();
            if (Reader_.IsEmptyElement) return email;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "id":
                                email.Id = ReadContentAsString();
                                break;
                            case "domain":
                                email.Domain = ReadContentAsString();
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return email;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxLink ReadGpxLink()
        {
            GpxLink link = new GpxLink();

            string elementName = Reader_.Name;
            bool isEmptyElement = Reader_.IsEmptyElement;

            while (Reader_.MoveToNextAttribute())
            {
                switch (Reader_.Name)
                {
                    case "href":
                        link.Href = Reader_.Value;
                        break;
                }
            }

            if (isEmptyElement) return link;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "text":
                                link.Text = ReadContentAsString();
                                break;
                            case "type":
                                link.MimeType = ReadContentAsString();
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return link;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxCopyright ReadGpxCopyright()
        {
            GpxCopyright copyright = new GpxCopyright();

            string elementName = Reader_.Name;
            bool isEmptyElement = Reader_.IsEmptyElement;

            while (Reader_.MoveToNextAttribute())
            {
                switch (Reader_.Name)
                {
                    case "author":
                        copyright.Author = Reader_.Value;
                        break;
                }
            }

            if (isEmptyElement) return copyright;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "year":
                                copyright.Year = ReadContentAsInt();
                                break;
                            case "license":
                                copyright.Licence = ReadContentAsString();
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return copyright;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxBounds ReadGpxBounds()
        {
            if (!Reader_.IsEmptyElement) throw new FormatException(Reader_.Name);

            GpxBounds bounds = new GpxBounds();

            while (Reader_.MoveToNextAttribute())
            {
                switch (Reader_.Name)
                {
                    case "minlat":
                        bounds.MinLatitude = double.Parse(Reader_.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "maxlat":
                        bounds.MaxLatitude = double.Parse(Reader_.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "minlon":
                        bounds.MinLongitude = double.Parse(Reader_.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "maxlon":
                        bounds.MaxLongitude = double.Parse(Reader_.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                }
            }

            return bounds;
        }

        private void ReadWayPointExtensions(GpxWayPoint wayPoint)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        if (Reader_.NamespaceURI == GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE || Reader_.NamespaceURI == GpxNamespaces.GARMIN_WAYPOINT_EXTENSIONS_NAMESPACE)
                        {
                            switch (Reader_.LocalName)
                            {
                                case "WaypointExtension":
                                    ReadGarminWayPointExtensions(wayPoint);
                                    break;
                                default:
                                    SkipElement();
                                    break;
                            }

                            break;
                        }

                        if (Reader_.NamespaceURI == GpxNamespaces.DLG_EXTENSIONS_NAMESPACE)
                        {
                            switch (Reader_.LocalName)
                            {
                                case "level":
                                    wayPoint.Level = ReadContentAsInt();
                                    break;
                                case "aliases":
                                    ReadWayPointAliases(wayPoint);
                                    break;
                                default:
                                    SkipElement();
                                    break;
                            }

                            break;
                        }

                        SkipElement();
                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadRouteExtensions(GpxRoute route)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        if (Reader_.NamespaceURI == GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE)
                        {
                            switch (Reader_.LocalName)
                            {
                                case "RouteExtension":
                                    ReadGarminTrackOrRouteExtensions(route);
                                    break;
                                default:
                                    SkipElement();
                                    break;
                            }

                            break;
                        }

                        SkipElement();
                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadRoutePointExtensions(GpxRoutePoint routePoint)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        if (Reader_.NamespaceURI == GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE)
                        {
                            switch (Reader_.LocalName)
                            {
                                case "RoutePointExtension":
                                    ReadGarminRoutePointExtensions(routePoint);
                                    break;
                                default:
                                    SkipElement();
                                    break;
                            }

                            break;
                        }

                        SkipElement();
                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadTrackExtensions(GpxTrack track)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        if (Reader_.NamespaceURI == GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE)
                        {
                            switch (Reader_.LocalName)
                            {
                                case "TrackExtension":
                                    ReadGarminTrackOrRouteExtensions(track);
                                    break;
                                default:
                                    SkipElement();
                                    break;
                            }

                            break;
                        }

                        SkipElement();
                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadTrackSegmentExtensions()
        {
            SkipElement();
        }

        private void ReadTrackPointExtensions(GpxTrackPoint trackPoint)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        if (Reader_.NamespaceURI == GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE ||
                            Reader_.NamespaceURI == GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V1_NAMESPACE ||
                            Reader_.NamespaceURI == GpxNamespaces.GARMIN_TRACKPOINT_EXTENSIONS_V2_NAMESPACE)
                        {
                            switch (Reader_.LocalName)
                            {
                                case "TrackPointExtension":
                                    ReadGarminTrackPointExtensions(trackPoint);
                                    break;
                                default:
                                    SkipElement();
                                    break;
                            }

                            break;
                        }

                        SkipElement();
                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadGarminWayPointExtensions(GpxWayPoint wayPoint)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (Reader_.LocalName)
                        {
                            case "Proximity":
                                wayPoint.Proximity = ReadContentAsDouble();
                                break;
                            case "Temperature":
                                wayPoint.Temperature = ReadContentAsDouble();
                                break;
                            case "Depth":
                                wayPoint.Depth = ReadContentAsDouble();
                                break;
                            case "DisplayMode":
                                wayPoint.DisplayMode = ReadContentAsString();
                                break;
                            case "Categories":
                                ReadGarminCategories(wayPoint);
                                break;
                            case "Address":
                                wayPoint.Address = ReadGarminGpxAddress();
                                break;
                            case "PhoneNumber":
                                wayPoint.Phones.Add(ReadGarminGpxPhone());
                                break;
                            case "Samples":
                                wayPoint.Samples = ReadContentAsInt();
                                break;
                            case "Expiration":
                                wayPoint.Expiration = ReadContentAsDateTime();
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadGarminTrackOrRouteExtensions(GpxTrackOrRoute trackOrRoute)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (Reader_.LocalName)
                        {
                            case "DisplayColor":
                                trackOrRoute.DisplayColor = (GpxColor)Enum.Parse(typeof(GpxColor), ReadContentAsString(), false);
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadGarminRoutePointExtensions(GpxRoutePoint routePoint)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (Reader_.LocalName)
                        {
                            case "rpt":
                                routePoint.RoutePoints.Add(ReadGarminAutoRoutePoint());
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadGarminTrackPointExtensions(GpxTrackPoint trackPoint)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (Reader_.LocalName)
                        {
                            case "Temperature":
                            case "atemp":
                                trackPoint.Temperature = ReadContentAsDouble();
                                break;
                            case "wtemp":
                                trackPoint.WaterTemperature = ReadContentAsDouble();
                                break;
                            case "Depth":
                            case "depth":
                                trackPoint.Depth = ReadContentAsDouble();
                                break;
                            case "hr":
                                trackPoint.HeartRate = ReadContentAsInt();
                                break;
                            case "cad":
                                trackPoint.Cadence = ReadContentAsInt();
                                break;
                            case "speed":
                                trackPoint.Speed = ReadContentAsDouble();
                                break;
                            case "course":
                                trackPoint.Course = ReadContentAsDouble();
                                break;
                            case "bearing":
                                trackPoint.Bearing = ReadContentAsDouble();
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadGarminCategories(GpxWayPoint wayPoint)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (Reader_.LocalName)
                        {
                            case "Category":
                                wayPoint.Categories.Add(ReadContentAsString());
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadWayPointAliases(GpxWayPoint wayPoint)
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (Reader_.LocalName)
                        {
                            case "alias":
                                wayPoint.Aliases.Add(ReadContentAsString());
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxPoint ReadGarminAutoRoutePoint()
        {
            GpxPoint point = new GpxPoint();

            string elementName = Reader_.Name;
            bool isEmptyElement = Reader_.IsEmptyElement;

            GetPointLocation(point);
            if (isEmptyElement) return point;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        SkipElement();
                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return point;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxAddress ReadGarminGpxAddress()
        {
            GpxAddress address = new GpxAddress();
            if (Reader_.IsEmptyElement) return address;

            string elementName = Reader_.Name;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (Reader_.LocalName)
                        {
                            case "StreetAddress":

                                if (string.IsNullOrEmpty(address.StreetAddress))
                                {
                                    address.StreetAddress = ReadContentAsString();
                                    break;
                                }

                                address.StreetAddress += " " + ReadContentAsString();
                                break;

                            case "City":
                                address.City = ReadContentAsString();
                                break;
                            case "State":
                                address.State = ReadContentAsString();
                                break;
                            case "Country":
                                address.Country = ReadContentAsString();
                                break;
                            case "PostalCode":
                                address.PostalCode = ReadContentAsString();
                                break;
                            default:
                                SkipElement();
                                break;
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != elementName) throw new FormatException(Reader_.Name);
                        return address;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxPhone ReadGarminGpxPhone()
        {
            return new GpxPhone
            {
                Category = Reader_.GetAttribute("Category", GpxNamespaces.GARMIN_EXTENSIONS_NAMESPACE),
                Number = ReadContentAsString()
            };
        }

        private void SkipElement()
        {
            if (Reader_.IsEmptyElement) return;

            string elementName = Reader_.Name;
            int depth = Reader_.Depth;

            while (Reader_.Read())
            {
                if (Reader_.NodeType == XmlNodeType.EndElement)
                {
                    if (Reader_.Depth == depth && Reader_.Name == elementName) return;
                }
            }

            throw new FormatException(elementName);
        }

        private void GetPointLocation(GpxPoint point)
        {
            while (Reader_.MoveToNextAttribute())
            {
                switch (Reader_.Name)
                {
                    case "lat":
                        point.Latitude = double.Parse(Reader_.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "lon":
                        point.Longitude = double.Parse(Reader_.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                }
            }
        }

        private bool ProcessPointField(GpxPoint point)
        {
            switch (Reader_.Name)
            {
                case "ele":
                    point.Elevation = ReadContentAsDouble();
                    return true;
                case "time":
                    point.Time = ReadContentAsDateTime();
                    return true;
                case "magvar":
                    point.MagneticVar = ReadContentAsDouble();
                    return true;
                case "geoidheight":
                    point.GeoidHeight = ReadContentAsDouble();
                    return true;
                case "name":
                    point.Name = ReadContentAsString();
                    return true;
                case "cmt":
                    point.Comment = ReadContentAsString();
                    return true;
                case "desc":
                    point.Description = ReadContentAsString();
                    return true;
                case "src":
                    point.Source = ReadContentAsString();
                    return true;
                case "link":
                    point.Links.Add(ReadGpxLink());
                    return true;
                case "sym":
                    point.Symbol = ReadContentAsString();
                    return true;
                case "type":
                    point.Type = ReadContentAsString();
                    return true;
                case "fix":
                    point.FixType = ReadContentAsString();
                    return true;
                case "sat":
                    point.Satelites = ReadContentAsInt();
                    return true;
                case "hdop":
                    point.Hdop = ReadContentAsDouble();
                    return true;
                case "vdop":
                    point.Vdop = ReadContentAsDouble();
                    return true;
                case "pdop":
                    point.Pdop = ReadContentAsDouble();
                    return true;
                case "ageofdgpsdata":
                    point.AgeOfData = ReadContentAsDouble();
                    return true;
                case "dgpsid":
                    point.DgpsId = ReadContentAsInt();
                    return true;
            }

            return false;
        }

        private string ReadContentAsString()
        {
            if (Reader_.IsEmptyElement) throw new FormatException(Reader_.Name);

            string elementName = Reader_.Name;
            string result = string.Empty;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Text:
                        result = Reader_.Value;
                        break;

                    case XmlNodeType.EndElement:
                        return result;

                    case XmlNodeType.Element:
                        throw new FormatException(elementName);
                }
            }

            throw new FormatException(elementName);
        }

        private int ReadContentAsInt()
        {
            string value = ReadContentAsString();
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        private double ReadContentAsDouble()
        {
            string value = ReadContentAsString();
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        private DateTime ReadContentAsDateTime()
        {
            string value = ReadContentAsString();
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }
    }
}