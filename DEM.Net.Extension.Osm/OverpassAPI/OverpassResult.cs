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
using DEM.Net.Extension.Osm;


#endregion

namespace DEM.Net.Extension.Osm.OverpassAPI
{

    /// <summary>
    /// The JSON result of an Overpass query.
    /// </summary>
    public struct OverpassResult
    {

        #region API Result

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

        #endregion

        #region Data

        /// <summary>
        /// The version of the result data structure.
        /// </summary>
        public readonly String                  Version;

        /// <summary>
        /// The generator of the result.
        /// </summary>
        public readonly String                  Generator;

        /// <summary>
        /// OSM timestamp.
        /// </summary>
        public readonly DateTime                timestamp_osm_base;

        /// <summary>
        /// Area timestamp.
        /// </summary>
        public readonly DateTime?               timestamp_areas_base;

        /// <summary>
        /// The copyright of the result.
        /// </summary>
        public readonly String                  Copyright;

        /// <summary>
        /// The enumeration of results.
        /// </summary>
        public readonly IEnumerable<JObject>    Elements;

        /// <summary>
        /// The original query.
        /// </summary>
        public readonly OverpassQuery           Query;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new Overpass query result.
        /// </summary>
        /// <param name="OverpassQuery">The original Overpass query.</param>
        /// <param name="Result">An OverpassAPI result.</param>
        public OverpassResult(OverpassQuery  OverpassQuery,
                              JObject        Result)
        {
            Query                 = OverpassQuery;
            Version               = Result["version"].  ToString();
            Generator             = Result["generator"].ToString();
            Copyright             = Result["osm3s"]["copyright"].ToString();
            timestamp_osm_base    = DateTime.Parse(Result["osm3s"]["timestamp_osm_base"].ToString());
            timestamp_areas_base  = Result["osm3s"]["timestamp_areas_base"] != null ? new Nullable<DateTime>(DateTime.Parse(Result["osm3s"]["timestamp_areas_base"].ToString())) : null;
            Elements              = Result["elements"]. Children<JObject>().ToArray();
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return this Overpass result as JSON object.
        /// </summary>
        /// <returns></returns>
        public JObject ToJSON()
        {

            return new JObject(new JProperty("version",    Version),
                               new JProperty("generator",  Generator),
                               new JProperty("osm3s", new JObject(
                                   new JProperty[] {
                                       new JProperty("timestamp_osm_base",    timestamp_osm_base.ToIso8601()),
                                       timestamp_areas_base.HasValue
                                           ? new JProperty("timestamp_areas_base",  timestamp_areas_base.Value.ToIso8601())
                                           : null,
                                       new JProperty("copyright",             Copyright)
                                   }.Where(v => v != null).ToArray()
                               )),
                               new JProperty("elements",    new JArray(Elements)));

        }

        #endregion

    }

}
