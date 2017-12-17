using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services
{
    public class DouglasPeucker
    {
        private List<GeoPoint> _pointsList;
        private double _errorBound;
        public DouglasPeucker(List<GeoPoint> pointsList, double errorBound)
        {
            _pointsList = new List<GeoPoint>();
            if (pointsList != null)
            {
                GeoPoint last = pointsList.First();
                _pointsList.Add(last);

                for (int i = 1; i < pointsList.Count; i++)
                {
                    if (!last.Equals(pointsList[i]))
                    {
                        _pointsList.Add(pointsList[i]);
                        last = pointsList[i];
                    }
                }
            }
            _errorBound = errorBound;
        }

        public List<GeoPoint> Compress()
        {
            if (_pointsList == null || _pointsList.Count <= 2)
            {
                return _pointsList;
            }

            List<GeoPoint> result = CompressHelper(_pointsList);
            result.Add(_pointsList.Last());

            return result;
        }

        private List<GeoPoint> CompressHelper(List<GeoPoint> pointsList)
        {
            if (pointsList.Count < 2)
            {
                return pointsList;
            }

            List<GeoPoint> result = new List<GeoPoint>();

            GeoSegment line = new GeoSegment(pointsList.First(), pointsList.Last());

            double maxDistance = 0;
            int maxIndex = 0;

            for (int i = 1; i < pointsList.Count - 1; i++)
            {
                var distance = Distance(pointsList[i], line);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }

            if (maxDistance <= _errorBound)
            {
                result.Add(pointsList.First());
            }
            else
            {
                var r1 = CompressHelper(pointsList.GetRange(0, maxIndex + 1));
                var r2 = CompressHelper(pointsList.GetRange(maxIndex, pointsList.Count - maxIndex));
                result.AddRange(r1);
                result.Add(pointsList[maxIndex]);
                result.AddRange(r2);
            }

            return result;
        }

        private double Distance(GeoPoint p, GeoSegment line)
        {
            //var p1 = line.Start;
            //var p2 = line.End;
            //return Math.Abs(
            //        ((p2.Longitude - p1.Longitude) * p.Latitude + (p1.Latitude - p2.Latitude) * p.Longitude + (p1.Longitude - p2.Longitude) * p1.Latitude + (p2.Latitude - p1.Latitude) * p1.Longitude) /
            //        Math.Sqrt((p2.Longitude - p1.Longitude) * (p2.Longitude - p1.Longitude) + (p1.Latitude - p2.Latitude) * (p1.Latitude - p2.Latitude))
            //    );
            var p1 = line.Start;
            var p2 = line.End;
            var p1Alt = p1.Altitude.GetValueOrDefault(0);
            var p2Alt = p2.Altitude.GetValueOrDefault(0);
            return Math.Abs(
                    ((p2.DistanceFromOriginMeters - p1.DistanceFromOriginMeters) * p.Altitude.GetValueOrDefault(0) + (p1Alt - p2Alt) * p.DistanceFromOriginMeters + (p1.DistanceFromOriginMeters - p2.DistanceFromOriginMeters) * p1Alt + (p2Alt - p1Alt) * p1.DistanceFromOriginMeters) /
                    Math.Sqrt((p2.DistanceFromOriginMeters - p1.DistanceFromOriginMeters) * (p2.DistanceFromOriginMeters - p1.DistanceFromOriginMeters) + (p1Alt - p2Alt) * (p1Alt - p2Alt))
                );
        }
    }
}
