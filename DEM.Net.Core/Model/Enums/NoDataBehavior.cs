using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public enum NoDataBehavior
    {
        SetToZero,
        UseNoDataDefinedInDem,
        LastElevation
    }
}
