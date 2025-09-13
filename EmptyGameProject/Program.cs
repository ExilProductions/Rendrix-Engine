using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using RendrixEngine;

namespace EmptyGameProject;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args, lifetime =>
            {
                var engine = new Engine(120, 40, 30, "Empty Game", 0.3f);
                var window = engine.Initialize();
                lifetime.MainWindow = window;
            });
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
