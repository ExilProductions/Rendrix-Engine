using RendrixEngine;

namespace CubeExample
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                int screenWidth = 120;
                int screenHeight = 40;
                float angularSpeedX = (float)Math.PI / 4;
                float angularSpeedY = (float)Math.PI / 2;
                float angularSpeedZ = (float)Math.PI / 3;
                float ambientStrength = 0.3f;
                float scaleMin = 0.5f;
                float scaleMax = 1.0f;
                float pulseFrequency1 = 0.5f;
                float pulseFrequency2 = 1.0f;


                Engine engine = new Engine(
                    width: screenWidth,
                    height: screenHeight,
                    targetFPS: 144,
                    title: "Rendrix Engine Cube Example",
                    ambientStrength: ambientStrength,
                    indirectLighting: 0.6f
                );

                var mesh = Mesh.CreateCube(2f);
                var camera = new SceneNode("Camera");
                Camera cameraComponent = camera.AddComponent<Camera>();
                camera.Transform.Position = new Vector3D(0, 0, -5);
                cameraComponent.Fov = 60f;
                cameraComponent.NearPlane = 0.1f;
                cameraComponent.FarPlane = 100f;

                Rotator rootNodeRotator = engine.RootNode.AddComponent<Rotator>();
                rootNodeRotator.direction = new Vector3D(0, 1, 0);
                rootNodeRotator.Speed = angularSpeedY;

                var cube1 = new SceneNode("Cube 1");
                cube1.Transform.Position = new Vector3D(1.5f, 0, 0);

                MeshRenderer cube1Renderer = cube1.AddComponent<MeshRenderer>();
                cube1Renderer.Mesh = mesh;

                Rotator rotator = cube1.AddComponent<Rotator>();
                rotator.direction = new Vector3D(1, 0, 0);
                rotator.Speed = angularSpeedX;

                Scaler scaler1 = cube1.AddComponent<Scaler>();
                scaler1.scaleMin = scaleMin;
                scaler1.scaleMax = scaleMax;
                scaler1.pulseFrequency = pulseFrequency1;

                var cube2 = new SceneNode("Cube 2");
                cube2.Transform.Position = new Vector3D(-1.5f, 0, 0);
                MeshRenderer cube2Renderer = cube2.AddComponent<MeshRenderer>();
                cube2Renderer.Mesh = mesh;

                Rotator rotator2 = cube2.AddComponent<Rotator>();
                rotator2.direction = new Vector3D(0, 0, 1);
                rotator2.Speed = angularSpeedZ;

                Scaler scaler2 = cube2.AddComponent<Scaler>();
                scaler2.scaleMin = scaleMin;
                scaler2.scaleMax = scaleMax;
                scaler2.pulseFrequency = pulseFrequency2; 

                var lightNode = new SceneNode("Light");
                lightNode.Transform.Position = new Vector3D(0, 0, 0);
                Light light = lightNode.AddComponent<Light>();
                light.Type = LightType.Directional;
                light.Intensity = 1;
                light.Range = 5;
                light.Direction = new Vector3D(-1, -1, -1).Normalized;

                engine.RootNode.AddChild(cube1);
                engine.RootNode.AddChild(cube2);
                engine.RootNode.AddChild(lightNode);

                engine.Initialize(); //always initialize after setting up everything
            }
            catch (Exception ex)
            {
                Console.CursorVisible = true;
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}