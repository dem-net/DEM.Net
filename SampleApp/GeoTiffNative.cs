using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace GeoTiffToInf
{
    /// <summary>
    /// A class to parse GeoTIFF files.
    /// Originally written by CrashTestDummy at the FSDeveloper forums.
    /// http://www.fsdeveloper.com/forum/showthread.php?p=130356#post130356
    /// </summary>
    class GeoTiff
    {

        // Private Fields
        private UInt16 _ImageWidth;         // pixels
        private UInt16 _ImageHeight;        // pixels
        private double _LEFT_LONG;          // top left longitude
        private double _TOP_LAT;            // top left latitude
        private double _dXScale;            // degreess / pixel (x direction)
        private double _dYScale;            // degreess / pixel (y direction)

        private UInt16 _GEOMODEL;           // 1 = Projected, 2 = Geographic, 3 = Geocentric
        private UInt16 _GEORASTER;          // 1 = Pixel Is Area, 2 = Pixel Is Point
        private UInt16 _GeographicType;     // 4326 = WGS 84 (EPSG reference number)
        private UInt16 _GeogAngularUnits;   // 9102 = Angular Degree

        private string _fileName;           // File name passed into init method

        // Public Properties
        public UInt16 ImageWidth
        {
            get { return _ImageWidth; }
            set { _ImageWidth = value; }
        }
        public UInt16 ImageHeight
        {
            get { return _ImageHeight; }
            set { _ImageHeight = value; }
        }
        public double LEFT_LONG
        {
            get { return _LEFT_LONG; }
            set { _LEFT_LONG = value; }
        }
        public double TOP_LAT
        {
            get { return _TOP_LAT; }
            set { _TOP_LAT = value; }
        }
        public double dXScale
        {
            get { return _dXScale; }
            set { _dXScale = value; }
        }
        public double dYScale
        {
            get { return _dYScale; }
            set { _dYScale = value; }
        }

        public UInt16 GEOMODEL
        {
            get { return _GEOMODEL; }
            set { _GEOMODEL = value; }
        }
        public UInt16 GEORASTER
        {
            get { return _GEORASTER; }
            set { _GEORASTER = value; }
        }
        public UInt16 GeographicType
        {
            get { return _GeographicType; }
            set { _GeographicType = value; }
        }
        public UInt16 GeogAngularUnits
        {
            get { return _GeogAngularUnits; }
            set { _GeogAngularUnits = value; }
        }

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }


        private enum EXIFTags
        {
            PixelScale = 0x830e,
            ModelTiePoint = 0x8482,
            GeoTiffDirectory = 0x87AF,
            ImageWidth = 0x0100,
            ImageHeight = 0x0101,
            BitsPerSample = 0x0102,
            Compression = 0x0103,
            Photometric = 0x0106,
            Thresholding = 0x0107,
            PlanarConfig = 0x011c
        }

        private enum GEOVector
        {
            Version = 0x0001,
            Model = 0x0400,
            Raster = 0x0401,
            Citation = 0x0402,
            GeographicType = 0x0800,
            GeogCitation = 0x0801,
            GeogGeodeticDatum = 0x0802,
            GeogPrimeMeridian = 0x0803,
            GeogLinearUnits = 0x0804,
            GeogLinearUnitSize = 0x0805,
            GeogAngularUnits = 0x0806,
            GeogAngularUnitSize = 0x0807,
            GeogEllipsoid = 0x0808,
            GeogSemiMajorAxis = 0x0809,
            GeogSemiMinorAxis = 0x080A,
            GeogInvFlattening = 0x080B,
            GeogAzimuthUnits = 0x080C,
            GeogPrimeMeridianLong = 0x080D,
            ProjectedCSType = 0x0c00,
            PCSCitation = 0x0c01,
            Projection = 0x0c02,
            ProjCoordTrans = 0x0c03,
            ProjLinearUnits = 0x0c04,
            ProjLinearUnitSize = 0x0c05,
            ProjStdParallel1 = 0x0c06,
            ProjStdParallel2 = 0x0c07,
            ProjNatOriginLong = 0x0c08,
            ProjNatOriginLat = 0x0c09,
            ProjFalseEasting = 0x0c0A,
            ProjFalseNorthing = 0x0c0B,
            ProjFalseOriginLong = 0x0c0C,
            ProjFalseOriginLat = 0x0c0D,
            ProjFalseOriginEasting = 0x0c0E,
            ProjFalseOriginNorthing = 0x0c0F,
            ProjCenterLong = 0x0c10,
            ProjCenterLat = 0x0c11,
            ProjCenterEasting = 0x0c12,
            ProjCenterNorthing = 0x0c13,
            ProjScaleAtNatOrigin = 0x0c14,
            ProjScaleAtCenter = 0x0c15,
            ProjAzimuthAngle = 0x0c16,
            ProjStraightVertPoleLong = 0x0c17,
            ProjRectifiedGridAngle = 0x0c18,
            VerticalCSType = 0x1000,
            VerticalCitation = 0x1001,
            VerticalDatum = 0x1002,
            VerticalUnits = 0x1003
        }

        private bool CaseIFD(BitmapMetadata meta, int tag)
        {
            object ifdData = meta.GetQuery(string.Concat("/ifd/{uint=", tag, "}"));
            if (ifdData != null)
            {
                switch (tag)
                {
                    case (int)EXIFTags.ImageWidth:
                        {
                            if (ifdData.GetType() == typeof(UInt16))
                            {
                                ImageWidth = (UInt16)ifdData;
                            }
                            else if (ifdData.GetType() == typeof(UInt32))
                            {
                                ImageWidth = (UInt16)((UInt32)ifdData);
                            }
                            else
                            {
                                throw new Exception($"Unexpected {ifdData.GetType().Name} type in tag.");
                            }

                            return true;
                        }
                    case (int)EXIFTags.ImageHeight:
                        {
                            if (ifdData.GetType() == typeof(UInt16))
                            {
                                ImageHeight = (UInt16)ifdData;
                            }
                            else if (ifdData.GetType() == typeof(UInt32))
                            {
                                ImageHeight = (UInt16)((UInt32)ifdData);
                            }
                            else
                            {
                                throw new Exception($"Unexpected {ifdData.GetType().Name} type in tag.");
                            }

                            return true;
                        }

                    case (int)EXIFTags.PixelScale:
                        {
                            if (ifdData.GetType() != typeof(double[])) throw new Exception($"Unexpected {ifdData.GetType().Name} type in tag.");
                            double[] data = (double[])ifdData;

                            dXScale = (double)data[0];
                            dYScale = (double)data[1];
                            double dZScale = (double)data[2];

                            return true;
                        }
                    case (int)EXIFTags.ModelTiePoint:
                        {

                            if (ifdData.GetType() != typeof(double[])) throw new Exception($"Unexpected {ifdData.GetType().Name} type in tag.");
                            double[] data = (double[])ifdData;

                            double d0 = (double)data[0];
                            double d1 = (double)data[1];
                            double d2 = (double)data[2];
                            LEFT_LONG = (double)data[3];
                            TOP_LAT = (double)data[4];
                            double d5 = (double)data[5];


                            return true;
                        }
                    case (int)EXIFTags.GeoTiffDirectory:
                        {

                            if (ifdData.GetType() != typeof(UInt16[])) throw new Exception($"Unexpected {ifdData.GetType().Name} type in tag.");
                            UInt16[] data = (UInt16[])ifdData;

                            int dataItem = 0;
                            while (dataItem < data.Length)
                            {

                                UInt16 vector = data[dataItem];
                                dataItem += 1;

                                switch (vector)
                                {
                                    case (int)GEOVector.Version:
                                        UInt16 bV1 = data[dataItem];
                                        dataItem += 1;
                                        UInt16 bV2 = data[dataItem];
                                        dataItem += 1;
                                        UInt16 bV3 = data[dataItem];
                                        dataItem += 1;

                                        string GeoVersion = bV1 + "." + bV2 + "." + bV3;

                                        break;

                                    case (int)GEOVector.Model:
                                        {
                                            UInt16 bSpacer1Unknown = data[dataItem];
                                            dataItem += 1;
                                            UInt16 bSpacer2Unknown = data[dataItem];
                                            dataItem += 1;
                                            GEOMODEL = data[dataItem]; // 1 = Projected,2 = Geographic,3 = Geocentric
                                            dataItem += 1;
                                        }
                                        break;


                                    case (int)GEOVector.Raster:
                                        {
                                            UInt16 bSpacer1Unknown = data[dataItem];
                                            dataItem += 1;
                                            UInt16 bSpacer2Unknown = data[dataItem];
                                            dataItem += 1;
                                            GEORASTER = data[dataItem]; // 1 = Pixel Is Area, 2 = Pixel Is Point
                                            dataItem += 1;
                                        }
                                        break;

                                    case (int)GEOVector.GeographicType:
                                        {
                                            UInt16 bSpacer1Unknown = data[dataItem];
                                            dataItem += 1;
                                            UInt16 bSpacer2Unknown = data[dataItem];
                                            dataItem += 1;
                                            GeographicType = data[dataItem]; // 4326 = WGS 84
                                            dataItem += 1;
                                        }
                                        break;

                                    case (int)GEOVector.GeogAngularUnits:
                                        {
                                            UInt16 bSpacer1Unknown = data[dataItem];
                                            dataItem += 1;
                                            UInt16 bSpacer2Unknown = data[dataItem];
                                            dataItem += 1;
                                            GeogAngularUnits = data[dataItem]; // 9102	= Angular Degree
                                            dataItem += 1;
                                        }

                                        break;
                                    default:
                                        Debug.WriteLine(" VECTOR Data that could be captured ");
                                        Debug.WriteLine(String.Format("{0:X}", vector));
                                        //Dummy Read
                                        dataItem += 3;
                                        break;
                                }// end vector switch

                            }// end while vector


                            return true;
                        }

                }
            }


            return false;

        }

        //----
        private void InitGeoTiff(String sPathToFile)
        {
            // Reset variables since they're data bound
            FileName = sPathToFile;
            ImageWidth = 0;
            ImageHeight = 0;
            LEFT_LONG = 0;
            TOP_LAT = 0;
            dXScale = 0;
            dYScale = 0;
            GEOMODEL = 0;
            GEORASTER = 0;
            GeographicType = 0;
            GeogAngularUnits = 0;

            using (Stream imageStreamSource = new FileStream(sPathToFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                TiffBitmapDecoder decoder = new TiffBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                BitmapSource bitmapSource = decoder.Frames[0];
                BitmapMetadata bitmapMetadata = (BitmapMetadata)decoder.Frames[0].Metadata;

                if ((!CaseIFD(bitmapMetadata, (int)EXIFTags.ImageWidth)) ||
                    (!CaseIFD(bitmapMetadata, (int)EXIFTags.ImageHeight)) ||
                    (!CaseIFD(bitmapMetadata, (int)EXIFTags.PixelScale)) ||
                    (!CaseIFD(bitmapMetadata, (int)EXIFTags.GeoTiffDirectory)) ||
                    (!CaseIFD(bitmapMetadata, (int)EXIFTags.ModelTiePoint))
                   ) throw new FormatException("GeoTIFF tags not found.");
            }
        }

        public static GeoTiff FromFile(string pathToFile)
        {
            GeoTiff geoTiff = new GeoTiff();
            geoTiff.InitGeoTiff(pathToFile);
            return geoTiff;
        }
    }
}