using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.glTF.SharpglTF
{
    [Flags]
    public enum GenOptions
    {
        None =                  0b000,
        Normals =               0b001,
        BoxedBaseElevation0 =   0b010,
        BoxedBaseElevationMin = 0b100,
        CropToNonEmpty =       0b1000,
    }
}
