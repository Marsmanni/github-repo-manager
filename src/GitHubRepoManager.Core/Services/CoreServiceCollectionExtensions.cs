using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.Core.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GitHubRepoManager.Core.Services;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<RepositoryChangeTracker>();
        services.AddSingleton<RepositoryApplyCoordinator>();
        services.AddSingleton<IRepositoryNameValidator, RepositoryNameValidator>();
        services.AddSingleton<ITopicNormalizer, TopicNormalizer>();
        return services;
    }
}
