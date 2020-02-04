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
    /// A OSM way.
    /// </summary>
    public class Way
    {

        #region Properties

        #region Id

        private readonly UInt64 _Id;

        /// <summary>
        /// The identification of an OSM way.
        /// </summary>
        public UInt64 Id
        {
            get
            {
                return _Id;
            }
        }

        #endregion

        #region Nodes

        private readonly List<Node> _Nodes;

        /// <summary>
        /// The nodes of an OSM way.
        /// </summary>
        public List<Node> Nodes
        {
            get
            {
                return _Nodes;
            }
        }

        #endregion

        #region Tags

        private readonly Dictionary<String, String> _Tags;

        /// <summary>
        /// The tags of an OSM way.
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
        /// Create a new OSM way.
        /// </summary>
        /// <param name="Id">The identification of an OSM way.</param>
        /// <param name="Nodes">An optional list of OSM nodes.</param>
        /// <param name="Tags">Optional tags for this OSM way.</param>
        public Way(UInt64                                     Id,
                   IEnumerable<Node>                          Nodes,
                   IEnumerable<KeyValuePair<String, String>>  Tags = null)
        {

            this._Id         = Id;

            this._Nodes      = Nodes != null
                                   ? new List<Node>(Nodes)
                                   : new List<Node>();

            this._Tags       = Tags  != null
                                   ? Tags.ToDictionary(kvp => kvp.Key,
                                                       kvp => kvp.Value)
                                   : new Dictionary<String, String>();

        }

        #endregion


        #region (static) Parse(JSON, NodeResolver)

        /// <summary>
        /// Parse the given OSM JSON way.
        /// </summary>
        /// <param name="JSON">Some JSON.</param>
        /// <param name="NodeResolver">A delegate to resolve OSM nodes.</param>
        public static Way Parse(JObject             JSON,
                                Func<UInt64, Node>  NodeResolver)
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

            return new Way(UInt64.Parse(JSON["id"]. ToString()),

                           JSON["nodes"] != null
                                        ? JSON["nodes"].Values().Select(v => NodeResolver(UInt64.Parse(v.ToString())))
                                        : null,

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
            return String.Concat("way(", Id, ") with ", Nodes.Count, " nodes", (Tags.Count > 0 ? " and " + Tags.Count + " tags" : ""));
        }

        #endregion

    }

}
