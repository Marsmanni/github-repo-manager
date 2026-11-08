using GitHubRepoManager.Core.Models;
using GitHubRepoManager.Core.Services;

namespace GitHubRepoManager.Tests.Services;

public sealed class RepositoryChangeTrackerTests
{
    [Fact]
    public void UpdateWorking_TracksModifiedState_AndDiscardRestoresBaseline()
    {
        var tracker = new RepositoryChangeTracker();
        var repository = CreateRepository(1, "repo-a");
        tracker.Initialize([repository]);

        tracker.UpdateWorking(1, new EditableRepositorySnapshot("repo-b", "description", ["topic"], RepositoryVisibility.Public, false));
        Assert.Equal(RepositoryChangeState.Modified, tracker.GetState(1).ChangeState);
        Assert.Equal(1, tracker.PendingChangesCount());

        tracker.Discard(1);
        Assert.Equal(RepositoryChangeState.Unchanged, tracker.GetState(1).ChangeState);
        Assert.Equal(0, tracker.PendingChangesCount());
    }

    private static RepositoryRecord CreateRepository(long id, string name) =>
        new()
        {
            Id = id,
            Owner = "owner",
            Name = name,
            FullName = $"owner/{name}",
            Description = "description",
            HtmlUrl = new Uri($"https://github.com/owner/{name}"),
            Visibility = RepositoryVisibility.Public,
            Archived = false,
            Disabled = false,
            IsFork = false,
            PrimaryLanguage = "C#",
            Topics = ["topic"],
            DefaultBranch = "main",
            Stars = 1,
            Forks = 0,
            OpenIssuesCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            PushedAt = DateTimeOffset.UtcNow,
            SizeInKilobytes = 10,
            LicenseName = "MIT"
        };
}
