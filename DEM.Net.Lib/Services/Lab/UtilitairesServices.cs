using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class UtilitairesServices : IUtilitairesServices
    {
        public string GethCodeGeogPoint(double[] p_coord, int p_nbreDecimalesMoins1SiToutes, char p_separateur)
        {
            string v_code = "";
            try
            {
                if (p_nbreDecimalesMoins1SiToutes >= 0)
                {
                    foreach (double v_c in p_coord)
                    {
                        v_code += Math.Round(v_c, p_nbreDecimalesMoins1SiToutes) + p_separateur;
                    }
                }
                else
                {
                    foreach (double v_c in p_coord)
                    {
                        v_code += p_nbreDecimalesMoins1SiToutes+ p_separateur;
                    }
                }
                v_code = v_code.Substring(0, v_code.Length - 1);
            }
            catch (Exception)
            {

                throw;
            }
            return v_code;
        }
        public string GethCodeGeogObjet(List<double[]> p_points, int p_nbreDecimalesMoins1SiToutes, char p_separateur)
        {
            string v_code = "";
            try
            {
                List<double[]> p_pointsOrd=p_points.OrderBy(c => c[0]).ThenBy(c => c[1]).ToList();

                foreach(double[] v_point in p_pointsOrd)
                {
                    v_code += GethCodeGeogPoint(v_point, p_nbreDecimalesMoins1SiToutes, p_separateur)+ p_separateur;
                }
                v_code = v_code.Substring(0, v_code.Length - 1);
            }
            catch (Exception)
            {

                throw;
            }
            return v_code;
        }
        public string GethCodeGeogSegment(double[] p_coord1, double[] p_coord2, int p_nbreDecimalesMoins1SiToutes, char p_separateur)
        {
            string v_code = "";
            try
            {
                List<double[]> p_points = new List<double[]>();
                p_points.Add(p_coord1);
                p_points.Add(p_coord2);
                //
                v_code = GethCodeGeogObjet(p_points, p_nbreDecimalesMoins1SiToutes, p_separateur);
            }
            catch (Exception)
            {

                throw;
            }
            return v_code;
        }
        //
        public SqlGeometry GetGeometryArc(BeanArc_internal p_arc, bool ifPt1AndPt2IqualReturnPointElseNull)
        {
            return GetGeometryLine(p_arc.p11_pointDbt.p10_coord, p_arc.p12_pointFin.p10_coord, p_arc.p11_pointDbt.p11_srid, ifPt1AndPt2IqualReturnPointElseNull);
        }
        public SqlGeometry GetGeometryLine(double[] p_coordPoint1, double[] p_coordPoint2, int p_srid,bool ifPt1AndPt2IqualReturnPointElseNull)
        {
            SqlGeometry v_geomArc = null;
            try
            {
                if(p_coordPoint1[0]== p_coordPoint2[0] && p_coordPoint1[1] == p_coordPoint2[1])
                {
                    if(ifPt1AndPt2IqualReturnPointElseNull)
                    {
                        v_geomArc = SqlGeometry.Point(p_coordPoint1[0], p_coordPoint1[1], p_srid);
                        return v_geomArc;
                    }
                    else
                    {
                      return null;
                    }
                }
                SqlGeometryBuilder v_builder = new SqlGeometryBuilder();

                v_builder.SetSrid(p_srid);
       
                v_builder.BeginGeometry(OpenGisGeometryType.LineString);

                v_builder.BeginFigure(p_coordPoint1[0], p_coordPoint1[1]);

                v_builder.AddLine(p_coordPoint2[0], p_coordPoint2[1]);

                v_builder.EndFigure();

                v_builder.EndGeometry();

                v_geomArc = v_builder.ConstructedGeometry;
            }
            catch (Exception)
            {

                throw;
            }
            return v_geomArc;
        }
        public SqlGeometry GetGeometryPolygon(List<double[]> p_coordPointsDuContour, int p_srid)
        {
            SqlGeometry v_geomArc = null;
            try
            {
                SqlGeometryBuilder v_builder = new SqlGeometryBuilder();
                v_builder.SetSrid(p_srid);
                v_builder.BeginGeometry(OpenGisGeometryType.Polygon);
                v_builder.BeginFigure(p_coordPointsDuContour[0][0], p_coordPointsDuContour[0][1]);
                for(int v_index=1; v_index< p_coordPointsDuContour.Count; v_index++)
                {
                    v_builder.AddLine(p_coordPointsDuContour[v_index][0], p_coordPointsDuContour[v_index][1]);
                }
                v_builder.AddLine(p_coordPointsDuContour[0][0], p_coordPointsDuContour[0][1]);
                v_builder.EndFigure();
                v_builder.EndGeometry();
                v_geomArc = v_builder.ConstructedGeometry;
            }
            catch (Exception)
            {

                throw;
            }
            return v_geomArc;
        }

    }
}
