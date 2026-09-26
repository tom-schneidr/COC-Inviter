# COC Inviter

COC Inviter is a Windows WPF desktop tool for finding and inviting Clash of Clans players using configurable player criteria. It combines the Clash of Clans API with optional Discord interactions and screen automation for the invite workflow.

## Current status

This is a legacy project. It has no automated CI workflow, and its compatibility with current Clash of Clans, Discord, Windows, and .NET Framework versions has not been verified in this repository.

The default branch targets **.NET Framework 4.8.1**. A local build also requires the matching developer or targeting pack and the NuGet packages listed by the project.

## Configuration

- The Clash of Clans API key is entered through the application UI.
- The optional Discord integration reads `BOT_TOKEN` from a local `.env` file. Keep that file outside Git and never commit credentials.
- The solution and project are under `COCInviter/`.

## Scope and limitations

The application automates desktop interactions and makes external API and Discord requests. Review the source, current game and platform rules, and the configured targets before using it. This repository does not claim current compatibility, production reliability, or safe operation against updated clients.

## Build

1. Install Visual Studio with the .NET Framework 4.8.1 developer pack.
2. Restore the packages referenced by `COCInviter/COCInviter.csproj`.
3. Open `COCInviter.sln` and build the WPF application.

No automated test suite or live-game validation is included in the repository.
