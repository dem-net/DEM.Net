using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DEM.Net.Core
{
    public static class MetadataExtensions
    {
        public static BoundingBox BoundingBox(this IEnumerable<FileMetadata> fileMetadatas)
        {
            double xmin = double.MaxValue,
                ymin = double.MaxValue,
                zmin = double.MaxValue,
                xmax = double.MinValue,
                ymax = double.MinValue,
                zmax = double.MinValue;

            foreach (var metadata in fileMetadatas)
            {
                xmin = Math.Min(xmin, metadata.BoundingBox.xMin);
                xmax = Math.Max(xmax, metadata.BoundingBox.xMax);

                ymin = Math.Min(ymin, metadata.BoundingBox.yMin);
                ymax = Math.Max(ymax, metadata.BoundingBox.yMax);
            }
            return new BoundingBox(xmin, xmax, ymin, ymax, zmin, zmax);
        }
    }
}
