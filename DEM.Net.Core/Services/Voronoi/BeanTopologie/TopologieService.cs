using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace DEM.Net.Core.Voronoi
{
    public class TopologieService : ITopologieService
    {
        public TopologieService()
        {
        }

        public string GetHashCodeGeometriePoint(double v_coordxPoint1, double v_coordyPoint1)
        {
            throw new NotImplementedException();
        }

        public Geometry GetLineStringByCoord(double v_OrigX_Left, double v_OrigY_Left, double v_OrigX_Right, double v_OrigY_Right)
        {
            throw new NotImplementedException();
        }

        public BeanTopologie GetTopologie(Dictionary<int, Geometry> v_DicoLignes)
        {
            throw new NotImplementedException();
        }

        public BeanTopologie GetTopologieDIlotsFusionnes(BeanTopologie v_topologieVoronoi, Dictionary<int, List<int>> p_dicoDesPointsParSurfaces)
        {
            throw new NotImplementedException();
        }

        public BeanTopologie GetTopologieSansImpassesEnrichiesDesIlots(BeanTopologie v_topologieLignes)
        {
            throw new NotImplementedException();
        }

        public Geometry GetUnPointIGeometryByCoordonneesXy(float v1, float v2)
        {
            throw new NotImplementedException();
        }

        public bool IsTopologieIlotsOk_vf(BeanTopologie v_topologieVoronoi)
        {
            throw new NotImplementedException();
        }

        public void MiseAJourDesIndicateursDeControleTopologieIlot(BeanTopologie v_topologieVoronoi)
        {
            throw new NotImplementedException();
        }

        public void UpdateIdIlotsByCotesArcs(BeanTopologie v_topologieVoronoi, Dictionary<int, int> v_DicoPointSourceDroitParArc, Dictionary<int, int> v_DicoPointSourceGaucheParArc)
        {
            throw new NotImplementedException();
        }

        internal bool IsGeometryEnsembleDeLignes(Geometry p_IGeometry)
        {
            throw new NotImplementedException();
        }
    }
}