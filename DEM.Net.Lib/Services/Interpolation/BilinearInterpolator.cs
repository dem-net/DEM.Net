using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Interpolation
{
    /// <summary>
    /// Standard interpolation
    /// 
    /// For a point lying between 4 nearest grid coordinates, this interpolator will do a linear interpolation on X axis and then on Y axis.
    /// 
    ///
    /// 
    /// "The concept of linear interpolation between two points can be extended to bilinear interpolation within 
    /// the grid cell. The function is said to be linear in each variable when the other is held fixed. 
    /// 
    /// For example, to determine the height hi at x, y in Figure 5, the elevations at y on the vertical 
    /// boundaries of the grid cell can be linearly interpolated between h1 and h3 at ha, and h2 and h4 at hb.
    /// Finally, the required elevation at x can be linearly interpolated between ha and hb."
    /// 
    /// Source : http://www.geocomputation.org/1999/082/gc_082.htm
    /// </summary>
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
