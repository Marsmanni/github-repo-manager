using GitHubRepoManager.Core.Validation;

namespace GitHubRepoManager.Tests.Validation;

public sealed class RepositoryNameValidatorTests
{
    private readonly RepositoryNameValidator _validator = new();

    [Theory]
    [InlineData("valid-repo")]
    [InlineData("repo_name")]
    [InlineData("repo.name")]
    public void Validate_ReturnsValid_ForSupportedNames(string name)
    {
        var result = _validator.Validate(name);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".repo")]
    [InlineData("repo.")]
    [InlineData("repo.git")]
    [InlineData("repo!")]
    public void Validate_ReturnsInvalid_ForUnsupportedNames(string name)
    {
        var result = _validator.Validate(name);
        Assert.False(result.IsValid);
    }
}
