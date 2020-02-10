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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using System.Threading;
using DEM.Net.Core;
using System.Globalization;

#endregion

namespace DEM.Net.Extension.Osm.OverpassAPI
{

    /// <summary>
    /// A query against an Overpass API.
    /// </summary>
    public class OverpassQuery
    {

        #region Documentation

        // http://wiki.openstreetmap.org/wiki/Overpass_API/Overpass_QL

        // A Overpass query example...
        // 
        // [out:json]
        // [timeout:100];
        // area($areaId)->.searchArea;
        // (
        //   node     ["leisure"]            (area.searchArea);
        //   way      ["waterway" = "river"] (area.searchArea);
        //   relation ["leisure"]            (area.searchArea);
        // );
        // out body;
        // >;
        // out skel qt;

        #endregion

        #region Data

        /// <summary>
        /// The URI of the OverpassAPI.
        /// </summary>
        //public static readonly Uri OverpassAPI_URI = new Uri("http://overpass-api.de/api/interpreter");
        public static readonly Uri OverpassAPI_URI = new Uri("http://overpass.openstreetmap.fr/api/interpreter");


        /// <summary>
        /// The URI of the NominatimAPI.
        /// </summary>
        public static readonly Uri NominatimAPI_URI = new Uri("http://nominatim.openstreetmap.org/search");


        private QueryContext CurrentContext;

        private List<List<KeyValuePair<String, String>>> Nodes;
        private List<List<KeyValuePair<String, String>>> Ways;
        private List<List<KeyValuePair<String, String>>> Relations;
        private List<String> NodesRelations;

        #endregion

        #region Properties

        #region AreaId

        private UInt64 _AreaId;
        private BoundingBox _BBox;
        private string _Filter;

        /// <summary>
        /// The area identification used for this query.
        /// </summary>
        public UInt64 AreaId
        {
            get
            {
                return _AreaId;
            }
        }


        /// <summary>
        /// The bounding box filter used for this query
        /// </summary>
        public BoundingBox BoundingBox
        {
            get
            {
                return _BBox;
            }
        }

        #endregion

        #region QueryTimeout

        private UInt32 _QueryTimeout = 100;

        /// <summary>
        /// The timeout of the query.
        /// </summary>
        public UInt32 QueryTimeout
        {
            get
            {
                return _QueryTimeout;
            }
        }

        #endregion

        #endregion

        #region (private, enum) QueryContext

        /// <summary>
        /// The current query state which allows to narrow down a query using AND.
        /// </summary>
        private enum QueryContext
        {

            /// <summary>
            /// Illegal state.
            /// </summary>
            none,

            /// <summary>
            /// Query nodes.
            /// </summary>
            Nodes,

            /// <summary>
            /// Query ways.
            /// </summary>
            Ways,

            /// <summary>
            /// Query relations.
            /// </summary>
            Relations,

            /// <summary>
            /// Query nodes, ways and relations.
            /// </summary>
            Any

        }

        #endregion

        #region Constructor(s)

        #region OverpassQuery()

        /// <summary>
        /// Create a new OverpassQuery.
        /// </summary>
        public OverpassQuery()
        {

            _AreaId = 0;
            _BBox = null;

            CurrentContext = QueryContext.none;

            Nodes = new List<List<KeyValuePair<String, String>>>();
            Ways = new List<List<KeyValuePair<String, String>>>();
            Relations = new List<List<KeyValuePair<String, String>>>();
            NodesRelations = new List<string>();

        }

        #endregion

        #region OverpassQuery(AreaId)

        /// <summary>
        /// Create a new OverpassQuery for the given area identification.
        /// </summary>
        /// <param name="AreaId">An area reference using an OpenStreetMap area identification.</param>
        public OverpassQuery(UInt64 AreaId)
            : this()
        {
            InArea(AreaId);
        }


        #endregion

        #region OverpassQuery(AreaName)

        /// <summary>
        /// Create a new OverpassQuery for the given area name.
        /// </summary>
        /// <param name="AreaName">an area reference using an OpenStreetMap area name. This will search for the given name via the Nominatim API and use the first matching result (normally this is the result having the highest importance).</param>
        public OverpassQuery(String AreaName)
            : this()
        {
            InArea(AreaName);
        }
        public OverpassQuery(BoundingBox bbox)
            : this()
        {
            InBBox(bbox);
        }

        #endregion

        #endregion


        #region InArea(AreaId)

        /// <summary>
        /// Add an area reference using an OpenStreetMap area identification.
        /// </summary>
        /// <param name="AreaId">A OpenStreetMap area identification</param>
        public OverpassQuery InArea(UInt64 AreaId)
        {
            this._AreaId = AreaId;
            return this;
        }

