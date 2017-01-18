using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public static class MathHelper
    {
        public static float Lerp(float value1, float value2, float amount)
        { return value1 + (value2 - value1) * amount; }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return value.CompareTo(min) == -1 ? min
                                    : value.CompareTo(max) == 1 ? max
                                    : value;

        }
    }
}
