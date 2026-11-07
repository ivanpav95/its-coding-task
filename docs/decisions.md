# Technical decisions

Why the code is built the way it is. One section per decision: the problem, what we chose, how it works technically, and what we gave up.
IDs match `docs/findings.md`, which is the short log. This file is the long explanation.

Status: ✅ done · 🔧 decided, not built yet

---

## 1. Audit fields are `required string` (R2) ✅

**Problem.** The app crashed at startup with `PendingModelChangesWarning`.

**Why it happened.** EF Core 9 and later compare the current model with the migration snapshot when `Migrate()` runs, and throw if they differ. The snapshot says `ClaimId`, `CoverId` and `HttpRequestType` are `NOT NULL`. The entities declared them as `string?`, which EF reads as "nullable column". Model and snapshot disagreed.

**Decision.** Change the properties to `required string`. The code now matches the schema that already exists, so no new migration is needed.

**Technical detail.** With nullable reference types enabled, plain `string` would already be enough for EF to make the column non-nullable. `required` adds a compile-time check that every object initializer sets the property, so an audit row without an id cannot even be constructed.

**Rejected.** A migration that makes the columns nullable. The schema was right and the code was wrong; an audit row without an id or a verb is meaningless.

---

## 2. One project, layered by folders (D1) ✅ folders · 🔧 contents

**Problem.** Controllers did data access, auditing, premium calculation and id generation. Nothing could be tested without HTTP and two databases.

**Decision.** Four folders in the one API project:

| Folder           | Contains                                                                                |
| ---------------- | --------------------------------------------------------------------------------------- |
| `Api`            | Controllers, request/response DTOs. Maps HTTP to service calls and back.                 |
| `Application`    | `ClaimService`, `CoverService`, repository interfaces, `IAuditer`, validators.          |
| `Domain`         | `Claim`, `Cover`, enums, premium calculator.                                            |
| `Infrastructure` | Mongo repositories (EF Core provider), SQL Server audit context, audit background worker. |

Dependencies point inward: `Api` → `Application` → `Domain`; `Infrastructure` implements interfaces from `Application`.

**Why folders, not projects.** Separate projects enforce the dependency direction at compile time. That's their real benefit. For two entities and one deployable it costs more than it gives: more `.csproj` files, more references, slower navigation. We keep the direction by convention. If the codebase grew, an architecture test (e.g. NetArchTest) or a project split would enforce it.

**Why the premium calculator is in Domain.** It's a pure business rule: same input, same price, no I/O. Application orchestrates (load, call the rule, save, audit); Domain holds the rules.

**Not used, on purpose.** MediatR (a request + handler + indirection per endpoint, for five endpoints), AutoMapper (see §4), a generic repository (see §3). The README asks for "less code, not more".

**SOLID is a hard rule, not a goal.** The README says "Adhere to the SOLID principles", so every step must keep all five; `CLAUDE.md` lists what each letter means in this codebase. In practice:

| Principle | Where it shows                                                                                                   |
| --------- | ---------------------------------------------------------------------------------------------------------------- |
| S         | Controllers: HTTP only. Services: use cases. Calculator and validators: rules. Repositories and auditer: I/O.    |
| O         | Premium rates in one table; a new cover type or rate is a data change, not a new `if`.                          |
| L         | Fakes in tests and real implementations honour the same contracts (e.g. `DeleteAsync` → `false` when unknown). |
| I         | One small interface per need: `ICoverRepository`, `IClaimRepository`, `IAuditer`, `ICoverService`, `IClaimService`. |
| D         | Every layer boundary is an interface resolved from DI (D10); no `new` of dependencies; no `Infrastructure` types in `Application`/`Domain` in any commit (D11). |

---

## 3. Small, specific repository interfaces ✅

**Problem.** Services need data access, but unit tests must run without Mongo.

