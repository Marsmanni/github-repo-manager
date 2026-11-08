using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Services;

public static class DuplicateRepositoryNameDetector
{
    public static IReadOnlyList<(long FirstId, long SecondId, string Owner, string Name)> Detect(
        IEnumerable<(long Id, string Owner, EditableRepositorySnapshot Snapshot)> values)
    {
        var collisions = new List<(long FirstId, long SecondId, string Owner, string Name)>();
        var seen = new Dictionary<(string Owner, string Name), long>();

        foreach (var value in values)
        {
            var key = (value.Owner, value.Snapshot.Name.Trim());
            if (seen.TryGetValue(key, out var existing))
            {
                collisions.Add((existing, value.Id, value.Owner, value.Snapshot.Name));
                continue;
            }

            seen[key] = value.Id;
        }

        return collisions;
    }
}
