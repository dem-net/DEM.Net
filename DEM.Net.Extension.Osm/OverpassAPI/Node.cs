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
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

#endregion

namespace DEM.Net.Extension.Osm.OverpassAPI
{

    /// <summary>
    /// A OSM node.
    /// </summary>
    public class Node
    {

        #region Properties

        #region Id

        private readonly UInt64 _Id;

        /// <summary>
        /// The identification of an OSM node.
        /// </summary>
        public UInt64 Id
        {
            get
            {
                return _Id;
            }
        }

        #endregion

        #region Latitude

        private readonly Double _Latitude;

        /// <summary>
        /// The latitude of an OSM node.
        /// </summary>
        public Double Latitude
        {
            get
            {
                return _Latitude;
            }
        }

        #endregion

        #region Longitude

        private readonly Double _Longitude;

        /// <summary>
        /// The longitude of an OSM node.
        /// </summary>
        public Double Longitude
        {
            get
            {
                return _Longitude;
            }
        }

        #endregion

        #region Tags

        private readonly Dictionary<String, String> _Tags;

        /// <summary>
        /// The tags of an OSM node.
        /// </summary>
        public Dictionary<String, String> Tags
        {
            get
            {
                return _Tags;
            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new OSM node.
        /// </summary>
        /// <param name="Id">The identification of an OSM node.</param>
        /// <param name="Latitude">The latitude of an OSM node.</param>
        /// <param name="Longitude">The longitude of an OSM node.</param>
        /// <param name="Tags">Optional tags for this OSM node.</param>
        public Node(UInt64                                     Id,
                    Double                                     Latitude,
                    Double                                     Longitude,
                    IEnumerable<KeyValuePair<String, String>>  Tags = null)
        {

            this._Id         = Id;
            this._Latitude   = Latitude;
            this._Longitude  = Longitude;

            this._Tags       = Tags != null
                                   ? Tags.ToDictionary(kvp => kvp.Key,
                                                       kvp => kvp.Value)
                                   : new Dictionary<String, String>();

        }

        #endregion


        #region (static) Parse(JSON)

        /// <summary>
        /// Parse the given OSM JSON node.
        /// </summary>
        /// <param name="JSON">Some JSON.</param>
        public static Node Parse(JObject JSON)
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

            return new Node(UInt64.Parse(JSON["id"]. ToString()),
                            Double.Parse(JSON["lat"].ToString()),
                            Double.Parse(JSON["lon"].ToString()),
                            JSON["tags"] != null
                                ? JSON["tags"].
                                      Children<JProperty>().
                                      Select(v => new KeyValuePair<String, String>(v.Name, v.Value.ToString()))
                                : null);

        }

        #endregion


        #region ToString()

        /// <summary>
        /// Return a string representation.
        /// </summary>
        public override String ToString()
        {
            return String.Concat("node(", Id, ") at ", Latitude, "/", Longitude, (Tags.Count > 0 ? " with " + Tags.Count + " tags" : ""));
        }

        #endregion

    }

}
