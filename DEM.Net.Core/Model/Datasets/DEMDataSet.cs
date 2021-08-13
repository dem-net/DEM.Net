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
        private const string ATTRIBUTION_SUBJECT = "Digital Elevation Model";

        public string Name { get; set; }
        public string Description { get; set; }
        public string PublicUrl { get; set; }
        /// <summary>
        /// Approximate meters resolution calculated at equator (no distortion)
        /// </summary>
        public int ResolutionMeters { get; set; }
        public float ResolutionArcSeconds { get; set; }
        public int NoDataValue { get; set; }
        public DEMFileDefinition FileFormat { get; set; }
        public Attribution Attribution { get; set; }
        public IDEMDataSource DataSource { get; set; }
        public int PointsPerDegree { get; private set; }
        public int SRID { get; set; } = Reprojection.SRID_GEODETIC;

        public override string ToString()
        {
            return Name;
        }

        private static readonly Lazy<Dictionary<string, DEMDataSet>> Datasets = new Lazy<Dictionary<string, DEMDataSet>>(GetRegisteredDatasets, true);

        public static IEnumerable<DEMDataSet> RegisteredDatasets => DEMDataSet.Datasets.Value.Values;
        public static IEnumerable<DEMDataSet> RegisteredNonLocalDatasets => RegisteredDatasets.Where(d => d.DataSource.DataSourceType != DEMDataSourceType.LocalFileSystem);

        private static Dictionary<string, DEMDataSet> GetRegisteredDatasets()
        {
            Dictionary<string, DEMDataSet> datasets = new Dictionary<string, DEMDataSet>();
            datasets.Add("SRTM_GL3", new DEMDataSet()
            {
                Name = nameof(SRTM_GL3),
                Description = "Shuttle Radar Topography Mission (SRTM GL3) Global 90m",
                PublicUrl = "http://opentopo.sdsc.edu/raster?opentopoID=OTSRTM.042013.4326.1",
                DataSource = new VRTDataSource("https://opentopography.s3.sdsc.edu/raster/SRTM_GL3/SRTM_GL3_srtm.vrt"),
                FileFormat = new DEMFileDefinition("Nasa SRTM HGT", DEMFileType.GEOTIFF, ".tif", DEMFileRegistrationMode.Grid),
                ResolutionMeters = 90,
                ResolutionArcSeconds = 3,
                PointsPerDegree = 1200,
                Attribution = new Attribution(ATTRIBUTION_SUBJECT, "SRTM_GL3 OpenTopography", "https://opentopography.org/"
                    , "Farr, T. G., and M. Kobrick, 2000, Shuttle Radar Topography Mission produces a wealth of data. Eos Trans. AGU, 81:583-583." + Environment.NewLine +
                        "Farr, T. G. et al., 2007, The Shuttle Radar Topography Mission, Rev. Geophys., 45, RG2004, doi:10.1029/2005RG000183. (Also available online at http://www2.jpl.nasa.gov/srtm/SRTM_paper.pdf)" + Environment.NewLine +
                        "Kobrick, M., 2006, On the toes of giants--How SRTM was born, Photogramm. Eng. Remote Sens., 72:206-210." + Environment.NewLine +
                        "Rosen, P. A. et al., 2000, Synthetic aperture radar interferometry, Proc. IEEE, 88:333-382." + Environment.NewLine +
                        "https://doi.org/10.5069/G9445JDF")
            });
            //datasets.Add("SRTM_GL1", new DEMDataSet()
            //{
            //    Name = nameof(SRTM_GL1),
            //    Description = "Shuttle Radar Topography Mission (SRTM GL1) Global 30m",
            //    PublicUrl = "http://opentopo.sdsc.edu/raster?opentopoID=OTSRTM.082015.4326.1",
            //    DataSource = new VRTDataSource("https://opentopography.s3.sdsc.edu/raster/SRTM_GL1/SRTM_GL1_srtm.vrt"),
            //    FileFormat = new DEMFileDefinition("Nasa SRTM HGT", DEMFileType.GEOTIFF, ".tif", DEMFileRegistrationMode.Grid),
            //    ResolutionMeters = 30,
            //    ResolutionArcSeconds = 1,
            //    PointsPerDegree = 3600,
            //    Attribution = new Attribution(ATTRIBUTION_SUBJECT, "SRTM_GL1 OpenTopography", "https://opentopography.org/", "http://www2.jpl.nasa.gov/srtm/srtmBibliography.html, https://doi.org/10.5069/G9445JDF")
            //});
            datasets.Add("AW3D30", new DEMDataSet()
            {
                Name = nameof(AW3D30),
                Description = "ALOS World 3D - 30m (nicest but contain void areas)",
                PublicUrl = "http://opentopo.sdsc.edu/raster?opentopoID=OTALOS.112016.4326.2",
                DataSource = new VRTDataSource("https://opentopography.s3.sdsc.edu/raster/AW3D30/AW3D30_global.vrt"),
                FileFormat = new DEMFileDefinition("GeoTiff file", DEMFileType.GEOTIFF, ".tif", DEMFileRegistrationMode.Cell),
                ResolutionMeters = 30,
                ResolutionArcSeconds = 1,
                PointsPerDegree = 3600,
                NoDataValue = -9999,
                Attribution = new Attribution(ATTRIBUTION_SUBJECT, "AW3D30 OpenTopography", "https://opentopography.org/"
                    , "J. Takaku, T. Tadono, K. Tsutsui : Generation of High Resolution Global DSM from ALOS PRISM, The International Archives of the Photogrammetry, Remote Sensing and Spatial Information Sciences, pp.243-248, Vol. XL-4, ISPRS TC IV Symposium, Suzhou, China, 2014. [http://www.int-arch-photogramm-remote-sens-spatial-inf-sci.net/XL-4/243/2014/isprsarchives-XL-4-243-2014.pdf]" + Environment.NewLine +
                    "T.Tadono, H.Ishida, F.Oda, S.Naito, K.Minakawa, H.Iwamoto : Precise Global DEM Generation By ALOS PRISM, ISPRS Annals of the Photogrammetry, Remote Sensing and Spatial Information Sciences, pp.71 - 76, Vol.II - 4, 2014. [http://www.isprs-ann-photogramm-remote-sens-spatial-inf-sci.net/II-4/71/2014/isprsannals-II-4-71-2014.pdf]" + Environment.NewLine +
                    "https://doi.org/10.5069/G94M92HB")
            });
            datasets.Add("ETOPO1", new DEMDataSet()
            {
                Name = nameof(ETOPO1),
                Description = "Global low res coverage with bathymetry (1km resolution)",
                PublicUrl = "https://www.ngdc.noaa.gov/mgg/global/",
                DataSource = new LocalFileSystem(localDirectory: Path.Combine("Data", "ETOPO1")),
                FileFormat = new DEMFileDefinition("GeoTiff file", DEMFileType.GEOTIFF, ".tif", DEMFileRegistrationMode.Grid),
                ResolutionMeters = 1800,
                ResolutionArcSeconds = 60,
                PointsPerDegree = 60,
                NoDataValue = -9999,
                Attribution = new Attribution(ATTRIBUTION_SUBJECT, "ETOPO1 - NOAA", "https://www.ngdc.noaa.gov/mgg/global/"
                    , "Amante, C. and B.W. Eakins, 2009. ETOPO1 1 Arc-Minute Global Relief Model: Procedures, Data Sources and Analysis. NOAA Technical Memorandum NESDIS NGDC-24. National Geophysical Data Center, NOAA. doi:10.7289/V5C8276M")
            });
            datasets.Add("IGN_5m", new DEMDataSet()
            {
                Name = nameof(IGN_5m),
                Description = "IGN RGE Alti 5 meter (France only)",
                PublicUrl = "https://ign.fr",
                DataSource = new LocalFileSystem(localDirectory: Path.Combine("Data", "IGN_5m_GZip")),
                FileFormat = new DEMFileDefinition("Esri Ascii Grid (GZipped)", DEMFileType.ASCIIGridGzip, ".asc.gz", DEMFileRegistrationMode.Cell),
                ResolutionMeters = 5,
                PointsPerDegree = 21600,
                NoDataValue = -99999,
                SRID = 2154,
                Attribution = new Attribution(ATTRIBUTION_SUBJECT, "IGN", "https://ign.fr", "https://www.etalab.gouv.fr/licence-ouverte-open-licence")
            });
            //datasets.Add("IGN_5m", new DEMDataSet()
            //{
            //    Name = nameof(IGN_5m),
            //    Description = "IGN RGE Alti 5 meter",
            //    PublicUrl = "https://ign.fr",
            //    DataSource = new LocalFileSystem(localDirectory: Path.Combine("Data", "IGN_5m")),
            //    FileFormat = new DEMFileDefinition("Esri Ascii Grid", DEMFileType.ASCIIGrid, ".asc", DEMFileRegistrationMode.Cell),
            //    ResolutionMeters = 5,
            //    PointsPerDegree = 21600,
            //    NoDataValue = -99999,
            //    SRID = 2154,
            //    Attribution = new Attribution(ATTRIBUTION_SUBJECT, "IGN", "https://ign.fr"
            //        , "TO BE DEFINED")
            //});
            //datasets.Add("IGN_1m", new DEMDataSet()
            //{
            //    Name = nameof(IGN_1m),
            //    Description = "IGN RGE Alti 1 meter",
            //    PublicUrl = "https://ign.fr",
            //    DataSource = new LocalFileSystem(localDirectory: Path.Combine("Data", "IGN_1m")),
            //    FileFormat = new DEMFileDefinition("Esri Ascii Grid", DEMFileType.ASCIIGrid, ".asc", DEMFileRegistrationMode.Cell),
            //    ResolutionMeters = 1,
            //    PointsPerDegree = 108000,
            //    NoDataValue = -99999,
            //    SRID = 2154,
            //    Attribution = new Attribution(ATTRIBUTION_SUBJECT, "IGN", "https://ign.fr"
            //        , "TO BE DEFINED")
            //});
            //datasets.Add("ASTER_GDEMV3", new DEMDataSet()
            //{
            //    Name = nameof(ASTER_GDEMV3),
            //    Description = "ASTER Global Digital Elevation Model 1 arc second (30m)",
            //    PublicUrl = "https://lpdaac.usgs.gov/products/astgtmv003",
            //    DataSource = new NasaGranuleDataSource(indexFilePath: "ASTGTM.003.json", collectionId: "C1575726572-LPDAAC_ECS"),
            //    FileFormat = new DEMFileDefinition("GeoTiff file", DEMFileType.GEOTIFF, "_dem.tif", DEMFileRegistrationMode.Grid),
            //    ResolutionMeters = 30,
            //    ResolutionArcSeconds = 1,
            //    PointsPerDegree = 3600,
            //    NoDataValue = -9999,
            //    Attribution = new Attribution(ATTRIBUTION_SUBJECT, "ASTER_GDEMV3",
            //                                    "https://doi.org/10.5067/ASTER/ASTGTM.003",
            //                                    "NASA/METI/AIST/Japan Spacesystems, and U.S./Japan ASTER Science Team. ASTER Global Digital Elevation Model V003. 2018, distributed by NASA EOSDIS Land Processes DAAC")
            //});
            datasets.Add("NASADEM", new DEMDataSet()
            {
                Name = nameof(NASADEM),
                Description = "NASADEM MEaSUREs Merged DEM Global 1 arc second (30m)",
                PublicUrl = "https://lpdaac.usgs.gov/products/nasadem_hgtv001/",
                DataSource = new NasaGranuleDataSource(indexFilePath: "NASADEM.json", collectionId: "C1546314043-LPDAAC_ECS"),
                FileFormat = new DEMFileDefinition("Nasa SRTM HGT", DEMFileType.SRTM_HGT, ".hgt", DEMFileRegistrationMode.Grid),
                ResolutionMeters = 30,
                ResolutionArcSeconds = 1,
                PointsPerDegree = 3600,
                NoDataValue = -9999,
                Attribution = new Attribution(ATTRIBUTION_SUBJECT, "NASADEM",
                                                "https://doi.org/10.5067/MEaSUREs/NASADEM/NASADEM_HGT.001",
                                                "NASA JPL. NASADEM Merged DEM Global 1 arc second V001. 2020, distributed by NASA EOSDIS Land Processes DAAC, https://doi.org/10.5067/MEaSUREs/NASADEM/NASADEM_HGT.001. Accessed 2020-03-06.")
            });
            datasets.Add("GEBCO_2019", new DEMDataSet()
            {
                Name = nameof(GEBCO_2019),
                Description = "400m with bathymetry",
                PublicUrl = "https://www.gebco.net/data_and_products/gridded_bathymetry_data/gebco_2019/gebco_2019_info.html",
                DataSource = new LocalFileSystem(localDirectory: Path.Combine("Data", "GEBCO_2019")),
                FileFormat = new DEMFileDefinition("netCDF file", DEMFileType.CF_NetCDF, ".nc", DEMFileRegistrationMode.Cell),
                ResolutionMeters = 464,
                ResolutionArcSeconds = 15,
                PointsPerDegree = 240,
                NoDataValue = -9999,
                Attribution = new Attribution(ATTRIBUTION_SUBJECT, "GEBCO Compilation Group (2019) GEBCO 2019 Grid (doi:10.5285/836f016a-33be-6ddc-e053-6c86abc0788e)",
                                                "https://www.gebco.net/data_and_products/gridded_bathymetry_data/gebco_2019/gebco_2019_info.html",
                                                "GEBCO Compilation Group (2019) GEBCO 2019 Grid (doi:10.5285/836f016a-33be-6ddc-e053-6c86abc0788e)")
            });
            //datasets.Add("GEBCO_2020", new DEMDataSet()
            //{
            //    Name = nameof(GEBCO_2020),
            //    Description = "GEBCO’s gridded bathymetric data set, a global terrain model for ocean and land at 15 arc-second intervals",
            //    PublicUrl = "https://www.gebco.net/data_and_products/gridded_bathymetry_data/gebco_2020/",
            //    DataSource = new LocalFileSystem(localDirectory: "GEBCO_2020"),
            //    FileFormat = new DEMFileDefinition("GeoTiff file", DEMFileType.GEOTIFF, ".tif", DEMFileRegistrationMode.Grid),
            //    ResolutionMeters = 464,
            //    ResolutionArcSeconds = 15,
            //    PointsPerDegree = 240,
            //    NoDataValue = -9999,
            //    Attribution = new Attribution(ATTRIBUTION_SUBJECT, "GEBCO Compilation Group (2020) GEBCO 2020 Grid (doi:10.5285/a29c5465-b138-234d-e053-6c86abc040b9)",
            //                                     "https://www.gebco.net/data_and_products/gridded_bathymetry_data/gebco_2020/",
            //                                     "GEBCO Compilation Group (2020) GEBCO 2020 Grid (doi:10.5285/a29c5465-b138-234d-e053-6c86abc040b9)")
            //});

            return datasets;
        }


        // Examples datasets
        // Add any new dataset

        /// <summary>
        /// Shuttle Radar Topography Mission (SRTM GL3) Global 90m
        /// </summary>
        public static DEMDataSet SRTM_GL3 => Datasets.Value[nameof(SRTM_GL3)];

        /// <summary>
        /// ALOS World 3D - 30m
        /// </summary>
        public static DEMDataSet AW3D30 => Datasets.Value[nameof(AW3D30)];

        /// <summary>
        /// Global low res coverage with bathymetry (1km resolution)
        /// </summary>
        public static DEMDataSet ETOPO1 => Datasets.Value[nameof(ETOPO1)];

        /// <summary>
        /// Global medium res coverage with bathymetry (500m resolution)
        /// </summary>
        public static DEMDataSet GEBCO_2019 => Datasets.Value[nameof(GEBCO_2019)];
        public static DEMDataSet GEBCO_2020 => Datasets.Value[nameof(GEBCO_2020)];

        /// <summary>
        /// NASADEM https://cmr.earthdata.nasa.gov/search/concepts/C1546314043-LPDAAC_ECS
        /// API: https://cmr.earthdata.nasa.gov/search/site/docs/search/api.html
        /// </summary>
        public static DEMDataSet NASADEM => Datasets.Value[nameof(NASADEM)];
        public static DEMDataSet IGN_5m => Datasets.Value[nameof(IGN_5m)];
        public static DEMDataSet IGN_1m => Datasets.Value[nameof(IGN_1m)];





    }
}
