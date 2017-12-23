using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace DEM.Net.Lib
{
    public static class SqlTypesExtensions
    {
        private const double INVALIDGEOM_BUFFER = 0.000001d;
        private const double INVALIDGEOM_REDUCE = 0.00000025d;
        private const double AIRE_MINI_SCORIES = 250d;

        public static SqlGeometry MakeValidIfInvalid(this SqlGeometry geom, int? retainDimension = null)
        {
            if (geom == null || geom.IsNull)
                return geom;

            if (geom.STIsValid().IsFalse)
                geom = geom.MakeValid();

            SqlGeometry retGeom = geom;
            if (retainDimension.HasValue)
            {
                // On garde les éléments de dimension fournie

                // 1 ere passe on vérifie que toutes les géometries ont la dimension voulue
                bool allDimensionsOK = true;
                for (int index = 1; index <= geom.STNumGeometries(); index++)
                {
                    if (geom.STGeometryN(index).STDimension().Value < retainDimension.Value)
                    {
                        allDimensionsOK = false;
                        break;
                    }
                }

                // 2 ème passe, on corrige si nécessaire
                if (!allDimensionsOK)
                {
                    retGeom = STGeomFromText("POINT EMPTY", geom.STSrid.Value);

                    for (int index = 1; index <= geom.STNumGeometries(); index++)
                    {
                        SqlGeometry curGeomAtIndex = geom.STGeometryN(index);
                        if (curGeomAtIndex.STDimension().Value >= retainDimension.Value)
                        {
                            retGeom = retGeom.STUnion(curGeomAtIndex);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("NON RETENU");
                        }
                    }
                }
            }

            return retGeom;
        }

        public static bool TryToGeography(this SqlGeometry geom, out SqlGeography outputGeography)
        {
            try
            {
                geom = geom.MakeValidIfInvalid();

                outputGeography = SqlGeography.STGeomFromText(new SqlChars(new SqlString(geom.ToString())), 4326);
                return true;
            }
            catch
            {
                outputGeography = null;
                return false;
            }
        }
        public static bool TryToGeometry(this SqlGeography geog, out SqlGeometry outputGeometry)
        {
            try
            {
                outputGeometry = SqlGeometry.STGeomFromText(new SqlChars(new SqlString(geog.ToString())), 4326);
                outputGeometry = outputGeometry.MakeValidIfInvalid();
                return true;
            }
            catch
            {
                outputGeometry = null;
                return false;
            }
        }

        public static SqlGeometry STGeomFromText(string wkt, int srid)
        {
            return SqlGeometry.STGeomFromText(new SqlChars(new SqlString(wkt)), srid);
        }

        public static SqlGeography STGeogFromText(string wkt, int srid)
        {
            return SqlGeography.STGeomFromText(new SqlChars(new SqlString(wkt)), srid);
        }

     

        /// <summary>
        /// Retourne la liste des boucles extérieures d'un polygone
        /// </summary>
        /// <param name="holeGeom"></param>
        /// <returns></returns>
        public static List<SqlGeometry> ExteriorRingsFromPolygon(SqlGeometry polygon)
        {
            if (polygon == null || polygon.IsNull)
                return new List<SqlGeometry>();

            List<SqlGeometry> ringList = new List<SqlGeometry>();
            for (int index = 1; index <= polygon.STNumGeometries(); index++)
            {
                SqlGeometry curPolygon = polygon.STGeometryN(index);

                if (curPolygon.InstanceOf(OpenGisGeometryType.Polygon.ToString()))
                {
                    ringList.Add(curPolygon.STExteriorRing());
                }
                else
                    Trace.TraceWarning("ExteriorRingsFromPolygon : current geometry is not a polygon");
            }

            return ringList;
        }

      

        public static SqlGeometry PointEmpty_SqlGeometry(int srid)
        {
            return SqlGeometry.STPointFromText(new SqlChars(new SqlString("POINT EMPTY")), srid);
        }
        public static SqlGeography PointEmpty_SqlGeography(int srid)
        {
            return SqlGeography.STPointFromText(new SqlChars(new SqlString("POINT EMPTY")), srid);
        }

        public static bool AreSridEqual(this IEnumerable<SqlGeometry> geometries, out int uniqueSrid)
        {
            List<int> srids = geometries.Select(g => g.STSrid.Value).Distinct().ToList();
            if (srids.Count == 1)
            {
                uniqueSrid = srids[0];
                return true;
            }
            else
            {
                uniqueSrid = 0;
                return false;
            }
        }


        #region Enumerators


        public static IEnumerable<SqlGeometry> Points(this SqlGeometry geom)
        {
            for (int i = 1; i <= geom.STNumPoints(); i++)
            {
                yield return geom.STPointN(i);
            }
        }
        public static IEnumerable<SqlGeography> Points(this SqlGeography geom)
        {
            for (int i = 1; i <= geom.STNumPoints(); i++)
            {
                yield return geom.STPointN(i);
            }
        }

        public static IEnumerable<SqlGeometry> Geometries(this SqlGeometry geom)
        {
            for (int i = 1; i <= geom.STNumGeometries(); i++)
            {
                yield return geom.STGeometryN(i);
            }
        }

        public static IEnumerable<SqlGeometry> Segments(this SqlGeometry lineGeom)
        {

            if (lineGeom == null || lineGeom.IsNull || lineGeom.STIsEmpty())
            {
                yield return null;
            }
            if (lineGeom.STGeometryType().ToString() != OpenGisGeometryType.LineString.ToString())
            {
                yield return null;
            }
            if (lineGeom.STNumPoints().Value < 2)
            {
                yield return null;
            }

            for (int i = 1; i < lineGeom.STNumPoints().Value; i++)
            {

                SqlGeometry ptStart = lineGeom.STPointN(i);
                SqlGeometry ptNext = lineGeom.STPointN(i + 1);

                SqlGeometryBuilder gb = new SqlGeometryBuilder();
                gb.SetSrid(lineGeom.STSrid.Value);
                gb.BeginGeometry(OpenGisGeometryType.LineString);
                gb.BeginFigure(ptStart.STX.Value, ptStart.STY.Value);
                gb.AddLine(ptNext.STX.Value, ptNext.STY.Value);
                gb.EndFigure();
                gb.EndGeometry();

                yield return gb.ConstructedGeometry;
            }
        }

        public static IEnumerable<SqlGeometry> InteriorRings(this SqlGeometry geom)
        {
            for (int i = 1; i <= geom.STNumInteriorRing(); i++)
            {
                yield return geom.STInteriorRingN(i);
            }
        }

        public static bool HasInteriorRings(this SqlGeometry geom)
        {
            return geom.STNumInteriorRing().Value > 0;
        }

        public static BoundingBox GetBoundingBox(this SqlGeometry geom)
        {
            SqlGeometry envelope = geom.STEnvelope();
            return new BoundingBox(envelope.Points().Min(pt => pt.STX.Value)
                                                            , envelope.Points().Max(pt => pt.STX.Value)
                                                            , envelope.Points().Min(pt => pt.STY.Value)
                                                            , envelope.Points().Max(pt => pt.STY.Value));
        }
        public static SqlGeometry AsGeomety(this BoundingBox bbox, int srid = 4326)
        {
            SqlGeometryBuilder gb = new SqlGeometryBuilder();
            gb.SetSrid(srid);
            gb.BeginGeometry(OpenGisGeometryType.Polygon);
            gb.BeginFigure(bbox.xMin, bbox.yMax);
            gb.AddLine(bbox.xMax, bbox.yMax);
            gb.AddLine(bbox.xMax, bbox.yMin);
            gb.AddLine(bbox.xMin, bbox.yMin);
            gb.AddLine(bbox.xMin, bbox.yMax);
            gb.EndFigure();
            gb.EndGeometry();
            return gb.ConstructedGeometry;
        }

        public static BoundingBox GetBoundingBox(this SqlGeography geog)
        {
            SqlGeometry geom = null;
            if (geog.TryToGeometry(out geom))
            { return geom.GetBoundingBox(); }
            else
            {
                return new BoundingBox(geog.Points().Min(pt => pt.Long.Value)
                                                                , geog.Points().Max(pt => pt.Long.Value)
                                                                , geog.Points().Min(pt => pt.Lat.Value)
                                                                , geog.Points().Max(pt => pt.Lat.Value));
            }
        }

        #endregion

        #region Serialization

        //private void ToFile(SqlGeometry geom)
        //{

        //	// To serialize the hashtable and its key/value pairs,  
        //	// you must first open a stream for writing. 
        //	// In this case, use a file stream.
        //	using (FileStream fs = new FileStream("geom.dat", FileMode.Create))
        //	{

        //		// Construct a BinaryFormatter and use it to serialize the data to the stream.
        //		BinaryFormatter formatter = new BinaryFormatter();
        //		try
        //		{
        //			formatter.Serialize(fs, geom.STAsBinary().Value);
        //		}
        //		catch (SerializationException e)
        //		{
        //			Console.WriteLine("Failed to serialize. Reason: " + e.Message);
        //			throw;
        //		}

        //	}

        /// <summary>
        /// Reads a serialized SqlGeometry
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static SqlGeometry Read(string path)
        {
            SqlGeometry geom = null;
            // To serialize the hashtable and its key/value pairs,  
            // you must first open a stream for writing. 
            // In this case, use a file stream.
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                // Construct a BinaryFormatter and use it to serialize the data to the stream.
                BinaryFormatter formatter = new BinaryFormatter();
                try
                {
                    geom = (SqlGeometry)formatter.Deserialize(fs);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }

            }

            return geom;
        }

        /// <summary>
        /// Reads a serialized List<SqlGeometry>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<SqlGeometry> ReadList(string path)
        {
            List<SqlGeometry> geomList = null;
            // To serialize the hashtable and its key/value pairs,  
            // you must first open a stream for writing. 
            // In this case, use a file stream.
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                // Construct a BinaryFormatter and use it to serialize the data to the stream.
                BinaryFormatter formatter = new BinaryFormatter();
                try
                {
                    geomList = (List<SqlGeometry>)formatter.Deserialize(fs);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }

            }

            return geomList;
        }

        //private void Save(SqlGeometry geom)
        //{

        //	// To serialize the hashtable and its key/value pairs,  
        //	// you must first open a stream for writing. 
        //	// In this case, use a file stream.
        //	using (FileStream fs = new FileStream("geom.dat", FileMode.Create))
        //	{

        //		// Construct a BinaryFormatter and use it to serialize the data to the stream.
        //		BinaryFormatter formatter = new BinaryFormatter();
        //		try
        //		{
        //			formatter.Serialize(fs, geom.STAsBinary().Value);
        //		}
        //		catch (SerializationException e)
        //		{
        //			Console.WriteLine("Failed to serialize. Reason: " + e.Message);
        //			throw;
        //		}

        //	}
        //}


        #endregion
    }
}
