using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    /// <summary>
    /// Dictionary with IDisposable values than can be used in a using block
    /// </summary>
    public class DisposableValueDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDisposable
        where TKey : IEquatable<TKey>
        where TValue : IDisposable
    {
        public void Dispose()
        {
            if (this != null && this.Any())
            {
                foreach (var keyValuePair in this)
                {
                    keyValuePair.Value.Dispose();
                }
                base.Clear();
                //GC.SuppressFinalize(this);
            }
        }

        public new void Clear()
        {
            this.Dispose();
        }
    }

    public class GeoTiffDictionary : DisposableValueDictionary<FileMetadata, IGeoTiff>
    { }
}
