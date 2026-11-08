using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Services;

public static class RepositoryConflictDetector
{
    public static IReadOnlyList<RepositoryConflict> Detect(
        EditableRepositorySnapshot baseline,
        EditableRepositorySnapshot remote,
        EditableRepositorySnapshot local)
    {
        var conflicts = new List<RepositoryConflict>();

        AppendConflictIfAny(
            conflicts,
            RepositoryField.Name,
            baseline.Name,
            remote.Name,
            local.Name);

        AppendConflictIfAny(
            conflicts,
            RepositoryField.Description,
            baseline.Description ?? string.Empty,
            remote.Description ?? string.Empty,
            local.Description ?? string.Empty);

        var baselineTopics = string.Join(",", baseline.Topics);
        var remoteTopics = string.Join(",", remote.Topics);
        var localTopics = string.Join(",", local.Topics);
        AppendConflictIfAny(
            conflicts,
            RepositoryField.Topics,
            baselineTopics,
            remoteTopics,
            localTopics);

        AppendConflictIfAny(
            conflicts,
            RepositoryField.Visibility,
            baseline.Visibility.ToString(),
            remote.Visibility.ToString(),
            local.Visibility.ToString());

        AppendConflictIfAny(
            conflicts,
            RepositoryField.Archived,
            baseline.Archived.ToString(),
            remote.Archived.ToString(),
            local.Archived.ToString());

        return conflicts;
    }

    private static void AppendConflictIfAny(
        ICollection<RepositoryConflict> conflicts,
        RepositoryField field,
        string baseline,
        string remote,
        string local)
    {
        var remoteChanged = !string.Equals(baseline, remote, StringComparison.Ordinal);
        var localChanged = !string.Equals(baseline, local, StringComparison.Ordinal);
        var diverged = !string.Equals(remote, local, StringComparison.Ordinal);

        if (remoteChanged && localChanged && diverged)
        {
            conflicts.Add(new RepositoryConflict(field, baseline, remote, local));
        }
    }
}
