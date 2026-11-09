# Claims API

An API for insurance covers and claims: create, read and delete them, with premiums computed per cover and every change audited. The original task is in [docs/README.md](docs/README.md).

## Run

You need the .NET 10 SDK and Docker.

```bash
docker compose up -d --wait
dotnet run --project Claims.Api --launch-profile Claims.Api
```

Swagger is at https://localhost:7052/swagger. The databases keep their data in Docker volumes:
- `docker compose stop` stops them and keeps the data;
- `docker compose down -v` removes them together with the data.

The first run downloads the SQL Server image (about 1.5 GB) and can take a few minutes.

## Test

```bash
dotnet test --project Claims.UnitTests   # no Docker needed, about 1 second
dotnet test                              # also runs the integration tests
```

The integration tests start the real app on their own SQL Server and MongoDB containers (Testcontainers), so they need Docker but not the compose databases.

## Where the databases come from

The app only knows two connection strings, `ConnectionStrings:Audit` (SQL Server) and `ConnectionStrings:Claims` (MongoDB).

| Where | Databases |
|---|---|
| Unit tests | None: hand-written in-memory fakes |
| Integration tests | Testcontainers: throwaway containers per test run |
| Local development | Docker Compose with persistent volumes; connection strings in `appsettings.Development.json` |
| Production (not built yet) | Managed services, such as Azure SQL and Cosmos DB for MongoDB; connection strings from configuration |

## Structure

One project per layer. Project references enforce the dependency direction, and architecture tests in `Claims.UnitTests/Architecture` check it too.

| Project | Holds | References |
|---|---|---|
| `Claims.Api` | Controllers (HTTP in, status codes out) and `Program.cs`, which composes the layers | `Claims.Application`, and `Claims.Infrastructure` only to register it |
| `Claims.Application` | Services, request and response contracts, validators, and the interfaces for repositories and auditing | `Claims.Domain` |
| `Claims.Domain` | `Cover`, `Claim`, `PremiumCalculator` and `CoverPeriod` | nothing |
| `Claims.Infrastructure` | MongoDB repositories, background audit writing to SQL Server, the business clock | `Claims.Application`, whose interfaces it implements |

Each layer registers its own types (`AddApplication()`, `AddInfrastructure()`).

## AI working notes

`CLAUDE.md`, `docs/handoff.md`, `docs/findings.md` and `docs/decisions.md` are the notes I kept while working with an AI assistant: its instructions, the plan, a log of findings and decisions, and the reasoning behind them. They were written for my own work, not as documentation of the solution. The task invites AI artifacts, so they stay in the repo.
