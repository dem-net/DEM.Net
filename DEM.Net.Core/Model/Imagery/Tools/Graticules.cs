using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NetTopologySuite.Triangulate.QuadEdge;

namespace DEM.Net.Core.Imagery
{
    /// <summary>
    /// Source : https://github.com/ThinkGeo/LongLatGraticuleSample-ForWinForms/blob/master/LongLatGraticule/GraticuleAdornmentLayer.cs
    /// </summary>
    public class Graticules
    {
        private enum LineType { Meridian, Parallel };
        private Collection<double> intervals = new Collection<double>();
        int graticuleDensity;

        //Structure used for labeling the meridians and parallels at the desired location on the map in screen coordinates.
        
        
        public Graticules()
        {
            //This property gives the approximate density of lines that the map will have.
            this.graticuleDensity = 10;
            //Sets all the intervals in degree to be displayed.
            SetDefaultIntervals();
        }

        //The intervals need to be added from the smallest to the largest.
        private void SetDefaultIntervals()
        {
            intervals.Add(0.0005);
            intervals.Add(0.001);
            intervals.Add(0.002);
            intervals.Add(0.005);
            intervals.Add(0.01);
            intervals.Add(0.02);
            intervals.Add(0.05);
            intervals.Add(0.1);
            intervals.Add(0.2);
            intervals.Add(0.5);
            intervals.Add(1);
            intervals.Add(2);
            intervals.Add(5);
            intervals.Add(10);
            intervals.Add(20);
            intervals.Add(40);
            intervals.Add(50);
        }

        
        public GraticuleLabels DrawCore(LatLong upperLeft, LatLong lowerRight)
        {
            BoundingBox currentExtent = new BoundingBox(upperLeft.Long
                ,lowerRight.Long
                ,lowerRight.Lat
                ,upperLeft.Lat);

            //Gets the increment according to the current extent of the map and the graticule density set 
            //by the GraticuleDensity property
            double increment;
            increment = GetIncrement(currentExtent.Width, graticuleDensity);

            //Collections of GraticuleLabel for labeling the different lines.
            List<GraticuleLabel> meridianGraticuleLabels = new List<GraticuleLabel>();
            List<GraticuleLabel> parallelGraticuleLabels = new List<GraticuleLabel>();
            
            //Loop for displaying the meridians (lines of common longitude).
            double x = 0;
            for (x = CeilingNumber(currentExtent.xMin, increment); x <= currentExtent.xMax; x += increment)
            {
                GraticuleLabel label = new GraticuleLabel(new LatLong(currentExtent.yMin, x));
                meridianGraticuleLabels.Add(label);
             }

            //Loop for displaying the parallels (lines of common latitude).
            double y = 0;
            for (y = CeilingNumber(currentExtent.yMin, increment); y <= currentExtent.yMax; y += increment)
            {
               GraticuleLabel label = new GraticuleLabel(new LatLong(y, currentExtent.xMax));
                parallelGraticuleLabels.Add(label);
            }


            return new GraticuleLabels(meridianGraticuleLabels, parallelGraticuleLabels);


            /*//Loop for displaying the label for the meridians.
           foreach (GraticuleLabel meridianGraticuleLabel in meridianGraticuleLabels)
           {
               Collection<ScreenPointF> locations = new Collection<ScreenPointF>();
               locations.Add(new ScreenPointF(meridianGraticuleLabel.location.X, meridianGraticuleLabel.location.Y + 6));

               canvas.DrawText(meridianGraticuleLabel.label, new GeoFont("Arial", 10), new GeoSolidBrush(GeoColor.StandardColors.Navy),
                   new GeoPen(GeoColor.StandardColors.White, 2), locations, DrawingLevel.LevelFour, 8, 0, 0);
           }

           //Loop for displaying the label for the parallels.
           foreach (GraticuleLabel parallelGraticuleLabel in parallelGraticuleLabels)
           {
               Collection< ScreenPointF> locations = new Collection<ScreenPointF>();
               locations.Add(new ScreenPointF(parallelGraticuleLabel.location.X,parallelGraticuleLabel.location.Y));

               canvas.DrawText(parallelGraticuleLabel.label, new GeoFont("Arial", 10), new GeoSolidBrush(GeoColor.StandardColors.Navy),
                   new GeoPen(GeoColor.StandardColors.White,2), locations, DrawingLevel.LevelFour, 8, 0, 90);
           }*/
        }

        //Formats the decimal degree value into Degree Minute and Seconds according to the increment. It also looks
        //if the longitude is East or West and the latitude North or South.
        private string FormatLatLong(double value, LineType lineType, double increment)
        {
            string result = value.ToString();
            /*try
            {
                if (increment >= 1)
                {
                    result = DecimalDegreesHelper.GetDegreesMinutesSecondsStringFromDecimalDegree(Math.Abs(value));
                   result = result.Substring(0, result.Length - 9);
                }
                else if (increment >= 0.1)
                {
                    result = DecimalDegreesHelper.GetDegreesMinutesSecondsStringFromDecimalDegree(Math.Abs(value));
                    result = result.Substring(0, result.Length - 5);
                }
                else if (increment >= 0.01)
                {
                    result = DecimalDegreesHelper.GetDegreesMinutesSecondsStringFromDecimalDegree(Math.Abs(value));
                }
                else 
                {
                    result = DecimalDegreesHelper.GetDegreesMinutesSecondsStringFromDecimalDegree(Math.Abs(value), 2);
                }

                if (lineType == LineType.Meridian)
                {
                    if (value > 0) result = result + " E";
                    else if (value < 0) result = result + " W";
                }

                if (lineType == LineType.Parallel)
                {
                    if (value > 0) result = result + " N";
                    else if (value < 0) result = result + " S";
                }
            }
            catch
            { result = "N/A"; }
            finally {}
*/

            return result;
        }

        //Function used for determining the degree value to use according to the interval.
        private double CeilingNumber(double Number, double Interval)
        {
            double result = 0;
            double IEEERemainder = Math.IEEERemainder(Number, Interval);
            if (IEEERemainder > 0)
                result = (Number - IEEERemainder) + Interval;
            else if (IEEERemainder < 0)
                result = Number + Math.Abs(IEEERemainder);
            else
                result = Number;
            return result;
        }

        //Gets the increment to used according to the with of the current extent and the graticule density.
        private double GetIncrement(double CurrentExtentWidth, double Divisor)
        {
            double result = 0;
            double rawInterval = CurrentExtentWidth / Divisor;

            int i = 0;
            foreach (double interval in intervals)
            {
                if (rawInterval < intervals[i])
                {
                    result = intervals[i];
                    break;
                }
                i++;
            }
            if (result == 0) result = intervals[intervals.Count - 1];
            return result;
        }
    }
    
    public struct GraticuleLabel
    {
        public string label;
        public Point<int> location;
        public LatLong worldLocation;
             
        public GraticuleLabel(string Label, Point<int> Location)
        {
            this.label = Label;
            this.location = Location;
            this.worldLocation = new LatLong(0, 0);
        }
        public GraticuleLabel(LatLong worldLocation)
        { this.label = null;
            this.location =new Point<int>(0,0);
            this.worldLocation = worldLocation;
        }
    }
    
    public class GraticuleLabels
    {
        public List<GraticuleLabel> VerticalLabels { get; set; }
        public List<GraticuleLabel> HorizontalLabels { get; set; }
             
        public GraticuleLabels(List<GraticuleLabel> verticalLabels, List<GraticuleLabel> horizontalLabels)
        {
            this.VerticalLabels = verticalLabels;
            this.HorizontalLabels = horizontalLabels;
        }
    }
}