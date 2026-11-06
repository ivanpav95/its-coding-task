# Findings & decisions log

Working notes for the Claims coding task. Updated as we go.
Status: ✅ fixed · 🔧 planned · ❓ open question

## Found while running the app

| #   | Symptom                                              | Cause                                                                                                                                            | Fix                                                                                                                                        | Status                       |
| --- | ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------- |
| R1  | First run looks stuck, Swagger never opens           | `Program.cs` starts Testcontainers before the app builds; the SQL Server image (~1.5 GB) downloads with no progress output                       | Pre-pull images with `docker pull`. Long term: don't start containers inside `Program.cs`                                                  | ✅ workaround · 🔧 long term |
| R2  | Startup throws `PendingModelChangesWarning`          | Audit entities had `string?` properties, the migration snapshot says `NOT NULL`. EF Core 9+ throws on model/snapshot mismatch instead of warning | Made `ClaimId`/`CoverId` and `HttpRequestType` `required string`: an audit row without them is meaningless, and no new migration is needed | ✅                           |
| R3  | Audit tables are created in `master`                 | Testcontainers' default connection string points at `master`; `Program.cs` passes it straight to EF                                              | Set `InitialCatalog` from config (`SqlServer:DatabaseName`); `Migrate()` creates the database. The key doesn't exist in `appsettings.json` yet: add it | ✅ (database `ClaimsAudit`) |
| R4  | `Auditer.cs` showed as modified without real changes | Editor removed trailing whitespace; line endings differ between Windows and the repo                                                             | Discarded the change; add `.gitattributes` with `* text=auto`                                                                              | ✅                           |
| R5  | `dotnet test` fails with `DirectoryNotFoundException` on this machine | The `'` in the path (`Ivan's PC`) breaks the `Microsoft.AspNetCore.Mvc.Testing` MSBuild target: it writes the content root as an unevaluated `$([System.IO.Path]::GetDirectoryName('...'))` into `MvcTestingAppManifest.json`. Not a code bug; reviewers won't hit it | Locally: set `ASPNETCORE_TEST_CONTENTROOT_CLAIMS` to the `Claims` folder (verified: test passes), or clone to a path without `'` | 🔧 deferred (Ivan solves later); use the env var meanwhile |
| R6  | A cover sent as starting `2026-12-01T00:00:00` comes back as `2026-11-30T23:00:00Z` | `DateTime` with `Kind = Unspecified` is treated as local time (UTC+1) by the Mongo driver and stored as UTC. The calendar date changes, which breaks D5.1 | See D7. Spike: `DateOnly` round-trips exactly with MongoDB.EntityFrameworkCore 10.0.4 (stored as `2026-12-01T00:00:00Z`) and works in LINQ queries | 🔧 D7 (step 4) |
| R7  | `POST /Covers` and `POST /Claims` without `id` return 400 "The Id field is required" | Non-nullable `string Id` + `[ApiController]` makes `Id` required in the request body, so a client must send an id that is then overwritten | Request DTOs without `Id` (step 4)                                                                                                         | 🔧                           |

Checked by running the app (2026-11-06): step 1 (folder move) builds, starts with no `PendingModelChangesWarning`, and Mongo writes work on the standalone Testcontainers server (no transaction/replica-set problem). The API listens on `https://localhost:7052`; plain HTTP on 5052 is redirected (307). An audit row is written for `DELETE` of an unknown id, and audit `Created` is local time (UTC+1).

## Template smells to address

**Layering & SOLID (Task 1)**

- Controllers do data access, auditing, premium calculation and ID generation.
- `ClaimsContext` is defined inside `ClaimsController.cs`. ✅ moved to `Infrastructure/Persistence` (step 1).
- `new Auditer(auditContext)` in constructors: no DI, can't be mocked.
- Entities used as API contracts: a client can send `Id` or `Premium` (needs request/response DTOs).
- Nullable warnings on non-nullable `string` properties.

**HTTP behaviour**

- `GET /{id}` for an unknown id returns 204 No Content (ASP.NET Core turns a `null` result into 204) instead of 404.
- `DELETE` audits before checking the item exists, and always returns 200.
- `GET /Covers/{id}` loads all covers into memory, then filters. ✅ `CoverRepository.GetByIdAsync` filters in Mongo (step 2).
- `POST /Claims` doesn't check that the cover exists.
- `ComputePremiumAsync` is `async` without `await`; a POST that takes query parameters (see D9).
- Deleting a cover leaves its claims pointing at a cover that no longer exists (see D8).

**Infrastructure**

- `Program.cs` creates a new `MongoClient` inside the `AddDbContext` lambda, so once per `ClaimsContext` (per request). `MongoClient` holds the connection pool and should be a singleton. ✅ fixed: registered once as `IMongoClient`.
- `ClaimsContext.Claims` is private with helper methods, `Covers` is public: two styles for the same thing. ✅ `ClaimsContext` is now a plain `DbContext` with two public sets; queries live in the repositories (step 2).
- Template leftovers (harmless): `1stMigration.Designer.cs` only describes `ClaimAudit` although `Up()` creates both tables; `Claims.sln` lists a `README.md` at the root, but it lives in `docs/`.

**Auditing (Task 3)**

- Synchronous `SaveChanges()` inside the request blocks the HTTP pipeline.
- `DateTime.Now` instead of UTC.

**Data**

- `Cover.Type` is stored as BSON field `"claimType"`; renaming it would break existing documents.

**Testing (Task 4)**

- The only test is an integration test that silently needs Docker (via `Program.cs`).

**Premium (Task 5)**

