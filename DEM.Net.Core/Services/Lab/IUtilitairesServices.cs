// IUtilitairesServices.cs
//
// Author:
//       Frédéric Aubin
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
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

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace DEM.Net.Core.Services.Lab
{

    public interface IUtilitairesServices
    {

        //string GethCodeGeogPoint(double[] p_coord, int p_nbreDecimalesMoins1SiToutes=2, char p_separateur = '_');
        string GetHCodeGeogPoint(double[] p_coordPoint, int p_nbreDecimales = 2, char p_separateur = '_');
        string GetHCodeGeogPoint(List<double[]> p_points, int p_nbreDecimales = 2, char p_separateur = '_');
        string GethCodeGeogSegment(double[] p_coord1, double[] p_coord2, int p_nbreDecimales = 2, char p_separateur = '_');
        Dictionary<string, List<BeanPoint_internal>> GetPointsRegroupesParHCode(List<BeanPoint_internal> p_dataPointsTests);
        //
        Geometry GetGeometryArc(BeanArc_internal p_arc, bool ifPt1AndPt2IqualReturnPointElseNull);
        Geometry GetGeometryLine(double[] p_coordPoint1, double[] p_coordPoint2, int p_srid, bool ifPt1AndPt2IqualReturnPointElseNull);
        Geometry GetGeometryPolygon(List<double[]> p_coordPointsDuContour, int p_srid);
        NetTopologySuite.Geometries.Point ConstructPoint(double x, double y, int srid);
    }
}