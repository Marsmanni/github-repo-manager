namespace GitHubRepoManager.Core.Models;

public sealed record RepositoryBatchApplyResult(
    IReadOnlyList<RepositoryApplyResult> Results)
{
    public int SucceededCount => Results.Count(static r => r.Succeeded);
    public int FailedCount => Results.Count(static r => !r.Succeeded);
}