**Decision.** `ICoverRepository` and `IClaimRepository` in `Application`, implemented in `Infrastructure/Persistence` with the existing EF Core Mongo provider. They expose only the operations the use cases need, e.g. `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `DeleteAsync`, and "does this cover have any claims" for §7.

**Why not use `DbContext` directly in services?** `DbContext` already is a unit of work and a set of repositories, so that would be defensible. We add the interfaces because they:
- give a seam to replace data access with in-memory fakes in service tests;
- keep Mongo-specific query details out of the services;
- name the operations the domain actually needs.

**Why not a generic `IRepository<T>`?** It exposes operations nobody needs (e.g. update-anything) and hides the real ones behind `Find(predicate)`. Two small interfaces are clearer.

**Bug fixed along the way.** `GET /Covers/{id}` loaded every cover into memory and filtered with `SingleOrDefault`. `GetByIdAsync` queries by id in the database.

---

## 3a. Services behind interfaces (D10) ✅

**Decision.** `CoverService` and `ClaimService` in `Application/Services` implement `ICoverService` and `IClaimService`. Controllers depend on the interfaces.

**Why.**
- Controllers can be unit-tested with a fake service, without repositories or databases behind them.
- Every boundary between layers is an interface, so the dependency inversion rule reads the same everywhere: Api → `I…Service`, Application → `I…Repository` / `IAuditer`.

**Trade-off.** Two interfaces with exactly one implementation each. The alternative was concrete services: their own dependencies are already interfaces, so the services themselves would still be testable. We chose consistency and controller testability over two fewer files.

## 3b. `IAuditer` before the services (D11) ✅

`IAuditer` (plan step 5) is built before the services (step 3). Built in the planned order, the services would have created `new Auditer(auditContext)` themselves, so `Application` would depend on an `Infrastructure` type for one commit. Swapping the order keeps D1 intact in every commit.

---

## 4. Request/response DTOs (Task 1 step 4, R7) ✅

**Problem.** The domain entities are the API contract.
- A client can send `Id` and `Premium`. They are overwritten, but Swagger shows them as inputs.
- Verified by running the app (R7): a POST **without** `id` returns 400 "The Id field is required". `[ApiController]` treats a non-nullable `string Id` as a required field, so clients must send a dummy id.

**Decision.** Records for the contract, e.g. `CreateCoverRequest(StartDate, EndDate, Type)` and `CoverResponse(Id, StartDate, EndDate, Type, Premium)`. The server sets the id and computes the premium.

**Why records.** Immutable data carriers with value equality and short syntax. A request is data, not behaviour.

**Why hand-written mapping, not AutoMapper.** It's a handful of properties. Hand-written mapping is explicit, checked by the compiler and searchable. AutoMapper turns mapping mistakes into runtime errors and hides where a value comes from.

**Where they live: `Application/Contracts`, not `Api`.** The services take requests and return responses, so the contract types must be visible to `Application`; if they lived in `Api`, `Application` would depend on `Api`. It also lets the FluentValidation validators (§12) validate the requests inside the services.

**How mapping works.**
- Entity → response: a static `From` on each response record (`CoverResponse.From(cover)`), one line each.
- Request → entity: in the service's `CreateAsync`, because that's where the server-owned values (`Id`, `Premium`) are set.

**Domain entities after the change.** Strings are `required` and properties are `init`: an entity can't be constructed without its id, and nothing changes it after creation. This removed the last 4 nullable warnings (CS8618): they existed because model binding used to fill the entities.

**Verified on the running app.** POST without `id` works (R7 fixed); an `id` or `premium` sent by a client is ignored; a missing `name` gives 400 from `[ApiController]` model validation; Swagger's `CreateCoverRequest` shows only `startDate`, `endDate` (format `date`) and `type`.

---

## 5. Mongo mapping lives in Infrastructure (D6) ✅

**Problem.** `[BsonId]` and `[BsonElement]` on `Claim` and `Cover` made Domain depend on the MongoDB package.

**Decision.** Domain has no MongoDB dependency. The element names are configured in `ClaimsContext.OnModelCreating` with `HasElementName(...)`; `[BsonId]` isn't needed because EF maps a property named `Id` to `_id` by convention.

**Why.**
- Domain is the business model; how it's stored is an infrastructure detail. Dependencies point inward (D1), so Domain depends on nothing.
- The storage can change (SQL Server, Cosmos DB) without touching the domain classes.
- Domain tests (e.g. the premium calculator) don't pull in a database package.

**Trade-off.** A reader sees the storage names in `ClaimsContext` instead of on the entity. Mapping in one place for the whole model is how EF is normally used anyway.

**Verified.** Raw documents were dumped from Mongo before and after the change: same element names (`_id`, `coverId`, `created`, `name`, `claimType`, `damageCost`, `startDate`, `endDate`, `premium`), same BSON types (`ISODate`, `Decimal128`, enums as numbers), same values. `Cover.Type` keeps the template's BSON name `"claimType"`: renaming it would make existing documents unreadable.

---

## 6. HTTP status codes (Task 1 step 6) ✅

Measured on the running template, and what each request will return:

| Request                    | Today                               | After                         |
| -------------------------- | ----------------------------------- | ----------------------------- |
| GET unknown id             | 204 No Content                      | 404 Not Found                 |
| DELETE unknown id          | 200, and an audit row is written    | 404, no audit                 |
| DELETE existing            | 200                                 | 204 No Content                |
| DELETE cover with claims   | 200, claims left orphaned           | 409 Conflict (§7)             |
| POST claim, unknown cover  | saved anyway                        | rejected                      |
| Invalid input              | accepted                            | 400 `ValidationProblemDetails` |
| `POST /Covers/compute?...` | POST with query parameters          | GET (§8)                      |

**Why a GET for an unknown id returned 204.** ASP.NET Core's built-in `HttpNoContentOutputFormatter` turns a `null` return value into 204. A client can't tell "not found" from "nothing to return", so the endpoints return `NotFound()` explicitly.

**Why 404 on DELETE of an unknown id.** DELETE is idempotent in its *effect* (the resource is gone either way), not in its response code. 404 tells a client that it used a wrong id. Returning 204 would also be valid; we chose to surface the mistake.

**Documentation.** XML doc comments on endpoints plus `[ProducesResponseType]`, so Swagger lists every status code. This covers Task 1's "documentation is missing".

### 6a. POST returns 201 Created (D12)

`CreatedAtAction(nameof(GetById), new { id }, response)` returns 201, the created resource in the body, and a `Location` header pointing at `GET /Covers/{id}`. That's what HTTP defines for "a new resource was created", and a client can follow `Location` without building the URL itself. Cost: one line per POST.

### 6b. A claim for an unknown cover is a 400 (D13)

The request goes to `/Claims`, which exists. What's wrong is a value in the body (`coverId`), so it's a client input error: **400** with `ValidationProblemDetails` and the message on the `coverId` field. That's the same status and JSON shape as the Task 2 validation errors, so a client handles one kind of "your input is wrong".

- Rejected: 404. It means "the resource at this URL doesn't exist", which would be misleading for `POST /Claims`.
- Rejected: 422 Unprocessable Content. Defensible for "well-formed but semantically wrong", but it would give clients two status codes for invalid input.

### 6c. Services return result values, not exceptions (D14)

Some operations have more than one expected outcome:

| Operation         | Outcomes                        | Returned as                                  | HTTP          |
| ----------------- | ------------------------------- | -------------------------------------------- | ------------- |
| Delete a cover    | deleted, not found, has claims  | `DeleteCoverResult` enum                     | 204, 404, 409 |
| Delete a claim    | deleted, not found              | `bool`                                       | 204, 404      |
| Create a claim    | created, cover doesn't exist    | `CreateClaimResult` (`Created` / `CoverNotFound`) | 201, 400 |

The controller maps each outcome with a `switch`, so every status code is visible in one place.

**Why not exceptions.** "Not found" and "has claims" are normal business outcomes, not failures. Exceptions for them hide the control flow (a reader can't see from the service signature what can come back), and they're slow. Exceptions stay for real failures, e.g. the database being down.

**Rejected.** Custom exceptions (`NotFoundException`, `ConflictException`) mapped to status codes by one `IExceptionHandler`. Less code in controllers, but the flow becomes implicit.

**Why not a generic `Result<T>`.** One small type per operation names its real outcomes; a generic result type is a mini-framework for three call sites.

---

## 7. A cover with claims cannot be deleted (D8) ✅

**Problem.** Deleting a cover left its claims pointing at a cover that no longer exists.

**Decision.** `DELETE /Covers/{id}` returns **409 Conflict** with `ProblemDetails` while the cover still has claims. Delete the claims first.

**Why 409.** The request itself is valid, but it conflicts with the current state of the resource. That's the definition of 409. (422 is for a request whose content is invalid on its own.)

**Rejected.**
- Cascading delete: deleting claim history as a side effect of deleting a cover is dangerous.
- Allowing orphans: today's behaviour, inconsistent data.

**Known limitation.** "Check for claims, then delete" is two operations. Without a Mongo transaction (which needs a replica set), a claim created between them can slip through. Accepted for this task. Fixes: a transaction, or a status on the cover that claim creation checks.

---

## 8. Premium preview is a GET (D9) ✅

**Decision.** `GET /Covers/compute?startDate=…&endDate=…&coverType=…` instead of POST.

**Why.** It changes nothing on the server and is safe to repeat. Those are the properties of GET, and they make the call cacheable and bookmarkable. It uses the same calculator as `CoverService`, so the preview and the stored premium can never differ.

---

## 9. `DateOnly` where the time isn't needed (D7, R6) ✅

**Problem found while running (R6).** A cover sent as starting `2026-12-01T00:00:00` came back as `2026-11-30T23:00:00Z`.

**Why it happens.** JSON without an offset deserializes to a `DateTime` with `Kind = Unspecified`. The Mongo driver treats `Unspecified` as local time (UTC+1 on this machine) and converts it to UTC before storing. Midnight local is 23:00 the day before in UTC, so the **calendar date changes**. Premium and validation count calendar days, so this is a real bug, not just formatting.

**Decision.** `Cover.StartDate`, `Cover.EndDate` and `Claim.Created` become `DateOnly`. No business rule needs a time of day on them. Audit `Created` stays a UTC `DateTime`, because there the time is the point.

**Why `DateOnly` and not "always send UTC".** UTC would work only if every client always sent `Z` or an offset, and the type wouldn't say so. `DateOnly` makes the wrong state impossible to express, and day maths becomes simple: `end.DayNumber - start.DayNumber + 1`.

**Verified with a spike** (MongoDB.EntityFrameworkCore 10.0.4): `DateOnly` is stored as midnight UTC, read back exactly, and translated in LINQ queries.

**Cost.** The API contract changes (`"2026-12-01"` instead of a timestamp). Existing documents would need migrating; there are none, because Testcontainers starts empty.

---

## 10. Date rules (D5.1, D5.2, D5.4) 🔧

**End date inclusive (D5.1).** A cover from 1 Jan to 1 Jan is one day. That's how insurance periods are usually read. Days = `end.DayNumber - start.DayNumber + 1`.

**At most one year (D5.2).** `EndDate <= StartDate.AddYears(1).AddDays(-1)`. Using the calendar instead of "365 days" handles leap years without a special case: a cover starting 2027-03-01 may end 2028-02-29, which is 366 days.

**Start date today is allowed (D5.4).** The spec only forbids the past. "Today" is the UTC date: `DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)`.

**Known limitation.** Between local midnight and 01:00 (02:00 in summer) in Norway, the UTC date is still yesterday. During that hour a Norwegian user could create a cover starting "yesterday" by their own calendar. The rule is lenient, never strict: it never rejects a valid local today. Fix if needed: a business time zone (`Europe/Oslo`) in config.

---

## 11. Premium calculation (Task 5, D5.3) 🔧

**Bugs in the template.**
- **P1.** The `if`s don't exclude each other, so one day is charged up to three times (days 1–30 three times, days 31–180 twice).
- **P2.** The loop stops at `i < 365`, so day 366 of a leap-year cover is free.
- **P3.** The day count is `(end - start).TotalDays`: a `double`, end-exclusive, and affected by the time of day.

**Rule.** Day rate = 1250 × multiplier (Yacht 1.1, PassengerShip 1.2, Tanker 1.5, others 1.3). Discounts apply to the base day rate:

| Type             | Days 1–30 | Days 31–180     | Day 181+        |
| ---------------- | --------: | --------------: | --------------: |
| Yacht            | 1,375.00  | 1,306.25 (−5%)  | 1,265.00 (−8%)  |
| PassengerShip    | 1,500.00  | 1,470.00 (−2%)  | 1,455.00 (−3%)  |
| Tanker           | 1,875.00  | 1,837.50 (−2%)  | 1,818.75 (−3%)  |
| Container / Bulk | 1,625.00  | 1,592.50 (−2%)  | 1,576.25 (−3%)  |

**"Additional" discount (D5.3).** Read as added to the previous discount: Yacht 5% + 3% = 8%, others 2% + 1% = 3%. The template's own constants (0.08, 0.03) point the same way.
- Rejected: compounding (0.95 × 0.97). For a Yacht that's 1,267.06 per day from day 181, about 2 per day more expensive.
- Rejected: 3% / 1% alone for the last band. That contradicts "additional".
- The rates live in one table, so a different reading is a one-line change.

**Computed per band, not per day.**

```csharp
var days  = end.DayNumber - start.DayNumber + 1;
var band1 = Math.Min(days, 30);
var band2 = Math.Clamp(days - 30, 0, 150);
var band3 = Math.Max(days - 180, 0);
return rate * (band1 + band2 * (1 - d2) + band3 * (1 - d3));
```

Example, Yacht, 200 days: 30 × 1,375 + 150 × 1,306.25 + 20 × 1,265 = **262,487.50**. The template charges 529,375.00.

**No rounding needed.** Every day rate has at most two decimals and the band counts are integers, so `decimal` totals are exact.

**Static or injected.** The calculator has no dependencies, so a static pure function is honest and easy to test. It would become an injected service only if rates came from configuration or a database.

---

## 12. Validation with FluentValidation, called explicitly (D2) 🔧

**Rules.**
- Claim: `DamageCost <= 100_000`; created date within the related cover's period (inclusive).
- Cover: `StartDate` not before today (§10); `EndDate >= StartDate`; at most one year (§10).

**Decision.** FluentValidation validators for the request DTOs, run with `ValidateAsync` from the services. No MVC auto-validation.

**Why FluentValidation over DataAnnotations.** These rules span several properties and depend on the clock. Annotations handle single-property checks well and cross-property rules badly. FluentValidation expresses them clearly and tests them without HTTP.

**Why explicit calls.** FluentValidation's author no longer recommends the ASP.NET auto-validation integration. It runs inside MVC's synchronous validation pipeline, so async rules can't run there, and it hides when validation happens. Calling `ValidateAsync` in the service makes validation part of the use case, which also applies if the service is called from somewhere other than a controller.

**Why "within the cover period" is in `ClaimService`, not in the validator.** It needs the cover from the database. I/O inside a validator hides a database call where nobody expects it. The service loads the cover anyway to answer "cover not found": one load, both checks.

**Open detail for step 6.** How a failure becomes a 400: a result object the controller maps to `ValidationProblem(...)`, or a `ValidationException` mapped by an `IExceptionHandler`. Both produce the same `ValidationProblemDetails` shape as ASP.NET's own model-binding errors.

---

## 13. `TimeProvider` for "today" (D3) 🔧

**Problem.** "Start date not in the past" depends on the current date. With `DateTime.UtcNow` a test that passes today fails tomorrow.

**Decision.** Inject `TimeProvider`, the built-in abstraction since .NET 8. Production registers `TimeProvider.System`; tests use `FakeTimeProvider` (package `Microsoft.Extensions.TimeProvider.Testing`, 10.11.0 checked on net10.0) with a fixed date.

---

## 14. Audit writes off the request path (D4, Task 3) 🔧

**Problem.** Every POST and DELETE calls `Auditer`, which runs a synchronous `SaveChanges()` against SQL Server inside the request. The request waits for the audit insert. The template also uses `DateTime.Now` (local time: verified rows are UTC+1) and audits a DELETE before it knows the item exists.

**Decision.** A bounded in-memory `Channel<AuditMessage>` plus a `BackgroundService`:

```csharp
// registration
Channel.CreateBounded<AuditMessage>(new BoundedChannelOptions(capacity)
    { FullMode = BoundedChannelFullMode.Wait });

