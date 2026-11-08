using GitHubRepoManager.Core.Services;
using GitHubRepoManager.Desktop.Services;
using GitHubRepoManager.Desktop.ViewModels;
using GitHubRepoManager.GitHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitHubRepoManager.Desktop;

internal static class AppHostBuilder
{
    public static IHost Create(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices(services =>
            {
                services.AddCoreServices();
                services.AddGitHubIntegration();

                services.AddSingleton<IAppSettingsService, JsonAppSettingsService>();
                services.AddSingleton<IUserDialogService, UserDialogService>();
                services.AddSingleton<MainWindowViewModel>();
            })
            .Build();
    }
}
