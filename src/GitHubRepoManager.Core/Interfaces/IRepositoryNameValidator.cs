using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Interfaces;

public interface IRepositoryNameValidator
{
    NameValidationResult Validate(string name);
}
