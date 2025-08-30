using System;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RendrixEngine
{

    public class Engine : IDisposable
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
            try
            {
                nint consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
                if (!GetConsoleMode(consoleHandle, out uint consoleMode)) return;
                SetConsoleMode(consoleHandle, consoleMode & ~ENABLE_QUICK_EDIT_MODE);
            }
            catch { }
        }

        private static void DisableResize()
        {
            try
            {
                nint handle = GetConsoleWindow();
                if (handle == nint.Zero) return;

                int style = GetWindowLong(handle, GWL_STYLE);
                if (style == 0) return;
                int newStyle = style & ~WS_MAXIMIZEBOX & ~WS_THICKFRAME;
                SetWindowLong(handle, GWL_STYLE, newStyle);
                DrawMenuBar(handle);
                Console.SetWindowSize(Console.WindowWidth, Console.WindowHeight);
            }
            catch { }
        }

        public string Title { get; private set; }
        public float AmbientStrength { get; private set; } = 0.3f;
        public float IndirectLighting { get; private set; } = 0.2f;

        public Camera Camera { get; private set; }
        public Renderer Renderer { get; private set; }
        public SceneNode RootNode { get; } = new SceneNode("Root");


        private volatile bool isRunning;

        private const string asciiChars = " ░▒▓█";


        private double targetFrameMs;

        public Engine(int width, int height, int targetFPS, string title, float ambientStrength, float indirectLighting = 0.2f)
        {
            if (width <= 0 || height <= 0) throw new ArgumentException("Width/Height must be positive.");
            if (targetFPS <= 0) targetFPS = 30;

            Window.Width = width;
            Window.Height = height;
            Window.targetFPS = targetFPS;
            targetFrameMs = 1000.0 / Window.targetFPS;

            Title = title ?? "Rendrix Engine";
            AmbientStrength = ambientStrength;
            IndirectLighting = indirectLighting;
        }


        public void Initialize()
        {

            Console.CursorVisible = false;
            Console.Title = Title;


            try { Console.OutputEncoding = Encoding.UTF8; } catch { }

            try
            {

                int maxWidth = Math.Min(Window.Width, Console.LargestWindowWidth);
                int maxHeight = Math.Min(Window.Height, Console.LargestWindowHeight);
                maxWidth = Math.Max(1, maxWidth);
                maxHeight = Math.Max(1, maxHeight);

                try { Console.SetWindowSize(maxWidth, maxHeight); } catch { }

                if (OperatingSystem.IsWindows())
                {
                    try { Console.SetBufferSize(maxWidth, maxHeight); } catch { }
                    DisableQuickEdit();
                    DisableResize();
                }

                Window.Width = maxWidth;
                Window.Height = maxHeight;
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("Warning: Console buffer/size adjustments not supported on this platform.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to set console size: {ex.Message}");
            }


            Renderer = new Renderer(
                screenWidth: Window.Width,
                screenHeight: Window.Height,
                asciiChars: asciiChars,
                ambientStrength: AmbientStrength,
                indirectLighting: IndirectLighting
            );


            RunLoop();
        }


        private void RunLoop()
        {
            isRunning = true;

            var renderer = Renderer ?? throw new InvalidOperationException("Renderer not initialized.");

            var stopwatch = Stopwatch.StartNew();
            long lastTicks = stopwatch.ElapsedTicks;
            double tickToMs = 1000.0 / Stopwatch.Frequency;


            try
            {
                while (isRunning)
                {
                    long frameStartTicks = stopwatch.ElapsedTicks;
                    double deltaTime = (frameStartTicks - lastTicks) * tickToMs / 1000.0;
                    lastTicks = frameStartTicks;


                    Time.DeltaTime = (float)deltaTime;
                    Time.TimeSinceStart = (float)(stopwatch.Elapsed.TotalSeconds);


                    renderer.Render(RootNode);
                    string frame = renderer.GetFrame();


                    try
                    {
                        Console.SetCursorPosition(0, 0);
                        Console.Write(frame);
                    }
                    catch
                    {

                        try { Console.Write(frame); } catch { }
                    }


                    double elapsedMs = (stopwatch.ElapsedTicks - frameStartTicks) * tickToMs;
                    int sleepMs = (int)Math.Max(0.0, targetFrameMs - elapsedMs);

                    if (sleepMs > 0)
                        Thread.Sleep(sleepMs);


                }
            }
            finally
            {

                try { Console.CursorVisible = true; } catch { }
                try { Console.SetCursorPosition(0, Window.Height); } catch { }
            }
        }


        public void Stop()
        {
            isRunning = false;
        }


        public void Dispose()
        {
            Stop();
            try { Console.CursorVisible = true; } catch { }
            try { Console.SetCursorPosition(0, Math.Max(0, Window.Height - 1)); } catch { }
        }
    }
}
