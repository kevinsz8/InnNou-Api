using Xunit;

// Every test opens a real ambient TransactionScope (default isolation level Serializable) against
// the SAME InnNou_Test database and often the SAME seeded reference rows (e.g. the one ASSOCIATE
// organization every test's fixtures hang off). xUnit parallelizes different test classes across
// threads by default, which under Serializable isolation reliably deadlocks unrelated tests on
// shared locks. Running the whole assembly sequentially trades some wall-clock time for tests that
// don't spuriously fail each other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
