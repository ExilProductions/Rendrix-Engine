using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RendrixEngine
{
    public class Texture : IDisposable
    {
        private bool _disposed;
        private readonly Image<Rgba32> _image;

        public int Width => _image.Width;
        public int Height => _image.Height;

        public Texture()
        {
        }

        /// <summary>
        /// Create a Texture from an ImageSharp image
        /// </summary>
        public Texture(Image<Rgba32> image)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
        }

        /// <summary>
        /// Create a Texture directly from an embedded resource byte array
        /// </summary>
        /// <param name="resourceData">Byte array containing image data</param>
        public static Texture FromBytes(byte[] resourceData)
        {
            if (resourceData == null || resourceData.Length == 0)
                throw new ArgumentException("Resource data cannot be null or empty", nameof(resourceData));

            using var ms = new MemoryStream(resourceData);
            var image = Image.Load<Rgba32>(ms);
            return new Texture(image);
        }

        /// <summary>
        /// Get the pixel color at (x, y)
        /// </summary>
        public Rgba32 GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException($"Pixel coordinates ({x},{y}) out of bounds");

            return _image[x, y];
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) _image?.Dispose();
            _disposed = true;
        }

        ~Texture()
        {
            Dispose(false);
        }
    }
}
