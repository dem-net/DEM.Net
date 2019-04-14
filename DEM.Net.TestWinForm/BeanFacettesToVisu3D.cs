using DEM.Net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.TestWinForm
{
    public class BeanFacettesToVisu3D
    {
       public List<GeoPoint> p00_geoPoint { get; set; }
      public List<List<int>> p01_listeIndexPointsfacettes { get; set; }
        public BeanFacettesToVisu3D()
        {
            p00_geoPoint = new List<GeoPoint>();
            p01_listeIndexPointsfacettes = new List<List<int>>(); 
        }
    }
}
