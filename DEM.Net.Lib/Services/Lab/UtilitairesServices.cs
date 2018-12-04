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
    }
}
