using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DEM.Net.Core.Voronoi
{
	public enum enumVoronoiTypeAjoutPointsFrontieres
	{
		standard
	}
	public enum enumVoronoiStrategieObjetsInvalides
	{
		arretTraitement
		,ignorerCesObjets
	}

	public enum enumVoronoiStrategiePointsDupliques
	{
		arretTraitement
		,deduplicationAleatoire
	}
	public enum enumVoronoiStrategieDistanceTropGrande
	{
		arretTraitement
		,reductionPrecision
	}
}
