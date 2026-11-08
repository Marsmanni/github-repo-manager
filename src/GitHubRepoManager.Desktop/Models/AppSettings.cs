namespace GitHubRepoManager.Desktop.Models;

public sealed record AppSettings
{
    public double WindowWidth { get; init; } = 1400;
    public double WindowHeight { get; init; } = 900;
    public string Theme { get; init; } = "System";
    public string SearchText { get; init; } = string.Empty;
    public string VisibilityFilter { get; init; } = "All";
    public bool ModifiedOnlyFilter { get; init; }
}
