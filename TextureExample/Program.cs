using RendrixEngine;


namespace TextureExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Engine engine = new Engine(120, 40, 144, "Rendrix Engine Texture Example", 0.6f);

            var mesh = Mesh.CreateCube(2.5f);
            mesh.Texture = new Texture(Assets.checker_texture);
            var cubeObject = new SceneNode("Cube");
            var meshRenderer = cubeObject.AddComponent<MeshRenderer>();
            meshRenderer.Mesh = mesh;
            var rotator = cubeObject.AddComponent<Rotator>();
            rotator.direction = new Vector3D(1.2f, 1.1f, 1.4f);
            rotator.Speed = 2.0f;

            var lightNode = new SceneNode("Light");
            lightNode.Transform.Position = new Vector3D(0, 5, 0);
            var lightComponent = lightNode.AddComponent<Light>();
            lightComponent.Type = LightType.Directional;
            lightComponent.Intensity = 1.0f;
            lightComponent.Range = 10.0f;

            engine.RootNode.AddChild(cubeObject);
            engine.RootNode.AddChild(lightNode);

            engine.Initialize();
        }
    }
}