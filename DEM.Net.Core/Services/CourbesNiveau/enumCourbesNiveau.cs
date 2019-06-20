using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.CourbesNiveau
{
	public enum enumModeCalculCourbe
	{
		interpolationLineaireSurTriangulation
	}

	public enum enumModeDeduplicationPoints
	{
		manhattanParArrondi,
		pasDeDeduplication
	}
	public enum enumPrecisionManahatan
		{
		precision100m,
		precision10m,
		precisionM,
		precisionDm
	}
	public enum enumModeAgregationDesPoints
	{
		valeurMin,
		valeurMax,
		moyenneA
	}

}
