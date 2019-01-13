using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class UtilitairesServices : IUtilitairesServices
    {
        public Point ConstructPoint(double x, double y, int srid)
        {
            return new Point(x, y) { SRID = srid };
        }
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
        public IGeometry GetGeometryArc(BeanArc_internal p_arc, bool ifPt1AndPt2IqualReturnPointElseNull)
        {
            return GetGeometryLine(p_arc.p11_pointDbt.p10_coord, p_arc.p12_pointFin.p10_coord, p_arc.p11_pointDbt.p11_srid, ifPt1AndPt2IqualReturnPointElseNull);
        }
        public IGeometry GetGeometryLine(double[] p_coordPoint1, double[] p_coordPoint2, int p_srid,bool ifPt1AndPt2IqualReturnPointElseNull)
        {
            Geometry v_geomArc = null;
            try
            {
                if(p_coordPoint1[0]== p_coordPoint2[0] && p_coordPoint1[1] == p_coordPoint2[1])
                {
                    if(ifPt1AndPt2IqualReturnPointElseNull)
                    {
                        v_geomArc = ConstructPoint(p_coordPoint1[0], p_coordPoint1[1], p_srid);
                        return v_geomArc;
                    }
                    else
                    {
                      return null;
                    }
                }

                Coordinate v_coordPoint1 = new Coordinate(p_coordPoint1[0], p_coordPoint1[1]);
                Coordinate v_coordPoint2 = new Coordinate(p_coordPoint2[0], p_coordPoint2[1]);
                v_geomArc = new LineString(new Coordinate[] { v_coordPoint1, v_coordPoint2 }) { SRID = p_srid };
            }
            catch (Exception)
            {

                throw;
            }
            return v_geomArc;
        }
        public IGeometry GetGeometryPolygon(List<double[]> p_coordPointsDuContour, int p_srid)
        {
            Polygon v_geomArc = null;
            try
            {
                List<Coordinate> v_coords = new List<Coordinate>(p_coordPointsDuContour.Count + 1);
                v_coords.Add(new Coordinate(p_coordPointsDuContour[0][0], p_coordPointsDuContour[0][1]));
              
                for(int v_index=1; v_index< p_coordPointsDuContour.Count; v_index++)
                {
                    v_coords.Add(new Coordinate(p_coordPointsDuContour[v_index][0], p_coordPointsDuContour[v_index][1]));
                }
                v_coords.Add(new Coordinate(p_coordPointsDuContour[0][0], p_coordPointsDuContour[0][1]));

                v_geomArc = new Polygon(new LinearRing(v_coords.ToArray())) { SRID = p_srid };
            }
            catch (Exception)
            {

                throw;
            }
            return v_geomArc;
        }

    }
}
