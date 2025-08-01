using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RendrixEngine.Models
{
    public class Texture : IDisposable
    {
        private bool _disposed;
        private readonly Bitmap _bitmap;

        public int Width => _bitmap.Width;
        public int Height => _bitmap.Height;
        public Size Size => _bitmap.Size;

        public Texture(Bitmap bitmap)
        {
            _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        }

        // Get the underlying bitmap (read-only access)
        public Bitmap GetBitmap() => _bitmap;

        // Example method to get pixel data
        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException($"Pixel coordinates ({x},{y}) out of bounds");
            return _bitmap.GetPixel(x, y);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _bitmap?.Dispose();
            }

            _disposed = true;
        }

        ~Texture()
        {
            Dispose(false);
        }
    }
}
