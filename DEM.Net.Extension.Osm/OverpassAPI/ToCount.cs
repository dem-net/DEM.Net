using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DEM.Net.Extension.Osm.Model;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DEM.Net.Extension.Osm.OverpassAPI
{

    /// <summary>
    /// The JSON result of an Overpass query.
    /// </summary>
    public static partial class OverpassAPIExtentions
    {

        public static Task<OverpassCountResult> ToCount(this Task<OverpassResult> ResultTask)
        {
            return ResultTask.ContinueWith(task => {
                return ResultTask.Result.Elements.First().ToObject<OverpassCountResult>();
            });
        }
    }
}
