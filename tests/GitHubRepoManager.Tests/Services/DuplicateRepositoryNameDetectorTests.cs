using GitHubRepoManager.Core.Models;
using GitHubRepoManager.Core.Services;

namespace GitHubRepoManager.Tests.Services;

public sealed class DuplicateRepositoryNameDetectorTests
{
    [Fact]
    public void Detect_ReturnsCollision_WhenOwnerAndTargetNameDuplicate()
    {
        var values = new[]
        {
            (1L, "owner", new EditableRepositorySnapshot("repo-a", null, [], RepositoryVisibility.Public, false)),
            (2L, "owner", new EditableRepositorySnapshot("repo-a", null, [], RepositoryVisibility.Public, false)),
            (3L, "other", new EditableRepositorySnapshot("repo-a", null, [], RepositoryVisibility.Public, false))
        };

        var duplicates = DuplicateRepositoryNameDetector.Detect(values);
        Assert.Single(duplicates);
        Assert.Equal(1L, duplicates[0].FirstId);
        Assert.Equal(2L, duplicates[0].SecondId);
    }
}
