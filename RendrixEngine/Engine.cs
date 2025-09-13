using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Threading;
using RendrixEngine.Audio;

namespace RendrixEngine
{
    public class Engine : IDisposable
    {
        public string Title { get; private set; }
        public float AmbientStrength { get; private set; } = 0.3f;
        public float IndirectLighting { get; private set; } = 0.2f;

        public Camera Camera { get; private set; }
        public Renderer Renderer { get; private set; }
        public SceneNode RootNode { get; } = new("Root");

        private volatile bool isRunning;
        private const string asciiChars = " ░▒▓█";
        private double targetFrameMs;

        private Stopwatch stopwatch;
        private long lastTicks;
        private double tickToMs;

        private Thread loopThread;
        private IScene? initialScene;

        private MainWindow? mainWindow;
        private Timer? uiTimer;

        public Engine(int width, int height, int targetFPS, string title,
                      float ambientStrength, float indirectLighting = 0.2f, IScene? initialScene = null)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width/Height must be positive.");
            if (targetFPS <= 0) targetFPS = 30;

            Title = title ?? "Rendrix Engine";
            AmbientStrength = ambientStrength;
            IndirectLighting = indirectLighting;
            this.initialScene = initialScene;

            targetFrameMs = 1000.0 / targetFPS;

            WindowSettings.Width = width;
            WindowSettings.Height = height;
        }

        /// <summary>
        /// Initializes engine systems and returns the Avalonia MainWindow to display.
        /// </summary>
        public MainWindow Initialize()
        {
            Renderer = new Renderer(
                screenWidth: WindowSettings.Width,
                screenHeight: WindowSettings.Height,
                asciiChars: asciiChars,
                ambientStrength: AmbientStrength,
                indirectLighting: IndirectLighting
            );

            SceneManager.Initialize(this);
            if (initialScene != null)
                SceneManager.LoadScene(initialScene);
            else
                SceneManager.LoadAutoScenesOnStartup();

            AudioEngine.Instance.Initialize();

            // create window
            mainWindow = new MainWindow();

            // start UI timer for pushing frames
            uiTimer = new Timer(UpdateFrame, null, 0, (int)targetFrameMs);

            StartLoop();

            return mainWindow;
        }

        private void UpdateFrame(object? state)
        {
            if (Renderer == null || mainWindow == null) return;
            string frame = Renderer.GetFrame();
            if (string.IsNullOrEmpty(frame)) return;

            Dispatcher.UIThread.Post(() =>
            {
                mainWindow.RenderOutput.Text = frame;
            });
        }

        private void StartLoop()
        {
            stopwatch = Stopwatch.StartNew();
            lastTicks = stopwatch.ElapsedTicks;
            tickToMs = 1000.0 / Stopwatch.Frequency;
            isRunning = true;

            loopThread = new Thread(GameLoop) { IsBackground = true };
            loopThread.Start();
        }

        private void GameLoop()
        {
            while (isRunning)
            {
                long frameStartTicks = stopwatch.ElapsedTicks;
                double rawDelta = (frameStartTicks - lastTicks) * tickToMs / 1000.0;
                lastTicks = frameStartTicks;

                Time.UnscaledDeltaTime = (float)rawDelta;
                Time.DeltaTime = Time.UnscaledDeltaTime * Time.TimeScale;
                Time.RealtimeSinceStartup = (float)stopwatch.Elapsed.TotalSeconds;
                Time.TimeSinceStart += Time.DeltaTime;
                Time.FrameCount++;

                AudioEngine.Instance.Update();
                Renderer.Render(RootNode);

                double frameTime = (stopwatch.ElapsedTicks - frameStartTicks) * tickToMs;
                int sleepTime = (int)(targetFrameMs - frameTime);
                if (sleepTime > 0)
                    Thread.Sleep(sleepTime);
            }
        }

        public void Stop()
        {
            isRunning = false;
            loopThread?.Join();
        }

        public void Dispose()
        {
            Stop();
            uiTimer?.Dispose();
            AudioEngine.Instance.Dispose();
        }
    }
}
