# Claims API

An API for insurance covers and claims: create, read and delete them, with premiums computed per cover and every change audited. The original task is in [docs/README.md](docs/README.md).

## Run

You need the .NET 10 SDK and Docker.

```bash
docker compose up -d --wait
dotnet run --project Claims --launch-profile Claims
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

One project, layered by folders:

| Folder | Holds | Depends on |
|---|---|---|
| `Api` | Controllers: HTTP in, status codes out | `Application` |
| `Application` | Services, request and response contracts, validators, and the interfaces for repositories and auditing | `Domain` |
| `Domain` | `Cover`, `Claim` and `PremiumCalculator` | nothing |
| `Infrastructure` | MongoDB repositories, background audit writing to SQL Server | implements `Application` interfaces |

## Design notes

- [docs/decisions.md](docs/decisions.md) explains each decision, with the options I rejected.
- [docs/findings.md](docs/findings.md) is the short log of problems found and decisions taken.
- `CLAUDE.md` and [docs/handoff.md](docs/handoff.md) are the working notes for the AI assistant I used.
