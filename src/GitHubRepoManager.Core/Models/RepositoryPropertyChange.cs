namespace GitHubRepoManager.Core.Models;

public sealed record RepositoryPropertyChange(
    RepositoryField Field,
    string Before,
    string After);
