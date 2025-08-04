using RendrixEngine.Components;
using RendrixEngine.Mathematics;
using RendrixEngine.Systems;

namespace TextureExample
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
