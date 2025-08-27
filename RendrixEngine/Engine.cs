﻿using System;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace RendrixEngine
{
    public class Engine
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DrawMenuBar(nint hWnd);

        private const int STD_INPUT_HANDLE = -10;
        private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_THICKFRAME = 0x00040000;
        private static void DisableQuickEdit()
        {
            nint consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
            if (!GetConsoleMode(consoleHandle, out uint consoleMode)) return;
            SetConsoleMode(consoleHandle, consoleMode & ~ENABLE_QUICK_EDIT_MODE);
        }
        private static void DisableResize()
        {
            nint handle = GetConsoleWindow();
            if (handle == nint.Zero) return;

            int style = GetWindowLong(handle, GWL_STYLE);
            if(style == 0) return;
            int newStyle = style & ~WS_MAXIMIZEBOX & ~WS_THICKFRAME;
            SetWindowLong(handle, GWL_STYLE, newStyle);
            DrawMenuBar(handle);
            Console.SetWindowSize(Console.WindowWidth, Console.WindowHeight);
        }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int targetFPS = 30;
        public string Title { get; private set; }
        public float AmbientStrength { get; private set; } = 0.3f;

        public float IndirectLighting { get; private set; } = 0.2f;

        public Camera Camera { get; private set; }
        public Renderer Renderer { get; private set; }
        public SceneNode RootNode { get; } = new SceneNode("Root");
        private bool isRunning;
        private float frameTime => 1000.0f / targetFPS;
        private const string asciiChars = " .:-=+*#%@\"";

        public Engine(int width, int height, int targetFPS, string title, float ambientStrength, float indirectLighting = 0.2f)
        {
            Width = width;
            Height = height;
            this.targetFPS = targetFPS;
            Title = title;
            AmbientStrength = ambientStrength;
            IndirectLighting = indirectLighting;
        }

        public void Initialize()
        {
            Console.CursorVisible = false;
            Console.Title = Title;
            try
            {
                int maxWidth = Math.Min(Width, Console.LargestWindowWidth);
                int maxHeight = Math.Min(Height, Console.LargestWindowHeight);
                Console.SetWindowSize(maxWidth, maxHeight);
                if (OperatingSystem.IsWindows())
                {
                    Console.SetBufferSize(maxWidth, maxHeight);
                    DisableQuickEdit();
                    DisableResize();
                }
                Width = maxWidth;
                Height = maxHeight;
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
            Camera.main = camera;
            var renderer = new Renderer(
                screenWidth: Width,
                screenHeight: Height,
                camera: camera,
                asciiChars: asciiChars,
                ambientStrength: AmbientStrength,
                indirectLighting: IndirectLighting
            );
            Renderer = renderer;
            isRunning = true;
            DateTime startTime = DateTime.Now;
            DateTime lastFrameTime = DateTime.Now;
            StringBuilder frameBuffer = new StringBuilder(Width * (Height));
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
        }
    }
}