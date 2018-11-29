using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    /// <summary>
    /// SRTM height file
    /// https://wiki.openstreetmap.org/wiki/SRTM
    /// </summary>
	public class HGTFile : IRasterFile
    {
        public const int HGT3601 = 25934402;
        public const int HGT1201 = 2884802;

        private readonly string _filename;
        public HGTFile(string filename)
        {
            _filename = filename;
        }
        public float GetElevationAtPoint(FileMetadata metadata, int x, int y)
        {
            using (Stream hgtStream = new FileStream(metadata.Filename, FileMode.Open))
            {
                switch (hgtStream.Length)
                {
                    case HGTFile.HGT1201:
                        GetHGTValue(x, y, hgtData, latAdj, lonAdj, 1200, 2402);
                        break;
                    case HGTFile.HGT3601:
                        GetHGTValue(x, y, hgtData, latAdj, lonAdj, 3600, 7202);
                        break;
                }
            }

        }
        private float GetHGTValue(int x, int y, byte[] hgtData, int latAdj, int lonAdj, int width, int stride)
        {
            double y = node.Latitude;
            double x = node.Longitude;
            var offset = ((int)((x - (int)x + lonAdj) * width) * 2 + (width - (int)((y - (int)y + latAdj) * width)) * stride);
            var h1 = hgtData[offset + 1] + hgtData[offset + 0] * 256;
            var h2 = hgtData[offset + 3] + hgtData[offset + 2] * 256;
            var h3 = hgtData[offset - stride + 1] + hgtData[offset - stride + 0] * 256;
            var h4 = hgtData[offset - stride + 3] + hgtData[offset - stride + 2] * 256;

            var m = Math.Max(h1, Math.Max(h2, Math.Max(h3, h4)));
            if (h1 == -32768)
                h1 = m;
            if (h2 == -32768)
                h2 = m;
            if (h3 == -32768)
                h3 = m;
            if (h4 == -32768)
                h4 = m;

            var fx = node.Longitude - (int)(node.Longitude);
            var fy = node.Latitude - (int)(node.Latitude);

            var elevation = (int)Math.Round((h1 * (1 - fx) + h2 * fx) * (1 - fy) + (h3 * (1 - fx) + h4 * fx) * fy);

            node.Elevation = elevation < -1000 ? 0 : elevation;
        }

        public FileMetadata ParseMetaData()
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // Pour détecter les appels redondants

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: supprimer l'état managé (objets managés).
                }

                // TODO: libérer les ressources non managées (objets non managés) et remplacer un finaliseur ci-dessous.
                // TODO: définir les champs de grande taille avec la valeur Null.

                disposedValue = true;
            }
        }

        // TODO: remplacer un finaliseur seulement si la fonction Dispose(bool disposing) ci-dessus a du code pour libérer les ressources non managées.
        // ~HGTFile() {
        //   // Ne modifiez pas ce code. Placez le code de nettoyage dans Dispose(bool disposing) ci-dessus.
        //   Dispose(false);
        // }

        // Ce code est ajouté pour implémenter correctement le modèle supprimable.
        public void Dispose()
        {
            // Ne modifiez pas ce code. Placez le code de nettoyage dans Dispose(bool disposing) ci-dessus.
            Dispose(true);
            // TODO: supprimer les marques de commentaire pour la ligne suivante si le finaliseur est remplacé ci-dessus.
            // GC.SuppressFinalize(this);
        }

        public HeightMap ParseGeoDataInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = float.MinValue)
        {
            throw new NotImplementedException();
        }

        public HeightMap ParseGeoData(FileMetadata metadata)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
