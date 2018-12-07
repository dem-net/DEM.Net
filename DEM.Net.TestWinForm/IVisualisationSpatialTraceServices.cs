using System.Collections.Generic;
using System.Windows.Media;
using DEM.Net.Lib.Services.Lab;

namespace DEM.Net.TestWinForm
{
    public interface IVisualisationSpatialTraceServices
    {
        Dictionary<string, Color> GetTableCouleursDegradees(List<string> p_classesTriees, enumProgressionCouleurs p_progressionCouleur, int p_alpha, bool p_croissantSinonDecroissant);
        Dictionary<int, Color> GetTableCouleursDegradees(int p_nbreClasses, enumProgressionCouleurs p_progressionCouleur, int p_alpha = 120, bool p_croissantSinonDecroissant = true);
        void VisuPoints(Dictionary<string, List<BeanPoint_internal>> p_pointsParClasse, Dictionary<string, Color> p_tableCouleurs);
    }
}