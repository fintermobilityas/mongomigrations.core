using System;
using MongoDB.Driver;

namespace MongoMigrations.Tests.Fixtures
{
    public sealed class MigrationRunnerFixture : IDisposable
    {
        readonly DatabaseFixture _databaseFixture;
        readonly string _collectionName;

        public MigrationRunner MigrationRunner { get; }
        public IMongoCollection<AppliedMigration> Collection => MigrationRunner.DatabaseStatus.Collection;

        public MigrationRunnerFixture(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
            _collectionName = Guid.NewGuid().ToString();

            MigrationRunner = new MigrationRunner(databaseFixture.Database, _collectionName);
        }

        public void Dispose()
        {
            _databaseFixture.MongoClient.DropDatabase(_collectionName);
        }
    }
}