# Claims API: take-home task

Ivan's take-home for Insurance Technology Solutions. The task is in `docs/README.md`. Ivan must be able to explain every line and decision in the interview; that matters more than speed.

## Read first

- `docs/handoff.md`: current state and the remaining plan, step by step.
- `docs/findings.md`: short log of findings (R…, P…) and decisions (D…).
- `docs/decisions.md`: the technical explanation behind each decision.

## How to work

- Small steps. Before each step, say what you'll change and why; after it, summarise the diff in plain words.
- One step = one focused commit. Suggest the commit message; **Ivan reviews and commits himself.** Never push.
- After every step: `dotnet build` and `dotnet test`. When behaviour could change, run the app and check the endpoints.
- Ask before any decision not recorded in `docs/findings.md`. When decided, add it there (short, with the reason) and explain it in `docs/decisions.md`. Change a decision's status from 🔧 to ✅ when its step is built.
- When a choice is debatable, give Ivan the trade-off in 2–3 sentences so he can defend it.

## Code rules

- Keep it small and clear: "more code is not better code" and "AI slop is frowned upon" (README).
- No MediatR, no AutoMapper, no generic repository. Hand-written mapping.
- One project, layered by folders: `Api` → `Application` → `Domain`; `Infrastructure` implements `Application` interfaces (D1).
- No comments that restate the code. XML doc comments on public endpoints are wanted (Swagger).
- `DateOnly` for calendar dates, UTC `DateTime` for timestamps (D7). Get "now" from the injected `TimeProvider`, never `DateTime.Now`/`UtcNow`.

## Environment

- Windows, .NET SDK 10 (`global.json`), Docker Desktop. `Program.cs` starts SQL Server and MongoDB with Testcontainers on every run, so Docker must be running.
- Tests run on xunit.v3 with Microsoft.Testing.Platform.
- The repo path contains `'` (`Ivan's PC`), which breaks `WebApplicationFactory` (R5). Until that's fixed, run tests with the content root set:

  ```bash
  ASPNETCORE_TEST_CONTENTROOT_CLAIMS="$(pwd -W)/Claims" dotnet test
  ```

  PowerShell: `$env:ASPNETCORE_TEST_CONTENTROOT_CLAIMS = "$PWD\Claims"; dotnet test`

- Run the app with `dotnet run --project Claims --launch-profile Claims`. The API is on `https://localhost:7052` (HTTP 5052 redirects); Swagger is at `/swagger`.
- If the build fails with "file is locked by Claims", the app is still running (often from Visual Studio). Stop it first.
