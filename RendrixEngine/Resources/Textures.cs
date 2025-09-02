using System.IO;
using System.Reflection;

namespace RendrixEngine.Resources
{
    public static class Textures
    {
        public static byte[] LoadResource(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string fullName = $"{assembly.GetName().Name}.{resourcePath.Replace("\\", ".").Replace("/", ".")}";
            using Stream? stream = assembly.GetManifestResourceStream(fullName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource '{fullName}' not found");

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
