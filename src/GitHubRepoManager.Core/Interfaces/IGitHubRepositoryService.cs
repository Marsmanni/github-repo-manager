using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Interfaces;

public interface IGitHubRepositoryService
{
    Task<IReadOnlyList<RepositoryRecord>> GetOwnedRepositoriesAsync(CancellationToken cancellationToken);

    Task<RepositoryRecord> GetRepositoryByIdAsync(long repositoryId, CancellationToken cancellationToken);

    Task<RepositoryApplyResult> ApplyChangesAsync(
        RepositoryUpdateRequest request,
        CancellationToken cancellationToken);
}
