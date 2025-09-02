using System.Numerics;

namespace RendrixEngine
{
    public class Camera : Component
    {
        public float Fov { get; set; }
        public float AspectRatio { get; internal set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }
        public Matrix4x4 ViewMatrix { get; internal set; }
        public Matrix4x4 ProjectionMatrix { get; internal set; }

        public static Camera Main { get; set; }

        public override void OnAwake()
        {
            if (Main == null)
                Main = this;

            AspectRatio = (float)WindowSettings.Width / WindowSettings.Height;
            Vector3D forward = Transform.Forward;
            Vector3D up = Transform.Up;
            ViewMatrix = Matrix4x4Extension.CreateLookAt(Transform.Position, Transform.Position + forward, up);
            ProjectionMatrix = Matrix4x4Extension.CreatePerspective(Fov, AspectRatio, NearPlane, FarPlane);
        }
    }
}