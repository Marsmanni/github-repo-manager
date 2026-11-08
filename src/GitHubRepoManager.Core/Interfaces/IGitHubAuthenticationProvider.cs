using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Interfaces;

public interface IGitHubAuthenticationProvider
{
    Task<AuthenticationResult> GetTokenAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
