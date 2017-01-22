using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Interpolation
{
    public class BilinearInterpolator : IInterpolator
    {
        public float Interpolate(float southWestHeight, float southEastHeight, float northWestHeight, float northEastHeight, float x, float y)
        {
            float h1 = southWestHeight, h2 = southEastHeight, h3 = northWestHeight, h4 = northEastHeight;
            // bilinear
            float ha = MathHelper.Lerp(h3, h1, y);
            float hb = MathHelper.Lerp(h4, h2, y);
            float hi_linear = MathHelper.Lerp(ha, hb, x);

            return hi_linear;
        }
    }
}
