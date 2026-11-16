using System;
using System.Collections.Generic;
using System.Linq;
using DEM.Net.Core.Gpx;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DEM.Net.Core.IO.SensorLog;

public static class SensorLogExtensions
{

    public static IEnumerable<GpxTrackPoint> ToGPX(this SensorLog sensorLog)
    {
        return sensorLog.Data
            .Where(d => d.HasCoordinates)
            .Select(d => new GpxTrackPoint()
            {
                Bearing = d.LocationTrueHeading
             ,
                Elevation = d.AltimeterRelativeAltitude
             ,
                Latitude = d.LocationLatitude.Value
             ,
                Longitude = d.LocationLongitude.Value
             ,
                Time = d.LoggingTime
            });
    }

    public static IEnumerable<GeoPoint> ToGeoPoints(this SensorLog sensorLog)
    {
        return sensorLog.Data.Where(d => d.HasCoordinates)
            .Select(d => new GeoPoint(d.LocationLatitude.Value, d.LocationLongitude.Value, d.AltimeterRelativeAltitude));
    }

    private class SeriesDefinition : Dictionary<string, (Func<SensorLogData, float?> getter, Predicate<float?> noDatavalue)>
    {
    }

    /// <param name="mergedImage">When true, all series are drawn in a single image. Otherwise (default) one image per series is written, prefixed by the series name.</param>
    public static void Plot(this SensorLog log, string outputFileName, int width = 1024, int height = 768, bool mergedImage = false)
    {
        GraphicsOptions graphicsOptions = new GraphicsOptions() { Antialias = true };
        DrawingOptions drawingOptions = new DrawingOptions { GraphicsOptions= graphicsOptions };

        SeriesDefinition seriesDefinition = new SeriesDefinition();
        var firstHeading = log.Data.First(d => d.LocationTrueHeading.HasValue).LocationTrueHeading.Value;
        //seriesDefinition.Add("LocationLatitude", (getter: data => data.LocationLatitude.Value, noDatavalue: v => v == null));
        //seriesDefinition.Add("LocationLongitude", (getter: data => data.LocationLongitude.Value, noDatavalue: v => v == 0));
        //seriesDefinition.Add("AltimeterRelativeAltitude", (getter: data => data.AltimeterRelativeAltitude, noDatavalue: v => v <= 0));
        seriesDefinition.Add("LocationMagneticHeading", (getter: data => data.LocationMagneticHeading, noDatavalue: v => v == null));
        seriesDefinition.Add("LocationTrueHeading", (getter: data => data.LocationTrueHeading, noDatavalue: v => v == null));
        seriesDefinition.Add("LocationHeadingAccuracy", (getter: data => data.LocationHeadingAccuracy, noDatavalue: v => v == null));
        int margin = 10;
        //width = log.Data.Count + 2 * margin;
        List<PointF[]> dataseries = GetDataSeries(log, seriesDefinition);
        Color[] palette = [Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Purple, Color.Yellow, Color.Gray, Color.LimeGreen];
        var SeriesAndRanges = UpdateRanges(dataseries, seriesDefinition, width, height, margin);


        var font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 12, FontStyle.Bold);
        float textSpacing = 3;

