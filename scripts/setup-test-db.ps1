<#
.SYNOPSIS
(Re)creates InnNou_Test as a clean copy of the local InnNou dev database's
schema + stored procedures, so integration tests run against real SQL Server
and real SPs - no mocking, no Testcontainers/Docker dependency.

.DESCRIPTION
Safe to re-run at any time. Backs up InnNou and restores it as InnNou_Test,
then redeploys every stored procedure from database/stored-procedures/ on top
(guarantees InnNou_Test's procedures exactly match the checked-in .sql files,
not whatever might be stale on the live dev DB).

Why restore instead of replaying database/migrations/*.sql from an empty DB:
the migrations were written incrementally against InnNou's own continuously-
evolving history and were never designed to bootstrap a blank database (the
earliest ones assume tables/columns that predate the migrations folder
itself, e.g. DROPping a table with no IF EXISTS guard). InnNou's live schema
is the only reliable source of truth for "what does the schema look like
today" - this script copies it wholesale rather than trying to rebuild it.

InnNou_Test starts out with whatever business data currently exists in the
InnNou dev DB (this is a straight copy, not a stripped-down seed). That's
intentional, not sloppy: every integration test wraps in a TransactionScope
and rolls back at the end (see TestFixtures/TransactionalTestBase.cs), so
nothing a test creates ever persists - pre-existing rows are simply inert
background noise a well-written test's own token-scoped assertions never
touch. Re-run this script whenever InnNou_Test's schema drifts from InnNou's
(new migration applied to InnNou) to pick up the change.

.EXAMPLE
powershell -File scripts/setup-test-db.ps1
#>

param(
    [string]$Server = "localhost\SQLEXPRESS",
    [string]$SourceDatabase = "InnNou",
    [string]$Database = "InnNou_Test"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

$dataDir = (& sqlcmd -S $Server -E -h -1 -W -Q "SET NOCOUNT ON; SELECT physical_name FROM sys.master_files WHERE database_id = DB_ID('$SourceDatabase') AND type = 0") | Select-Object -First 1
if (-not $dataDir) { throw "Could not resolve the data file path for '$SourceDatabase' - is it running on '$Server'?" }
$dataDir = Split-Path -Parent $dataDir.Trim()

# Backing up into the caller's own %TEMP% fails with "Access is denied" - BACKUP DATABASE runs
# server-side, and the SQL Server service account has no reason to have access to a random
# Windows user's profile folder. Its own DATA directory is always writable by the service, since
# that's where it writes every .mdf/.ldf it owns - reuse it as scratch space for the .bak too.
$backupPath = Join-Path $dataDir "$SourceDatabase-for-test-restore.bak"

Write-Host "Backing up '$SourceDatabase' to $backupPath..."
sqlcmd -S $Server -E -b -Q "BACKUP DATABASE [$SourceDatabase] TO DISK = N'$backupPath' WITH INIT, COPY_ONLY"
if ($LASTEXITCODE -ne 0) { throw "Backup of '$SourceDatabase' failed." }

Write-Host "Dropping existing '$Database' (if any)..."
sqlcmd -S $Server -E -b -Q "IF DB_ID('$Database') IS NOT NULL BEGIN ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Database]; END"
if ($LASTEXITCODE -ne 0) { throw "Could not drop existing '$Database'." }

$dataFile = Join-Path $dataDir "$Database.mdf"
$logFile = Join-Path $dataDir "${Database}_log.ldf"

Write-Host "Restoring '$Database' from the backup..."
$restoreSql = "RESTORE DATABASE [$Database] FROM DISK = N'$backupPath' WITH MOVE N'$SourceDatabase' TO N'$dataFile', MOVE N'${SourceDatabase}_log' TO N'$logFile', REPLACE;"
sqlcmd -S $Server -E -b -Q $restoreSql
if ($LASTEXITCODE -ne 0) { throw "Restore into '$Database' failed." }
Remove-Item $backupPath -Force -ErrorAction SilentlyContinue

Write-Host "Deploying stored procedures onto '$Database' (source of truth: database/stored-procedures/*.sql)..."
Get-ChildItem "$repoRoot\database\stored-procedures" -Recurse -Filter "*.sql" | Sort-Object FullName | ForEach-Object {
    sqlcmd -S $Server -d $Database -E -i $_.FullName -b
    if ($LASTEXITCODE -ne 0) { throw "Stored procedure deploy failed: $($_.FullName)" }
}

Write-Host "'$Database' is ready (schema+data copied from '$SourceDatabase', procedures redeployed from disk)."
