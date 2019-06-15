using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DEM.Net.Core.Services.Lab
{
    public class CalculServicesVoronoi : ICalculServicesVoronoi
    {
        public BeanTopologieFacettes GetTopologieVoronoi(List<BeanPoint_internal> p_points, int p_srid, double p_ecartMiniEnM)
        {
            BeanTopologieFacettes v_topol = null;
            try
            {
                List<GraphEdge> v_edges;
                v_edges = GetArcsBrutsVoronoiFromListBeanPointInternal(p_points, p_ecartMiniEnM);
                //
                v_topol = GetTopologieBruteFromListGraphEdges(v_edges, p_srid);
                //
                FServices.createCalculMedium().RecalculFacettes(ref v_topol);
            }
            catch (Exception)
            {

                throw;
            }
            return v_topol;
        }

        private BeanTopologieFacettes GetTopologieBruteFromListGraphEdges(List<GraphEdge> p_edges, int p_srid)
        {
            BeanTopologieFacettes v_topol = new BeanTopologieFacettes();
            try
            {
                Dictionary<string, BeanPoint_internal> v_points = new Dictionary<string, BeanPoint_internal>();
                Dictionary<string, BeanArc_internal> v_arcs = new Dictionary<string, BeanArc_internal>();
               //
                List<BeanPoint_internal> v_ptsDeLArc;
                BeanPoint_internal v_pt_dbt;
                BeanPoint_internal v_pt_fin;
                BeanArc_internal v_arc;
                foreach (GraphEdge v_edge in p_edges)
                {
                    v_ptsDeLArc = GetBeanArcInternalFromGraphEdges(v_edge, p_srid);
                    v_pt_dbt = v_ptsDeLArc.First();
                    v_pt_fin = v_ptsDeLArc.Last();
                    //
                    if (!v_points.ContainsKey(v_pt_dbt.p01_hCodeGeog))
                    {
                        v_points.Add(v_pt_dbt.p01_hCodeGeog, v_pt_dbt);
                     }
                    else
                    {
                        v_pt_dbt = v_points[v_pt_dbt.p01_hCodeGeog];
                    }
                    //
                    if (!v_points.ContainsKey(v_pt_fin.p01_hCodeGeog))
                    {
                        v_points.Add(v_pt_fin.p01_hCodeGeog, v_pt_fin);
                    }
                    else
                    {
                        v_pt_fin = v_points[v_pt_fin.p01_hCodeGeog];
                    }
                    //
                    v_arc = new BeanArc_internal(v_pt_dbt, v_pt_fin);
                    //
                    v_arcs.Add(v_arc.p01_hcodeArc, v_arc);

                    v_pt_dbt.p41_arcsAssocies.Add(v_arc.p01_hcodeArc, v_arc);
                    v_pt_fin.p41_arcsAssocies.Add(v_arc.p01_hcodeArc, v_arc);
                }
                //
                v_topol.p12_arcsByCode = v_arcs;
                v_topol.p00_pointsSources = v_points.Values.ToList();        
            }
            catch (Exception)
            {

                throw;
            }
            return v_topol;
        }
        private List<BeanPoint_internal> GetBeanArcInternalFromGraphEdges(GraphEdge p_edge, int p_srid)
        {
            List<BeanPoint_internal> v_points = new List<BeanPoint_internal>();
            try
            {
                BeanPoint_internal v_point1 = new BeanPoint_internal(p_edge.x1, p_edge.y1,0, p_srid);
                BeanPoint_internal v_point2 = new BeanPoint_internal(p_edge.x2, p_edge.y2, 0, p_srid);
                //
                v_points.Add(v_point1);
                v_points.Add(v_point2);
            }
            catch (Exception)
            {

                throw;
            }
            return v_points;
        }
        private List<GraphEdge> GetArcsBrutsVoronoiFromListBeanPointInternal(List<BeanPoint_internal> p_points, double p_ecartMiniEnM)
        {
            List<GraphEdge> v_edges = null;
            try
            {
                Voronoi v_Voronoi = new Voronoi(p_ecartMiniEnM);

                double[] xVal = new double[p_points.Count];
                double[] yVal = new double[p_points.Count];
                int v_indPoint = 0;
                foreach (BeanPoint_internal v_pt in p_points)
                {
                    xVal[v_indPoint] = v_pt.p10_coord[0];
                    yVal[v_indPoint] = v_pt.p10_coord[1];
                    v_indPoint++;
                }
                double v_largeur = xVal.Max() - xVal.Min();
                double v_hauteur = yVal.Max() - yVal.Min();

                v_edges=v_Voronoi.generateVoronoi(xVal, yVal, 0, v_largeur, 0, v_hauteur);
            }
            catch (Exception)
            {
                throw;
            }
            return v_edges;
        }
    }
}
