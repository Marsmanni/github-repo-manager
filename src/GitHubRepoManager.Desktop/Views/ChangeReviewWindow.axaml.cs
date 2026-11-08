using Avalonia.Controls;
using GitHubRepoManager.Desktop.ViewModels;

namespace GitHubRepoManager.Desktop.Views;

public partial class ChangeReviewWindow : Window
{
    public ChangeReviewWindow()
    {
        InitializeComponent();
    }

    public IReadOnlyList<RepositoryRowViewModel> SelectedRepositories { get; private set; } = [];

    public void SetItems(IReadOnlyList<ReviewChangeItemViewModel> items)
    {
        DataContext = new ChangeReviewContext(items);
    }

    private void OnApplyClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ChangeReviewContext context)
        {
            Close(false);
            return;
        }

        SelectedRepositories = context.Items
            .Where(static i => i.Apply)
            .Select(static i => i.Repository)
            .ToList();

        Close(true);
    }

    private void OnCancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(false);

    public sealed class ChangeReviewContext
    {
        public ChangeReviewContext(IReadOnlyList<ReviewChangeItemViewModel> items)
        {
            Items = items;
        }

        public IReadOnlyList<ReviewChangeItemViewModel> Items { get; }
    }
}
