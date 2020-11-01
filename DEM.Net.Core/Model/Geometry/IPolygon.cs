using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public interface IPolygon<T>
    {
        List<T> ExteriorRing { get; set; }

        List<List<T>> InteriorRings { get; set; }
    }
}
