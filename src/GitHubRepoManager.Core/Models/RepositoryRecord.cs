namespace GitHubRepoManager.Core.Models;

public sealed record RepositoryRecord
{
    public required long Id { get; init; }
    public required string Owner { get; init; }
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
    public required Uri HtmlUrl { get; init; }
    public required RepositoryVisibility Visibility { get; init; }
    public required bool Archived { get; init; }
    public required bool Disabled { get; init; }
    public required bool IsFork { get; init; }
    public string? PrimaryLanguage { get; init; }
    public required IReadOnlyList<string> Topics { get; init; }
    public required string DefaultBranch { get; init; }
    public required int Stars { get; init; }
    public required int Forks { get; init; }
    public required int OpenIssuesCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required DateTimeOffset PushedAt { get; init; }
    public required int SizeInKilobytes { get; init; }
    public string? LicenseName { get; init; }

    public EditableRepositorySnapshot ToEditableSnapshot() =>
        new(Name, Description, Topics, Visibility, Archived);
}
