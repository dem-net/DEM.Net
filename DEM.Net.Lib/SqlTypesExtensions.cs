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
    public static partial class SqlTypesExtensions
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

        public static SqlGeography ToGeography(this SqlGeometry geom)
        {
            try
            {
                geom = geom.MakeValidIfInvalid();

                SqlGeography geog = null;
                if (geom.TryToGeography(out geog))
                {
                    return geog;
                }

                // En cas d'échec, on appelle STBuffer avec un param faible puis on le réapelle avec le même param négatif
                // Cela ne change pas la geom mais corrige les erreurs.
                // Source : http://www.beginningspatial.com/fixing_invalid_geography_data
                SqlGeometry v_geomBuffered = geom.STBuffer(INVALIDGEOM_BUFFER).STBuffer(-INVALIDGEOM_BUFFER).Reduce(INVALIDGEOM_REDUCE);
                if (v_geomBuffered.TryToGeography(out geog))
                {
                    return geog;
                }

                // Inverse buffer
                v_geomBuffered = geom.STBuffer(-INVALIDGEOM_BUFFER).STBuffer(INVALIDGEOM_BUFFER).Reduce(INVALIDGEOM_REDUCE);
                if (v_geomBuffered.TryToGeography(out geog))
                {
                    return geog;
                }

                throw new ArgumentException("La géométrie ne peut pas être convertie en géographie");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static double GetAireEnMetres(this SqlGeometry geom)
        {
            try
            {
                SqlGeography geog = geom.ToGeography();
                double area = geog.STArea().Value;
                return area;
            }
            catch (Exception)
            {
                throw;
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

        public static SqlGeometry PolygonFromRings(SqlGeometry outerRing, List<SqlGeometry> holes)
        {
            // Check si les parametres sont des LINESTRING
            #region Check params
            if (outerRing == null || outerRing.IsNull)
                throw new ArgumentException("La boucle extérieure est null", "outerRing");

            if (outerRing.STGeometryType().Value != OpenGisGeometryType.LineString.ToString())
                throw new ArgumentException("La boucle extérieure doit être un LINESTRING", "outerRing");
            if (holes != null)
            {
                foreach (var hole in holes)
                {
                    if (hole.STGeometryType().Value != OpenGisGeometryType.LineString.ToString())
                        throw new ArgumentException("Les boucles intérieures doivent être un LINESTRING", "holes");
                }
            }
            #endregion


            StringBuilder sb = new StringBuilder();
            sb.Append("POLYGON (");
            sb.Append(outerRing.ToString().Replace("LINESTRING ", ""));

            if (holes != null)
            {
                foreach (SqlGeometry hole in holes)
                {

                    SqlGeometry polyFromHole = PolygonFromRings(hole, null);

                    if (SqlTypesExtensions.GetAireEnMetres(polyFromHole) < AIRE_MINI_SCORIES)
                        continue;

                    //Debug.WriteLine(polyFromHole.STArea().Value);

                    sb.Append(",");
                    sb.Append(hole.ToString().Replace("LINESTRING ", ""));
                }
            }

            sb.Append(")");

            SqlGeometry ret = SqlTypesExtensions.STGeomFromText(sb.ToString(), outerRing.STSrid.Value);
            ret = ret.MakeValidIfInvalid(2);

            return ret;
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

        public static SqlGeometry CorrigerUnionGeometry(SqlGeometry geom, int srid)
        {
            SqlGeometry geomBase = SqlTypesExtensions.STGeomFromText("POINT EMPTY", srid);

            for (int i = 1; i <= geom.STNumGeometries(); i++)
            {
                SqlGeometry curGeom = geom.STGeometryN(i);
                if (curGeom.STDimension().Value == 2)
                {
                    SqlGeometry outerRing = curGeom.STExteriorRing();

                    List<SqlGeometry> holes = new List<SqlGeometry>();

                    for (int hole = 1; hole <= curGeom.STNumInteriorRing(); hole++)
                    {
                        SqlGeometry holeGeom = SqlTypesExtensions.PolygonFromRings(curGeom.STInteriorRingN(hole), null); // trou converti en polygone
                        double aire = holeGeom.GetAireEnMetres();
                        if (aire > AIRE_MINI_SCORIES)
                        {
                            List<SqlGeometry> nativeHoles = SqlTypesExtensions.ExteriorRingsFromPolygon(holeGeom); // polygone corrigé reconverti en linestring
                            holes.AddRange(nativeHoles);
                        }
                    }

                    curGeom = SqlTypesExtensions.PolygonFromRings(outerRing, holes);
                    geomBase = geomBase.STUnion(curGeom);
                }
            }

            return geomBase;
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

        public static IEnumerable<SqlGeometry> Geometries(this SqlGeometry geom)
        {
            for (int i = 1; i <= geom.STNumGeometries(); i++)
            {
                yield return geom.STGeometryN(i);
            }
        }

        public static IEnumerable<SqlGeometry> Segments(this SqlGeometry geom)
        {

            for (int i = 1; i < geom.STNumPoints(); i++)
            {
                SqlGeometry curPoint = geom.STPointN(i);
                SqlGeometry nextPoint = geom.STPointN(i + 1);
                SqlGeometryBuilder gb = new SqlGeometryBuilder();
                gb.BeginGeometry(OpenGisGeometryType.LineString);
                gb.BeginFigure(curPoint.STX.Value, curPoint.STY.Value);
                gb.AddLine(nextPoint.STX.Value, nextPoint.STY.Value);
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
