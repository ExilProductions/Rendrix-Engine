using Ascii3DRenderer.Mathematics;

namespace Ascii3DRenderer.Models
{
    /// <summary>
    /// Represents a light source in the scene, either a point light or directional light, with scalar intensity and optional range for point lights.
    /// </summary>
    public class Light
    {
        public enum LightType
        {
            Point,
            Directional
        }

        public LightType Type { get; }
        public Vector3D Position { get; } // Used for point lights
        public Vector3D Direction { get; } // Used for directional lights
        public float Intensity { get; } // Light strength
        public float Range { get; } // Maximum effective distance for point lights

        public Light(LightType type, Vector3D positionOrDirection, float intensity, float range = float.PositiveInfinity)
        {
            if (type == LightType.Point && positionOrDirection == default)
                throw new System.ArgumentException("Point light must have a valid position.", nameof(positionOrDirection));
            if (type == LightType.Directional && positionOrDirection.Normalized == default)
                throw new System.ArgumentException("Directional light must have a valid direction.", nameof(positionOrDirection));
            if (intensity < 0)
                throw new System.ArgumentOutOfRangeException(nameof(intensity), "Intensity must be non-negative.");
            if (type == LightType.Point && range <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(range), "Range for point lights must be positive.");

            Type = type;
            Position = type == LightType.Point ? positionOrDirection : default;
            Direction = type == LightType.Directional ? positionOrDirection.Normalized : default;
            Intensity = intensity;
            Range = type == LightType.Point ? range : float.PositiveInfinity;
        }
    }
}