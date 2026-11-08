namespace GitHubRepoManager.Core.Models;

public sealed record EditableRepositorySnapshot(
    string Name,
    string? Description,
    IReadOnlyList<string> Topics,
    RepositoryVisibility Visibility,
    bool Archived);
