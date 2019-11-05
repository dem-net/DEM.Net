using DEM.Net.Core.Imagery;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Configuration
{
    public class DEMNetOptions
    {
        public List<ImageryProvider> ImageryProviders { get; set; } = new List<ImageryProvider>();
    }
}
