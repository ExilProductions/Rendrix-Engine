using System.Numerics;
using RendrixEngine;

namespace RendrixEngine
{
    public class Camera : Component
{
    public Vector3D Target { get; set; }
    public Vector3D Up { get; set; }
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

        AspectRatio = (float)Window.Width / Window.Height;
        ViewMatrix = Matrix4x4Extension.CreateLookAt(Transform.Position, Target, Up);
        ProjectionMatrix = Matrix4x4Extension.CreatePerspective(Fov, AspectRatio, NearPlane, FarPlane);
    }
}
}