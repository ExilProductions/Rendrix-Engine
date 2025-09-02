using System;
using System.Diagnostics;
using Avalonia.Threading;

namespace RendrixEngine
{
    public class Engine : IDisposable
    {
        public string Title { get; private set; }
        public float AmbientStrength { get; private set; } = 0.3f;
        public float IndirectLighting { get; private set; } = 0.2f;

        public Camera Camera { get; private set; }
        public Renderer Renderer { get; private set; }
        public SceneNode RootNode { get; } = new SceneNode("Root");

        public ConsoleWindow renderWindow { get; private set; }

        private volatile bool isRunning;
        private const string asciiChars = " ░▒▓█";
        private double targetFrameMs;

        private Stopwatch stopwatch;
        private long lastTicks;
        private double tickToMs;

        public Engine(int width, int height, int targetFPS, string title, float ambientStrength, float indirectLighting = 0.2f)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width/Height must be positive.");
            if (targetFPS <= 0) targetFPS = 30;

            Title = title ?? "Rendrix Engine";
            AmbientStrength = ambientStrength;
            IndirectLighting = indirectLighting;

            targetFrameMs = 1000.0 / targetFPS;

            renderWindow = new ConsoleWindow();
            WindowSettings.Width = width;
            WindowSettings.Height = height;
        }

        public void Initialize()
        {
            renderWindow.Title = Title;
            renderWindow.SetResolution(WindowSettings.Width, WindowSettings.Height);
            renderWindow.ResizeToResolution(cellWidth: 12, cellHeight: 18);

            Renderer = new Renderer(
                screenWidth: WindowSettings.Width,
                screenHeight: WindowSettings.Height,
                asciiChars: asciiChars,
                ambientStrength: AmbientStrength,
                indirectLighting: IndirectLighting
            );

            _ = new InputManager(renderWindow);

            StartLoop();
        }

        private void StartLoop()
        {
            stopwatch = Stopwatch.StartNew();
            lastTicks = stopwatch.ElapsedTicks;
            tickToMs = 1000.0 / Stopwatch.Frequency;
            isRunning = true;


            Dispatcher.UIThread.Post(UpdateLoop, DispatcherPriority.Render);
        }

        private void UpdateLoop()
        {
            if (!isRunning) return;


            long frameStartTicks = stopwatch.ElapsedTicks;
            double rawDelta = (frameStartTicks - lastTicks) * tickToMs / 1000.0;
            lastTicks = frameStartTicks;


            Time.UnscaledDeltaTime = (float)rawDelta;
            Time.DeltaTime = Time.UnscaledDeltaTime * Time.TimeScale;
            Time.RealtimeSinceStartup = (float)stopwatch.Elapsed.TotalSeconds;
            Time.TimeSinceStart += Time.DeltaTime;
            Time.FrameCount++;


            Renderer.Render(RootNode);
            string frame = Renderer.GetFrame();

            renderWindow.Clear();
            int index = 0;
            for (int y = 0; y < renderWindow.Surface.ResolutionRows; y++)
            {
                for (int x = 0; x < renderWindow.Surface.ResolutionColumns; x++)
                {
                    if (index >= frame.Length) break;
                    renderWindow.PutChar(x, y, frame[index]);
                    index++;
                }
            }
            renderWindow.EndFrame();


            Dispatcher.UIThread.Post(UpdateLoop, DispatcherPriority.Render);
        }

        public void Stop()
        {
            isRunning = false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}