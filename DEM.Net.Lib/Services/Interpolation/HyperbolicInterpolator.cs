using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Interpolation
{
    /// <summary>
    /// HyperbolicInterpolator : gives smoother results, but can skip small flat areas
    /// Other implementation described briefly in article.
    /// 
    /// Source : http://www.geocomputation.org/1999/082/gc_082.htm
    /// </summary>
    public class HyperbolicInterpolator : IInterpolator
    {
        public float Interpolate(float southWestHeight, float southEastHeight, float northWestHeight, float northEastHeight, float x, float y)
        {
            float h1 = southWestHeight, h2 = southEastHeight, h3 = northWestHeight, h4 = northEastHeight;
            // hyperbolic
            float a00 = h1;
            float a10 = h2 - h1;
            float a01 = h3 - h1;
            float a11 = h1 - h2 - h3 + h4;
            float hi_hyperbolic = a00 + a10 * x + a01 * y + a11 * x * y;

            return hi_hyperbolic;
        }
    }
}