        if (mergedImage)
        {
            using Image<Rgba32> img = new Image<Rgba32>(width, height);
            img.Mutate(o =>
                    {
                        o.BackgroundColor(Color.White);
                        DrawAxis(o, width, height, margin);

                        int serieIndex = 0; int colorIndex = 0;
                        foreach (var serieDef in seriesDefinition)
                        {
                            var serie = SeriesAndRanges.Series[serieIndex];
                            Console.WriteLine($"Drawing serie {serieDef.Key}...");
                            var pointsWithData = serie.Where(p => p.Y >= margin && p.Y <= (height - margin)).ToArray();
                            o.DrawLine(drawingOptions, palette[colorIndex], 1f, pointsWithData);


                            serieIndex++;
                            colorIndex = (colorIndex + 1) % palette.Length;
                        }

                        // Legend
                        serieIndex = 0; colorIndex = 0;
                        Console.WriteLine($"Drawing legend...");
                        foreach (var serieDef in seriesDefinition)
                        {
                            var serie = SeriesAndRanges.Series[serieIndex];

                            var text = $"{serieDef.Key} {SeriesAndRanges.Ranges[serieIndex].min:F2} to {SeriesAndRanges.Ranges[serieIndex].max:F2}";
                            var textSize = TextMeasurer.MeasureSize(text, new TextOptions(font));
                            var yText = margin + (textSize.Height + textSpacing) * serieIndex;
                            o.DrawLine(drawingOptions, palette[colorIndex], 2f, new PointF(margin * 2 + 2, yText), new PointF(margin * 2 + 45, yText));
                            o.DrawText(drawingOptions, text, font, Color.Black, new PointF(margin * 2 + 50, yText));

                            serieIndex++;
                            colorIndex = (colorIndex + 1) % palette.Length;
                        }
                    }
            );

            img.Save(outputFileName);
        }
        else
        {
            int serieIndex = 0; int colorIndex = 0;
            foreach (var serieDef in seriesDefinition)
            {
                using (Image<Rgba32> img = new Image<Rgba32>(width, height))
                {
                    img.Mutate(o =>
                    {
                        o.BackgroundColor(Color.White);
                        DrawAxis(o, width, height, margin);


                        var serie = SeriesAndRanges.Series[serieIndex];
                        Console.WriteLine($"Drawing serie {serieDef.Key}...");
                        var pointsWithData = serie.Where(p => p.Y >= margin && p.Y <= (height - margin)).ToArray();
                        o.DrawLine(drawingOptions, palette[colorIndex], 1f, pointsWithData);
                        

                        // Legend
                        Console.WriteLine($"Drawing legend...");

                        var text = $"{serieDef.Key} {SeriesAndRanges.Ranges[serieIndex].min:F2} to {SeriesAndRanges.Ranges[serieIndex].max:F2}";
                        var textSize = TextMeasurer.MeasureSize(text, new TextOptions(font));
                        var yText = margin + (textSize.Height + textSpacing);
                        o.DrawLine(drawingOptions, palette[colorIndex], 2f, new PointF(margin * 2 + 2, yText), new PointF(margin * 2 + 45, yText));
                        o.DrawText(drawingOptions, text, font, Color.Black, new PointF(margin * 2 + 50, yText));
                    }
                    );

                    img.Save($"{serieDef.Key}_{outputFileName}");
                }
                serieIndex++;
                colorIndex = (colorIndex + 1) % palette.Length;
            }
        }
    }


    private static List<PointF[]> GetDataSeries(SensorLog log, SeriesDefinition seriesDefinition)
    {
        List<PointF[]> series = [];
        int gettersCount = seriesDefinition.Count;
        foreach (var def in seriesDefinition)
        {
            var g = def.Value.getter;
            series.Add(new PointF[log.Count]);
        }

        for (int i = 0; i < log.Count; i++)
        {
            var data = log.Data[i];
            int defIndex = 0;
            foreach (var def in seriesDefinition)
            {
                var getter = def.Value.getter;
                float? dataValue = def.Value.getter(data);
                if (dataValue.HasValue)
                {
                    series[defIndex][i] = new PointF(i, def.Value.getter(data).Value);
                }
                defIndex++;
            }
        }
        return series;
    }
    private static (List<PointF[]> Series, List<(float min, float max)> Ranges) UpdateRanges(List<PointF[]> dataseries, SeriesDefinition seriesDefinition, int width, int height, int margin)
    {
        int dataCount = dataseries.First().Length;
        (float min, float max)[] ranges = ComputeRanges(dataseries, [.. seriesDefinition.Select(kvp => kvp.Value.noDatavalue)]);
        var seriesKeys = seriesDefinition.Keys.ToArray();
        for (int i = 0; i < dataseries.Count; i++)
        {
            float MapY(float value) => MathHelper.Map(ranges[i].min, ranges[i].max, height - margin, margin, ranges[i].max - value + ranges[i].min, false);

            for (int j = 0; j < dataCount; j++)
            {
                // Map index to xAxis
                dataseries[i][j].X = MathHelper.Map(0, dataCount, margin, width - margin, dataseries[i][j].X, false);

                // Map Y value to yAxis bounded
                dataseries[i][j].Y = MapY(dataseries[i][j].Y);
            }
        }
        return (dataseries, ranges.ToList());
    }
    private static (float min, float max)[] ComputeRanges(List<PointF[]> dataseries, List<Predicate<float?>> noDataValues)
    {
        if (noDataValues == null)
        {
            return [.. dataseries.Select((data, index) => (
                data.Min(d => d.Y)
                , data.Max(d => d.Y)
            ))];
        }
        else
        {
            return [.. dataseries.Select((data, index) => (
                data.Where(d => !noDataValues[index](d.Y)).Min(d => d.Y)
                , data.Where(d => !noDataValues[index](d.Y)).Max(d => d.Y)
            ))];
        }
    }


    private static void DrawAxis(IImageProcessingContext ctx, int width, int height, int margin)
    {
        PointF[] xAxis = [new(margin, height - margin), new(width - margin, height - margin)];
        PointF[] yAxis = [new(margin, margin), new(margin, height - margin)];

        ctx.DrawLine(Color.Black, 1, xAxis)
           .DrawLine(Color.Black, 1, yAxis);
    }

}
