using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
	public static class Smoothing
	{
		
		/// <summary>
		/// WindowSmooth
		/// Source : https://github.com/Andy9FromSpace/map-elevation
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="smoothingFilter">test with new float[] { 0.1f, 1f, 0.1f } for 1arc /second</param>
		public static void WindowSmooth(List<GeoPoint> nodes, double[] smoothingFilter)
		{
			for (var i = smoothingFilter.Length / 2; i < nodes.Count - smoothingFilter.Length / 2; i++)
			{
				double elevationSum = 0;
				for (var j = -smoothingFilter.Length / 2; j <= smoothingFilter.Length / 2; j++)
				{
					elevationSum += smoothingFilter[j + smoothingFilter.Length / 2] * nodes[i - j].Elevation.GetValueOrDefault(elevationSum);
				}
				nodes[i].Elevation = elevationSum / smoothingFilter.Sum();
			}
		}

		/// <summary>
		/// FeedbackSmooth
		/// Source : https://github.com/Andy9FromSpace/map-elevation
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="smoothingFilter">test with 1, 3 for 1arc /second</param>
		public static void FeedbackSmooth(List<GeoPoint> nodes, float feedbackWeight, float currentWeight)
		{
			if (nodes.Count == 0)
				return;
			var filteredValue = nodes[0].Elevation;
			for (var i = 0; i < nodes.Count; i++)
			{
				filteredValue = (filteredValue * feedbackWeight + nodes[i].Elevation * currentWeight) / (feedbackWeight + currentWeight);
				nodes[i].Elevation = filteredValue;
			}
			filteredValue = nodes[nodes.Count - 1].Elevation;
			for (var i = nodes.Count - 1; i >= 0; i--)
			{
				filteredValue = (filteredValue * feedbackWeight + nodes[i].Elevation * currentWeight) / (feedbackWeight + currentWeight);
				nodes[i].Elevation = filteredValue;
			}
		}

	}
}
