namespace DEM.Net.Core.Imagery
{
    public interface ITileGenerator
    {
        byte[] GenerateTile(int x, int y, int zoom);
    }
}