// AuditWorker : BackgroundService
await foreach (var message in reader.ReadAllAsync(ct))
{
    await using var scope = scopeFactory.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AuditContext>();
    db.Add(message.ToEntity());
    await db.SaveChangesAsync(ct);
}
```

**How it works.**
- The service writes a plain message (id, verb, UTC timestamp) to the channel after the main operation succeeded. The request continues immediately.
- The channel holds messages, never a `DbContext`.
- The worker creates a new DI scope per write. `BackgroundService` is a singleton, and `AuditContext` is scoped and not thread-safe. Injecting it directly would be a captive dependency.
- On graceful shutdown the worker writes what's still queued before it exits.
- If an insert fails, the worker logs it with the ids and continues. One bad write must not stop auditing.

**Why bounded with `FullMode.Wait`.** Unbounded can grow until memory runs out if SQL Server is slow or down. Dropping messages loses audit records silently, which is the worst outcome for an audit trail. `Wait` applies backpressure: requests slow down instead of losing data.

**Why not `async`/`await` or `Task.Run`.**
- `await SaveChangesAsync()` frees the thread, but the request still waits for the insert, so latency doesn't improve.
- `Task.Run` fire-and-forget would use the request's scoped `DbContext` after the request ended (disposed object, or two threads on one context), swallow exceptions, and has no backpressure.

**Accepted trade-off.** Messages still in the channel are lost if the process crashes. The durable next step is the outbox pattern (store the audit in the same transaction as the data, publish it later) or a broker such as Azure Service Bus or RabbitMQ. `IAuditer` means that swap doesn't touch the services.

---

## 15. `MongoClient` is a singleton ✅

**Problem.** `Program.cs` calls `new MongoClient(...)` inside the `AddDbContext` options lambda. `AddDbContext` registers the context and its options as scoped, so the lambda runs for every new scope: once per HTTP request.

**What a `MongoClient` is.** Not a connection, but the entry point to the driver's cluster machinery:
- a connection pool per server, so requests reuse open TCP/TLS connections;
- server monitoring: background heartbeats that track which servers are up and which is primary;
- the parsed settings: connection string, timeouts, serializers.

It's thread-safe, and MongoDB's documentation says to keep one instance per application (static or a DI singleton).

**What the template actually costs.** Less than you might think. The .NET driver keeps an internal registry of clusters keyed by their settings, so a second `new MongoClient` with the same connection string reuses the existing cluster and pool. No new connections per request. What's left per request: parsing the connection string, building settings, a registry lookup, and a client object that nobody owns or disposes.

**Why change it anyway.**
- It's the documented usage.
- The template is only cheap because of that registry, a hidden driver detail. If the settings ever differed per request (e.g. a callback configured in the lambda), every request would get a new pool.
- As a DI singleton the container owns the client and disposes it on shutdown.

**How.**

```csharp
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoContainer.GetConnectionString()));

