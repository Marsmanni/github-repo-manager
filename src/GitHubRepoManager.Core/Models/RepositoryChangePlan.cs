namespace GitHubRepoManager.Core.Models;

public sealed record RepositoryChangePlan(
    long RepositoryId,
    IReadOnlyList<RepositoryPropertyChange> Changes)
{
    public bool HasChanges => Changes.Count > 0;
}
