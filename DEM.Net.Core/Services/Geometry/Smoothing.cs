// Smoothing.cs
//
// Author:
//       Xavier Fischer
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
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
