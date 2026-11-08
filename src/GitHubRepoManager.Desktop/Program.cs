using Avalonia;
using Microsoft.Extensions.Hosting;

namespace GitHubRepoManager.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = AppHostBuilder.Create(args);
        App.ServicesHost = host;
        BuildAvaloniaApp(host).StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(IHost host) =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
