namespace GitHubRepoManager.Core.Models;

public sealed record AuthenticationResult(bool Succeeded, string? Token, string? ErrorMessage)
{
    public static AuthenticationResult Success(string token) => new(true, token, null);

    public static AuthenticationResult Failure(string message) => new(false, null, message);
}
