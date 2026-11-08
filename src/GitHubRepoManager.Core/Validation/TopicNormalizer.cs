using System.Text.RegularExpressions;
using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Validation;

public sealed partial class TopicNormalizer : ITopicNormalizer
{
    private const int MaxLength = 50;

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex ValidTopicRegex();

    public TopicNormalizationResult Normalize(IEnumerable<string> topics)
    {
        var normalized = new HashSet<string>(StringComparer.Ordinal);
        var errors = new List<string>();

        foreach (var topic in topics)
        {
            var value = topic.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (value.Length > MaxLength)
            {
                errors.Add($"Topic '{topic}' exceeds 50 characters.");
                continue;
            }

            if (!ValidTopicRegex().IsMatch(value))
            {
                errors.Add($"Topic '{topic}' contains invalid characters. Use lowercase letters, digits, and '-'.");
                continue;
            }

            normalized.Add(value);
        }

        return new TopicNormalizationResult(normalized.OrderBy(static x => x).ToList(), errors);
    }
}
