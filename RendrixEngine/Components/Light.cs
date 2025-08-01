using RendrixEngine.Mathematics;

namespace RendrixEngine.Components
{
    public class Light : Component
    {
        public enum LightType
        {
            Point,
            Directional
        }

        public LightType Type { get; set; }
        public Vector3D Position { get; set; }
        public Vector3D Direction { get; set; }
        public float Intensity { get; set; }
        public float Range { get; set; }

        public Light() { }

        public Light(LightType type, Vector3D positionOrDirection, float intensity, float range = float.PositiveInfinity)
        {
            if (type == LightType.Point && positionOrDirection == default)
                throw new ArgumentException("Point light must have a valid position.", nameof(positionOrDirection));
            if (type == LightType.Directional && positionOrDirection.Normalized == default)
                throw new ArgumentException("Directional light must have a valid direction.", nameof(positionOrDirection));
            if (intensity < 0)
                throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be non-negative.");
            if (type == LightType.Point && range <= 0)
                throw new ArgumentOutOfRangeException(nameof(range), "Range for point lights must be positive.");

            Type = type;
            Position = type == LightType.Point ? positionOrDirection : default;
            Direction = type == LightType.Directional ? positionOrDirection.Normalized : default;
            Intensity = intensity;
            Range = type == LightType.Point ? range : float.PositiveInfinity;
        }
    }
}