namespace GitHubRepoManager.Core.Models;

public sealed record RepositoryConflict(
    RepositoryField Field,
    string BaselineValue,
    string RemoteValue,
    string LocalValue);
