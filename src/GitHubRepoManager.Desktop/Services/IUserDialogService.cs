using Avalonia.Controls;
using GitHubRepoManager.Core.Models;
using GitHubRepoManager.Desktop.ViewModels;

namespace GitHubRepoManager.Desktop.Services;

public interface IUserDialogService
{
    Task<bool> ConfirmAsync(Window owner, string title, string message, string confirmText = "Confirm", string cancelText = "Cancel");

    Task<ConflictResolutionChoice> ResolveConflictAsync(
        Window owner,
        string repositoryName,
        IReadOnlyList<RepositoryConflict> conflicts);

    Task<IReadOnlyList<RepositoryRowViewModel>?> ShowReviewChangesAsync(
        Window owner,
        IReadOnlyList<RepositoryRowViewModel> candidates);

    Task ShowCloneAllScriptAsync(Window owner, string script);

    Task ShowErrorAsync(Window owner, string title, string message);
}
