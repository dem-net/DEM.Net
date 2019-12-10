using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DEM.Net.Core.IO.SensorLog
{
    public static class SensorLog
    {
        public static List<SensorLogData> FromJson(string filePath)
        {
            return SensorLogData.FromJson(File.ReadAllText(filePath, Encoding.UTF8));
        }

    }
}
