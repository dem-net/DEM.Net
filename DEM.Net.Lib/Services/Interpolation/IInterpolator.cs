using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public interface IInterpolator
    {
        /// <summary>
        /// 
        /// 
        /// The concept of linear interpolation between two points can be extended to bilinear interpolation within 
        /// the grid cell. The function is said to be linear in each variable when the other is held fixed. 
        /// 
        /// For example, to determine the height hi at x, y in Figure 5, the elevations at y on the vertical 
        /// boundaries of the grid cell can be linearly interpolated between h1 and h3 at ha, and h2 and h4 at hb.
        /// Finally, the required elevation at x can be linearly interpolated between ha and hb. 
        /// 
        /// Source : http://www.geocomputation.org/1999/082/gc_082.htm
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <param name="h3"></param>
        /// <param name="h4"></param>
        /// <returns></returns>
        float Interpolate(float southWestHeight, float southEastHeight, float northWestHeight, float northEastHeight, float x, float y);
    }
}
