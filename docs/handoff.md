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

Commits on the branch (local only, not pushed):

1. `1c4fc6b` Make audit fields `required` to match the migration snapshot (fixes EF Core 9+ `PendingModelChangesWarning`).
2. `663f5e0` Task 1 step 1: folders `Api`, `Domain`, `Infrastructure`; namespaces follow folders. No behaviour change.
3. `193ec7d` These working notes.

Step 1 is verified: build passes (5 nullable warnings in `Claim`/`Cover`/`ClaimsContext`), the app starts, the existing test passes (with the R5 workaround), and the endpoints behave as before.

Small open items:

- Make `MongoClient` a singleton instead of creating one per `ClaimsContext`.
- D7–D9 are decided: `DateOnly` for cover dates and `Claim.Created` (step 4), 409 when deleting a cover that still has claims (step 6), `compute` becomes GET (step 6). R5 is deferred; Ivan solves it later.

## Remaining plan

Decisions behind every step are in `docs/findings.md` (D1–D9, D5.x). Do the steps in this order:

**Task 1: layering (continue)** 2. Repository interfaces in `Application`, Mongo implementations in `Infrastructure/Persistence`. `GetById` must query by id, not load the whole collection. 3. `ClaimService` and `CoverService` in `Application`. Controllers become thin: map HTTP to service calls. 4. Request/response DTOs, so clients can't set `Id` or `Premium`. Revisit D6 (Mongo attributes on domain entities) here. 5. Auditing behind an interface (`IAuditer` or similar) registered in DI, replacing `new Auditer(...)`. 6. HTTP correctness: 404 when not found, 204 on delete, 400 with `ProblemDetails` for validation errors; delete returns 404 for unknown ids; claim creation checks that the cover exists.
Also covers README Task 1 "documentation is missing": XML doc comments on the public endpoints (shown in Swagger) and `ProducesResponseType` attributes.

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

