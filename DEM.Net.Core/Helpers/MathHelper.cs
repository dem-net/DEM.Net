// MathHelper.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    /// <summary>
    /// Various math extensions
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Linear interpolation between two values
        /// </summary>
        /// <param name="value1">First input value</param>
        /// <param name="value2">Second input value</param>
        /// <param name="amount">Amount of interpolation between 0 and 1.
        /// For any other value, this function maps then range 0->1 to value1->value2</param>
        /// <example>If 0 then <paramref name="value1"/> will be returned, if 1 then <paramref name="value2"/>will be returned.</example>
        /// <returns>Value in the units of value1 and value2 interpolated</returns>
        public static float Lerp(float value1, float value2, float amount)
        { return value1 + (value2 - value1) * amount; }

        /// <summary>
        /// Maps two ranges of values and interpolates.
        /// </summary>
        /// <param name="inputMin">Input minimum range</param>
        /// <param name="inputMax">Input maximum range</param>
        /// <param name="outputMin">Output range min value (ie: whats the destination minimal value)</param>
        /// <param name="outputMax">Output range max value (ie: whats the destination maximal value)</param>
        /// <param name="value">Interpolation amout in the units of <paramref name="inputMin"/> and <paramref name="inputMax"/>
        /// if <paramref name="inputMin"/>, then output will be <paramref name="outputMin"/>
        /// if <paramref name="inputMax"/>, then output will be <paramref name="outputMax"/></param>
        /// <param name="clamp"></param>
        /// <returns></returns>
        public static float Map(float inputMin, float inputMax, float outputMin, float outputMax, float value, bool clamp)
        {

            if (Math.Abs(inputMin - inputMax) < float.Epsilon)
            {
                return outputMin;
            }
            else
            {
                float outVal = ((value - inputMin) / (inputMax - inputMin) * (outputMax - outputMin) + outputMin);

                if (clamp)
                {
                    MathHelper.Clamp(outVal, outputMin, outputMax);
                }
                return outVal;
            }

        }

        /// <summary>
        /// Linear interpolation between two values
        /// </summary>
        /// <param name="value1">First input value</param>
        /// <param name="value2">Second input value</param>
        /// <param name="amount">Amount of interpolation between 0 and 1.
        /// For any other value, this function maps then range 0->1 to value1->value2</param>
        /// <example>If 0 then <paramref name="value1"/> will be returned, if 1 then <paramref name="value2"/>will be returned.</example>
        /// <returns>Value in the units of value1 and value2 interpolated</returns>
        public static double Lerp(double value1, double value2, double amount)
        { return value1 + (value2 - value1) * amount; }

        /// <summary>
        /// Maps two ranges of values and interpolates.
        /// </summary>
        /// <param name="inputMin">Input minimum range</param>
        /// <param name="inputMax">Input maximum range</param>
        /// <param name="outputMin">Output range min value (ie: whats the destination minimal value)</param>
        /// <param name="outputMax">Output range max value (ie: whats the destination maximal value)</param>
        /// <param name="value">Interpolation amout in the units of <paramref name="inputMin"/> and <paramref name="inputMax"/>
        /// if <paramref name="inputMin"/>, then output will be <paramref name="outputMin"/>
        /// if <paramref name="inputMax"/>, then output will be <paramref name="outputMax"/></param>
        /// <param name="clamp"></param>
        /// <returns></returns>
        public static double Map(double inputMin, double inputMax, double outputMin, double outputMax, double value, bool clamp)
        {

            if (Math.Abs(inputMin - inputMax) < double.Epsilon)
            {
                return outputMin;
            }
            else
            {
                double outVal = ((value - inputMin) / (inputMax - inputMin) * (outputMax - outputMin) + outputMin);

                if (clamp)
                {
                    MathHelper.Clamp(outVal, outputMin, outputMax);
                }
                return outVal;
            }

        }

        /// <summary>
        /// Degrees to radians conversion
        /// </summary>
        /// <param name="angleInDegrees"></param>
        /// <returns></returns>
        public static double ToRadians(double angleInDegrees)
        {
            return angleInDegrees * Math.PI / 180d;
        }
        /// <summary>
        /// Radians to degree conversion
        /// </summary>
        /// <param name="angleinRadians"></param>
        /// <returns></returns>
        public static double ToDegrees(double angleinRadians)
        {
            return angleinRadians * 180d / Math.PI;
        }


        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return value.CompareTo(min) == -1 ? min
                                    : value.CompareTo(max) == 1 ? max
                                    : value;

        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        public static IEnumerable<double> MetersToKm(this IEnumerable<double> meters)
        {
            return meters.Select(d => d * 0.001d);
        }



    }
}
