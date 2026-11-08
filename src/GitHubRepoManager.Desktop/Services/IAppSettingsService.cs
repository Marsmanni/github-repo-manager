using GitHubRepoManager.Desktop.Models;

namespace GitHubRepoManager.Desktop.Services;

public interface IAppSettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
