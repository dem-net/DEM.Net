// DEMDataSet.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public class DEMDataSet
    {
        public virtual string Name { get; set; }

        public virtual string Description { get; set; }
        public virtual string PublicUrl { get; set; }
        public virtual int ResolutionMeters { get; set; }

        public virtual DEMFileFormat FileFormat { get; set; }

        /// <summary>
        /// GDAL Virtual 
        /// </summary>
        public virtual string VRTFileUrl { get; set; }

        public DEMDataSet()
        {
        }

        // Examples datasets
        // Add any new dataset

        /// <summary>
        /// Shuttle Radar Topography Mission (SRTM GL3) Global 90m
        /// </summary>
        public static DEMDataSet SRTM_GL3
        {
            get
            {
                return new DEMDataSet()
                {
                    Name = "SRTM_GL3",
                    Description = "Shuttle Radar Topography Mission (SRTM GL3) Global 90m",
                    PublicUrl = "http://opentopo.sdsc.edu/raster?opentopoID=OTSRTM.042013.4326.1",
                    VRTFileUrl = "https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL3/SRTM_GL3_srtm.vrt",
                    FileFormat = DEMFileFormat.SRTM_HGT,
                    ResolutionMeters = 90
                };
            }
        }
        /// <summary>
        /// Shuttle Radar Topography Mission (SRTM GL1) Global 30m
        /// </summary>
        public static DEMDataSet SRTM_GL1
        {
            get
            {
                return new DEMDataSet()
                {
                    Name = "SRTM_GL1",
                    Description = "Shuttle Radar Topography Mission (SRTM GL1) Global 30m",
                    PublicUrl = "http://opentopo.sdsc.edu/raster?opentopoID=OTSRTM.082015.4326.1",
                    VRTFileUrl = "https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL1/SRTM_GL1_srtm.vrt",
                    FileFormat = DEMFileFormat.SRTM_HGT,
                    ResolutionMeters = 30
                };
            }
        }
        /// <summary>
        /// ALOS World 3D - 30m
        /// </summary>
        public static DEMDataSet AW3D30
        {
            get
            {
                return new DEMDataSet()
                {
                    Name = "AW3D30",
                    Description = "ALOS World 3D - 30m",
                    PublicUrl = "http://opentopo.sdsc.edu/raster?opentopoID=OTALOS.112016.4326.2",
                    VRTFileUrl = "https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/AW3D30/AW3D30_alos.vrt",
                    FileFormat = DEMFileFormat.GEOTIFF,
                    ResolutionMeters = 30
                };
            }
        }

        public static IEnumerable<DEMDataSet> RegisteredDatasets
        {
            get
            {
                yield return DEMDataSet.SRTM_GL3;
                yield return DEMDataSet.SRTM_GL1;
                yield return DEMDataSet.AW3D30;
                // 
                // add any new dataset here
                // yield return DEMDataSet.<newdataset>
            }

        }

        public override string ToString()
        {
            return Name;
        }
    }
}
