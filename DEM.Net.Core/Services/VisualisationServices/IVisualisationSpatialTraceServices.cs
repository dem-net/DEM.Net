// IVisualisationSpatialTraceServices.cs
//
// Author:
//       Frédéric Aubin
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
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

using System.Collections.Generic;
using System.Drawing;
using DEM.Net.Core.Services.Lab;

namespace DEM.Net.Core.Services.VisualisationServices
{
    public interface IVisualisationSpatialTraceServices
    {
        Dictionary<string, Color> GetTableCouleursDegradees(List<string> p_classesTriees, enumProgressionCouleurs p_progressionCouleur, int p_alpha, bool p_croissantSinonDecroissant);
        Dictionary<int, Color> GetTableCouleursDegradees(int p_nbreClasses, enumProgressionCouleurs p_progressionCouleur, int p_alpha = 120, bool p_croissantSinonDecroissant = true);
        //
        int GetTailleAffichageDuPoint(List<BeanPoint_internal> p_points, double p_ratioTaillePoint);
        int GetTailleAffichageDuPoint(double p_largeurEspaceReference, double p_ratioTaillePoint);
        //
        void GetVisuPointsAlti(Dictionary<string, List<BeanPoint_internal>> p_pointsParClasse,  Dictionary<string, Color> p_tableCouleurs, int p_taillePoint);
        void GetVisuPointsAlti(List<BeanPoint_internal> p_points,  int p_nbreClasses = 10, bool p_croissantSinonDecroissant = true, enumModeSeuillage p_methodeDeSeuillage = enumModeSeuillage.memeNombreDIndividus, enumProgressionCouleurs p_progressionCouleurs = enumProgressionCouleurs.greenVersRed, int p_alpha = 120, int p_taillePointAutoSi0OuMoins = -1);
        //
        void GetVisuPoints2D(List<BeanPoint_internal> p_points, string p_label, int p_taillePointAutoSi0OuMoins = -1);
        void GetVisuPoints2D(List<BeanPoint_internal> p_points, string p_label, Color p_couleurCourante, int p_taillePointAutoSi0OuMoins);
        void GetVisuPoint2D(BeanPoint_internal v_point,  string p_label, Color p_couleurCourante, int p_taillePoint = 10);
        void GetVisuPoint2D(BeanPoint_internal p_point, string p_label, int p_taillePoint);
        //
        void GetVisuArc2D(BeanArc_internal p_arc, string p_label, Color p_couleurCourante);
        //
        void GetVisuVecteur2D(double[] p_vecteur, double[] p_origine, int p_srid, string p_label, Color p_couleurCourante, double p_coeff = 1);
        void GetVisuVecteur2D(double[] p_vecteur, double[] p_origine, int p_srid, string p_label, double p_coeff = 1);
        //
        void GetVisuTopologieFacettes(BeanTopologieFacettes p_topologieFacettes, bool p_visualiserPointsInclus_vf, bool p_afficherMemeSiInvalide_vf);
        void GetVisuFacette(BeanFacette_internal p_facette,string p_message, Color p_couleurCourante, bool p_visualiserPointsInclus_vf, bool p_afficherMemeSiInvalide_vf);
        //
        void GetVisuCreteEtTalweg(BeanTopologieFacettes p_topologieFacettes, HashSet<enum_qualificationMorpho_arc> p_nePasAfficher = null);
        //
        void GetVisuPentesFacettes(BeanTopologieFacettes p_topologieFacettes, int p_nbreClasses);
        //
        void GetVisuPoints(List<BeanPoint_internal> p_points, Color p_couleur, int p_taillePoint, string p_prefixeLabel);
        void GetVisuArcsTopologie(BeanTopologieFacettes p_topologie, Color p_couleur, string p_prefixeLabel);
        void GetVisuIlots(BeanTopologieFacettes p_topologie, Color p_couleur, string p_prefixeLabel);
        //
        void AfficheVisu();
        void ClearSpatialTrace();
    }
}