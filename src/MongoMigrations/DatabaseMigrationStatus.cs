using System;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace MongoMigrations
{
    public sealed class DatabaseMigrationStatus
    {
        readonly MigrationRunner _runner;
        IMongoCollection<AppliedMigration> _collection;

        public string VersionCollectionName = "DatabaseVersion";
        public IMongoCollection<AppliedMigration> Collection => _collection ??= _runner.Database.GetCollection<AppliedMigration>(VersionCollectionName);

        public DatabaseMigrationStatus(MigrationRunner runner)
        {
            _runner = runner;
        }

        public bool IsNotLatestVersion()
        {
            return _runner.MigrationLocator.LatestVersion() != GetVersion();
        }

        [UsedImplicitly]
        public void ThrowIfNotLatestVersion()
        {
            if (!IsNotLatestVersion())
            {
                return;
            }

            var databaseVersion = GetVersion();
            var migrationVersion = _runner.MigrationLocator.LatestVersion();

            throw new ApplicationException(
                $"Database is not the expected version, database is at version: {databaseVersion}, migrations are at version: {migrationVersion}");
        }

        public MigrationVersion GetVersion()
        {
            var lastAppliedMigration = GetLastAppliedMigration();
            return lastAppliedMigration?.Version ?? MigrationVersion.Default();
        }

        public AppliedMigration GetLastAppliedMigration()
        {
            return Collection
                .Find(FilterDefinition<AppliedMigration>.Empty)
                .ToList() // in memory but this will never get big enough to matter
                .OrderByDescending(v => v.Version)
                .FirstOrDefault();
        }

        public AppliedMigration StartMigration(IMigration migration)
        {
            var appliedMigration = new AppliedMigration(migration);
            Collection.InsertOne(appliedMigration);
            return appliedMigration;
        }

        public void CompleteMigration(AppliedMigration appliedMigration)
        {
            appliedMigration.CompletedOn = DateTime.Now;
            Collection.UpdateOne(Builders<AppliedMigration>.Filter.Eq(x => x.Version, appliedMigration.Version),
                Builders<AppliedMigration>.Update.Set(x => x.CompletedOn, appliedMigration.CompletedOn));
        }
    }
}