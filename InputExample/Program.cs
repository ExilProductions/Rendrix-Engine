using RendrixEngine;

namespace InputExample
{
    
    public class Program
    {
        public static void Main(string[] args)
        {
            Engine engine = new Engine(
                width: 120,
                height: 40,
                targetFPS: 30,
                title: "Rendrix Engine Input Example",
                ambientStrength: 0.3f
            );

            var cubeMesh = Mesh.CreateCube(1f);
            var cube = new SceneNode("Cube");
            var meshRenderer = cube.AddComponent<MeshRenderer>();
            meshRenderer.Mesh = cubeMesh;
            var objectMover = cube.AddComponent<ObjectMover>();
            objectMover.speed = 5f;
            var light = new SceneNode("Sun");
            var lightComponent = light.AddComponent<Light>();
            lightComponent.Intensity = 1.0f;
            lightComponent.Direction = new Vector3D(0, 45, 0);

            engine.RootNode.AddChild(cube);
            engine.RootNode.AddChild(light);

            engine.Initialize();
        }
    }
}