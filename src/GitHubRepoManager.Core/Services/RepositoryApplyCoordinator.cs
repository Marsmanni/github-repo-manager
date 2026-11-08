using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.Core.Models;
using Microsoft.Extensions.Logging;

namespace GitHubRepoManager.Core.Services;

public sealed class RepositoryApplyCoordinator(
    IGitHubRepositoryService repositoryService,
    RepositoryChangeTracker changeTracker,
    ILogger<RepositoryApplyCoordinator> logger)
{
    public async Task<RepositoryBatchApplyResult> ApplyAsync(
        IReadOnlyCollection<long> repositoryIds,
        Func<long, IReadOnlyList<RepositoryConflict>, Task<ConflictResolutionChoice>> conflictResolver,
        CancellationToken cancellationToken)
    {
        var results = new List<RepositoryApplyResult>();

        var orderedIds = repositoryIds
            .Distinct()
            .Select(id => changeTracker.GetState(id))
            .OrderBy(static s => s.Repository.Owner, StringComparer.Ordinal)
            .ThenBy(static s => s.Repository.Name, StringComparer.Ordinal)
            .ThenBy(static s => s.Repository.Id)
            .Select(static s => s.Repository.Id)
            .ToList();

        foreach (var id in orderedIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!changeTracker.TryGetState(id, out var state) || state is null)
            {
                continue;
            }

            var changedFields = RepositoryChangePlanner.GetChangedFields(state.Baseline, state.Working);
            if (changedFields.Count == 0)
            {
                continue;
            }

            changeTracker.SetStatus(id, RepositoryChangeState.Applying);

            RepositoryRecord remote;
            try
            {
                remote = await repositoryService.GetRepositoryByIdAsync(id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch remote repository state for {RepositoryId}", id);
                var fetchFailure = new RepositoryApplyResult(id, false, "Failed to load latest remote state.", null);
                changeTracker.SetStatus(id, RepositoryChangeState.Failed, fetchFailure.ErrorMessage);
                results.Add(fetchFailure);
                continue;
            }

            var remoteSnapshot = remote.ToEditableSnapshot();
            var conflicts = RepositoryConflictDetector.Detect(state.Baseline, remoteSnapshot, state.Working);
            if (conflicts.Count > 0)
            {
                var resolution = await conflictResolver(id, conflicts).ConfigureAwait(false);
                if (resolution == ConflictResolutionChoice.Cancel)
                {
                    changeTracker.SetStatus(id, RepositoryChangeState.Conflict, "Apply was cancelled due to conflicts.");
                    results.Add(new RepositoryApplyResult(id, false, "Conflict detected.", null));
                    continue;
                }

                if (resolution == ConflictResolutionChoice.KeepRemote)
                {
                    changeTracker.AcceptServerState(remote);
                    results.Add(new RepositoryApplyResult(id, true, null, remote));
                    continue;
                }
            }

            var hasNonArchiveUpdates = changedFields.Any(static f => f is not RepositoryField.Archived);
            if (remote.Archived && state.Working.Archived && hasNonArchiveUpdates)
            {
                var archivedFailure = new RepositoryApplyResult(
                    id,
                    false,
                    "Repository is archived and read-only. Unarchive it first to apply name/description/topic/visibility updates.",
                    null);
                changeTracker.SetStatus(id, RepositoryChangeState.Failed, archivedFailure.ErrorMessage);
                results.Add(archivedFailure);
                continue;
            }

            var request = new RepositoryUpdateRequest(
                id,
                remote.Owner,
                remote.Name,
                state.Working,
                changedFields);

            var applyResult = await repositoryService.ApplyChangesAsync(request, cancellationToken).ConfigureAwait(false);
            if (applyResult.Succeeded && applyResult.UpdatedRepository is not null)
            {
                changeTracker.AcceptServerState(applyResult.UpdatedRepository);
                results.Add(applyResult);
                continue;
            }

            changeTracker.SetStatus(id, RepositoryChangeState.Failed, applyResult.ErrorMessage ?? "Unknown error.");
            results.Add(applyResult with { Succeeded = false });
        }

        return new RepositoryBatchApplyResult(results);
    }
}
