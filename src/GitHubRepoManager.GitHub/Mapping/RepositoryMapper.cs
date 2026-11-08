using GitHubRepoManager.Core.Models;
using Octokit;
using CoreVisibility = GitHubRepoManager.Core.Models.RepositoryVisibility;

namespace GitHubRepoManager.GitHub.Mapping;

public static class RepositoryMapper
{
    public static RepositoryRecord Map(Repository repository)
    {
        var visibility = repository.Visibility switch
        {
            Octokit.RepositoryVisibility.Internal => CoreVisibility.Internal,
            _ when repository.Private => CoreVisibility.Private,
            _ => CoreVisibility.Public
        };

        return new RepositoryRecord
        {
            Id = repository.Id,
            Owner = repository.Owner.Login,
            Name = repository.Name,
            FullName = repository.FullName,
            Description = repository.Description,
            HtmlUrl = new Uri(repository.HtmlUrl),
            Visibility = visibility,
            Archived = repository.Archived,
            Disabled = false,
            IsFork = repository.Fork,
            PrimaryLanguage = repository.Language,
            Topics = repository.Topics.OrderBy(static x => x, StringComparer.Ordinal).ToList(),
            DefaultBranch = repository.DefaultBranch,
            Stars = repository.StargazersCount,
            Forks = repository.ForksCount,
            OpenIssuesCount = repository.OpenIssuesCount,
            CreatedAt = repository.CreatedAt,
            UpdatedAt = repository.UpdatedAt,
            PushedAt = repository.PushedAt ?? repository.UpdatedAt,
            SizeInKilobytes = (int)Math.Clamp(repository.Size, 0, int.MaxValue),
            LicenseName = repository.License?.Name
        };
    }
}
