using DEM.Net.Core.Imagery;
using DEM.Net.Core.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;

#pragma warning disable CA1416

namespace DEM.Net.Core
{
    public class AdornmentsService
    {
        private readonly MeshService _meshService;
        private readonly ILogger<AdornmentsService> _logger;

        public AdornmentsService(MeshService meshService, ILogger<AdornmentsService> logger)
        {

            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
              throw new PlatformNotSupportedException( "AdornmentsService is not supported on Linux platform." );

            this._meshService = meshService;
            this._logger = logger;
        }

        public TriangulationList<Vector3> CreateModelAdornments(DEMDataSet dataset, ImageryProvider imageryProvider, BoundingBox bboxDemSpace, BoundingBox bboxModelSpace)
        {
            var width = (float)new GeoPoint(latitude: bboxDemSpace.Center[1],
                                     longitude: bboxDemSpace.Center[0] - bboxDemSpace.Width / 2f)
                            .DistanceTo(
                                new GeoPoint(latitude: bboxDemSpace.Center[1],
                                    longitude: bboxDemSpace.Center[0] + bboxDemSpace.Width / 2f)
                              );

            // bbox size
            float projHeight = (float)bboxModelSpace.Height;
            float arrowSizeFactor = projHeight / 3f;
            float zCenter = (float)bboxModelSpace.Center[2];
            float projWidth = (float)bboxModelSpace.Width;
            // 
            float PI = (float)Math.PI;

            // Arrow
            TriangulationList<Vector3> adornments = _meshService.CreateArrow().ToGlTFSpace()
                .Scale(arrowSizeFactor)
                .Translate(new Vector3(-projWidth * 0.55f, 0, zCenter));

            // North text 'N'
            adornments += this.CreateText("N", VectorsExtensions.CreateColor(255, 255, 255)).ToGlTFSpace()
                       .Scale(projHeight / 200f / 5f)
                       .RotateX(-PI / 2)
                       .Translate(new Vector3(-projWidth * 0.55f, arrowSizeFactor * 1.1f, zCenter));

            // Scale bar
            var scaleBar = this.CreateScaleBar(width, projWidth, radius: projHeight / 200f).ToGlTFSpace();
            var scaleBarSize = scaleBar.GetBoundingBox().Height;
            adornments += scaleBar
                .RotateZ(PI / 2f)
                .Translate(new Vector3(projWidth / 2, -projHeight / 2 - projHeight * 0.05f, zCenter));

            var text = $"{dataset.Attribution.Subject}: {dataset.Attribution.Text}";
            if (imageryProvider != null)
                text = string.Concat(text, $"{Environment.NewLine}{imageryProvider.Attribution.Subject}: {imageryProvider.Attribution.Text}");
            var text3D = this.CreateText(text, VectorsExtensions.CreateColor(255, 255, 255)).ToGlTFSpace();
            var textWidth = (float)text3D.GetBoundingBox().Width;
            var scale = (float)(((projWidth - scaleBarSize) * 0.9f) / textWidth);

            text3D = text3D.Scale((float)scale)
                            .RotateX(-PI / 2)
                            .Translate(new Vector3((-projWidth + textWidth * scale) / 2f, -projHeight * 0.55f, zCenter));
            adornments += text3D;

            return adornments;

        }

        public TriangulationList<Vector3> CreateText(string text, Vector4 color)
        {
            List<Polygon<Vector3>> letterPolygons = GetTextPolygons(text);
            TriangulationList<Vector3> triangulation = _meshService.Extrude(letterPolygons);
            triangulation.Colors = triangulation.Positions.Select(p => color).ToList();
            triangulation = triangulation.CenterOnOrigin();
            return triangulation;
        }

        private TriangulationList<Vector3> CreateScaleBar(float modelSize4326, float modelSizeProjected, float radius = 10f)
        {
            int nSteps = 4;
            ScaleBarInfo scaleInfo = GetScaleBarWidth(modelSize4326, modelSizeProjected, scaleBarSizeRelativeToModel: 0.5f, nSteps);

            Vector3 currentPosition = Vector3.Zero;
            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();
            for (int i = 0; i < nSteps; i++)
            {
                currentPosition.Z = scaleInfo.StepSizeProjected * i;

                triangulation += _meshService.CreateCylinder(currentPosition, radius, scaleInfo.StepSizeProjected
                        , color: i % 2 == 0 ? VectorsExtensions.CreateColor(0, 0, 0) : VectorsExtensions.CreateColor(255, 255, 255));
            }

            // scale units (m or km ?)
            string scaleLabel = (scaleInfo.TotalSize / 1000f > 1) ? $"{scaleInfo.TotalSize / 1000:F0} km" : $"{scaleInfo.TotalSize} m";

            triangulation += CreateText(scaleLabel, color: VectorsExtensions.CreateColor(255, 255, 255))
                            .Scale(radius / 5)
                            .RotateY((float)Math.PI / 2)
                            .RotateZ((float)Math.PI / 2)
                            .Translate(new Vector3(radius * 5, -radius, scaleInfo.TotalSizeProjected / 2));

            return triangulation;
        }

