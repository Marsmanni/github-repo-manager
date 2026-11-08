using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Services;

public static class RepositoryChangePlanner
{
    public static RepositoryChangePlan CreatePlan(
        long repositoryId,
        EditableRepositorySnapshot baseline,
        EditableRepositorySnapshot working)
    {
        var changes = new List<RepositoryPropertyChange>();

        if (!string.Equals(baseline.Name, working.Name, StringComparison.Ordinal))
        {
            changes.Add(new RepositoryPropertyChange(RepositoryField.Name, baseline.Name, working.Name));
        }

        if (!string.Equals(NormalizeDescription(baseline.Description), NormalizeDescription(working.Description), StringComparison.Ordinal))
        {
            changes.Add(new RepositoryPropertyChange(
                RepositoryField.Description,
                baseline.Description ?? "<empty>",
                working.Description ?? "<empty>"));
        }

        if (!baseline.Topics.SequenceEqual(working.Topics, StringComparer.Ordinal))
        {
            changes.Add(new RepositoryPropertyChange(
                RepositoryField.Topics,
                string.Join(", ", baseline.Topics),
                string.Join(", ", working.Topics)));
        }

        if (baseline.Visibility != working.Visibility)
        {
            changes.Add(new RepositoryPropertyChange(
                RepositoryField.Visibility,
                baseline.Visibility.ToString(),
                working.Visibility.ToString()));
        }

        if (baseline.Archived != working.Archived)
        {
            changes.Add(new RepositoryPropertyChange(
                RepositoryField.Archived,
                baseline.Archived.ToString(),
                working.Archived.ToString()));
        }

        return new RepositoryChangePlan(repositoryId, changes);
    }

    public static IReadOnlySet<RepositoryField> GetChangedFields(
        EditableRepositorySnapshot baseline,
        EditableRepositorySnapshot working)
        => CreatePlan(0, baseline, working).Changes.Select(static c => c.Field).ToHashSet();

    private static string NormalizeDescription(string? description) => description ?? string.Empty;
}
