namespace DEM.Net.Lib.Services
{
    public enum enElevationStrategy
    {
        /// <summary>
        /// Return all line points plus intersections with DEM grid
        /// </summary>
        MaximumDetail,

        /// <summary>
        /// Return only elevation for line points
        /// All DEM data between points will not be returned
        /// </summary>
        OnlyLinePoints,

    }
}