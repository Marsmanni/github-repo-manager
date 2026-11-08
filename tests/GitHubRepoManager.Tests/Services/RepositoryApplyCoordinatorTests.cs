using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.Core.Models;
using GitHubRepoManager.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GitHubRepoManager.Tests.Services;

public sealed class RepositoryApplyCoordinatorTests
{
    [Fact]
    public async Task ApplyAsync_ReportsPartialFailures_AndPreservesFailedPendingChanges()
    {
        var repo1 = CreateRepository(1, "repo-a");
        var repo2 = CreateRepository(2, "repo-b");

        var tracker = new RepositoryChangeTracker();
        tracker.Initialize([repo1, repo2]);
        tracker.UpdateWorking(1, new EditableRepositorySnapshot("repo-a-new", repo1.Description, repo1.Topics, repo1.Visibility, repo1.Archived));
        tracker.UpdateWorking(2, new EditableRepositorySnapshot("repo-b-new", repo2.Description, repo2.Topics, repo2.Visibility, repo2.Archived));

        var service = new FakeRepositoryService(
            fetchMap: new Dictionary<long, RepositoryRecord> { { 1, repo1 }, { 2, repo2 } },
            applyMap: new Dictionary<long, RepositoryApplyResult>
            {
                { 1, new RepositoryApplyResult(1, true, null, CreateRepository(1, "repo-a-new")) },
                { 2, new RepositoryApplyResult(2, false, "validation failed", null) }
            });

        var coordinator = new RepositoryApplyCoordinator(service, tracker, NullLogger<RepositoryApplyCoordinator>.Instance);

        var result = await coordinator.ApplyAsync(
            [1, 2],
            (_, _) => Task.FromResult(ConflictResolutionChoice.KeepLocal),
            CancellationToken.None);

        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(RepositoryChangeState.Unchanged, tracker.GetState(1).ChangeState);
        Assert.Equal(RepositoryChangeState.Failed, tracker.GetState(2).ChangeState);
    }

    [Fact]
    public async Task ApplyAsync_AllSuccessful_ClearsPendingChanges()
    {
        var repo = CreateRepository(10, "repo");
        var updated = CreateRepository(10, "repo-new");
        var tracker = new RepositoryChangeTracker();
        tracker.Initialize([repo]);
        tracker.UpdateWorking(10, new EditableRepositorySnapshot("repo-new", repo.Description, repo.Topics, repo.Visibility, repo.Archived));

        var service = new FakeRepositoryService(
            fetchMap: new Dictionary<long, RepositoryRecord> { { 10, repo } },
            applyMap: new Dictionary<long, RepositoryApplyResult>
            {
                { 10, new RepositoryApplyResult(10, true, null, updated) }
            });

        var coordinator = new RepositoryApplyCoordinator(service, tracker, NullLogger<RepositoryApplyCoordinator>.Instance);

        var result = await coordinator.ApplyAsync(
            [10],
            (_, _) => Task.FromResult(ConflictResolutionChoice.KeepLocal),
            CancellationToken.None);

        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(RepositoryChangeState.Unchanged, tracker.GetState(10).ChangeState);
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

    private sealed class FakeRepositoryService(
        IReadOnlyDictionary<long, RepositoryRecord> fetchMap,
        IReadOnlyDictionary<long, RepositoryApplyResult> applyMap) : IGitHubRepositoryService
    {
        public Task<IReadOnlyList<RepositoryRecord>> GetOwnedRepositoriesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RepositoryRecord>>([]);

        public Task<RepositoryRecord> GetRepositoryByIdAsync(long repositoryId, CancellationToken cancellationToken) =>
            Task.FromResult(fetchMap[repositoryId]);

        public Task<RepositoryApplyResult> ApplyChangesAsync(RepositoryUpdateRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(applyMap[request.RepositoryId]);
    }
}
