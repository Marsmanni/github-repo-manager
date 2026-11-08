namespace GitHubRepoManager.Core.Models;

public enum RepositoryChangeState
{
    Unchanged,
    Modified,
    Applying,
    Applied,
    Failed,
    Conflict
}