builder.Services.AddDbContext<ClaimsContext>((services, options) =>
    options.UseMongoDB(services.GetRequiredService<IMongoClient>(), mongoDatabaseName));
```

- The singleton is registered with a **factory**, not `AddSingleton(new MongoClient(...))`. The container disposes only objects it created itself, so with a factory it disposes the client on shutdown (`MongoClient` is `IDisposable` since driver 3.0; checked on 3.11.2).
- The `(services, options)` overload of `AddDbContext` gets the shared client from DI.
- The database name is read once with `GetRequiredSection`, which fails at startup with a clear message if the setting is missing.
- This also removes the template's round trip `client.GetDatabase(...)` → `database.Client`.

---

## 16. Audit database name from configuration (R3) ✅

**Problem.** Verified in the container: the audit tables are in `master`. Testcontainers' connection string has no database, so SQL Server's default (`master`) is used.

**Decision.** Add `SqlServer:DatabaseName` to `appsettings.json` and set it on the connection string:

```csharp
var sql = new SqlConnectionStringBuilder(sqlContainer.GetConnectionString())
{
    InitialCatalog = builder.Configuration["SqlServer:DatabaseName"]
};
```

`Migrate()` creates the database if it doesn't exist. `SqlConnectionStringBuilder` instead of string concatenation, because it handles escaping and keys that are already present.

---

## 17. Testcontainers stay in `Program.cs` (R1) — accepted trade-off

**Problem.** Every app start launches SQL Server and MongoDB containers. The first run silently pulls a ~1.5 GB image and looks stuck.

**Decision.** Keep it. The template chose it so the app runs with only Docker installed, and changing hosting is outside the tasks.

**What we'd do in production.** Connection strings from configuration (Key Vault in Azure, user secrets locally), containers via docker-compose or .NET Aspire for local development, Testcontainers only in test fixtures, and migrations run by the deployment pipeline instead of at app start.

---

## 18. Line endings (R4) ✅

**Problem.** Files showed as modified with no real change: the editor and Git disagreed on CRLF vs LF.

**Decision.** `.gitattributes` with `* text=auto`. Git stores LF in the repository and converts to the platform's line endings on checkout, so diffs show only real changes.

---

## 19. Tests fail on a path with an apostrophe (R5) — deferred

**Problem.** On this machine `dotnet test` fails with `DirectoryNotFoundException`.

**Why.** The `Microsoft.AspNetCore.Mvc.Testing` MSBuild target writes the app's content root using `$([System.IO.Path]::GetDirectoryName('<path>'))`. The `'` in `Ivan's PC` ends the quoted string early, so the expression is written to `MvcTestingAppManifest.json` unevaluated, and `WebApplicationFactory` can't find the folder.

**Status.** Not a code bug; reviewers won't hit it. Until it's fixed, set `ASPNETCORE_TEST_CONTENTROOT_CLAIMS` to the `Claims` folder (verified: the test passes), or clone to a path without `'`.

---

## 20. Vulnerable transitive package pinned (R8) ✅

**Problem.** `dotnet build` warned NU1902/NU1903: `System.Security.Cryptography.Pkcs` 9.0.11 has a known moderate and a known high severity vulnerability. It isn't referenced directly; `dotnet nuget why` shows the path `Microsoft.EntityFrameworkCore.SqlServer` 10.0.14 → `Microsoft.Data.SqlClient` 6.1.7 → `System.Security.Cryptography.Pkcs` 9.0.11.

**Decision.** A direct `PackageReference` to `System.Security.Cryptography.Pkcs` 10.0.14 in `Claims.csproj`. NuGet resolves the highest requested version for the whole graph, so SqlClient gets the patched library; the test project inherits it through its project reference.

**Why this and not upgrading SqlClient.** The pin changes only the vulnerable library, to the same 10.0.14 servicing version as the other Microsoft packages. Upgrading SqlClient to 7.x would be a major-version change underneath EF Core 10.0.14, which EF wasn't released against. When EF Core ships with a fixed SqlClient, the pin can be removed.

**Verified.** `dotnet list package --vulnerable --include-transitive` reports nothing for either project; the app still writes audit rows through SqlClient.
