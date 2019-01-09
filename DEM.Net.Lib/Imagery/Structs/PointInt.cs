using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public struct PointInt
    {
        public int X;
        public int Y;


        public PointInt(int x, int y)
        {
            X = x;
            Y = y;
        }
        public override string ToString()
        {
            return $"x={X}, y={Y}";
        }
    }
}
