using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.netCDF
{
    public class netCDFField
    {
        public string FieldName { get; set; }
        public Type Type { get; set; }

        public static netCDFField Create<T>(string fieldName)
        {
            return new netCDFField() { FieldName = fieldName, Type = typeof(T) };
        }
    }
}
