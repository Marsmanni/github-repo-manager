using GitHubRepoManager.Core.Validation;

namespace GitHubRepoManager.Tests.Validation;

public sealed class TopicNormalizerTests
{
    private readonly TopicNormalizer _normalizer = new();

    [Fact]
    public void Normalize_NormalizesToLowercaseAndRemovesDuplicates()
    {
        var result = _normalizer.Normalize(["DotNet", "dotnet", "api-client"]);
        Assert.True(result.IsValid);
        Assert.Equal(["api-client", "dotnet"], result.NormalizedTopics);
    }

    [Fact]
    public void Normalize_ReportsInvalidTopics()
    {
        var result = _normalizer.Normalize(["bad topic", "ok-topic"]);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }
}
