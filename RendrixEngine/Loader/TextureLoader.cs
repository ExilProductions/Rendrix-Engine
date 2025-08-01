using RendrixEngine.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RendrixEngine.Loader
{
    public class TextureLoader : IDisposable
    {
        private bool _disposed;
        public Texture LoadTexture(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            try
            {
                return new Texture(new Bitmap(bitmap));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load texture from bitmap resource", ex);
            }
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
            }

            _disposed = true;
        }

        ~TextureLoader()
        {
            Dispose(false);
        }
    }
}
