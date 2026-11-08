using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GitHubRepoManager.Core.Models;
using GitHubRepoManager.Desktop.ViewModels;
using GitHubRepoManager.Desktop.Views;

namespace GitHubRepoManager.Desktop.Services;

public sealed class UserDialogService : IUserDialogService
{
    public async Task<bool> ConfirmAsync(Window owner, string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        var result = await ShowChoiceDialogAsync(
                owner,
                title,
                message,
                [confirmText, cancelText],
                0)
            .ConfigureAwait(false);

        return result == 0;
    }

    public async Task<ConflictResolutionChoice> ResolveConflictAsync(
        Window owner,
        string repositoryName,
        IReadOnlyList<RepositoryConflict> conflicts)
    {
        var lines = string.Join(
            Environment.NewLine,
            conflicts.Select(static c => $"- {c.Field}: remote='{c.RemoteValue}' local='{c.LocalValue}'"));

        var selection = await ShowChoiceDialogAsync(
                owner,
                $"Conflict in {repositoryName}",
                $"Conflicts were detected:\n{lines}\n\nHow do you want to continue?",
                ["Keep local value", "Keep remote value", "Cancel"],
                0)
            .ConfigureAwait(false);

        return selection switch
        {
            0 => ConflictResolutionChoice.KeepLocal,
            1 => ConflictResolutionChoice.KeepRemote,
            _ => ConflictResolutionChoice.Cancel
        };
    }

    public async Task<IReadOnlyList<RepositoryRowViewModel>?> ShowReviewChangesAsync(
        Window owner,
        IReadOnlyList<RepositoryRowViewModel> candidates)
    {
        var items = candidates
            .Select(c => new ReviewChangeItemViewModel
            {
                Repository = c,
                Summary = BuildSummary(c)
            })
            .ToList();

        var reviewWindow = new ChangeReviewWindow();
        reviewWindow.SetItems(items);
        var accepted = await reviewWindow.ShowDialog<bool>(owner).ConfigureAwait(false);
        return accepted ? reviewWindow.SelectedRepositories : null;
    }

    public Task ShowErrorAsync(Window owner, string title, string message)
        => ShowChoiceDialogAsync(owner, title, message, ["Close"], 0);

    private static string BuildSummary(RepositoryRowViewModel row)
    {
        var lines = row
            .GetChanges()
            .Select(static change => $"{change.Field}: {change.Before} → {change.After}");
        return string.Join("; ", lines);
    }

    private static async Task<int> ShowChoiceDialogAsync(
        Window owner,
        string title,
        string message,
        IReadOnlyList<string> buttons,
        int defaultButtonIndex)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var window = new Window
        {
            Title = title,
            Width = 640,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        for (var i = 0; i < buttons.Count; i++)
        {
            var index = i;
            var button = new Button
            {
                Content = buttons[i],
                MinWidth = 120
            };
            button.Click += (_, _) =>
            {
                tcs.TrySetResult(index);
                window.Close();
            };
            buttonPanel.Children.Add(button);
        }

        var grid = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        grid.Children.Add(new ScrollViewer
        {
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap
            }
        });

        var buttonsHost = new Border
        {
            Margin = new Thickness(0, 12, 0, 0),
            Child = buttonPanel
        };
        Grid.SetRow(buttonsHost, 1);
        grid.Children.Add(buttonsHost);
        window.Content = grid;

        window.Closed += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(defaultButtonIndex);
            }
        };

        _ = window.ShowDialog(owner);
        return await tcs.Task.ConfigureAwait(false);
    }
}
