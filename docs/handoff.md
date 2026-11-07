# Handoff: Claims coding task

Context for an AI coding assistant (e.g. Claude Code) picking up this work.
Read this file and `docs/findings.md` before doing anything.

## Who and why

- Candidate: Ivan, senior .NET developer, applying to Insurance Technology Solutions (Norway).
- This repo is their take-home task (`docs/README.md`). AI tools are allowed.
- Target: share the repo and a pull request by **Monday morning**. A technical interview will walk through the solution.
- **Ivan must be able to explain every line and decision.** That matters more than speed.

## How to work, environment

Working rules, code rules and environment notes are in `CLAUDE.md` at the repo root (loaded automatically by Claude Code).

- Branch: `feature/claims-refactor`. Final step is a PR into `main` with a written explanation.
- First run pulls large container images with no progress output (R1).

## Current state

Done (local only, not pushed; see `git log`):

- Audit fields `required` (R2), Task 1 step 1 (folders), working notes and `CLAUDE.md`.
- Small items: R3 (audit database `ClaimsAudit`), R4 (`.gitattributes`), `MongoClient` singleton.
- Task 1 step 2: `ICoverRepository`/`IClaimRepository` in `Application/Repositories`, Mongo implementations in `Infrastructure/Persistence`. Controllers use them; behaviour unchanged (still 204 for unknown ids, still `new Auditer(...)`).
- Task 1 step 5 (done before step 3, D11): `IAuditer` in `Application/Auditing`, `Auditer` registered as scoped. Still synchronous until Task 3.
- Task 1 step 3: `ICoverService`/`IClaimService` and implementations in `Application/Services`; controllers are thin, depend on the interfaces and pass the `CancellationToken`. Premium formula moved unchanged (bugs fixed in Task 5). Behaviour unchanged.
- Task 1 step 4: request/response records in `Application/Contracts`; `DateOnly` for cover dates and `Claim.Created` (D7); Mongo mapping moved to `ClaimsContext` so Domain has no MongoDB dependency (D6). Premium still the template formula.
- Task 1 step 6: 404/204/409/201 as decided (D8, D12–D14), `compute` is GET, no audit for operations that didn't happen, XML docs and `[ProducesResponseType]` on every endpoint (Swagger). **Task 1 is complete.**

Build: 0 compiler warnings. Two non-compiler warnings to fix: R8 (vulnerable transitive package) and R9 (xUnit1051, Task 4).

Notes for the next steps:

- D7–D9 are decided: `DateOnly` for cover dates and `Claim.Created` (step 4), 409 when deleting a cover that still has claims (step 6, uses `IClaimRepository.AnyForCoverAsync`), `compute` becomes GET (step 6). R5 is deferred; Ivan solves it later.

## Remaining plan

Decisions behind every step are in `docs/findings.md` (D1–D14, D5.x). Every step must keep the SOLID rules in `CLAUDE.md`. Do the steps in this order:

**Task 1: layering.** Step numbers are kept as originally planned (other docs refer to them), but the order changed: step 5 was done before step 3 (D11). Execution order:

1. ✅ Step 1: folders `Api`, `Application`, `Domain`, `Infrastructure`.
2. ✅ Step 2: repositories (`ICoverRepository`, `IClaimRepository`); `GetById` queries by id.
3. ✅ Step 5: `IAuditer` in DI, replacing `new Auditer(...)`. Moved forward so the services never depend on `Infrastructure` (D11).
4. ✅ Step 3: `ICoverService`/`IClaimService` with `CoverService`/`ClaimService` in `Application` (D10). Controllers become thin and depend on the interfaces; they pass the request's `CancellationToken`.
5. ✅ Step 4: request/response DTOs, so clients can't set `Id` or `Premium` (R7). `DateOnly` for cover dates and `Claim.Created` (D7). Revisit D6 (Mongo attributes on domain entities).
6. ✅ Step 6: HTTP correctness: 404 when not found, 204 on delete, 404 on delete of an unknown id (and no audit row), 409 when deleting a cover with claims (D8), 201 Created with `Location` on POST (D12), 400 `ValidationProblemDetails` when a claim's cover doesn't exist (D13), outcomes returned as result values, not exceptions (D14), `compute` as GET (D9). Also README Task 1 "documentation is missing": XML doc comments on the public endpoints (Swagger) and `[ProducesResponseType]`.

**Task 5: premium (D5.1–D5.3)**

- Pure calculator in `Domain`, injected or static, used by `CoverService` and the `compute` endpoint.
- Days are calendar dates, end date inclusive. Day rate 1250 × multiplier (Yacht 1.1, PassengerShip 1.2, Tanker 1.5, others 1.3).
- Bands: days 1–30 full rate; days 31–180: Yacht −5%, others −2%; day 181+: Yacht −8%, others −3% (cumulative, off the base rate).
- Write tests from the spec first. Expected Yacht totals for N days: 10 → 13,750.00; 100 → 132,687.50; 200 → 262,487.50 (computed with the template's exclusive day count; with inclusive dates, a cover of N days runs from day D to D+N−1).
- Rates in one place so a different reading of "additional" is a one-line change.

**Task 2: validation (D2, D3, D5.2, D5.4)**

- FluentValidation validators for requests, run explicitly with `ValidateAsync` from the services (no auto-validation).
- Claim: `DamageCost <= 100_000`. "Created within cover period" is checked in `ClaimService` after loading the cover.
- Cover: `StartDate` not before today (UTC date, today allowed), `EndDate >= StartDate`, `EndDate <= StartDate.AddYears(1).AddDays(-1)`.
- Inject `TimeProvider` (`TimeProvider.System` in DI, `FakeTimeProvider` in tests).

**Task 3: async auditing (D4)**

- Bounded `Channel<AuditMessage>` (`FullMode = Wait`), a writer used by the services, and a `BackgroundService` that creates a DI scope per write to get `AuditContext`.
- Use UTC timestamps. Drain the channel on graceful shutdown. Document: queued audits are lost on a crash; outbox or a broker would fix that.

**Task 4: tests**

- Unit tests: premium calculator (`[Theory]` per type and band boundaries 30/31, 180/181, leap year), validators, services with fakes/mocks.
- Keep/repair the integration test; it needs Docker because of `Program.cs`. Assert more than status 200.

**Finish**

- README section: how to run, design decisions and assumptions (copy from `docs/findings.md`).
- PR description: where to look first, trade-offs, assumptions, what Ivan wants to discuss.
- AI artifacts (encouraged by the README): keep `docs/handoff.md` and `docs/findings.md` in the repo and mention them in the PR. Review their wording for a reviewer audience first.
- Hosting: no Azure deployment; demo runs locally. Name the Testcontainers-in-`Program.cs` limitation (R1 long term) as a known trade-off in the PR.
- Mock interview: quiz Ivan on his own solution.

