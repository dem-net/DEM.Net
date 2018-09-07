using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
	public class HGTFile : IGeoTiff
	{
		private readonly string _filename;
		public HGTFile(string filename)
		{
			_filename = filename;
		}
		public float GetElevationAtPoint(FileMetadata metadata, int x, int y)
		{
			throw new NotImplementedException();
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
		#endregion

	}
}
