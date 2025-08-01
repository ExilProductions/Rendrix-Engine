using RendrixEngine.Mathematics;

namespace RendrixEngine.Components
{
    public enum LightType
    {
        Point,
        Directional
    }
    public class Light : Component
    {
        public LightType Type { get; set; }
        public Vector3D Direction { get; set; }
        public float Intensity { get; set; }
        public float Range { get; set; }
    }
}