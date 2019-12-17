using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DEM.Net.Core.IO.SensorLog
{
    public class SensorLog
    {
        public List<SensorLogData> Data { get; private set; }
        public int LocationCount { get; private set; }
        public int Count { get; private set; }
        public int AltitudeCount { get; private set; }
        public float AltitudeMin { get; private set; }
        public float AltitudeMax { get; private set; }

        public static SensorLog FromJson(string filePath)
        {
            SensorLog sensorLog = new SensorLog();

            sensorLog.Data = SensorLogData.FromJson(File.ReadAllText(filePath, Encoding.UTF8));

            sensorLog.Count = sensorLog.Data.Count;
            foreach(var data in sensorLog.Data)
            {
                sensorLog.LocationCount += (data.LocationLongitude != 0 && data.LocationLatitude.HasValue) ? 1 : 0;
                sensorLog.AltitudeCount += (data.LocationAltitude.HasValue) ? 1 : 0;
            }
            sensorLog.AltitudeMin = sensorLog.Data.Where(s=>s.LocationAltitude.HasValue).Min(s => s.LocationAltitude.Value);
            sensorLog.AltitudeMax = sensorLog.Data.Where(s => s.LocationAltitude.HasValue).Max(s => s.LocationAltitude.Value);

            return sensorLog;
        }

    }
}
