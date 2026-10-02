# AGENTS.md

## Project overview

This repository contains a Windows desktop vehicle meter and engine-audio simulator built with C# 14, .NET 10, and WPF.

Work in **one Issue = one branch = one Codex task** units. Keep `main` releasable, do not commit directly to it, and merge through a reviewed Pull Request using Squash merge.

## Build and verification

Run from the repository root:

```powershell
dotnet restore VehicleMeterSimulator.csproj
dotnet build VehicleMeterSimulator.csproj --no-restore --configuration Release
```

There is currently no automated test project. Do not describe the build as a test run. Changes to WPF views, vehicle data, or audio behavior also require a Windows desktop smoke check and the result must be recorded in the Pull Request.

## Change rules

- Preserve the existing WPF structure, vehicle JSON schema, and audio behavior unless the Issue requires a change.
- Avoid unrelated refactoring, renaming, dependency updates, or bulk formatting.
- Keep local audio inputs, tuning exports, official-reference materials, credentials, and generated build output out of Git.
- Do not remove or replace tracked media without confirming its origin, license, and use in the application.
- Treat `Assets/Sounds/README.md` and `Assets/Sounds/THIRD_PARTY_AUDIO.md` as the source of truth for audio asset handling.
- Review `git diff --check`, `git diff`, and `git status` before committing.

## Branches and Pull Requests

Use `feature/`, `fix/`, `refactor/`, `docs/`, or `chore/` followed by a short kebab-case name. Pull Requests must link the Issue, state the build result, and list any GUI or audio checks that were not run.
