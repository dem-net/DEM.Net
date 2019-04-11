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
    public static class MathHelper
    {
        public static float Lerp(float value1, float value2, float amount)
        { return value1 + (value2 - value1) * amount; }

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

        internal static double ToRadians(double angle)
        {
            return angle * Math.PI / 180d;
        }
        internal static double ToDegrees(double angle)
        {
            return angle * 180d / Math.PI;
        }


        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return value.CompareTo(min) == -1 ? min
                                    : value.CompareTo(max) == 1 ? max
                                    : value;

        }

        #region Max/Min/Average

        #region Max

        public static double Max(params double[] args)
        {
            return args.Max();
        }
        public static decimal Max(params decimal[] args)
        {
            return args.Max();
        }
        public static float Max(params float[] args)
        {
            return args.Max();
        }
        public static long Max(params long[] args)
        {
            return args.Max();
        }
        public static int Max(params int[] args)
        {
            return args.Max();
        }
        public static double? Max(params double?[] args)
        {
            return args.Max();
        }
        public static decimal? Max(params decimal?[] args)
        {
            return args.Max();
        }
        public static float? Max(params float?[] args)
        {
            return args.Max();
        }
        public static long? Max(params long?[] args)
        {
            return args.Max();
        }
        public static int? Max(params int?[] args)
        {
            return args.Max();
        }

        #endregion

        #region Min

        public static double Min(params double[] args)
        {
            return args.Min();
        }
        public static decimal Min(params decimal[] args)
        {
            return args.Min();
        }
        public static float Min(params float[] args)
        {
            return args.Min();
        }
        public static long Min(params long[] args)
        {
            return args.Min();
        }
        public static int Min(params int[] args)
        {
            return args.Min();
        }
        public static double? Min(params double?[] args)
        {
            return args.Min();
        }
        public static decimal? Min(params decimal?[] args)
        {
            return args.Min();
        }
        public static float? Min(params float?[] args)
        {
            return args.Min();
        }
        public static long? Min(params long?[] args)
        {
            return args.Min();
        }
        public static int? Min(params int?[] args)
        {
            return args.Min();
        }

        #endregion

        #region Average

        public static double Average(params double[] args)
        {
            return args.Average();
        }
        public static decimal Average(params decimal[] args)
        {
            return args.Average();
        }
        public static float Average(params float[] args)
        {
            return args.Average();
        }
        public static double? Average(params double?[] args)
        {
            return args.Average();
        }
        public static decimal? Average(params decimal?[] args)
        {
            return args.Average();
        }
        public static float? Average(params float?[] args)
        {
            return args.Average();
        }


        #endregion

        #endregion

    }
}
