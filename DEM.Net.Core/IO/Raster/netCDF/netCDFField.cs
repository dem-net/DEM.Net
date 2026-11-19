using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.netCDF;

public class netCDFField
{
    public string FieldName { get; set; } = null!;
    public Type Type { get; set; } = null!;

    public static netCDFField Create<T>(string fieldName)
    {
        return new netCDFField() { FieldName = fieldName, Type = typeof(T) };
    }
}
