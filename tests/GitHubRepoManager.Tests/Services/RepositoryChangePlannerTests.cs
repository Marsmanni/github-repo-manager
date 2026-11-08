using GitHubRepoManager.Core.Models;
using GitHubRepoManager.Core.Services;

namespace GitHubRepoManager.Tests.Services;

public sealed class RepositoryChangePlannerTests
{
    [Fact]
    public void CreatePlan_ReturnsPropertyDifferences()
    {
        var baseline = new EditableRepositorySnapshot("repo", "old", ["dotnet"], RepositoryVisibility.Public, false);
        var working = new EditableRepositorySnapshot("repo-new", null, ["dotnet", "avalonia"], RepositoryVisibility.Private, true);

        var plan = RepositoryChangePlanner.CreatePlan(1, baseline, working);

        Assert.True(plan.HasChanges);
        Assert.Equal(5, plan.Changes.Count);
        Assert.Contains(plan.Changes, static c => c.Field == RepositoryField.Name);
        Assert.Contains(plan.Changes, static c => c.Field == RepositoryField.Description);
        Assert.Contains(plan.Changes, static c => c.Field == RepositoryField.Topics);
        Assert.Contains(plan.Changes, static c => c.Field == RepositoryField.Visibility);
        Assert.Contains(plan.Changes, static c => c.Field == RepositoryField.Archived);
    }
}
