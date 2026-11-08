namespace GitHubRepoManager.Desktop.ViewModels;

public sealed class ReviewChangeItemViewModel
{
    public required RepositoryRowViewModel Repository { get; init; }
    public bool Apply { get; set; } = true;
    public required string Summary { get; init; }
}
