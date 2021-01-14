// UtilitairesServices.cs
//
// Author:
//       Frédéric Aubin
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

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DEM.Net.Core.Services.Lab
{

    public class UtilitairesServices : IUtilitairesServices
    {
        public NetTopologySuite.Geometries.Point ConstructPoint(double x, double y, int srid)
        {
            return new NetTopologySuite.Geometries.Point(x, y) { SRID = srid };
        }


        //public string GetHCodeGeogPoint(double[] p_coordPoint, int p_nbreCaractSignifiants, char p_separateur)
        //{
        //    string v_composante1 = p_coordPoint[0].ToString();
        //    if (v_composante1.Length > p_nbreCaractSignifiants)
        //    {
        //        v_composante1 = v_composante1.ToString().Substring(0, p_nbreCaractSignifiants);
        //    }
        //    string v_composante2 = p_coordPoint[1].ToString();
        //    if (v_composante2.Length > p_nbreCaractSignifiants)
        //    {
        //        v_composante2 = v_composante2.ToString().Substring(0, p_nbreCaractSignifiants);
        //    }
        //    return v_composante1 + p_separateur + v_composante2;
        //}
        //public string GetHCodeGeogPoint(List<double[]> p_points, int p_nbreCaractSignifiants, char p_separateur)
        //{
        //    string v_code = "";
        //    try
        //    {
        //        List<double[]> p_pointsOrd = p_points.OrderBy(c => c[0]).ThenBy(c => c[1]).ToList();

        //        foreach (double[] v_point in p_pointsOrd)
        //        {
        //            v_code += GetHCodeGeogPoint(v_point, p_nbreCaractSignifiants, p_separateur);
        //            v_code += p_separateur;
        //        }
        //        v_code = v_code.Substring(0, v_code.Length - 1);
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //    return v_code;
        //}
        //public string GethCodeGeogSegment(double[] p_coord1, double[] p_coord2, int p_nbreCaractSignifiants, char p_separateur)
        //{
        //    string v_code = "";
        //    try
        //    {
        //        List<double[]> p_points = new List<double[]>();
        //        p_points.Add(p_coord1);
        //        p_points.Add(p_coord2);
        //        //
        //        v_code = GetHCodeGeogPoint(p_points, p_nbreCaractSignifiants, p_separateur);
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //    return v_code;
        //}

        public string GetHCodeGeogPoint(double[] p_coordPoint, int p_nbreDecimales, char p_separateur)
        {
            string v_composante1 = Math.Round(p_coordPoint[0], p_nbreDecimales).ToString();
            string v_composante2 = Math.Round(p_coordPoint[1], p_nbreDecimales).ToString();
            return v_composante1 + p_separateur + v_composante2;
        }
        public string GetHCodeGeogPoint(List<double[]> p_points, int p_nbreDecimales, char p_separateur)
        {
            string v_code = "";
            try
            {
                List<double[]> p_pointsOrd = p_points.OrderBy(c => c[0]).ThenBy(c => c[1]).ToList();

                foreach (double[] v_point in p_pointsOrd)
                {
                    v_code += GetHCodeGeogPoint(v_point, p_nbreDecimales, p_separateur);
                    v_code += p_separateur;
                }
                v_code = v_code.Substring(0, v_code.Length - 1);
            }
            catch (Exception)
            {

                throw;
            }
            return v_code;
        }
        public string GethCodeGeogSegment(double[] p_coord1, double[] p_coord2, int p_nbreDecimales, char p_separateur)
        {
            string v_code = "";
            try
            {
                List<double[]> p_points = new List<double[]>();
                p_points.Add(p_coord1);
                p_points.Add(p_coord2);
                //
                v_code = GetHCodeGeogPoint(p_points, p_nbreDecimales, p_separateur);
            }
            catch (Exception)
            {

                throw;
            }
            return v_code;
        }
        public Dictionary<string, List<BeanPoint_internal>> GetPointsRegroupesParHCode(List<BeanPoint_internal> p_dataPointsTests)
        {
            Dictionary<string, List<BeanPoint_internal>> v_pourDeduplication = new Dictionary<string, List<BeanPoint_internal>>();
            try
            {
                foreach (BeanPoint_internal v_point in p_dataPointsTests)
                {
                    if (!v_pourDeduplication.ContainsKey(v_point.p01_hCodeGeog))
                    {
                        v_pourDeduplication.Add(v_point.p01_hCodeGeog, new List<BeanPoint_internal>());
                    }
                    v_pourDeduplication[v_point.p01_hCodeGeog].Add(v_point);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_pourDeduplication;
        }

        //
        public Geometry GetGeometryArc(BeanArc_internal p_arc, bool ifPt1AndPt2IqualReturnPointElseNull)
        {
            return GetGeometryLine(p_arc.p11_pointDbt.p10_coord, p_arc.p12_pointFin.p10_coord, p_arc.p11_pointDbt.p11_srid, ifPt1AndPt2IqualReturnPointElseNull);
        }
        public Geometry GetGeometryLine(double[] p_coordPoint1, double[] p_coordPoint2, int p_srid,bool ifPt1AndPt2IqualReturnPointElseNull)
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
        public Geometry GetGeometryPolygon(List<double[]> p_coordPointsDuContour, int p_srid)
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
