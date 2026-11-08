using Avalonia.Controls;
using Avalonia.Interactivity;
using GitHubRepoManager.Desktop.ViewModels;

namespace GitHubRepoManager.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.OwnerWindow = this;
            await vm.InitializeAsync();
            Width = vm.WindowWidth;
            Height = vm.WindowHeight;
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.WindowWidth = Width;
            vm.WindowHeight = Height;
            await vm.SaveSettingsAsync();
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || sender is not DataGrid dataGrid)
        {
            return;
        }

        vm.UpdateSelection(dataGrid.SelectedItems.OfType<RepositoryRowViewModel>());
    }
}
