/*
 * Copyright (c) 2010-2014 Achim 'ahzf' Friedland <achim@graphdefined.org>
 * This file is part of Illias <http://www.github.com/Vanaheimr/Illias>
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
using System.Text;

#endregion

namespace DEM.Net.Extension.Osm
{

    /// <summary>
    /// Extensions to the DateTime class.
    /// </summary>
    public static class DateTimeExtensions
    {

        #region UnixEpoch

        /// <summary>
        /// The UNIX epoch.
        /// </summary>
        public static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0);

        #endregion

        #region ToUnixTimeStamp(this DateTime)

        /// <summary>
        /// Convert the given DateTime object to UNIX timestamp.
        /// </summary>
        /// <param name="Date">A DateTime object.</param>
        /// <returns>The seconds since 1. January 1970</returns>
        public static Int64 ToUnixTimeStamp(this DateTime DateTime)
        {
            return (Int64) Math.Round(DateTime.Subtract(UnixEpoch).TotalSeconds);
        }

        #endregion

        #region FromUnixTimeStamp(this UnixTimestamp)

        /// <summary>
        /// Convert the given UNIX timestamp to a .NET DateTime object.
        /// </summary>
        /// <param name="UnixTimestamp">A UNIX timestamp (seconds since 1. January 1970)</param>
        public static DateTime FromUnixTimeStamp(this Int64 UnixTimestamp)
        {
            return UnixEpoch.AddTicks(UnixTimestamp);
        }

        #endregion


        #region ToIso8601(this DateTime)

        /// <summary>
        /// Convert the given DateTime object to an ISO 8601 datetime string.
        /// </summary>
        /// <param name="DateTime">A DateTime object.</param>
        /// <returns>The DateTime formated as "yyyy-MM-ddTHH:mm:ss.fff" + "Z"</returns>
        public static String ToIso8601(this DateTime DateTime)
        {
            return DateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";
        }

        #endregion

    }

}
