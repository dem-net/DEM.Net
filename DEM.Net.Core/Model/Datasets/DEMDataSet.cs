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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.Datasets;

namespace DEM.Net.Core
{
    public class DEMDataSet
    {
        public string Name { get; set; }

        public string Description { get; set; }
        public string PublicUrl { get; set; }
        public int ResolutionMeters { get; set; }
        public int NoDataValue { get; set; }

        public DEMFileFormat FileFormat { get; set; }

        public Attribution Attribution { get; set; }

        public IDEMDataSource DataSource { get; set; }

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
                    DataSource = new VRTDataSource("https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL3/SRTM_GL3_srtm.vrt"),
                    FileFormat = DEMFileFormat.SRTM_HGT,
                    ResolutionMeters = 90,
                    PointsPerDegree = 1200,
                    Attribution = new Attribution("OpenTopography", "https://opentopography.org/"
                    , @"Citing LP DAVV and Data Products: https://lpdaac.usgs.gov/about/citing_lp_daac_and_data

If you wish to cite the SRTM products in a report or publication please use: 
Farr, T. G., and M. Kobrick, 2000, Shuttle Radar Topography Mission produces a wealth of data. Eos Trans. AGU, 81:583-583.

Farr, T. G. et al., 2007, The Shuttle Radar Topography Mission, Rev. Geophys., 45, RG2004, doi:10.1029/2005RG000183. (Also available online at http://www2.jpl.nasa.gov/srtm/SRTM_paper.pdf)

Kobrick, M., 2006, On the toes of giants--How SRTM was born, Photogramm. Eng. Remote Sens., 72:206-210.

Rosen, P. A. et al., 2000, Synthetic aperture radar interferometry, Proc. IEEE, 88:333-382.
https://doi.org/10.5069/G9445JDF")
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
                    DataSource = new VRTDataSource("https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL1/SRTM_GL1_srtm.vrt"),
                    FileFormat = DEMFileFormat.SRTM_HGT,
                    ResolutionMeters = 30,
                    PointsPerDegree = 3600,
                    Attribution = new Attribution("OpenTopography", "https://opentopography.org/", "http://www2.jpl.nasa.gov/srtm/srtmBibliography.html, https://doi.org/10.5069/G9445JDF")
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
                    Description = "ALOS World 3D - 30m (nicest but contain void areas)",
                    PublicUrl = "http://opentopo.sdsc.edu/raster?opentopoID=OTALOS.112016.4326.2",
                    DataSource = new VRTDataSource("https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/AW3D30/AW3D30_alos.vrt"),
                    FileFormat = DEMFileFormat.GEOTIFF,
                    ResolutionMeters = 30,
                    PointsPerDegree = 3600,
                    NoDataValue = -9999,
                    Attribution = new Attribution("OpenTopography", "https://opentopography.org/"
                    , @"J. Takaku, T. Tadono, K. Tsutsui : Generation of High Resolution Global DSM from ALOS PRISM, The International Archives of the Photogrammetry, Remote Sensing and Spatial Information Sciences, pp.243-248, Vol. XL-4, ISPRS TC IV Symposium, Suzhou, China, 2014. [http://www.int-arch-photogramm-remote-sens-spatial-inf-sci.net/XL-4/243/2014/isprsarchives-XL-4-243-2014.pdf]

T.Tadono, H.Ishida, F.Oda, S.Naito, K.Minakawa, H.Iwamoto : Precise Global DEM Generation By ALOS PRISM, ISPRS Annals of the Photogrammetry, Remote Sensing and Spatial Information Sciences, pp.71 - 76, Vol.II - 4, 2014. [http://www.isprs-ann-photogramm-remote-sens-spatial-inf-sci.net/II-4/71/2014/isprsannals-II-4-71-2014.pdf]
https://doi.org/10.5069/G94M92HB
")
                };
            }
        }

        public static DEMDataSet ETOPO1
        {
            get
            {
                return new DEMDataSet()
                {
                    Name = "ETOPO1",
                    Description = "Global low res coverage with bathymetry (1km resolution)",
                    PublicUrl = "https://www.ngdc.noaa.gov/mgg/global/",
                    DataSource = new  SingleFileDataSource(Path.Combine("Data", "ETOPO1", "ETOPO1_Ice_g_geotiff.tif")),
                    FileFormat = DEMFileFormat.GEOTIFF,
                    ResolutionMeters = 1800,
                    PointsPerDegree = 60,
                    NoDataValue = -9999,
                    Attribution = new Attribution("NOAA", "https://www.ngdc.noaa.gov/mgg/global/"
                    , "Amante, C. and B.W. Eakins, 2009. ETOPO1 1 Arc-Minute Global Relief Model: Procedures, Data Sources and Analysis. NOAA Technical Memorandum NESDIS NGDC-24. National Geophysical Data Center, NOAA. doi:10.7289/V5C8276M")
                };
            }
            
        }

        /// <summary>
        /// ASTER GDEM V3 https://cmr.earthdata.nasa.gov/search/concepts/C1575726572-LPDAAC_ECS/11
        /// API: https://cmr.earthdata.nasa.gov/search/site/docs/search/api.html
        /// </summary>
        public static DEMDataSet ASTER_GDEMV3
        {
            get
            {
                return new DEMDataSet()
                {
                    Name = "ASTER_GDEMV3",
                    Description = "ASTER Global Digital Elevation Model 1 arc second (30m)",
                    PublicUrl = "https://lpdaac.usgs.gov/products/astgtmv003",
                    DataSource = new NasaGranuleDataSource("https://e4ftl01.cr.usgs.gov/ASTT/ASTGTM.003/2000.03.01/"),
                    FileFormat = DEMFileFormat.GEOTIFF,
                    ResolutionMeters = 30,
                    PointsPerDegree = 3600,
                    NoDataValue = -9999,
                    Attribution = new Attribution("NASA/METI/AIST/Japan Spacesystems, and U.S./Japan ASTER Science Team", "https://lpdaac.usgs.gov/products/astgtmv003"
                    , "NASA/METI/AIST/Japan Spacesystems, and U.S./Japan ASTER Science Team. ASTER Global Digital Elevation Model V003. 2019, distributed by NASA EOSDIS Land Processes DAAC, https://doi.org/10.5067/ASTER/ASTGTM.003. Accessed 2019-10-15. DOI: 10.5067/ASTER/ASTGTM.003")
                };
            }

        }

        public static IEnumerable<DEMDataSet> RegisteredDatasets
        {
            get
            {
                yield return DEMDataSet.ETOPO1;
                yield return DEMDataSet.SRTM_GL3;
                yield return DEMDataSet.SRTM_GL1;
                yield return DEMDataSet.AW3D30;
                //yield return DEMDataSet.ASTER_GDEMV3;
                // 
                // add any new dataset here
                // yield return DEMDataSet.<newdataset>
            }

        }

        public int PointsPerDegree { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
