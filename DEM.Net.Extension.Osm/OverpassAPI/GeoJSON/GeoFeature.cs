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
using GeoJSON.Net.Geometry;
using GeoJSON.Net;


#endregion

namespace DEM.Net.Extension.Osm.OverpassAPI
{

    /// <summary>
    /// A geo feature.
    /// </summary>
    public struct GeoFeature
    {

        #region Data

        /// <summary>
        /// A list of geo coordinates.
        /// </summary>
        public readonly List<Position> GeoCoordinates;

        /// <summary>
        /// A geo latitude.
        /// </summary>
        public GeoJSONObjectType Type;

        #endregion

        #region GeoFeature(GeoCoordinates, Type = GeoType.undefined)

        /// <summary>
        /// Create a new geo feature.
        /// </summary>
        /// <param name="_GeoCoordinates">A list of geo coordinates.</param>
        /// <param name="_Type">The type of the feature.</param>
        public GeoFeature(IEnumerable<Position>  _GeoCoordinates,
                          GeoJSONObjectType _Type = GeoJSONObjectType.Point)
        {
            GeoCoordinates  = new List<Position>(_GeoCoordinates);
            Type            = _Type;
        }

        #endregion



        #region GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
        {
            return GeoCoordinates.GetHashCode() ^ Type.GetHashCode();
        }

        #endregion

        #region ToString()

        /// <summary>
        /// Returns a formated string representation of this object.
        /// </summary>
        /// <returns>A formated string representation of this object</returns>
        public override String ToString()
        {
            return Type.ToString();
        }

        #endregion

    }

}
