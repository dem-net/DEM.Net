using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Imagery
{

#pragma warning disable CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement

#if NETFULL
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;

    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }

        public void SetPixel(int x, int y, Color colour)
        {
            int index = x + (y * Width);
            int col = colour.ToArgb();

            Bits[index] = col;
        }

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            Color result = Color.FromArgb(col);

            return result;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }
#else
    public class DirectBitmap : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
#endif

#pragma warning restore CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement
}
