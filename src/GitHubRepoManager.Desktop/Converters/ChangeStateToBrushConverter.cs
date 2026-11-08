using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Desktop.Converters;

public sealed class ChangeStateToBrushConverter : IValueConverter
{
    public static readonly ChangeStateToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not RepositoryChangeState state)
        {
            return Brushes.Transparent;
        }

        return state switch
        {
            RepositoryChangeState.Modified => new SolidColorBrush(Color.FromArgb(35, 216, 151, 18)),
            RepositoryChangeState.Failed => new SolidColorBrush(Color.FromArgb(35, 220, 38, 38)),
            RepositoryChangeState.Conflict => new SolidColorBrush(Color.FromArgb(35, 165, 94, 234)),
            RepositoryChangeState.Applying => new SolidColorBrush(Color.FromArgb(35, 59, 130, 246)),
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