        #endregion

        #region SelectFilter(filter)

        /// <summary>
        /// Add an area reference using an OpenStreetMap area identification.
        /// </summary>
        /// <param name="AreaId">A OpenStreetMap area identification</param>
        public OverpassQuery SelectFilter(string filter)
        {
            this._Filter = filter;
            return this;
        }

        #endregion

        #region InArea(AreaName)

        /// <summary>
        /// Add an area reference using an OpenStreetMap area name.
        /// This will search for the given name via the Nominatim API and use the first matching result (normally this is the result having the highest importance).
        /// </summary>
        /// <param name="AreaName">A OpenStreetMap area name</param>
        public OverpassQuery InArea(String AreaName)
        {

            using (var HTTPClient = new HttpClient())
            {

                try
                {
                    HTTPClient.DefaultRequestHeaders.Referrer = new Uri("https://elevationapi.com");

                    // Note: This query currently does not support to narrow down the results to be of "osm_type = relation".
                    //       Therefore we query up to 100 results and hope that at least one will be of this relation type.
                    using (var ResponseMessage = HTTPClient.GetAsync(NominatimAPI_URI + "/" + AreaName + "?format=json&addressdetails=1&limit=100"))
                    {
                        ResponseMessage.Wait();

                        if (ResponseMessage.Result.StatusCode == HttpStatusCode.OK)
                        {

                            using (var ResponseContent = ResponseMessage.Result.Content)
                            {

                                var result = ResponseContent.ReadAsStringAsync();

                                // [
                                //    {
                                //        "place_id:        "158729066",
                                //        "licence:         "Data © OpenStreetMap contributors, ODbL 1.0. http://www.openstreetmap.org/copyright",
                                //        "osm_type:        "relation",
                                //        "osm_id: "        62693",
                                //        "boundingbox: [
                                //              "50.856077",
                                //              "50.988898",
                                //              "11.4989589",
                                //              "11.6728014"
                                //        "],
                                //        "lat:             "50.9221871",
                                //        "lon:             "11.5888846280636",
                                //        "display_name:    "Jena, Thüringen, Deutschland",
                                //        "class:           "boundary",
                                //        "type:            "administrative",
                                //        "importance:      0.72701320621596,
                                //        "icon:            "http://nominatim.openstreetmap.org/images/mapicons/poi_boundary_administrative.p.20.png",
                                //        "address: {
                                //              "county:          "Jena",
                                //              "state:           "Thüringen",
                                //              "country:         "Deutschland",
                                //              "country_code:    "de"
                                //        }
                                //    }
                                // ]
                                var JSON = JArray.Parse(result.Result).
                                               Children<JObject>().
                                               Where(JSONObject => JSONObject["osm_type"].ToString() == "relation").
                                               FirstOrDefault();

                                // https://wiki.openstreetmap.org/wiki/Overpass_API/Overpass_QL#
                                //
                                //By convention the area id can be calculated from an existing OSM way 
                                // by adding 2400000000 to its OSM id, 
                                // or in case of a relation by adding 3600000000 respectively

                                if (JSON != null)
                                    this._AreaId = UInt64.Parse(JSON["osm_id"].ToString()) + 3600000000;

                                return this;

                            }

                        }

                    }

                }

                catch (OperationCanceledException)
                { }

            }

            throw new Exception();

        }

        #endregion

        #region InBBox(bbox)

        public OverpassQuery InBBox(BoundingBox bbox)
        {
            this._BBox = bbox;
            return this;
        }

        #endregion


        #region WithAny(Type, Value = "")

        /// <summary>
        /// Query nodes, ways and relations.
        /// </summary>
        /// <param name="Type">The key to search for.</param>
        /// <param name="Value">The value to search for.</param>
        public OverpassQuery WithAny(String Type, String Value = "")
        {

            Nodes.Add(new List<KeyValuePair<String, String>>() { new KeyValuePair<String, String>(Type, Value) });
            Ways.Add(new List<KeyValuePair<String, String>>() { new KeyValuePair<String, String>(Type, Value) });
            Relations.Add(new List<KeyValuePair<String, String>>() { new KeyValuePair<String, String>(Type, Value) });

            CurrentContext = QueryContext.Any;

            return this;

        }

        #endregion

        #region WithNodes(NodeType, Value = "")

        /// <summary>
        /// Query nodes.
        /// </summary>
        /// <param name="NodeType">The key to search for.</param>
        /// <param name="Value">The value to search for.</param>
        public OverpassQuery WithNodes(String NodeType, String Value = "")
        {

            Nodes.Add(new List<KeyValuePair<String, String>>() { new KeyValuePair<String, String>(NodeType, Value) });

            CurrentContext = QueryContext.Nodes;

            return this;

        }
        public OverpassQuery WithNodesHavingRelation(String RelationType, String Value = "")
        {

            NodesRelations.Add(RelationType);

            CurrentContext = QueryContext.Nodes;

            return this;

        }
        #endregion

