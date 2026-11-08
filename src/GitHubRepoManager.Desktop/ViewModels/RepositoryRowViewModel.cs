using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.Core.Models;
using GitHubRepoManager.Core.Services;

namespace GitHubRepoManager.Desktop.ViewModels;

public sealed partial class RepositoryRowViewModel : ViewModelBase
{
    private readonly IRepositoryNameValidator _nameValidator;
    private readonly ITopicNormalizer _topicNormalizer;
    private RepositoryRecord _baselineRecord;
    private EditableRepositorySnapshot _working;
    private bool _isLoadingSnapshot;

    public RepositoryRowViewModel(
        RepositoryRecord record,
        IRepositoryNameValidator nameValidator,
        ITopicNormalizer topicNormalizer)
    {
        _baselineRecord = record;
        _nameValidator = nameValidator;
        _topicNormalizer = topicNormalizer;
        _working = record.ToEditableSnapshot();
        LoadFromSnapshot(_working);
    }

    public long Id => _baselineRecord.Id;
    public string Owner => _baselineRecord.Owner;
    public string FullName => _baselineRecord.FullName;
    public Uri HtmlUrl => _baselineRecord.HtmlUrl;
    public string PrimaryLanguage => _baselineRecord.PrimaryLanguage ?? string.Empty;
    public int Stars => _baselineRecord.Stars;
    public DateTimeOffset LastUpdated => _baselineRecord.UpdatedAt;
    public bool IsFork => _baselineRecord.IsFork;
    public IReadOnlyList<RepositoryVisibility> VisibilityOptions { get; } =
        Enum.GetValues<RepositoryVisibility>();

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (!_isLoadingSnapshot)
                {
                    RebuildWorking();
                }
            }
        }
    }

    private string _description = string.Empty;
    private string _descriptionDisplay = "—";

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                DescriptionDisplay = string.IsNullOrWhiteSpace(value) ? "—" : value;
                if (!_isLoadingSnapshot)
                {
                    RebuildWorking();
                }
            }
        }
    }

    public string DescriptionDisplay
    {
        get => _descriptionDisplay;
        private set => SetProperty(ref _descriptionDisplay, value);
    }

    private string _topicsText = string.Empty;
    public string TopicsText
    {
        get => _topicsText;
        set
        {
            if (SetProperty(ref _topicsText, value))
            {
                if (!_isLoadingSnapshot)
                {
                    RebuildWorking();
                }
            }
        }
    }

    private RepositoryVisibility _visibility;
    public RepositoryVisibility Visibility
    {
        get => _visibility;
        set
        {
            if (SetProperty(ref _visibility, value))
            {
                if (!_isLoadingSnapshot)
                {
                    RebuildWorking();
                }
            }
        }
    }

    private bool _archived;
    public bool Archived
    {
        get => _archived;
        set
        {
            if (SetProperty(ref _archived, value))
            {
                if (!_isLoadingSnapshot)
                {
                    RebuildWorking();
                }
            }
        }
    }

    private RepositoryChangeState _changeState = RepositoryChangeState.Unchanged;
    public RepositoryChangeState ChangeState
    {
        get => _changeState;
        set => SetProperty(ref _changeState, value);
    }

    private string _validationError = string.Empty;
    public string ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    public EditableRepositorySnapshot Baseline => _baselineRecord.ToEditableSnapshot();
    public EditableRepositorySnapshot Working => _working;
    public bool IsModified => ChangeState is RepositoryChangeState.Modified or RepositoryChangeState.Failed or RepositoryChangeState.Conflict;
    public string ChangeStatus => ChangeState.ToString();

    public void AcceptRemote(RepositoryRecord updatedRecord)
    {
        _baselineRecord = updatedRecord;
        LoadFromSnapshot(updatedRecord.ToEditableSnapshot());
        ChangeState = RepositoryChangeState.Unchanged;
    }

    public void DiscardChanges()
    {
        LoadFromSnapshot(_baselineRecord.ToEditableSnapshot());
    }

    public IReadOnlyList<RepositoryPropertyChange> GetChanges() =>
        RepositoryChangePlanner.CreatePlan(Id, Baseline, Working).Changes;

    public IReadOnlySet<RepositoryField> GetChangedFields() =>
        RepositoryChangePlanner.GetChangedFields(Baseline, Working);

    private void RebuildWorking()
    {
        var nameValidation = _nameValidator.Validate(Name.Trim());
        var normalizedTopics = _topicNormalizer.Normalize(TopicsText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        ValidationError = !nameValidation.IsValid
            ? nameValidation.ErrorMessage ?? "Invalid repository name."
            : normalizedTopics.Errors.FirstOrDefault() ?? string.Empty;

        _working = new EditableRepositorySnapshot(
            Name.Trim(),
            NormalizeDescription(Description),
            normalizedTopics.NormalizedTopics,
            Visibility,
            Archived);

        ChangeState = RepositoryChangePlanner.CreatePlan(Id, Baseline, _working).HasChanges
            ? RepositoryChangeState.Modified
            : RepositoryChangeState.Unchanged;

        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(ChangeStatus));
    }

    private void LoadFromSnapshot(EditableRepositorySnapshot snapshot)
    {
        _isLoadingSnapshot = true;
        try
        {
            Name = snapshot.Name;
            Description = snapshot.Description ?? string.Empty;
            TopicsText = string.Join(", ", snapshot.Topics);
            Visibility = snapshot.Visibility;
            Archived = snapshot.Archived;
        }
        finally
        {
            _isLoadingSnapshot = false;
        }

        RebuildWorking();
    }

    private static string? NormalizeDescription(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var buffer = input
            .Select(static c => char.IsControl(c) ? ' ' : c)
            .ToArray();

        var collapsed = string.Join(
            ' ',
            new string(buffer)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return string.IsNullOrWhiteSpace(collapsed) ? null : collapsed;
    }
}
