using Microsoft.SqlServer.Types;
using System.Collections.Generic;

namespace DEM.Net.Lib.Services.Lab
{
    public interface IUtilitairesServices
    {
        string GethCodeGeogPoint(double[] p_coord, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur = '_');
        string GethCodeGeogObjet(List<double[]> p_points, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur = '_');
        string GethCodeGeogSegment(double[] p_coord1, double[] p_coord2, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur='_');
        //
        SqlGeometry GetGeometryArc(BeanArc_internal p_arc, bool ifPt1AndPt2IqualReturnPointElseNull);
        SqlGeometry GetGeometryLine(double[] p_coordPoint1, double[] p_coordPoint2, int p_srid, bool ifPt1AndPt2IqualReturnPointElseNull);
        SqlGeometry GetGeometryPolygon(List<double[]> p_coordPointsDuContour, int p_srid);
    }
}