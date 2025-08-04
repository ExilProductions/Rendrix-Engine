using RendrixEngine;
using RendrixEngine.Models;
using RendrixEngine.Systems;
using RendrixEngine.Components;
using RendrixEngine.Loader;
using RendrixEngine.Mathematics;


namespace TextureExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Engine engine = new Engine(120, 40, 30, "Rendrix Engine Texture Example", 0.6f);

            var mesh = Mesh.CreateCube(2.5f);
            mesh.Texture = TextureLoader.LoadTexture(Assets.checker_texture);
            var cubeObject = new SceneNode("Cube");
            var meshRenderer = cubeObject.AddComponent<MeshRenderer>();
            meshRenderer.Mesh = mesh;
            var rotator = cubeObject.AddComponent<Rotator>();
            rotator.direction = new Vector3D(1.2f, 1.1f, 1.4f);
            rotator.Speed = 3.0f;

            var lightNode = new SceneNode("Light");
            var lightComponent = lightNode.AddComponent<Light>();
            lightComponent.Type = LightType.Directional;
            lightComponent.Intensity = 1.0f;
            lightComponent.Direction = new Vector3D(1, 0, 0);

            engine.RootNode.AddChild(cubeObject);
            engine.RootNode.AddChild(lightNode);

            engine.Initialize();
        }
    }
}