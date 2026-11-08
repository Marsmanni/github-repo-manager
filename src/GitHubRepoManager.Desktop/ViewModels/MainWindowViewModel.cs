using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.Core.Models;
using GitHubRepoManager.Core.Services;
using GitHubRepoManager.Desktop.Models;
using GitHubRepoManager.Desktop.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace GitHubRepoManager.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGitHubRepositoryService _repositoryService;
    private readonly IRepositoryNameValidator _nameValidator;
    private readonly ITopicNormalizer _topicNormalizer;
    private readonly IAppSettingsService _settingsService;
    private readonly IUserDialogService _dialogService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly Dictionary<long, RepositoryRowViewModel> _indexById = [];

    public MainWindowViewModel(
        IGitHubRepositoryService repositoryService,
        IRepositoryNameValidator nameValidator,
        ITopicNormalizer topicNormalizer,
        IAppSettingsService settingsService,
        IUserDialogService dialogService,
        ILogger<MainWindowViewModel> logger)
    {
        _repositoryService = repositoryService;
        _nameValidator = nameValidator;
        _topicNormalizer = topicNormalizer;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _logger = logger;
    }

    public Window? OwnerWindow { get; set; }

    public ObservableCollection<RepositoryRowViewModel> Repositories { get; } = [];
    public ObservableCollection<RepositoryRowViewModel> FilteredRepositories { get; } = [];
    public ObservableCollection<RepositoryRowViewModel> SelectedRepositories { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _visibilityFilter = "All";

    [ObservableProperty]
    private bool _modifiedOnly;

    [ObservableProperty]
    private bool _activeOnly;

    [ObservableProperty]
    private bool _forksOnly;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Ready";

    public double WindowWidth { get; set; } = 1400;
    public double WindowHeight { get; set; } = 900;

    public int PendingChangesCount => Repositories.Count(static r => r.IsModified);
    public int VisibleRepositoriesCount => FilteredRepositories.Count;

    public IReadOnlyList<string> VisibilityFilters { get; } = ["All", "Public", "Private", "Internal"];

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnVisibilityFilterChanged(string value) => ApplyFilters();
    partial void OnModifiedOnlyChanged(bool value) => ApplyFilters();
    partial void OnActiveOnlyChanged(bool value) => ApplyFilters();
    partial void OnForksOnlyChanged(bool value) => ApplyFilters();

    [RelayCommand]
    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync(CancellationToken.None);
        SearchText = settings.SearchText?.Trim() ?? string.Empty;
        VisibilityFilter = VisibilityFilters.Contains(settings.VisibilityFilter, StringComparer.OrdinalIgnoreCase)
            ? settings.VisibilityFilter
            : "All";
        ModifiedOnly = settings.ModifiedOnlyFilter;
        WindowWidth = settings.WindowWidth;
        WindowHeight = settings.WindowHeight;
        await RefreshAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsBusy = true;
            StatusText = "Loading repositories...";
        });

        try
        {
            var repositories = await _repositoryService.GetOwnedRepositoriesAsync(CancellationToken.None);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Repositories.Clear();
                _indexById.Clear();

                foreach (var repository in repositories)
                {
                    var row = new RepositoryRowViewModel(repository, _nameValidator, _topicNormalizer);
                    row.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName is nameof(RepositoryRowViewModel.ChangeState))
                        {
                            OnPropertyChanged(nameof(PendingChangesCount));
                        }
                    };
                    Repositories.Add(row);
                    _indexById[row.Id] = row;
                }

                ApplyFilters();
                StatusText = repositories.Count == 0
                    ? "No owned repositories found."
                    : FilteredRepositories.Count == repositories.Count
                        ? $"Loaded {repositories.Count} repositories."
                        : $"Loaded {repositories.Count} repositories ({FilteredRepositories.Count} shown by current filters).";
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh repositories.");
            await Dispatcher.UIThread.InvokeAsync(() => StatusText = "Failed to load repositories.");
            if (OwnerWindow is not null)
            {
                await _dialogService.ShowErrorAsync(OwnerWindow, "Load failed", ex.Message);
            }
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsBusy = false);
        }
    }

    [RelayCommand]
    public void DiscardSelectedChanges()
    {
        foreach (var row in SelectedRepositories)
        {
            row.DiscardChanges();
        }

        StatusText = "Discarded selected changes.";
        OnPropertyChanged(nameof(PendingChangesCount));
        ApplyFilters();
    }

    [RelayCommand]
    public void DiscardAllChanges()
    {
        foreach (var row in Repositories.Where(static x => x.IsModified))
        {
            row.DiscardChanges();
        }

        StatusText = "Discarded all pending changes.";
        OnPropertyChanged(nameof(PendingChangesCount));
        ApplyFilters();
    }

    [RelayCommand]
    public void ResetFilters()
    {
        SearchText = string.Empty;
        VisibilityFilter = "All";
        ActiveOnly = false;
        ForksOnly = false;
        ModifiedOnly = false;

        StatusText = Repositories.Count == 0
            ? "Filters reset."
            : $"Filters reset. Showing {FilteredRepositories.Count} of {Repositories.Count} repositories.";
    }

    [RelayCommand]
    public async Task ShowCloneAllScriptAsync()
    {
        if (OwnerWindow is null)
        {
            return;
        }

        if (Repositories.Count == 0)
        {
            StatusText = "No repositories loaded.";
            return;
        }

        var script = BuildCloneAllScript(Repositories);
        await _dialogService.ShowCloneAllScriptAsync(OwnerWindow, script);
    }

    [RelayCommand]
    public async Task ApplyChangesAsync()
    {
        if (OwnerWindow is null)
        {
            return;
        }

        var candidates = Repositories.Where(static x => x.IsModified).ToList();
        if (candidates.Count == 0)
        {
            StatusText = "No pending changes.";
            return;
        }

        var duplicates = DuplicateRepositoryNameDetector.Detect(
            candidates.Select(static c => (c.Id, c.Owner, c.Working)));

        if (duplicates.Count > 0)
        {
            await _dialogService.ShowErrorAsync(
                    OwnerWindow,
                    "Duplicate target names",
                    "Duplicate repository names were detected for the same owner. Resolve duplicates before applying.");
            return;
        }

        var selected = await _dialogService.ShowReviewChangesAsync(OwnerWindow, candidates);
        if (selected is null || selected.Count == 0)
        {
            return;
        }

        if (ContainsSensitiveChanges(selected))
        {
            var confirmed = await _dialogService
                .ConfirmAsync(
                    OwnerWindow,
                    "Sensitive operations",
                    "Visibility and archive changes can impact access, pages, forks, and integrations. Continue?",
                    "Apply changes");

            if (!confirmed)
            {
                return;
            }
        }

        IsBusy = true;
        StatusText = $"Applying changes to {selected.Count} repositories...";

        var tracker = new RepositoryChangeTracker();
        tracker.Initialize(Repositories.Select(static r => new RepositoryRecord
        {
            Id = r.Id,
            Owner = r.Owner,
            Name = r.Baseline.Name,
            FullName = r.FullName,
            Description = r.Baseline.Description,
            HtmlUrl = r.HtmlUrl,
            Visibility = r.Baseline.Visibility,
            Archived = r.Baseline.Archived,
            Disabled = false,
            IsFork = r.IsFork,
            PrimaryLanguage = r.PrimaryLanguage,
            Topics = r.Baseline.Topics,
            DefaultBranch = "main",
            Stars = r.Stars,
            Forks = 0,
            OpenIssuesCount = 0,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = r.LastUpdated,
            PushedAt = r.LastUpdated,
            SizeInKilobytes = 0
        }));

        foreach (var row in Repositories)
        {
            tracker.UpdateWorking(row.Id, row.Working);
        }

        var coordinator = new RepositoryApplyCoordinator(
            _repositoryService,
            tracker,
            NullLogger<RepositoryApplyCoordinator>.Instance);

        var applyResult = await coordinator.ApplyAsync(
                selected.Select(static s => s.Id).ToList(),
                async (id, conflicts) =>
                {
                    var repo = _indexById[id];
                    return await _dialogService.ResolveConflictAsync(OwnerWindow, repo.FullName, conflicts);
                },
                CancellationToken.None);

        foreach (var result in applyResult.Results)
        {
            if (!_indexById.TryGetValue(result.RepositoryId, out var row))
            {
                continue;
            }

            if (result.Succeeded && result.UpdatedRepository is not null)
            {
                row.AcceptRemote(result.UpdatedRepository);
                continue;
            }

            row.ChangeState = RepositoryChangeState.Failed;
        }

        StatusText = $"Applied: {applyResult.SucceededCount}, Failed: {applyResult.FailedCount}.";
        IsBusy = false;
        OnPropertyChanged(nameof(PendingChangesCount));
        ApplyFilters();
    }

    [RelayCommand]
    public void OpenSelectedRepository()
    {
        var target = SelectedRepositories.FirstOrDefault();
        if (target is null)
        {
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = target.HtmlUrl.ToString(),
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open repository URL.");
        }
    }

    public void UpdateSelection(IEnumerable<RepositoryRowViewModel> selectedRows)
    {
        SelectedRepositories.Clear();
        foreach (var row in selectedRows)
        {
            SelectedRepositories.Add(row);
        }
    }

    public async Task SaveSettingsAsync()
    {
        var settings = new AppSettings
        {
            SearchText = SearchText,
            VisibilityFilter = VisibilityFilter,
            ModifiedOnlyFilter = ModifiedOnly,
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight
        };
        await _settingsService.SaveAsync(settings, CancellationToken.None);
    }

    private void ApplyFilters()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(ApplyFilters);
            return;
        }

        var filtered = Repositories.Where(MatchesFilter).ToList();
        FilteredRepositories.Clear();
        foreach (var row in filtered)
        {
            FilteredRepositories.Add(row);
        }

        OnPropertyChanged(nameof(VisibleRepositoriesCount));
    }

    private bool MatchesFilter(RepositoryRowViewModel row)
    {
        if (ModifiedOnly && !row.IsModified)
        {
            return false;
        }

        if (ActiveOnly && row.Archived)
        {
            return false;
        }

        if (ForksOnly && !row.IsFork)
        {
            return false;
        }

        if (VisibilityFilter != "All"
            && !string.Equals(row.Visibility.ToString(), VisibilityFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var search = SearchText.Trim();
        return row.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
               || row.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
               || row.TopicsText.Contains(search, StringComparison.OrdinalIgnoreCase)
               || row.PrimaryLanguage.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsSensitiveChanges(IEnumerable<RepositoryRowViewModel> rows)
        => rows.Any(static r =>
            r.GetChangedFields().Contains(RepositoryField.Visibility)
            || r.GetChangedFields().Contains(RepositoryField.Archived));

    private static string BuildCloneAllScript(IEnumerable<RepositoryRowViewModel> repositories)
    {
        var lines = repositories
            .Select(static r => r.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static fullName => fullName, StringComparer.OrdinalIgnoreCase)
            .Select(static fullName => $"git clone https://github.com/{fullName}.git")
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("#!/usr/bin/env bash");
        sb.AppendLine("set -euo pipefail");
        sb.AppendLine();
        sb.AppendLine("# Clone all owned repositories");
        sb.AppendLine("mkdir -p github-repos");
        sb.AppendLine("cd github-repos");
        sb.AppendLine();
        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
