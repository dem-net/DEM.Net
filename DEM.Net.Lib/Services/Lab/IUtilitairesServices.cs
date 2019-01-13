using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace DEM.Net.Lib.Services.Lab
{
    public interface IUtilitairesServices
    {
        string GethCodeGeogPoint(double[] p_coord, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur = '_');
        string GethCodeGeogObjet(List<double[]> p_points, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur = '_');
        string GethCodeGeogSegment(double[] p_coord1, double[] p_coord2, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur='_');
        //
        IGeometry GetGeometryArc(BeanArc_internal p_arc, bool ifPt1AndPt2IqualReturnPointElseNull);
        IGeometry GetGeometryLine(double[] p_coordPoint1, double[] p_coordPoint2, int p_srid, bool ifPt1AndPt2IqualReturnPointElseNull);
        IGeometry GetGeometryPolygon(List<double[]> p_coordPointsDuContour, int p_srid);
        Point ConstructPoint(double x, double y, int srid);
    }
}