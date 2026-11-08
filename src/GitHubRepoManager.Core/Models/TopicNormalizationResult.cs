namespace GitHubRepoManager.Core.Models;

public sealed record TopicNormalizationResult(
    IReadOnlyList<string> NormalizedTopics,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
