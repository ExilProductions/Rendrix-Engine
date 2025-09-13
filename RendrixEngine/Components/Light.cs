using System.Numerics;

namespace RendrixEngine
{
    public enum LightType
    {
        Point,
        Directional
    }
    public class Light : Component
    {
        public LightType Type { get; set; }
        public Vector3 Direction { get; set; }
        public float Intensity { get; set; }
        public float Range { get; set; }
    }
}