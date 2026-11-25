using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Model;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;

#pragma warning disable CA1416

namespace DEM.Net.Core;

public class AdornmentsService(MeshService meshService, ILogger<AdornmentsService> logger)
{
    private readonly MeshService _meshService = meshService;
    private readonly ILogger<AdornmentsService> _logger = logger;

    public TriangulationList<Vector3> CreateModelAdornments(DEMDataSet dataset, ImageryProvider? imageryProvider, BoundingBox bboxDemSpace, BoundingBox bboxModelSpace)
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

        // Dataset short name only: the full attributions are in the model credits
        var text3D = this.CreateText(ShortName(dataset), VectorsExtensions.CreateColor(255, 255, 255)).ToGlTFSpace();
        var textWidth = (float)text3D.GetBoundingBox().Width;
        // Fit between the west edge and the scale bar, but no larger than the other labels
        var scale = Math.Min((float)((projWidth - scaleBarSize) * 0.9f / textWidth), LabelScale(projHeight));

        text3D = text3D.Scale((float)scale)
                        .RotateX(-PI / 2)
                        .Translate(new Vector3((-projWidth + textWidth * scale) / 2f, -projHeight * 0.55f, zCenter));
        adornments += text3D;

        return adornments;

    }

    /// <summary>Dataset name shown on the model: <see cref="DEMDataSet.Name"/>, without the zoom suffix for Mapterhorn.</summary>
    public static string ShortName(DEMDataSet dataset) => dataset.IsMapterhorn ? DEMDataSet.MapterhornName : dataset.Name;

    /// <summary>Text scale of the labels (north, scale bar, altitudes): 1/1000 of the model's north-south size per font pixel.</summary>
    private static float LabelScale(float projHeight) => projHeight / 200f / 5f;

    /// <summary>
    /// Vertical bar at the north-east corner of the model, from its lowest to its highest point, labelled with
    /// those altitudes. Model space (Z up), in final coordinates: not to be moved with the other adornments.
    /// </summary>
    /// <param name="bboxModelSpace">Model extent (X, Y)</param>
    /// <param name="zBottom">Model Z of the lowest terrain point</param>
    /// <param name="zTop">Model Z of the highest terrain point</param>
    /// <param name="minMeters">Lowest altitude, in meters (label)</param>
    /// <param name="maxMeters">Highest altitude, in meters (label)</param>
    public TriangulationList<Vector3> CreateElevationBar(BoundingBox bboxModelSpace, float zBottom, float zTop, float minMeters, float maxMeters)
    {
        float projWidth = (float)bboxModelSpace.Width;
        float projHeight = (float)bboxModelSpace.Height;
        float radius = projHeight / 200f;
        float x = (float)bboxModelSpace.xMax + projWidth * 0.05f;
        float y = (float)bboxModelSpace.yMax;
        float tickLength = radius * 8f;
        var white = VectorsExtensions.CreateColor(255, 255, 255);
        bool flat = maxMeters - minMeters < 1f || zTop - zBottom <= 0f;

        TriangulationList<Vector3> bar = new();
        if (!flat)
            bar += _meshService.CreateCylinder(new Vector3(x, y, zBottom), radius, zTop - zBottom, white);

        var labels = flat ? new[] { (z: zBottom, meters: minMeters) } : [(z: zBottom, meters: minMeters), (z: zTop, meters: maxMeters)];
        var texts = labels.Select(l => CreateText(string.Create(CultureInfo.InvariantCulture, $"{l.meters:F0} m"), white)
                                        .ToGlTFSpace() // upright, facing south (the default view)
                                        .Scale(LabelScale(projHeight))).ToList();
        float textHeight = texts.Max(t => (float)t.GetBoundingBox().Depth);

        // Labels centered on their tick, moved apart when the bar is shorter than them
        float[] textZ = [.. labels.Select(l => l.z)];
        if (!flat && zTop - zBottom < textHeight * 1.2f)
        {
            float mid = (zBottom + zTop) / 2f;
            textZ = [mid - textHeight * 0.6f, mid + textHeight * 0.6f];
        }

        for (int i = 0; i < labels.Length; i++)
        {
            // Tick: a cylinder along Z turned to point east
            bar += _meshService.CreateCylinder(Vector3.Zero, radius * 0.6f, tickLength, white)
                .RotateY((float)Math.PI / 2f)
                .Translate(new Vector3(x, y, labels[i].z));
            float textWidth = (float)texts[i].GetBoundingBox().Width;
            bar += texts[i].Translate(new Vector3(x + tickLength + radius * 2f + textWidth / 2f, y, textZ[i]));
        }

        return bar;
    }

    public TriangulationList<Vector3> CreateText(string text, Vector4 color)
    {
        List<Polygon<Vector3>> letterPolygons = GetTextPolygons(text);
        TriangulationList<Vector3> triangulation = _meshService.Extrude(letterPolygons);
        triangulation.Colors = [.. triangulation.Positions.Select(p => color)];
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
        float[] smallestScaleStep = [1, 2, 5, 10, 20, 25, 50, 100, 250, 500, 1000, 2000, 2500, 5000, 10000, 20000, 25000, 50000, 100000, 200000, 500000, 1000000, 2000000, 5000000, 10000000];

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
    private static readonly string[] PreferredFontFamilies = ["Calibri", "Arial", "Liberation Sans", "DejaVu Sans", "Helvetica", "Noto Sans"];
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
        Dictionary<int, Polygon<Vector3>> letterPolygons = [];

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

        return [.. letterPolygons.Values];
    }

    private List<int> GetContainerPolygons(List<Vector3> currentPolygon, Dictionary<int, Polygon<Vector3>> polygons)
    {
        List<int> parents = [.. polygons.Where(p => IsPointInPolygon(p.Value.ExteriorRing, currentPolygon[0])).Select(p => p.Key)];
        return parents;
    }

    private List<int> GetIncludedPolygons(List<Vector3> currentPolygon, Dictionary<int, Polygon<Vector3>> polygons)
    {
        List<int> childs = [.. polygons.Where(p => IsPointInPolygon(currentPolygon, p.Value.ExteriorRing[0])).Select(p => p.Key)];
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

#pragma warning restore CA1416
