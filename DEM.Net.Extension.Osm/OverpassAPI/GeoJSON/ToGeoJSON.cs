/*
 * Copyright (c) 2014, Achim 'ahzf' Friedland <achim@graphdefined.org>
 * This file is part of OpenDataAPI <http://www.github.com/GraphDefined/OpenDataAPI>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;
using DEM.Net.Extension.Osm;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using GeoJSON.Net;
//using DEM.Net.Extension.Osm;

#endregion

namespace DEM.Net.Extension.Osm.OverpassAPI
{

    /// <summary>
    /// Convert the OSM JSON result of an Overpass query to GeoJSON.
    /// </summary>
    public static partial class GeoJSONExtentions
    {

        #region ToGeoJSON(this OverpassQuery)

        /// <summary>
        /// Run the given Overpass query and convert the result to GeoJSON.
        /// </summary>
        /// <param name="OverpassQuery">An Overpass query.</param>
        public static Task<FeatureCollection> ToGeoJSON(this OverpassQuery OverpassQuery)
        {

            return OverpassQuery.
                       RunQuery().
                       ToGeoJSON();

        }

        #endregion

        #region ToGeoJSON(this ResultTask)

        /// <summary>
        /// Convert the given Overpass query result to GeoJSON.
        /// </summary>
        /// <param name="ResultTask">A Overpass query result task.</param>
        public static Task<FeatureCollection> ToGeoJSON(this Task<OverpassResult> ResultTask)
        {

            return ResultTask.ContinueWith(task => {

                // The order of the nodes, ways and relations seem not to be sorted by default!

                #region 1st: Add all nodes

                Node Node;
                var Nodes = new Dictionary<UInt64, Node>();

                foreach (var element in ResultTask.Result.Elements)
                {

                    // {
                    //   "type": "node",
                    //   "id":    35304749,
                    //   "lat":   50.8926376,
                    //   "lon":   11.6023278,
                    //   "tags": {
                    //       "highway":     "bus_stop",
                    //       "name":        "Lobeda",
                    //       "operator":    "JES",
                    //       "wheelchair":  "yes"
                    //   }
                    // }

                    if (element["type"].ToString() == "node")
                    {

                        Node = Node.Parse(element);

                        if (Nodes.ContainsKey(Node.Id))
                            Console.WriteLine("Duplicate node id detected!");
                        else
                            Nodes.Add(Node.Id, Node);

                    }

                }

                #endregion

                #region 2nd: Add all ways

                Way Way;
                var Ways = new Dictionary<UInt64, Way>();

                foreach (var element in ResultTask.Result.Elements)
                {

                    // {
                    //   "type": "way",
                    //   "id":   154676600,
                    //   "nodes": [
                    //     747761494,
                    //     582476538,
                    //     582476541,
                    //     582476543,
                    //     1671750275,
                    //     407850195,
                    //     407850192,
                    //     407850188,
                    //     1671750245,
                    //     1671750330,
                    //     1671750408,
                    //     1671750415,
                    //     1671750433,
                    //     1671750438,
                    //     1671750441,
                    //     747761494
                    //   ],
                    //   "tags": {
                    //     "landuse": "farm"
                    //   }
                    // }

                    if (element["type"].ToString() == "way")
                    {

                        Way = Way.Parse(element,
                                        NodeResolver: nodeId => Nodes[nodeId]);

                        if (Ways.ContainsKey(Way.Id))
                            Console.WriteLine("Duplicate way id detected!");
                        else
                            Ways.Add(Way.Id, Way);

                    }

                }

                #endregion

                #region 3rd: Add all relations

                Relation Relation;
                var Relations = new Dictionary<UInt64, Relation>();

                foreach (var element in ResultTask.Result.Elements)
                {

                    // {
                    //   "type": "relation",
                    //   "id":   3806843,
                    //   "members": [
                    //        {
                    //          "type": "way",
                    //          "ref":  71002045,
                    //          "role": "outer"
                    //        },
                    //        {
                    //          "type": "way",
                    //          "ref":  286959663,
                    //          "role": "outer"
                    //        },
                    //        {
                    //          "type": "way",
                    //          "ref":  286959664,
                    //          "role": "outer"
                    //        },
                    //        {
                    //          "type": "way",
                    //          "ref":  286959641,
                    //          "role": "outer"
                    //        }
                    //   ],
                    //   "tags": {
                    //       "landuse": "farm",
                    //       "type":    "multipolygon"
                    //   }
                    // }

                    if (element["type"].ToString() == "relation")
                    {

                        Relation = Relation.Parse(element,
                                                  NodeResolver: nodeId => Nodes[nodeId],
                                                  WayResolver:  wayId  => Ways [wayId]);

                        if (Relations.ContainsKey(Relation.Id))
                            Console.WriteLine("Duplicate relation id detected!");
                        else
                            Relations.Add(Relation.Id, Relation);

                    }

                }

                #endregion

                // {
                //    "type":      "FeatureCollection",
                //    "generator": "overpass-turbo",
                //    "copyright": "The data included in this document is from www.openstreetmap.org. The data is made available under ODbL.",
                //    "timestamp": "2014-11-29T23:08:02Z",
                //    "features": [ ]
                // }

                List<Feature> features = Nodes.Values.Where(n => n.Tags.Count > 0).Select(n => n.ToGeoJSON())
                                        .Concat(Ways.Values.Where(n => n.Tags.Count > 0).Select(n => n.ToGeoJSON()))
                                        .Concat(Relations.Values.Select(n => n.ToGeoJSON()))
                                        .ToList();


                var featureCollection = new FeatureCollection(features);

                return featureCollection;

            });

        }

        #endregion


        #region ToGeoJSONFile(this OverpassQuery, Filename)

        /// <summary>
        /// Run the given Overpass query, cenvert the result to GeoJSON and write it to the given file.
        /// </summary>
        /// <param name="OverpassQuery">An Overpass query.</param>
        /// <param name="Filename">A file name.</param>
        public static Task<FeatureCollection> ToGeoJSONFile(this OverpassQuery OverpassQuery, String Filename)
        {

            return OverpassQuery.
                       RunQuery().
                       ToGeoJSONFile(Filename);

        }

        #endregion

        #region ToGeoJSONFile(this ResultTask, Filename)

        /// <summary>
        /// Convert the given Overpass query result to GeoJSON and write it to the given file.
        /// </summary>
        /// <param name="ResultTask">A Overpass query result task.</param>
        /// <param name="Filename">A file name.</param>
        public static Task<FeatureCollection> ToGeoJSONFile(this Task<OverpassResult>  ResultTask,
                                                  String                     Filename)
        {
            return ResultTask.ToGeoJSON().ToFile(Filename);
        }

        #endregion

        #region ToGeoJSONFile(this ResultTask, FilenameBuilder)

        /// <summary>
        /// Convert the given Overpass query result to GeoJSON and write it to the given file.
        /// </summary>
        /// <param name="ResultTask">A Overpass query result task.</param>
        /// <param name="FilenameBuilder">A file name.</param>
        public static Task<FeatureCollection> ToGeoJSONFile(this Task<OverpassResult>  ResultTask,
                                                  Func<FeatureCollection, String>      FilenameBuilder)
        {
            return ResultTask.ToGeoJSON().ContinueWith(t1 => t1.ToFile(FilenameBuilder(t1.Result)).Result);
        }

        #endregion

        #region ToGeoJSONFile(this JSONTask)

        /// <summary>
        /// Write the given GeoJSON it to the given file.
        /// </summary>
        /// <param name="JSONTask">A GeoJSON task.</param>
        /// <param name="Filename">A file name.</param>
        public static Task<JObject> ToGeoJSONFile(this Task<JObject>  JSONTask,
                                                  String              Filename)
        {
            return JSONTask.ToFile(Filename);
        }

        #endregion

        #region ToGeoJSONFile(this JSONTask)

        /// <summary>
        /// Write the given GeoJSON it to the given file.
        /// </summary>
        /// <param name="JSONTask">A GeoJSON task.</param>
        /// <param name="FilenameBuilder">A file name.</param>
        public static Task<JObject> ToGeoJSONFile(this Task<JObject>     JSONTask,
                                                  Func<JObject, String>  FilenameBuilder)
        {
            return JSONTask.ContinueWith(t1 => t1.ToFile(FilenameBuilder(t1.Result)).Result);
        }

        #endregion


        #region ToGeoJSONFile(this GeoJSONTask, FilenameBuilder)

        /// <summary>
        /// Write the given GeoJSON it to the given file.
        /// </summary>
        /// <param name="GeoJSONTask">A GeoJSON task.</param>
        /// <param name="FilenameBuilder">A file name.</param>
        public static Task<IEnumerable<JObject>> ToGeoJSONFile(this Task<IEnumerable<JObject>>  GeoJSONTask,
                                                               Func<JObject, String>            FilenameBuilder)
        {
            return GeoJSONTask.ContinueWith(GeoJSONTasks => GeoJSONTasks.Result.Select(GeoJSON => GeoJSON.ToFile(FilenameBuilder)));
        }

        #endregion



        #region ToGeoJSON(this Node)

        /// <summary>
        /// Convert the given OSM node to a GeoJSON point feature.
        /// </summary>
        /// <param name="Node">An OSM node.</param>
        public static Feature ToGeoJSON(this Node Node)
        {

            // {
            //     "type":  "Feature",
            //     "id":    "node/35304749",
            //     "properties": {
            //         "@id":         "node/35304749",
            //         "highway":     "bus_stop",
            //         "name":        "Lobeda",
            //         "operator":    "JES",
            //         "wheelchair":  "yes"
            //     },
            //     "geometry": {
            //         "type":        "Point",
            //         "coordinates": [ 11.6023278, 50.8926376 ]
            //     }
            // }

            var id = string.Concat("node/", Node.Id);
            Dictionary<string, object> props = new Dictionary<string, object>();
            props.Add("@id", id);
            foreach(var tag in Node.Tags)
            {
                props.Add(tag.Key, tag.Value);
            }
            var feature = new Feature(new Point(new Position(Node.Latitude, Node.Longitude)), props, id);

            return feature;

        }

        #endregion



        #region ToGeoJSON(this Way)

        /// <summary>
        /// Convert the given OSM way to a GeoJSON line feature.
        /// </summary>
        /// <param name="Way">An OSM way.</param>
        public static Feature ToGeoJSON(this Way Way)
        {

            // {
            //     "type":  "Feature",
            //     "id":    "way/305352912",
            //     "properties": {
            //         "@id":         "way/305352912",
            //     },
            //     "geometry": {
            //         "type":        "LineString",
            //         "coordinates": [ [ 11.6023278, 50.8926376 ], [ 11.5054540, 50.7980146 ], [ 11.6023278, 50.8926376 ] ]
            //     }
            // }

            // https://wiki.openstreetmap.org/wiki/Overpass_turbo/Polygon_Features

            var FirstNode = Way.Nodes.First();
            var LastNode  = Way.Nodes.Last();
            var isClosed  = FirstNode.Latitude == LastNode.Latitude && FirstNode.Longitude == LastNode.Longitude;

            var id = string.Concat("way/", Way.Id);
            Dictionary<string, object> props = new Dictionary<string, object>();
            props.Add("@id", id);
            foreach (var tag in Way.Tags)
            {
                props.Add(tag.Key, tag.Value);
            }
            IGeometryObject geometry;
            var ring = new LineString(Way.Nodes.Select(n => new Position(n.Latitude, n.Longitude)));
            if (isClosed)
            {
                geometry = new Polygon(Enumerable.Range(1, 1).Select(_ => ring));
            }
            else
            {

                geometry = ring;
            }
            var feature = new Feature(geometry, props, id);

            return feature;

        }

        #endregion

        #region ToGeoJSON(this Relation)

        /// <summary>
        /// Convert the given OSM relation to a GeoJSON line feature.
        /// </summary>
        /// <param name="Relation">An OSM relation.</param>
        public static Feature ToGeoJSON(this Relation Relation)
        {
            // {
            //     "type":  "Feature",
            //     "id":    "way/305352912",
            //     "properties": {
            //         "@id":         "way/305352912",
            //     },
            //     "geometry": {
            //         "type":        "LineString",
            //         "coordinates": [ [ 11.6023278, 50.8926376 ], [ 11.5054540, 50.7980146 ], [ 11.6023278, 50.8926376 ] ]
            //     }
            // }

            // https://wiki.openstreetmap.org/wiki/Overpass_turbo/Polygon_Features

            // 1) Combine all ways into a single big list of geo coordinate (it's a puzzle! Some ways must be reversed in order to find matches!)
            // 2) Check if first geo coordinate is the same as the last
            // 3) If yes => polygon (exceptions see wiki link)


            // Relation.Ways.Select(Way => new JArray(Way.Nodes.Select(Node => new JArray(Node.Longitude, Node.Latitude))))

            var RemainingGeoFeatures  = Relation.Ways.Select(Way => new GeoFeature(Way.Nodes.Select(Node => new Position(Node.Latitude, Node.Longitude)))).ToList();
            var ResultList            = new List<GeoFeature>();

            Byte            Found = 0;
            GeoFeature      CurrentGeoFeature;

            if (Relation.Id == 3484638)
            {
            }

            //if (Relation.Tags["type"].ToString() != "multipolygon")
            //{
            //    Console.WriteLine("Broken OSM multipolygon relation found!");
            //}

            do
            {

                CurrentGeoFeature = RemainingGeoFeatures.RemoveAndReturnFirst();

                // The current geo feature is closed -> a polygon!
                if ((!Relation.Tags.ContainsKey("type") || Relation.Tags["type"].ToString()         != "route") &&
                    CurrentGeoFeature.GeoCoordinates.First() == CurrentGeoFeature.GeoCoordinates.Last())
                {
                    CurrentGeoFeature.Type =  GeoJSONObjectType.Polygon;
                    ResultList.Add(CurrentGeoFeature);
                }

                // The current geo feature is not closed
                // Try to extend the geo feature by finding fitting other geo features
                else
                {

                    do
                    {

                        Found = 0;

                        foreach (var AdditionalPath in RemainingGeoFeatures)
                        {

                            if (AdditionalPath.GeoCoordinates.First() == CurrentGeoFeature.GeoCoordinates.Last())
                            {
                                RemainingGeoFeatures.Remove(AdditionalPath);
                                // Skip first GeoCoordinate as it is redundant!
                                CurrentGeoFeature.GeoCoordinates.AddRange(AdditionalPath.GeoCoordinates);//.Skip(1));
                                Found = 1;
                                break;
                            }

                            else if (AdditionalPath.GeoCoordinates.Last() == CurrentGeoFeature.GeoCoordinates.Last())
                            {
                                RemainingGeoFeatures.Remove(AdditionalPath);
                                // Skip first GeoCoordinate as it is redundant!
                                CurrentGeoFeature.GeoCoordinates.AddRange(AdditionalPath.GeoCoordinates.ReverseAndReturn());//.Skip(1));
                                Found = 1;
                                break;
                            }

                            else if (AdditionalPath.GeoCoordinates.First() == CurrentGeoFeature.GeoCoordinates.First())
                            {
                                RemainingGeoFeatures.Remove(AdditionalPath);
                                CurrentGeoFeature.GeoCoordinates.Reverse();
                                // Skip first GeoCoordinate as it is redundant!
                                CurrentGeoFeature.GeoCoordinates.AddRange(AdditionalPath.GeoCoordinates);//.Skip(1));
                                Found = 1;
                                break;
                            }

                            else if (AdditionalPath.GeoCoordinates.Last() == CurrentGeoFeature.GeoCoordinates.First())
                            {
                                RemainingGeoFeatures.Remove(AdditionalPath);
                                CurrentGeoFeature.GeoCoordinates.Reverse();
                                // Skip first GeoCoordinate as it is redundant!
                                CurrentGeoFeature.GeoCoordinates.AddRange(AdditionalPath.GeoCoordinates.ReverseAndReturn());//.Skip(1));
                                Found = 1;
                                break;
                            }

                        }

                    } while (RemainingGeoFeatures.Count > 0 && Found > 0);

                    CurrentGeoFeature.Type  = (Relation.Tags["type"].ToString()         != "route" &&
                                               CurrentGeoFeature.GeoCoordinates.First() == CurrentGeoFeature.GeoCoordinates.Last())
                                                  ? GeoJSONObjectType.Polygon
                                                  : GeoJSONObjectType.LineString;

                    ResultList.Add(CurrentGeoFeature);

                }

            } while (RemainingGeoFeatures.Count > 0);


            IGeometryObject geometry = null;

            if (ResultList.Count == 1)
            {
                var ring = new LineString(CurrentGeoFeature.GeoCoordinates.Select(c => new Position(c.Latitude, c.Longitude)));
                if (ResultList.First().Type == GeoJSONObjectType.Polygon)
                {
                    geometry = new Polygon(Enumerable.Range(1, 1).Select(_ => ring));
                }                  
                else
                {
                    geometry = ring;
                }
            }
            else
            {
                var multiRing = ResultList.Select(g => new LineString(g.GeoCoordinates.Select(c => new Position(c.Latitude, c.Longitude))));

                if (ResultList.First().Type == GeoJSONObjectType.Polygon)
                {
                    geometry = new Polygon(multiRing);
                }
                else
                {
                    geometry = new MultiLineString(multiRing);
                }
            }

            var id = string.Concat("relation/", Relation.Id);
            Dictionary<string, object> props = new Dictionary<string, object>();
            props.Add("@id", id);
            foreach (var tag in Relation.Tags)
            {
                props.Add(tag.Key, tag.Value);
            }
            var feature = new Feature(geometry, props, id);

            return feature;

        }

        #endregion



        

        

    }

}
