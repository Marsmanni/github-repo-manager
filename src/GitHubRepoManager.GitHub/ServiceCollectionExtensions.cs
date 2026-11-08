using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.GitHub.Authentication;
using GitHubRepoManager.GitHub.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace GitHubRepoManager.GitHub;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IGitHubAuthenticationProvider, GitHubCliAuthenticationProvider>();
        services.AddSingleton<IGitHubRepositoryService, OctokitRepositoryService>();
        return services;
    }
}