        #region WithWays(WayType, Value = "")

        /// <summary>
        /// Query OSM ways.
        /// </summary>
        /// <param name="WayType">The key to search for.</param>
        /// <param name="Value">The value to search for.</param>
        public OverpassQuery WithWays(String WayType, String Value = "")
        {

            Ways.Add(new List<KeyValuePair<String, String>>() { new KeyValuePair<String, String>(WayType, Value) });

            CurrentContext = QueryContext.Ways;

            return this;

        }

        #endregion

        #region WithRelations(RelationType, Value = "")

        /// <summary>
        /// Query OSM relations.
        /// </summary>
        /// <param name="RelationType">The key to search for.</param>
        /// <param name="Value">The value to search for.</param>
        public OverpassQuery WithRelations(String RelationType, String Value = "")
        {

            Relations.Add(new List<KeyValuePair<String, String>>() { new KeyValuePair<String, String>(RelationType, Value) });

            CurrentContext = QueryContext.Relations;

            return this;

        }

        #endregion

        #region And(Type, Value = "")

        /// <summary>
        /// Query the current context. This allows you to chain a query via AND.
        /// </summary>
        /// <param name="Type">The key to search for.</param>
        /// <param name="Value">The value to search for.</param>
        public OverpassQuery And(String Type, String Value = "")
        {

            switch (CurrentContext)
            {

                case QueryContext.none: throw new Exception("Bad request!");

                case QueryContext.Nodes: Nodes.Last().Add(new KeyValuePair<String, String>(Type, Value)); break;
                case QueryContext.Ways: Ways.Last().Add(new KeyValuePair<String, String>(Type, Value)); break;
                case QueryContext.Relations: Relations.Last().Add(new KeyValuePair<String, String>(Type, Value)); break;

                case QueryContext.Any:
                    Nodes.Last().Add(new KeyValuePair<String, String>(Type, Value));
                    Ways.Last().Add(new KeyValuePair<String, String>(Type, Value));
                    Relations.Last().Add(new KeyValuePair<String, String>(Type, Value)); break;

            }

            return this;

        }

        #endregion


        #region SetTimeout(Timeout)

        /// <summary>
        /// Set the query timeout.
        /// </summary>
        /// <param name="Timeout">The timeout value.</param>
        public OverpassQuery SetTimeout(UInt32 Timeout)
        {

            if (Timeout > 0)
                _QueryTimeout = Timeout;

            return this;

        }

        #endregion

        #region RunQuery(Timeout = 0)

        /// <summary>
        /// Execute this Overpass query.
        /// </summary>
        /// <returns>A Overpass query result.</returns>
        public async Task<OverpassResult> RunQuery(UInt32 Timeout = 0)
        {

            if (Timeout > 0)
                _QueryTimeout = Timeout;

            using (var HTTPClient = new HttpClient())
            {

                try
                {
                    string queryBody = this.ToString();
                    using (var ResponseMessage = await HTTPClient.PostAsync(OverpassAPI_URI, new StringContent(queryBody)))
                    {

                        if (ResponseMessage.StatusCode == HttpStatusCode.OK)
                        {

                            using (var ResponseContent = ResponseMessage.Content)
                            {

                                // {
                                //   "version": 0.6,
                                //   "generator": "Overpass API",
                                //   "osm3s": {
                                //     "timestamp_osm_base":    "2014-11-29T20:02:02Z",
                                //     "timestamp_areas_base":  "2014-11-29T08:42:02Z",
                                //     "copyright":             "The data included in this document is from www.openstreetmap.org. The data is made available under ODbL."
                                //   },
                                //   "elements": [
                                //                   {
                                //                     "type": "node",
                                //                     "id": 1875593753,
                                //                     "lat": 50.9292604,
                                //                     "lon": 11.5824008,
                                //                     "tags": {
                                //                       "addr:city": "Jena",
                                //                       "addr:housenumber": "26",
                                //                       "addr:postcode": "07743",
                                //                       "addr:street": "Krautgasse",
                                //                       "amenity": "community_centre",
                                //                       "building:level": "1",
                                //                       "club": "it",
                                //                       "contact:phone": "0162/6318746",
                                //                       "contact:website": "https://www.krautspace.de",
                                //                       "drink:club-mate": "yes",
                                //                       "leisure": "hackerspace",
                                //                       "name": "Krautspace",
                                //                       "office": "club",
                                //                       "operator": "Hackspace Jena e.V."
                                //                     }
                                //                   }
                                //               ]
                                // }

                                return await ResponseContent.
                                                 ReadAsStringAsync().
                                                 ContinueWith(QueryTask => new OverpassResult(this,
                                                                                              JObject.Parse(QueryTask.Result)));

                            }

                        }

                        else if (ResponseMessage.StatusCode == HttpStatusCode.BadRequest)
                        {
                            var message = await ResponseMessage.Content.ReadAsStringAsync();
                            throw new Exception("Bad request: " + message);
                        }


                        else if (((Int32)ResponseMessage.StatusCode) == 429)
                            throw new Exception("Too Many Requests!");

                        else
                        {
                        }

                    }

                }

                catch (OperationCanceledException)
                { }

                catch (Exception e)
                {
                    throw new Exception("The OverpassQuery led to an error!", e);
                }

            }

            throw new Exception("General HTTP client error!");

        }

