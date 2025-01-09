using System;
using MongoDB.Driver;

namespace MongoMigrations.Core.Tests.Fixtures;

public sealed class MigrationRunnerFixture : IDisposable
{
    readonly DatabaseFixture _databaseFixture;
    readonly string _collectionName;

    public IMigrationRunner MigrationRunner { get; }
    public IMongoCollection<AppliedMigration> Collection => MigrationRunner.DatabaseStatus.Collection;

    public MigrationRunnerFixture(DatabaseFixture databaseFixture, IMigrationLocator migrationLocator = null)
    {
        _databaseFixture = databaseFixture;
        _collectionName = Guid.NewGuid().ToString();

        MigrationRunner = new MigrationRunner(databaseFixture.Database, _collectionName, migrationLocator);
    }

    public void Dispose()
    {
        _databaseFixture.Database.DropCollection(_collectionName);
    }
}