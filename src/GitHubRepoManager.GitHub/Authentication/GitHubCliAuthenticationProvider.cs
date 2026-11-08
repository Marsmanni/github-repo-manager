using System.Diagnostics;
using System.Text;
using GitHubRepoManager.Core.Interfaces;
using GitHubRepoManager.Core.Models;
using Microsoft.Extensions.Logging;

namespace GitHubRepoManager.GitHub.Authentication;

public sealed class GitHubCliAuthenticationProvider(ILogger<GitHubCliAuthenticationProvider> logger) : IGitHubAuthenticationProvider
{
    public async Task<AuthenticationResult> GetTokenAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!await IsGhInstalledAsync(timeout, cancellationToken).ConfigureAwait(false))
        {
            return AuthenticationResult.Failure("GitHub CLI was not found. Install 'gh' and authenticate with 'gh auth login'.");
        }

        var tokenResult = await RunProcessAsync("gh", ["auth", "token"], timeout, cancellationToken).ConfigureAwait(false);
        if (!tokenResult.Succeeded)
        {
            return AuthenticationResult.Failure(
                "Could not retrieve a GitHub token from GitHub CLI. Ensure you are authenticated with 'gh auth login'.");
        }

        var token = tokenResult.StdOut.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticationResult.Failure("GitHub CLI returned an empty token. Re-authenticate with 'gh auth login'.");
        }

        return AuthenticationResult.Success(token);
    }

    private async Task<bool> IsGhInstalledAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync("gh", ["--version"], timeout, cancellationToken).ConfigureAwait(false);
        return result.Succeeded;
    }

    private async Task<ProcessExecutionResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> args,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        var stdOutBuilder = new StringBuilder();
        var stdErrBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdOutBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdErrBuilder.AppendLine(e.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                return ProcessExecutionResult.Failed("Failed to start process.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to start process {FileName}", fileName);
            return ProcessExecutionResult.Failed("Process start failed.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            return cancellationToken.IsCancellationRequested
                ? ProcessExecutionResult.Failed("Operation was cancelled.")
                : ProcessExecutionResult.Failed("Operation timed out.");
        }

        if (process.ExitCode != 0)
        {
            var stderr = stdErrBuilder.ToString();
            logger.LogWarning("Process {FileName} exited with {ExitCode}.", fileName, process.ExitCode);
            return ProcessExecutionResult.Failed(stderr);
        }

        return ProcessExecutionResult.Ok(stdOutBuilder.ToString(), stdErrBuilder.ToString());
    }

    private sealed record ProcessExecutionResult(bool Succeeded, string StdOut, string StdErr)
    {
        public static ProcessExecutionResult Ok(string stdOut, string stdErr) => new(true, stdOut, stdErr);

        public static ProcessExecutionResult Failed(string stdErr) => new(false, string.Empty, stdErr);
    }
}
