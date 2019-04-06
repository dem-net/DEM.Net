using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Mesh
{
    public class TriangulationResult
    {
        public IEnumerable<GeoPoint> Positions { get; internal set; }
        public IEnumerable<int> Indices { get; internal set; }

        public int NumPositions { get; internal set; }
        public int NumTriangles => NumIndices / 3;
        public int NumIndices { get; internal set; }
    }
}
