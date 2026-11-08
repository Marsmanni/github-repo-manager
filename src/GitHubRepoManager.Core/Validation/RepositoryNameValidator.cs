using System.Text.RegularExpressions;
using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Validation;

public sealed partial class RepositoryNameValidator : IRepositoryNameValidator
{
    private const int MaxLength = 100;

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex ValidCharactersRegex();

    public NameValidationResult Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return NameValidationResult.Invalid("Repository name cannot be empty.");
        }

        if (name.Length > MaxLength)
        {
            return NameValidationResult.Invalid("Repository name cannot exceed 100 characters.");
        }

        if (name.StartsWith('.') || name.EndsWith('.'))
        {
            return NameValidationResult.Invalid("Repository name cannot start or end with a dot.");
        }

        if (name.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            return NameValidationResult.Invalid("Repository name cannot end with '.git'.");
        }

        if (!ValidCharactersRegex().IsMatch(name))
        {
            return NameValidationResult.Invalid("Repository name may only contain letters, digits, '.', '_' and '-'.");
        }

        return NameValidationResult.Valid();
    }
}
