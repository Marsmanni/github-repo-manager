using GitHubRepoManager.Core.Models;

namespace GitHubRepoManager.Core.Services;

public sealed class RepositoryChangeTracker
{
    private readonly Dictionary<long, TrackedRepositoryState> _states = [];

    public IReadOnlyCollection<TrackedRepositoryState> States => _states.Values;

    public void Initialize(IEnumerable<RepositoryRecord> repositories)
    {
        _states.Clear();
        foreach (var repository in repositories)
        {
            _states[repository.Id] = new TrackedRepositoryState(
                repository,
                repository.ToEditableSnapshot(),
                RepositoryChangeState.Unchanged,
                null);
        }
    }

    public TrackedRepositoryState GetState(long repositoryId) => _states[repositoryId];

    public bool TryGetState(long repositoryId, out TrackedRepositoryState? state) => _states.TryGetValue(repositoryId, out state);

    public void UpdateWorking(long repositoryId, EditableRepositorySnapshot working)
    {
        var state = _states[repositoryId];
        var plan = RepositoryChangePlanner.CreatePlan(repositoryId, state.Baseline, working);
        var status = plan.HasChanges ? RepositoryChangeState.Modified : RepositoryChangeState.Unchanged;
        _states[repositoryId] = state with { Working = working, ChangeState = status, LastError = null };
    }

    public void SetStatus(long repositoryId, RepositoryChangeState status, string? lastError = null)
    {
        var state = _states[repositoryId];
        _states[repositoryId] = state with { ChangeState = status, LastError = lastError };
    }

    public void AcceptServerState(RepositoryRecord repository)
    {
        _states[repository.Id] = new TrackedRepositoryState(
            repository,
            repository.ToEditableSnapshot(),
            RepositoryChangeState.Unchanged,
            null);
    }

    public void Discard(long repositoryId)
    {
        var state = _states[repositoryId];
        _states[repositoryId] = state with
        {
            Working = state.Baseline,
            ChangeState = RepositoryChangeState.Unchanged,
            LastError = null
        };
    }

    public void DiscardAll()
    {
        foreach (var id in _states.Keys.ToList())
        {
            Discard(id);
        }
    }

    public int PendingChangesCount() =>
        _states.Values.Count(static s =>
            s.ChangeState is RepositoryChangeState.Modified or RepositoryChangeState.Failed or RepositoryChangeState.Conflict);
}

public sealed record TrackedRepositoryState(
    RepositoryRecord Repository,
    EditableRepositorySnapshot Working,
    RepositoryChangeState ChangeState,
    string? LastError)
{
    public EditableRepositorySnapshot Baseline => Repository.ToEditableSnapshot();
}
