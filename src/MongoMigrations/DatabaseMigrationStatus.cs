using System;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace MongoMigrations
{
    public sealed class DatabaseMigrationStatus
    {
        readonly MigrationRunner _runner;

        [UsedImplicitly] public string VersionCollectionName = "DatabaseVersion";

        public DatabaseMigrationStatus(MigrationRunner runner)
        {
            _runner = runner;
        }

        [UsedImplicitly]
        public IMongoCollection<AppliedMigration> GetMigrationsApplied()
        {
            return _runner.Database.GetCollection<AppliedMigration>(VersionCollectionName);
        }

        public bool IsNotLatestVersion()
        {
            return _runner.MigrationLocator.LatestVersion()
                   != GetVersion();
        }

        [UsedImplicitly]
        public void ThrowIfNotLatestVersion()
        {
            if (!IsNotLatestVersion())
                return;
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
            return GetMigrationsApplied()
                .Find(FilterDefinition<AppliedMigration>.Empty)
                .SortByDescending(v => v.Version)
                .FirstOrDefault();
        }

        public AppliedMigration StartMigration(IMigration migration)
        {
            var appliedMigration = new AppliedMigration(migration);
            GetMigrationsApplied().InsertOne(appliedMigration);
            return appliedMigration;
        }

        public void CompleteMigration(AppliedMigration appliedMigration)
        {
            appliedMigration.CompletedOn = DateTime.Now;
            GetMigrationsApplied().UpdateOne(Builders<AppliedMigration>.Filter.Eq(x => x.Version, appliedMigration.Version),
                Builders<AppliedMigration>.Update.Set(x => x.CompletedOn, appliedMigration.CompletedOn));
        }

        [UsedImplicitly]
        public void MarkUpToVersion(MigrationVersion version)
        {
            _runner.MigrationLocator.GetAllMigrations()
                .Where(m => m.Version <= version)
                .ToList()
                .ForEach(m => MarkVersion(m.Version));
        }

        [UsedImplicitly]
        public void MarkVersion(MigrationVersion version)
        {
            var appliedMigration = AppliedMigration.MarkerOnly(version);
            GetMigrationsApplied().InsertOne(appliedMigration);
        }
    }
}