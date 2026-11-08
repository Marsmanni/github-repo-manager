using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GitHubRepoManager.Desktop.Views;

public partial class CloneAllScriptWindow : Window
{
    public CloneAllScriptWindow()
    {
        InitializeComponent();
    }

    public void SetScript(string script)
    {
        DataContext = new CloneAllScriptContext(script);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    public sealed class CloneAllScriptContext(string script)
    {
        public string Script { get; } = script;
    }
}
