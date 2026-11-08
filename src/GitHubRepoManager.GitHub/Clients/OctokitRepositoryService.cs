using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.Core.Models;
using GitHubRepoManager.GitHub.Mapping;
using Microsoft.Extensions.Logging;
using Octokit;
using CoreVisibility = GitHubRepoManager.Core.Models.RepositoryVisibility;

namespace GitHubRepoManager.GitHub.Clients;

public sealed class OctokitRepositoryService(
    IGitHubAuthenticationProvider authenticationProvider,
    ILogger<OctokitRepositoryService> logger) : IGitHubRepositoryService
{
    private readonly SemaphoreSlim _clientSemaphore = new(1, 1);
    private GitHubClient? _client;

    public async Task<IReadOnlyList<RepositoryRecord>> GetOwnedRepositoriesAsync(CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var mapped = new List<RepositoryRecord>();
        var currentUser = await client.User.Current().ConfigureAwait(false);

        var page = 1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new ApiOptions
            {
                StartPage = page,
                PageCount = 1,
                PageSize = 100
            };

            IReadOnlyList<Repository> batch;
            try
            {
                batch = await client.Repository.GetAllForCurrent(
                        new RepositoryRequest
                        {
                            Type = RepositoryType.Owner
                        },
                        options)
                    .ConfigureAwait(false);
            }
            catch (RateLimitExceededException ex)
            {
                logger.LogWarning(ex, "GitHub API rate limit exceeded while loading repositories.");
                throw new InvalidOperationException("GitHub API rate limit exceeded. Try again later.", ex);
            }
            catch (AuthorizationException ex)
            {
                logger.LogWarning(ex, "GitHub authorization failed while loading repositories.");
                throw new InvalidOperationException("GitHub authentication failed or token permissions are insufficient.", ex);
            }
            catch (ApiException ex)
            {
                logger.LogWarning(ex, "GitHub API error while loading repositories.");
                throw new InvalidOperationException($"GitHub API error while loading repositories: {ex.ApiError?.Message ?? ex.Message}", ex);
            }

            var owned = batch.Where(r => string.Equals(r.Owner.Login, currentUser.Login, StringComparison.OrdinalIgnoreCase)).ToList();
            mapped.AddRange(owned.Select(RepositoryMapper.Map));

            if (batch.Count < 100)
            {
                break;
            }

            page++;
        }

        return mapped
            .OrderBy(static r => r.Owner, StringComparer.Ordinal)
            .ThenBy(static r => r.Name, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<RepositoryRecord> GetRepositoryByIdAsync(long repositoryId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var repository = await client.Repository.Get(repositoryId).ConfigureAwait(false);
            return RepositoryMapper.Map(repository);
        }
        catch (NotFoundException ex)
        {
            throw new InvalidOperationException("Repository was not found or is no longer accessible.", ex);
        }
        catch (AuthorizationException ex)
        {
            throw new InvalidOperationException("GitHub authentication failed or token permissions are insufficient.", ex);
        }
    }

    public async Task<RepositoryApplyResult> ApplyChangesAsync(
        RepositoryUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var owner = request.Owner;
            var currentName = request.CurrentName;
            var nameAfterUpdate = request.DesiredState.Name;

            if (request.ChangedFields.Contains(RepositoryField.Name)
                || request.ChangedFields.Contains(RepositoryField.Description)
                || request.ChangedFields.Contains(RepositoryField.Visibility)
                || request.ChangedFields.Contains(RepositoryField.Archived))
            {
                var update = new RepositoryUpdate
                {
                    Name = nameAfterUpdate,
                    Description = string.IsNullOrWhiteSpace(request.DesiredState.Description)
                        ? null
                        : request.DesiredState.Description,
                    Archived = request.DesiredState.Archived
                };

                if (request.ChangedFields.Contains(RepositoryField.Visibility))
                {
                    update.Visibility = request.DesiredState.Visibility switch
                    {
                        CoreVisibility.Private => Octokit.RepositoryVisibility.Private,
                        CoreVisibility.Internal => Octokit.RepositoryVisibility.Internal,
                        _ => Octokit.RepositoryVisibility.Public
                    };
                }

                var updatedRepo = await client.Repository.Edit(owner, currentName, update).ConfigureAwait(false);
                currentName = updatedRepo.Name;
            }

            if (request.ChangedFields.Contains(RepositoryField.Topics))
            {
                var topicsRequest = new RepositoryTopics(request.DesiredState.Topics.ToArray());
                await client.Repository.ReplaceAllTopics(owner, currentName, topicsRequest).ConfigureAwait(false);
            }

            var refreshed = await client.Repository.Get(request.RepositoryId).ConfigureAwait(false);
            var mapped = RepositoryMapper.Map(refreshed);
            return new RepositoryApplyResult(request.RepositoryId, true, null, mapped);
        }
        catch (AuthorizationException ex)
        {
            logger.LogWarning(ex, "Unauthorized applying repository changes for {RepositoryId}", request.RepositoryId);
            return new RepositoryApplyResult(
                request.RepositoryId,
                false,
                "GitHub authorization failed or token permissions are insufficient.",
                null);
        }
        catch (ApiValidationException ex)
        {
            logger.LogWarning(ex, "Validation error applying repository changes for {RepositoryId}", request.RepositoryId);
            return new RepositoryApplyResult(
                request.RepositoryId,
                false,
                ex.ApiError?.Message ?? "GitHub validation failed for this repository update.",
                null);
        }
        catch (RateLimitExceededException ex)
        {
            logger.LogWarning(ex, "Rate limit exceeded applying repository changes for {RepositoryId}", request.RepositoryId);
            return new RepositoryApplyResult(
                request.RepositoryId,
                false,
                "GitHub API rate limit exceeded. Try again later.",
                null);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "GitHub API error applying repository changes for {RepositoryId}", request.RepositoryId);
            return new RepositoryApplyResult(
                request.RepositoryId,
                false,
                ex.ApiError?.Message ?? "GitHub API returned an error while applying changes.",
                null);
        }
    }

    private async Task<GitHubClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            return _client;
        }

        await _clientSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is not null)
            {
                return _client;
            }

            var auth = await authenticationProvider
                .GetTokenAsync(TimeSpan.FromSeconds(10), cancellationToken)
                .ConfigureAwait(false);

            if (!auth.Succeeded || string.IsNullOrWhiteSpace(auth.Token))
            {
                throw new InvalidOperationException(auth.ErrorMessage ?? "Authentication failed.");
            }

            _client = new GitHubClient(new ProductHeaderValue("GitHubRepoManager"))
            {
                Credentials = new Credentials(auth.Token)
            };

            return _client;
        }
        finally
        {
            _clientSemaphore.Release();
        }
    }
}
