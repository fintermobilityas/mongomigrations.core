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

        public IMongoCollection<AppliedMigration> Collection => _collection ??= _runner.Database.GetCollection<AppliedMigration>("DatabaseVersion");

        public DatabaseMigrationStatus([NotNull] MigrationRunner runner)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public bool IsNotLatestVersion(out MigrationVersion version)
        {
            version = GetVersion();
            return _runner.MigrationLocator.GetLatestVersion() != version;
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

        public AppliedMigration StartMigration([NotNull] IMigration migration)
        {
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            var appliedMigration = new AppliedMigration(migration);
            Collection.InsertOne(appliedMigration);
            return appliedMigration;
        }

        public void CompleteMigration([NotNull] AppliedMigration appliedMigration)
        {
            if (appliedMigration == null) throw new ArgumentNullException(nameof(appliedMigration));
            appliedMigration.CompletedOn = DateTime.Now;
            Collection.UpdateOne(Builders<AppliedMigration>.Filter.Eq(x => x.Version, appliedMigration.Version),
                Builders<AppliedMigration>.Update.Set(x => x.CompletedOn, appliedMigration.CompletedOn));
        }
    }
}