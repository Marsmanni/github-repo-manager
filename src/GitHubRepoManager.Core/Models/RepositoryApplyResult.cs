namespace GitHubRepoManager.Core.Models;

public sealed record RepositoryApplyResult(
    long RepositoryId,
    bool Succeeded,
    string? ErrorMessage,
    RepositoryRecord? UpdatedRepository);
