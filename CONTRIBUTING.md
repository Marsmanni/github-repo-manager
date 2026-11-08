# Contributing

Thanks for your interest in contributing.

## Getting Started

1. Fork the repository and create a feature branch from `main`.
2. Install prerequisites:
   - .NET SDK (see `global.json`)
   - GitHub CLI (`gh`) if needed for app flows
3. Run:
   - `dotnet restore GitHubRepoManager.slnx`
   - `dotnet build GitHubRepoManager.slnx`
   - `dotnet test GitHubRepoManager.slnx`

## Development Guidelines

- Keep changes focused and small.
- Follow existing project structure and naming patterns.
- Add or update tests when behavior changes.
- Update `README.md` when user-facing behavior changes.

## Pull Requests

- Use clear commit messages.
- Describe motivation and scope in the PR.
- Include screenshots for UI changes.
- Ensure build and tests pass before requesting review.
