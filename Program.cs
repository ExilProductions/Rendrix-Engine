using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Ascii3DRenderer.Mathematics;
using Ascii3DRenderer.Models;
using Ascii3DRenderer.Rendering;

namespace Ascii3DRenderer
{
    class Program
    {
        #region P/Invoke Declarations

        // P/Invoke definitions for disabling console QuickEdit mode on Windows
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        // P/Invoke definitions for disabling console resize on Windows
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DrawMenuBar(IntPtr hWnd);

        #endregion

        #region Constants

        private const int STD_INPUT_HANDLE = -10;
        private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_THICKFRAME = 0x00040000;

        #endregion

        #region Console Configuration Methods

        private static void DisableQuickEdit()
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // Get current console mode
            if (!GetConsoleMode(consoleHandle, out uint consoleMode)) return;

            // Disable QuickEdit Mode
            SetConsoleMode(consoleHandle, consoleMode & ~ENABLE_QUICK_EDIT_MODE);
        }

        private static void DisableResize()
        {
            IntPtr handle = GetConsoleWindow();
            if (handle == IntPtr.Zero) return;

            int style = GetWindowLong(handle, GWL_STYLE);
            SetWindowLong(handle, GWL_STYLE, style & ~WS_MAXIMIZEBOX & ~WS_THICKFRAME);
            DrawMenuBar(handle);
        }

        #endregion

        #region Main Program Entry

        static void Main(string[] args)
        {
            try
            {
                #region Scene and Renderer Parameters

                int screenWidth = 120;
                int screenHeight = 40;
                string asciiChars = " .:-=+*#%@";
                float targetFps = 30.0f;
                float frameTime = 1000.0f / targetFps;
                float angularSpeedX = (float)Math.PI / 4;
                float angularSpeedY = (float)Math.PI / 2;
                float angularSpeedZ = (float)Math.PI / 3;
                float ambientStrength = 0.3f;
                float diffuseStrength = 0.7f;
                float scaleMin = 0.5f;
                float scaleMax = 1.0f;
                float pulseFrequency1 = 0.5f;
                float pulseFrequency2 = 1.0f;
                float pointLightRange = 100f;

                #endregion

                #region Console Setup

                Console.CursorVisible = false;
                Console.Title = "Cube";
                try
                {
                    Console.SetWindowSize(screenWidth, screenHeight + 1);
                    if (OperatingSystem.IsWindows())
                    {
                        Console.SetBufferSize(screenWidth, screenHeight + 1);
                        DisableQuickEdit();
                        DisableResize();
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    Console.WriteLine("Warning: Console buffer size adjustment is not supported on this platform.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to set console size: {ex.Message}");
                }

                #endregion

                #region Camera and Renderer Initialization

                var camera = new Camera(
                    position: new Vector3D(0, 0, 15),
                    target: new Vector3D(0, 0, 0),
                    up: new Vector3D(0, 1, 0),
                    fov: 60.0f * (float)Math.PI / 180.0f,
                    aspectRatio: (float)screenWidth / screenHeight,
                    nearPlane: 0.1f,
                    farPlane: 100.0f
                );

                var renderer = new Renderer(
                    screenWidth: screenWidth,
                    screenHeight: screenHeight,
                    camera: camera,
                    asciiChars: asciiChars,
                    ambientStrength: ambientStrength
                );

                #endregion

                #region Scene Hierarchy Creation

                var rootNode = new SceneNode(new Transform());
                var cubeMesh = Mesh.CreateCube(2f);

                // Create two cubes as children of the root node
                var cube1 = new SceneNode(
                    new Transform
                    {
                        Position = new Vector3D(1.5f, 0, 0) // Offset to the right
                    },
                    cubeMesh
                );
                var cube2 = new SceneNode(
                    new Transform
                    {
                        Position = new Vector3D(-1.5f, 0, 0) // Offset to the left
                    },
                    cubeMesh
                );

                // Create a point light orbiting the origin
                var lightNode = new SceneNode(
                    new Transform
                    {
                        Position = new Vector3D(2, 0, 0) // Start at right
                    },
                    null,
                    new Light(
                        type: Light.LightType.Point,
                        positionOrDirection: new Vector3D(2, 0, 0),
                        intensity: 100.0f,
                        range: pointLightRange
                    )
                );

                rootNode.AddChild(cube1);
                rootNode.AddChild(cube2);
                rootNode.AddChild(lightNode);

                #endregion

                #region Main Render Loop

                DateTime startTime = DateTime.Now;
                DateTime lastFrameTime = DateTime.Now;
                StringBuilder frameBuffer = new StringBuilder(screenWidth * (screenHeight + 1));

                while (true)
                {
                    DateTime frameStart = DateTime.Now;
                    float deltaTime = (float)(frameStart - lastFrameTime).TotalSeconds;
                    float elapsedTime = (float)(frameStart - startTime).TotalSeconds;

                    // Update rotations
                    rootNode.Transform.Rotate(new Vector3D(0, 1, 0), angularSpeedY * deltaTime); // Root rotates around Y
                    cube1.Transform.Rotate(new Vector3D(1, 0, 0), angularSpeedX * deltaTime); // Cube1 rotates around X
                    cube2.Transform.Rotate(new Vector3D(0, 0, 1), angularSpeedZ * deltaTime); // Cube2 rotates around Z

                    // Update pulsating scale
                    float scale1 = scaleMin + (scaleMax - scaleMin) * (float)(0.5 * (1 + Math.Sin(2 * Math.PI * pulseFrequency1 * elapsedTime)));
                    float scale2 = scaleMin + (scaleMax - scaleMin) * (float)(0.5 * (1 + Math.Sin(2 * Math.PI * pulseFrequency2 * elapsedTime)));
                    cube1.Transform.Scale = new Vector3D(scale1, scale1, scale1);
                    cube2.Transform.Scale = new Vector3D(scale2, scale2, scale2);

                    // Render frame
                    renderer.Render(rootNode);
                    string frame = renderer.GetFrame();

                    // Update frame buffer
                    frameBuffer.Clear();
                    frameBuffer.Append(frame);

                    // Write frame to console in one operation
                    Console.SetCursorPosition(0, 0);
                    Console.Write(frameBuffer.ToString());

                    // Control frame rate
                    lastFrameTime = frameStart;
                    float elapsedMs = (float)(DateTime.Now - frameStart).TotalMilliseconds;
                    int sleepMs = (int)Math.Max(0, frameTime - elapsedMs);
                    Thread.Sleep(sleepMs);
                }

                #endregion
            }
            catch (Exception ex)
            {
                Console.CursorVisible = true;
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        #endregion
    }
}