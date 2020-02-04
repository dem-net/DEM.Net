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
using System.Linq;
using System.Collections.Generic;

#endregion

namespace DEM.Net.Extension.Osm
{

    /// <summary>
    /// Extension methods for the IList interface.
    /// </summary>
    public static class IListExtensions
    {

        /// <summary>
        /// Reverse and return the given list;
        /// </summary>
        /// <param name="List">A list of elements.</param>
        public static IEnumerable<T> ReverseAndReturn<T>(this IList<T> List)
        {
            return List.Reverse();
        }


        /// <summary>
        /// Remove and return first element of the given list;
        /// </summary>
        /// <param name="List">A list of elements.</param>
        public static T RemoveAndReturnFirst<T>(this IList<T> List)
        {

            var Element = List.First();
            List.Remove(Element);

            return Element;

        }


    }

}
