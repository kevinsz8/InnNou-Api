# InnNou-Api tests

## Strategy

`InnNou.IntegrationTests` runs against the real MediatR pipeline (`IMediator.Send(...)`) and a real
SQL Server database, not mocks. Given this codebase has no repository pattern (services use
`IDbConnectionFactory` + Dapper directly) and real logic lives in stored procedures, a mocked unit
test would prove nothing about the SQL/mapping boundary where the actual bugs happen. The test DI
container is built with the exact same `AddApplication()` + `AddInfrastructure()` calls `Program.cs`
uses, so it gets real handlers, real mapper registrations, and real pipeline behaviors for free -
just pointed at a separate `InnNou_Test` database instead of dev.

Each test runs inside a `TransactionScope` that is never completed, so every Dapper connection
opened anywhere during the test (any service, any depth) auto-enlists and rolls back on dispose.
Nothing a test creates ever persists - no cleanup code needed, and tests can run in any order.

**The whole assembly runs sequentially** (`AssemblyInfo.cs`, `DisableTestParallelization = true`).
`TransactionScope`'s default isolation level is Serializable, and enough tests share the same
seeded rows (e.g. the one `ASSOCIATE` organization every test's fixtures hang off) that xUnit's
default cross-class parallelism reliably deadlocked unrelated tests on shared locks. If a future
test module is provably independent of shared state, it's fine to scope parallelism back on for
just that collection - but the default here is off for a reason, not an oversight.

Test data is built by calling the app's own `Create*` commands via `IMediator.Send()`
(`TestFixtures/TestDataBuilder.cs`), not raw SQL inserts, so every test run re-verifies those
create paths too. Stable reference/lookup data (Families' tax categories, `TaxCategories`,
`TaxJurisdictions`, `UnitsOfMeasure`, an `ASSOCIATE` organization) is looked up dynamically by
`Code` rather than hardcoded, so tests stay valid across a DB rebuild.

## One-time setup

1. Have a local `InnNou` dev database (the normal one you run the API against).
2. Create/refresh `InnNou_Test` as a clean copy of it:

   ```powershell
   .\scripts\setup-test-db.ps1
   ```

   This backs up `InnNou`, restores it as `InnNou_Test`, and redeploys every stored procedure from
   `database/stored-procedures/**/*.sql` on top - so procedures always match checked-in source,
   independent of whatever might be stale on your local dev DB. Safe to re-run any time; it always
   drops and recreates `InnNou_Test` from scratch. Requires `sqlcmd` on PATH and enough SQL Server
   permissions to `BACKUP`/`RESTORE DATABASE`.

3. `tests/InnNou.IntegrationTests/appsettings.test.json` already points at
   `Server=localhost\SQLEXPRESS;Database=InnNou_Test;...`. Override via environment variables
   (prefix `INNOU_TEST_`, double-underscore for nesting) if your instance differs, e.g.:

   ```
   INNOU_TEST_ConnectionStrings__InnNouConnection=Server=.;Database=InnNou_Test;...
   ```

## Running

```
dotnet test tests/InnNou.IntegrationTests
```

## Adding a new test module

- Derive from `TestFixtures/TransactionalTestBase.cs` - gives you `Mediator`, `Context` (a settable
  `TestRequestContext` for simulating any `RoleLevel`/`OrganizationId`/impersonation shape without
  needing a real JWT), and `Data` (a fresh `TestDataBuilder` for the test's scope).
- Prefer adding fixture-building methods to `TestFixtures/TestDataBuilder.cs` over inlining `Send`
  calls in the test itself, so later tests can reuse them.
- Create fresh entities (fresh `Family`, fresh `Supplier`, etc.) rather than reusing seeded ones
  whose state might carry leftover config from another test or a manual session, unless the test
  is specifically about seeded reference data.
- The one exception is the shared `ASSOCIATE` organization (`GetAssociateOrganizationAsync`) -
  there's only one to reuse, and `InnNou_Test` is a snapshot of real dev data, so it may already
  carry real config on it (e.g. a `SupplierInvoiceMatchTolerance` row from manual UI testing).
  When a test's correctness depends on that org's config being a specific value, set it explicitly
  at the top of the test rather than assuming it's unconfigured/default.
