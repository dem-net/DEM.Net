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
    /// A OSM relation.
    /// </summary>
    public class Relation
    {

        #region Properties

        #region Id

        private readonly UInt64 _Id;

        /// <summary>
        /// The identification of an OSM relation.
        /// </summary>
        public UInt64 Id
        {
            get
            {
                return _Id;
            }
        }

        #endregion

        public List<Member> Members { get; set; } = new List<Member>();

        #region Tags

        private readonly Dictionary<String, String> _Tags;

        /// <summary>
        /// The tags of an OSM relation.
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
        /// Create a new OSM relation.
        /// </summary>
        /// <param name="Id">The identification of an OSM relation.</param>
        /// <param name="Nodes">Optional nodes for this OSM relation.</param>
        /// <param name="Ways">Optional ways for this OSM relation.</param>
        /// <param name="Tags">Optional tags for this OSM relation.</param>
        public Relation(UInt64                                     Id,
                        IEnumerable<Member>                        Members,
                        IEnumerable<KeyValuePair<String, String>>  Tags = null)
        {

            this._Id         = Id;


            this.Members = Members != null
                                   ? new List<Member>(Members)
                                   : new List<Member>();

            this._Tags       = Tags != null
                                   ? Tags.ToDictionary(kvp => kvp.Key,
                                                       kvp => kvp.Value)
                                   : new Dictionary<String, String>();

        }

        #endregion


        #region (static) Parse(JSON, NodeResolver, WayResolver)

        /// <summary>
        /// Parse the given OSM JSON relation.
        /// </summary>
        /// <param name="JSON">Some JSON.</param>
        /// <param name="NodeResolver">A delegate to resolve OSM nodes.</param>
        /// <param name="WayResolver">A delegate to resolve OSM ways.</param>
        public static Relation Parse(JObject             JSON,
                                     Func<UInt64, Node>  NodeResolver,
                                     Func<UInt64, Way>   WayResolver)
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

            var r =  new Relation(UInt64.Parse(JSON["id"].ToString()),

                                JSON["members"] != null
                                             ? JSON["members"].Children<JObject>().Select(v =>
                                             {
                                                 if (v["type"].ToString() == "node")
                                                     return new Member() { Role = v["role"].ToString(), Node = NodeResolver(UInt64.Parse(v["ref"].ToString())) };
                                                 else
                                                     return new Member() { Role = v["role"].ToString(), Way = WayResolver(UInt64.Parse(v["ref"].ToString())) };
                                             })
                                             : null);

            return r;

        }

        #endregion


        #region ToString()

        /// <summary>
        /// Return a string representation.
        /// </summary>
        public override String ToString()
        {
            return String.Concat("relation(", Id, ")", (Tags.Count > 0 ? " with " + Tags.Count + " tags" : ""));
        }

        #endregion

    }

    public class Member
    {
        public string Role { get; set; }
        public Node Node { get; set; }
        public Way Way { get; set; }

        public override string ToString()
        {
            return Node != null ? $"Node {Node.Id} with role {Role}"
                : $"Way {Way.Id} with role {Role}";
        }
    }

}
