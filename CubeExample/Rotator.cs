using RendrixEngine;
namespace CubeExample
{
    public class Rotator : Component
    {
        public float Speed { get; set; } = 1.0f;
        public Vector3D direction = new Vector3D(1, 0, 0);
        public override void Update()
        {
            Transform.Rotate(direction, Speed * Time.DeltaTime);
        }
    }
}
