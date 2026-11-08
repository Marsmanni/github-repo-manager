namespace GitHubRepoManager.Core.Models;

public sealed record NameValidationResult(bool IsValid, string? ErrorMessage)
{
    public static NameValidationResult Valid() => new(true, null);

    public static NameValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}
