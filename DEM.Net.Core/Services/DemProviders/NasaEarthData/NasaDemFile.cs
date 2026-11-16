using System;
using System.Collections.Generic;
using System.Text;
using DEM.Net.Core.EarthData;

namespace DEM.Net.Core.EarthData;

/// <summary>
/// Extract of relevant granule information for DEM Net
/// (whole granule is too big)
/// </summary>
public class NasaDemFile(string producerGranuleId, string box, string zipFileLink)
{
    public string Box { get; set; } = box;

    public string ZipFileLink { get; set; } = zipFileLink;

    public string GranuleId { get; set; } = producerGranuleId;

    [System.Text.Json.Serialization.JsonConstructor]
    internal NasaDemFile() : this(string.Empty, string.Empty, string.Empty)
    {
    }
}
