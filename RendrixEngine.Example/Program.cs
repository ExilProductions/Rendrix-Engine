using RendrixEngine.Components;
using RendrixEngine.Mathematics;
using RendrixEngine.Models;

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


                RendrixEngine.Engine.RendrixEngine engine = new RendrixEngine.Engine.RendrixEngine(
                    width: screenWidth,
                    height: screenHeight,
                    targetFPS: 30,
                    title: "Rendrix Engine Demo",
                    ambientStrength: ambientStrength
                );

                engine.Initialize();

                var cubeMesh = Mesh.CreateCube(2f);

                Rotator rootNodeRotator = engine.RootNode.AddComponent<Rotator>();
                rootNodeRotator.direction = new Vector3D(0, 1, 0);
                rootNodeRotator.Speed = angularSpeedY;

                var cube1 = new SceneNode("Cube 1");
                cube1.Transform.Position = new Vector3D(1.5f, 0, 0);

                MeshRenderer cube1Renderer = cube1.AddComponent<MeshRenderer>();
                cube1Renderer.Mesh = cubeMesh;

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
                cube2Renderer.Mesh = cubeMesh;

                Rotator rotator2 = cube2.AddComponent<Rotator>();
                rotator2.direction = new Vector3D(0, 0, 1);
                rotator2.Speed = angularSpeedZ;

                Scaler scaler2 = cube2.AddComponent<Scaler>();
                scaler2.scaleMin = scaleMin;
                scaler2.scaleMax = scaleMax;
                scaler2.pulseFrequency = pulseFrequency2; 

                var lightNode = new SceneNode("Light");
                lightNode.Transform.Position = new Vector3D(2, 0, 0);
                Light light = lightNode.AddComponent<Light>();
                light.Type = Light.LightType.Point;
                light.Intensity = 1;
                light.Range = 5;


                engine.RootNode.AddChild(cube1);
                engine.RootNode.AddChild(cube2);
                engine.RootNode.AddChild(lightNode);
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