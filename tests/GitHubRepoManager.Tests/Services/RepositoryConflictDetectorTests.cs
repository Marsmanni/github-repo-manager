using GitHubRepoManager.Core.Models;
using GitHubRepoManager.Core.Services;

namespace GitHubRepoManager.Tests.Services;

public sealed class RepositoryConflictDetectorTests
{
    [Fact]
    public void Detect_ReturnsConflict_WhenRemoteAndLocalDivergeFromBaseline()
    {
        var baseline = new EditableRepositorySnapshot("repo", "old", ["dotnet"], RepositoryVisibility.Public, false);
        var remote = new EditableRepositorySnapshot("repo", "remote", ["dotnet"], RepositoryVisibility.Public, false);
        var local = new EditableRepositorySnapshot("repo", "local", ["dotnet"], RepositoryVisibility.Public, false);

        var conflicts = RepositoryConflictDetector.Detect(baseline, remote, local);

        Assert.Single(conflicts);
        Assert.Equal(RepositoryField.Description, conflicts[0].Field);
    }
}
