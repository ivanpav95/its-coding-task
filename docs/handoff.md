# Handoff: Claims coding task

> AI working notes: kept by me and the AI assistant while working on the task, not written as documentation for reviewers. They stay in the repo because the task invites AI artifacts.

Context for an AI coding assistant (e.g. Claude Code) picking up this work.
Read this file and `docs/findings.md` before doing anything.

## Who and why

- This repo is a take-home task (`docs/README.md`). AI tools are allowed.
- Target: share the repo and a pull request by **Monday morning**. A technical interview will walk through the solution.
- **Ivan must be able to explain every line and decision.** That matters more than speed.

## How to work, environment

Working rules, code rules and environment notes are in `CLAUDE.md` at the repo root (loaded automatically by Claude Code).

- Branch: `feature/claims-refactor`. Pull request #1 into `main` is open, with a written explanation.
- Local databases: `docker compose up -d --wait`; integration tests start their own containers (D29). The first pull of the SQL Server image (~1.5 GB) is slow.

## Current state

Done (pushed; see `git log` and PR #1):

- Audit fields `required` (R2), Task 1 step 1 (folders), working notes and `CLAUDE.md`.
- Small items: R3 (audit database `ClaimsAudit`), R4 (`.gitattributes`), `MongoClient` singleton.
- Task 1 step 2: `ICoverRepository`/`IClaimRepository` in `Application/Repositories`, Mongo implementations in `Infrastructure/Persistence`. Controllers use them; behaviour unchanged (still 204 for unknown ids, still `new Auditer(...)`).
- Task 1 step 5 (done before step 3, D11): `IAuditer` in `Application/Auditing`, `Auditer` registered as scoped. Replaced by the queued auditer in Task 3.
- Task 1 step 3: `ICoverService`/`IClaimService` and implementations in `Application/Services`; controllers are thin, depend on the interfaces and pass the `CancellationToken`. Premium formula moved unchanged (bugs fixed in Task 5). Behaviour unchanged.
- Task 1 step 4: request/response records in `Application/Contracts`; `DateOnly` for cover dates and `Claim.Created` (D7); Mongo mapping moved to `ClaimsContext` so Domain has no MongoDB dependency (D6). Premium still the template formula.
- Task 1 step 6: 404/204/409/201 as decided (D8, D12–D14), `compute` is GET, no audit for operations that didn't happen, XML docs and `[ProducesResponseType]` on every endpoint (Swagger). **Task 1 is complete.**
- Task 5: `PremiumCalculator` in `Domain` replaces the template formula (P1–P3 fixed); new `Claims.UnitTests` project with 21 spec tests (D15).
- Task 2: FluentValidation validators in `Application/Validation`, called with `ValidateAsync` from the services; claim date checked against the cover period in `ClaimService`; `TimeProvider` injected; `compute` uses the cover rules (D17); one `Invalid(errors)` path to 400 (D18); sanity rules (D19); 18 validator tests (D20). The 500 for an end date before the start date is gone.
- Task 3: `ChannelAuditer` (singleton `IAuditer`) queues `AuditMessage`s on a bounded channel; `AuditWriter` (`BackgroundService`) saves them, one scope per message (D4, D21–D25). The old `Auditer` is deleted. 3 unit tests.
- Task 4a: `Claims.Tests` renamed to `Claims.IntegrationTests`; R9 fixed.
- Task 4b: `CoverServiceTests` (7) and `ClaimServiceTests` (7) with hand-written fakes (D26). `Claims.UnitTests` has 56 tests.
- Task 4c: 8 integration tests in `Claims.IntegrationTests` on one shared app start with a fixed clock; error responses now `application/problem+json` and Swagger lists exact media types (D28). **Task 4 is complete.** 64 tests at that point.
- Databases out of `Program.cs` (R1, D29): the app reads `ConnectionStrings:Audit` and `ConnectionStrings:Claims`; `docker-compose.yml` with volumes for local runs; Testcontainers moved into `ClaimsApiFactory`.
- Root `README.md` (run, test, where the databases come from, structure) and the PR description.
- Business time zone (D30): "today" is the date in `Europe/Oslo` (`BusinessTimeZone` in config), through `BusinessTimeProvider` and `GetLocalNow()`. With UTC, a cover could start "yesterday" for an hour or two after midnight in Oslo.
- No backdating (D31): the cover keeps its dates and gets server-set `StartsAt` (later of now and midnight on `StartDate`) and `EndsAt` (midnight after `EndDate`), in `Domain/CoverPeriod`. `Claim.Created` is the moment of the damage (`DateTimeOffset`, offset required by `RequireOffsetDateTimeOffsetConverter`): `StartsAt <= Created < EndsAt`, not in the future.

- Services don't know HTTP (D32): `IAuditer` takes an `AuditAction`; `ChannelAuditer` maps it to the verb stored in the audit tables.
- One project per layer (D33, replaces D1): `Claims.Domain`, `Claims.Application`, `Claims.Infrastructure`, `Claims.Api` (was `Claims`), all at the solution root. `AddApplication()`/`AddInfrastructure()` register each layer; `Program.cs` only composes. 4 architecture tests in `Claims.UnitTests/Architecture`. R5's variable is now `ASPNETCORE_TEST_CONTENTROOT_CLAIMS_API`.
- Global exception handler (D34): `AddProblemDetails()` + `UseExceptionHandler()`, in every environment. Unexpected errors are a logged 500 `application/problem+json` without internal details.

Build: 0 warnings, no vulnerable packages (R8 fixed). Tests: 86 (75 unit, 11 integration).

Notes:

- D7 still holds for the cover dates; since D31, `Claim.Created` is a moment again. R5 (the `'` in the local path) is worked around with a user environment variable, not fixed in code.

## Plan

Decisions behind every step are in `docs/findings.md` (D1–D34, D5.x) and explained in `docs/decisions.md`. Every step must keep the SOLID rules in `CLAUDE.md`. The steps were done in this order:

**Task 1: layering.** Step numbers are kept as originally planned (other docs refer to them), but the order changed: step 5 was done before step 3 (D11). Execution order:

1. ✅ Step 1: folders `Api`, `Application`, `Domain`, `Infrastructure`.
2. ✅ Step 2: repositories (`ICoverRepository`, `IClaimRepository`); `GetById` queries by id.
3. ✅ Step 5: `IAuditer` in DI, replacing `new Auditer(...)`. Moved forward so the services never depend on `Infrastructure` (D11).
4. ✅ Step 3: `ICoverService`/`IClaimService` with `CoverService`/`ClaimService` in `Application` (D10). Controllers become thin and depend on the interfaces; they pass the request's `CancellationToken`.
5. ✅ Step 4: request/response DTOs, so clients can't set `Id` or `Premium` (R7). `DateOnly` for cover dates and `Claim.Created` (D7). Revisit D6 (Mongo attributes on domain entities).
6. ✅ Step 6: HTTP correctness: 404 when not found, 204 on delete, 404 on delete of an unknown id (and no audit row), 409 when deleting a cover with claims (D8), 201 Created with `Location` on POST (D12), 400 `ValidationProblemDetails` when a claim's cover doesn't exist (D13), outcomes returned as result values, not exceptions (D14), `compute` as GET (D9). Also README Task 1 "documentation is missing": XML doc comments on the public endpoints (Swagger) and `[ProducesResponseType]`.

**Task 5: premium (D5.1–D5.3)** ✅ done: `Domain/PremiumCalculator`, 21 unit tests in `Claims.UnitTests` (D15, D16).

- Pure calculator in `Domain`, injected or static, used by `CoverService` and the `compute` endpoint.
- Days are calendar dates, end date inclusive. Day rate 1250 × multiplier (Yacht 1.1, PassengerShip 1.2, Tanker 1.5, others 1.3).
- Bands: days 1–30 full rate; days 31–180: Yacht −5%, others −2%; day 181+: Yacht −8%, others −3% (cumulative, off the base rate).
- Write tests from the spec first. Expected Yacht totals for N days: 10 → 13,750.00; 100 → 132,687.50; 200 → 262,487.50 (computed with the template's exclusive day count; with inclusive dates, a cover of N days runs from day D to D+N−1).
- Rates in one place so a different reading of "additional" is a one-line change.

**Task 2: validation (D2, D3, D5.2, D5.4)** ✅ done (D17–D20).

- FluentValidation validators for requests, run explicitly with `ValidateAsync` from the services (no auto-validation).
- Claim: `DamageCost <= 100_000`. "Created within cover period" is checked in `ClaimService` after loading the cover.
- Cover: `StartDate` not before today (today allowed; the date in the business time zone since D30), `EndDate >= StartDate`, `EndDate <= StartDate.AddYears(1).AddDays(-1)`.
- Inject `TimeProvider` (`BusinessTimeProvider` in DI since D30, `FakeTimeProvider` in tests).

**Task 3: async auditing (D4)** ✅ done (D21–D25).

- Bounded `Channel<AuditMessage>` (`FullMode = Wait`), a writer used by the services, and a `BackgroundService` that creates a DI scope per write to get `AuditContext`.
- Use UTC timestamps. Drain the channel on graceful shutdown. Document: queued audits are lost on a crash; outbox or a broker would fix that.

**Task 4: tests**, three commits:

- ✅ 4a: rename to `Claims.IntegrationTests`, fix R9.
- ✅ 4b: service unit tests with hand-written fakes and real validators (D26); no controller unit tests (D27).
- ✅ 4c: integration tests on the real app: one shared `WebApplicationFactory` (collection fixture), `FakeTimeProvider` via `ConfigureTestServices`; cover round trip (201 + `Location` → GET), unknown cover → 400 on `CoverId`, delete flow 409 → 204 → 204 → 404, `GET /Claims` contains the created claim, audit rows appear in SQL Server (poll up to 5 s). Not covered: the shutdown drain. Found and fixed: error responses had the wrong media type (D28).

**Databases out of `Program.cs` (R1, D29)** ✅ done.

- The app only knows two connection strings and starts no containers. Local: `docker-compose.yml` with volumes; connection strings in `appsettings.Development.json`. Integration tests: Testcontainers in `ClaimsApiFactory`.
- Migrations still run at startup; with several instances they would move to the deployment pipeline.

**No backdating (D30, D31)** ✅ done, found while reviewing the solution.

- D30: "today" in the business time zone (`Europe/Oslo`), decided by the server, never by the client.
- D31: a date says which days a cover includes, a moment says from when it is in force. `StartsAt`/`EndsAt` set by the server; claims carry the moment of the damage with a required offset; future claims rejected. The first day is charged in full.

**One project per layer (D33)** ✅ done, after the PR was submitted.

- One commit per project: `Claims.Domain`, `Claims.Application`, `Claims.Infrastructure`, then `Claims.Api`; registration per layer; architecture tests.
- Same namespaces and migration ID, so the code barely changed and existing databases don't re-migrate.

**Finish**

- ✅ README: how to run, how to test, where the databases come from, structure.
- ✅ PR description: where to look first, trade-offs, assumptions, other fixes, what's not in scope.
- ✅ AI artifacts (encouraged by the README): `CLAUDE.md`, `docs/handoff.md`, `docs/findings.md` and `docs/decisions.md` stay in the repo as working notes, not as documentation for reviewers; the README and the PR say so.