        #endregion


        #region ClearAll()

        /// <summary>
        /// Clear all internal node, way and relation queries and the area identification.
        /// </summary>
        public OverpassQuery ClearAll()
        {

            _AreaId = 0;

            Nodes.Clear();
            Ways.Clear();
            Relations.Clear();

            return this;

        }

        #endregion

        #region ClearAll_ExceptAreaId()

        /// <summary>
        /// Clear all internal node, way and relation queries without the area identification.
        /// </summary>
        public OverpassQuery ClearAll_ExceptAreaId()
        {

            Nodes.Clear();
            Ways.Clear();
            Relations.Clear();

            return this;

        }

        #endregion


        #region (private) FormatQuery(Type, Collection)

        /// <summary>
        /// Format the given query based on its type and collection.
        /// </summary>
        /// <param name="Type">The type of the query (node, way, relation).</param>
        /// <param name="Collection">The collection of query items.</param>
        private String FormatQuery(String Type,
                                   IEnumerable<KeyValuePair<String, String>> Collection)
        {

            string query = String.Concat(Type,
                                 Collection.Select(Item => String.Concat(@"[""", Item.Key, @"""", (Item.Value != "" ? @"=""" + Item.Value + @"""" : ""), "]")).
                                    Aggregate((a, b) => a + b));


            if (_AreaId > 0)
            {
                query = string.Concat(query, " (area.searchArea)");
            }
            if (_BBox != null)
            {
                query = string.Concat(query, " (", this.BboxAsOverpassString(_BBox), ")");
            }
            if (_Filter != null)
            {
                query = string.Concat(query, "->", _Filter);
            }

            query = string.Concat(query, ";");

            return query;

        }

        private string BboxAsOverpassString(BoundingBox bbox)
        {
            var str = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", bbox.yMin, bbox.xMin, bbox.yMax, bbox.xMax);
            return str;
        }

        #endregion

        #region ToString()

        /// <summary>
        /// Return a string representation of this object.
        /// </summary>
        public override String ToString()
        {

            var QueryString = new StringBuilder();

            QueryString.AppendLine("[out:json]");
            QueryString.AppendLine("[timeout:" + _QueryTimeout + "];");

            if (_AreaId > 0)
            {

                if (NodesRelations.Count == 0)
                {
                    QueryString.AppendLine("area(" + _AreaId.ToString() + ")->.searchArea;");
                }
                else
                {
                    QueryString.AppendLine($"area({_AreaId});");
                    QueryString.AppendLine("out body;");
                }
            }

            if (NodesRelations.Count == 0) QueryString.AppendLine("(");

            if (Nodes.Count > 0)
                Nodes.ForEach(Node => QueryString.AppendLine(FormatQuery("node", Node)));
            if (NodesRelations.Count > 0)
            {
                if (_AreaId > 0)
                {
                    QueryString.AppendLine($"rel(area)->.relations;");
                    QueryString.AppendLine($"(");
                }
                NodesRelations.ForEach(rel => QueryString.AppendLine(string.Concat("node(r.relations:", '"', rel, '"', ");")));
                if (_AreaId > 0)
                {
                    QueryString.AppendLine($");");
                }
            }

            if (Ways.Count > 0)
                Ways.ForEach(Way => QueryString.AppendLine(FormatQuery("way", Way)));

            if (Relations.Count > 0)
                Relations.ForEach(Relation => QueryString.AppendLine(FormatQuery("relation", Relation)));


            if (NodesRelations.Count == 0)
                QueryString.AppendLine(");");

            QueryString.AppendLine("out body;");
            QueryString.AppendLine(">;");
            QueryString.AppendLine("out skel qt;");

            var result = QueryString.ToString();
            return result;

        }

        #endregion

    }

}
