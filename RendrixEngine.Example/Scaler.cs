using RendrixEngine.Components;
using RendrixEngine.Mathematics;
using RendrixEngine;

namespace CubeExample
{
    public class Scaler : Component
    {
        public float scaleMin = 0.5f;
        public float scaleMax = 1.0f;
        public float pulseFrequency = 0.5f;
        public override void Update(float deltaTime)
        {
            float scale = scaleMin + (scaleMax - scaleMin) * (float)(0.5 * (1 + Math.Sin(2 * Math.PI * pulseFrequency * Time.TimeSinceStart)));
            Transform.Scale = new Vector3D(scale, scale, scale);
        }
    }
}