        private ScaleBarInfo GetScaleBarWidth(float totalWidth, float modelSizeProjected, float scaleBarSizeRelativeToModel = 0.5f, int nSteps = 4)
        {
            // must be divisible by 4
            float[] smallestScaleStep = { 1, 2, 5, 10, 20, 25, 50, 100, 250, 500, 1000, 2000, 2500, 5000, 10000, 20000, 25000, 50000, 100000, 200000, 500000, 1000000, 2000000, 5000000, 10000000 };

            var scaleBarTotalRequestedSize = totalWidth * scaleBarSizeRelativeToModel;
            var bestScale = smallestScaleStep.Select(s => new { Step = s, diff = Math.Abs(1 - (scaleBarTotalRequestedSize / (s * nSteps))) })
                              .OrderBy(s => s.diff)
                              .First();
            var scaleBarTotalSize = bestScale.Step * nSteps;

            var projSize = MathHelper.Map(0, totalWidth, 0, modelSizeProjected, scaleBarTotalSize, false);
            var projStepSize = MathHelper.Map(0, totalWidth, 0, modelSizeProjected, bestScale.Step, false);
            return new ScaleBarInfo
            {
                NumSteps = nSteps,
                TotalSizeProjected = projSize,
                StepSizeProjected = projStepSize,
                StepSize = bestScale.Step,
                TotalSize = scaleBarTotalSize
            };

        }

        // Preferred font first (historical GDI+ choice), then common cross-platform fallbacks.
        private static readonly string[] PreferredFontFamilies = { "Calibri", "Arial", "Liberation Sans", "DejaVu Sans", "Helvetica", "Noto Sans" };
        private static FontFamily? _textFontFamily;

        private FontFamily GetTextFontFamily()
        {
            if (_textFontFamily.HasValue)
                return _textFontFamily.Value;

            foreach (var name in PreferredFontFamilies)
            {
                if (SystemFonts.TryGet(name, out FontFamily family))
                {
                    _textFontFamily = family;
                    return family;
                }
            }

            var any = SystemFonts.Families.FirstOrDefault();
            if (string.IsNullOrEmpty(any.Name))
                throw new InvalidOperationException("No system font found to render model adornment text. Install at least one TrueType font on the host.");

            _logger.LogWarning("None of the preferred fonts ({Fonts}) found, using {Font}.", string.Join(", ", PreferredFontFamilies), any.Name);
            _textFontFamily = any;
            return any;
        }

        public List<Polygon<Vector3>> GetTextPolygons(string text)
        {
            Dictionary<int, Polygon<Vector3>> letterPolygons = new Dictionary<int, Polygon<Vector3>>();

            // 40pt at 72 dpi = 40 px em size, same as the former GDI+ GraphicsPath.AddString(..., 40f, ...)
            Font font = GetTextFontFamily().CreateFont(40f);
            IPathCollection glyphPaths = TextBuilder.GenerateGlyphs(text, new TextOptions(font));

            // Flatten curves into polylines: each simple path is one glyph contour (outer ring or hole)
            int i = -1;
            foreach (ISimplePath subPath in glyphPaths.SelectMany(p => p.Flatten()))
            {
                i++;
                if (!subPath.IsClosed)
                {
                    _logger.LogWarning("Unclosed text shape. Skipping.");
                    continue;
                }

                var currentRing = new List<Vector3>(subPath.Points.Length);
                foreach (var pt in subPath.Points.Span)
                    currentRing.Add(new Vector3(pt.X, pt.Y, 0));
                if (currentRing.Count < 3)
                    continue;

                List<int> childs = GetIncludedPolygons(currentRing, letterPolygons);
                List<int> parents = GetContainerPolygons(currentRing, letterPolygons);
                // contains other polygon ?
                if (childs.Any())
                {
                    Polygon<Vector3> newPoly = new Polygon<Vector3>(currentRing);
                    foreach (var key in childs)
                    {
                        var child = letterPolygons[key];
                        letterPolygons.Remove(key);
                        newPoly.InteriorRings.Add(child.ExteriorRing);
                    }
                    letterPolygons.Add(i, newPoly);
                }
                else if (parents.Any())
                {
                    Debug.Assert(parents.Count == 1);
                    letterPolygons[parents.First()].InteriorRings.Add(currentRing);
                }
                else
                {
                    letterPolygons.Add(i, new Polygon<Vector3>(currentRing));
                }
            }

            return letterPolygons.Values.ToList();
        }

        private List<int> GetContainerPolygons(List<Vector3> currentPolygon, Dictionary<int, Polygon<Vector3>> polygons)
        {
            List<int> parents = polygons.Where(p => IsPointInPolygon(p.Value.ExteriorRing, currentPolygon[0]))
                                        .Select(p => p.Key)
                                        .ToList();
            return parents;
        }

        private List<int> GetIncludedPolygons(List<Vector3> currentPolygon, Dictionary<int, Polygon<Vector3>> polygons)
        {
            List<int> childs = polygons.Where(p => IsPointInPolygon(currentPolygon, p.Value.ExteriorRing[0]))
                                        .Select(p => p.Key)
                                        .ToList();
            return childs;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/4243042/c-sharp-point-in-polygon
        /// Determines if the given point is inside the polygon. (does not support inner 'holes' rings)
        /// </summary>
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        private bool IsPointInPolygon(List<Vector3> polygon, Vector3 testPoint)
        {
            bool result = false;
            int j = polygon.Count - 1;
            for (int i = 0; i < polygon.Count; i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

    }
}

#pragma warning restore CA1416