- P1: the `if`s don't exclude each other, so one day is added up to 3 times (days 0–29: three times, days 30–179: twice). Should be one rate per day, by band.
- P2: the loop stops at `i < 365`, so a 366th day (leap year, allowed by D5.2) would be free.
- P3: day count uses `(end - start).TotalDays` (a double, end exclusive, includes time of day); must follow D5.1 (calendar dates, end inclusive).
- Readability: `if` chain for the type multiplier, magic numbers, loop over days instead of computing each band.
- Yacht, end date exclusive as in the template: 10 days → template 39,462.50, correct 13,750.00; 100 days → 298,375.00 vs 132,687.50; 200 days → 529,375.00 vs 262,487.50. The 39,462.50 was confirmed against the running `compute` endpoint.
- No rounding needed: every day rate has at most 2 decimals (Yacht 1375 / 1306.25 / 1265; PassengerShip 1500 / 1470 / 1455; Tanker 1875 / 1837.50 / 1818.75; others 1625 / 1592.50 / 1576.25), so totals are exact in `decimal`.

## Decisions

| #    | Question                                                          | Decision & why                                                                                                                                                                                                                                                                                                                                                                                              | Status                      |
| ---- | ----------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------- |
| D1   | Project structure                                                 | One API project, layered by folders (`Api`, `Application`, `Domain`, `Infrastructure`). Two entities don't justify several projects; no MediatR/CQRS. Dependency direction kept by convention. Premium calculator lives in **Domain** (pure business rule); Application use cases call it                                                                                                                   | ✅                          |
| D2   | Validation                                                        | FluentValidation for rules on the request itself (DamageCost, cover dates, required fields), run with `ValidateAsync` from the services. The "claim date within cover period" rule lives in `ClaimService`: it loads the cover once (also needed for "cover not found") and checks the dates                                                                                                                | ✅                          |
| D3   | Testing "StartDate not in the past"                               | Inject `TimeProvider` (.NET 8+); `FakeTimeProvider` in tests, so tests don't depend on the real date                                                                                                                                                                                                                                                                                                        | ✅                          |
| D4   | Async auditing                                                    | In-memory bounded `Channel<T>` (`FullMode = Wait`) + `BackgroundService`. The channel holds plain audit messages, never a `DbContext`; the worker creates its own scope per write. Trade-off: messages still queued are lost if the process crashes; next step would be the outbox pattern or a durable broker (RabbitMQ / Service Bus)                                                                     | ✅                          |
| D5.1 | End date inclusive?                                               | Yes. Dates are calendar dates (time ignored); a cover from 1 Jan to 1 Jan is 1 day. Matches how insurance periods are usually read                                                                                                                                                                                                                                                                          | ✅                          |
| D5.2 | "Cannot exceed 1 year"                                            | `EndDate <= StartDate.AddYears(1).AddDays(-1)` (inclusive end): a real calendar year, so leap years are handled without special cases                                                                                                                                                                                                                                                                       | ✅                          |
| D5.3 | Discounts                                                         | Additive, off the base day rate: Yacht 5% then 8%, others 2% then 3%. "Additional" read as added to the previous discount; the template's own constants (0.08, 0.03) point the same way. Rejected: compounded (0.95 × 0.97, about 2 per day more expensive for a Yacht from day 181) and a separate 3% / 1% for the last band only (contradicts "additional"). Rates kept in one place so a different reading is a one-line change | ✅                          |
| D5.4 | StartDate today allowed?                                          | Yes. The spec only forbids the past. Compared as dates in UTC; known limitation around midnight in Norwegian time                                                                                                                                                                                                                                                                                           | ✅                          |
| D6   | Mongo attributes (`[BsonId]`, `[BsonElement]`) on domain entities | Kept for now: pragmatic, no behaviour change in the move. Alternative: map element names in `ClaimsContext.OnModelCreating` so Domain has no MongoDB dependency                                                                                                                                                                                                                                             | 🔧 revisit in step 4 (DTOs) |
| D7   | `DateTime` or `DateOnly` for cover dates (R6)                      | Rule: `DateOnly` wherever the time of day isn't needed. So `Cover.StartDate`/`EndDate` and `Claim.Created` become `DateOnly`: no business rule uses a time, the type says "calendar date", it fixes the one-day shift (R6), and D5.1/D5.2 become simple (`end.DayNumber - start.DayNumber + 1`). Audit `Created` stays `DateTime` (UTC): there the time is the point. Cost: the API contract changes (`"2026-12-01"` instead of a timestamp); existing documents would need migrating, which doesn't matter for this task (Testcontainers starts empty) | ✅ (step 4)                 |
| D8   | Deleting a cover that has claims                                  | Not allowed: `DELETE /Covers/{id}` returns 409 Conflict with `ProblemDetails` while the cover still has claims; delete the claims first. Keeps data consistent without silently deleting claims. Rejected: deleting the claims too (destroys claim history as a side effect) and allowing orphans (today's behaviour). Needs a repository method like "does this cover have claims", which queries by `coverId` | ✅ (step 6)                 |
| D9   | `POST /Covers/compute` with query parameters                      | Make it `GET /Covers/compute?startDate=…&endDate=…&coverType=…`: it changes nothing and is safe to repeat, so GET is the right verb. Uses the same calculator as `CoverService`                                                                                                                                                                                                                             | ✅ (step 6)                 |

**Packages checked (2026-11-06, spike outside the repo):** `FluentValidation` 12.2.0 and `Microsoft.Extensions.TimeProvider.Testing` 10.11.0 restore and work on net10.0. The test project runs on xunit.v3 with Microsoft.Testing.Platform (`global.json`).

