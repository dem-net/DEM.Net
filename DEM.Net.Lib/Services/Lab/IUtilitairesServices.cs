using System.Collections.Generic;

namespace DEM.Net.Lib.Services.Lab
{
    public interface IUtilitairesServices
    {
        string GethCodeGeogPoint(double[] p_coord, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur = '_');
        string GethCodeGeogObjet(List<double[]> p_points, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur = '_');
        string GethCodeGeogSegment(double[] p_coord1, double[] p_coord2, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur='_');
    }
}