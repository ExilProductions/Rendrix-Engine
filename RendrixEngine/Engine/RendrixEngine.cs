using RendrixEngine.Mathematics;
using RendrixEngine.Models;
using RendrixEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RendrixEngine.Engine
{
    public class RendrixEngine
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DrawMenuBar(IntPtr hWnd);

        private const int STD_INPUT_HANDLE = -10;
        private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_THICKFRAME = 0x00040000;
        private static void DisableQuickEdit()
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
            if (!GetConsoleMode(consoleHandle, out uint consoleMode)) return;
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
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int targetFPS = 30;
        public string Title { get; private set; }
        public float AmbientStrength { get; private set; } = 0.3f;
        public Camera Camera { get; private set; }
        public Renderer Renderer { get; private set; }
        public SceneNode RootNode { get; } = new SceneNode("Root");
        private bool isRunning;
        private float frameTime => 1000.0f / targetFPS;
        private const string asciiChars = " .:-=+*#%@\"";

        public RendrixEngine(int width, int height, int targetFPS, string title, float ambientStrength)
        {
            Width = width;
            Height = height;
            this.targetFPS = targetFPS;
            Title = title;
            AmbientStrength = ambientStrength;
        }

        public void Initialize()
        {
            Console.CursorVisible = false;
            Console.Title = Title;
            try
            {
                Console.SetWindowSize(Width, Height + 1);
                if (OperatingSystem.IsWindows())
                {
                    Console.SetBufferSize(Width, Height + 1);
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
            var camera = new Camera(
                position: new Vector3D(0, 0, 15),
                target: new Vector3D(0, 0, 0),
                up: new Vector3D(0, 1, 0),
                fov: 60.0f * (float)Math.PI / 180.0f,
                aspectRatio: (float)Width / Height,
                nearPlane: 0.1f,
                farPlane: 100.0f
            );
            Camera = camera;
            var renderer = new Renderer(
                screenWidth: Width,
                screenHeight: Height,
                camera: camera,
                asciiChars: asciiChars,
                ambientStrength: AmbientStrength
            );
            Renderer = renderer;
            isRunning = true;
            DateTime startTime = DateTime.Now;
            DateTime lastFrameTime = DateTime.Now;
            StringBuilder frameBuffer = new StringBuilder(Width * (Height + 1));
            while (isRunning)
            {
                DateTime frameStart = DateTime.Now;
                float deltaTime = (float)(frameStart - lastFrameTime).TotalSeconds;
                float elapsedTime = (float)(frameStart - startTime).TotalSeconds;
                Time.DeltaTime = deltaTime;
                Time.TimeSinceStart = elapsedTime;
                renderer.Render(RootNode);
                string frame = renderer.GetFrame();
                frameBuffer.Clear();
                frameBuffer.Append(frame);
                Console.SetCursorPosition(0, 0);
                Console.Write(frameBuffer.ToString());
                lastFrameTime = frameStart;
                float elapsedMs = (float)(DateTime.Now - frameStart).TotalMilliseconds;
                int sleepMs = (int)Math.Max(0, frameTime - elapsedMs);
                Thread.Sleep(sleepMs);
            }
        }

        public void Stop()
        {
            isRunning = false;
            Console.CursorVisible = true;
            Console.SetCursorPosition(0, Height);
            Console.WriteLine("Engine stopped. Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}
