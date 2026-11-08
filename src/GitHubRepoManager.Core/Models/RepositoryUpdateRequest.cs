namespace GitHubRepoManager.Core.Models;

public sealed record RepositoryUpdateRequest(
    long RepositoryId,
    string Owner,
    string CurrentName,
    EditableRepositorySnapshot DesiredState,
    IReadOnlySet<RepositoryField> ChangedFields);
