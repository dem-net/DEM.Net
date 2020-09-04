using DEM.Net.Core.Imagery;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Configuration
{
    public class DEMNetOptions
    {
        public float RenderGpxZTranslateTrackMeters { get; set; } = 5f;

        public float RenderGpxTrailWidthMeters { get; set; } = 15f;
        public List<ImageryProvider> ImageryProviders { get; set; } = new List<ImageryProvider>();

        public bool UseImageryDiskCache { get; set; } = true;
        public float ImageryDiskCacheExpirationHours { get; set; } = 5f;

        public float ImageryCacheExpirationMinutes { get; set; } = 5f;
    }
}
