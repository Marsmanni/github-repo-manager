using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Interfaces;

public interface ITopicNormalizer
{
    TopicNormalizationResult Normalize(IEnumerable<string> topics);
}
