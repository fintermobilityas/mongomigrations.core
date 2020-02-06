using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace MongoMigrations
{
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public interface IDatabaseMigrationStatus
    {
        string CollectionName { get; }
        IMongoCollection<AppliedMigration> Collection { get; }
        bool IsNotLatestVersion(out MigrationVersion version);
        MigrationVersion GetVersion();
        List<AppliedMigration> GetMigrations();
        AppliedMigration GetLastAppliedMigration();
        AppliedMigration StartMigration([NotNull] IMigration migration, string serverName);
        void CompleteMigration([NotNull] AppliedMigration appliedMigration);
    }

    public sealed class DatabaseMigrationStatus : IDatabaseMigrationStatus
    {
        readonly IMigrationRunner _runner;
        IMongoCollection<AppliedMigration> _collection;

        public string CollectionName { get; }
        public IMongoCollection<AppliedMigration> Collection => _collection ??= _runner.Database.GetCollection<AppliedMigration>(CollectionName);

        public DatabaseMigrationStatus([NotNull] IMigrationRunner runner, [NotNull] string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(collectionName));
            CollectionName = collectionName;
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
            return lastAppliedMigration?.Version ?? MigrationVersion.Default;
        }

        public List<AppliedMigration> GetMigrations()
        {
            return Collection
                .Find(FilterDefinition<AppliedMigration>.Empty)
                .SortBy(v => v.Version)
                .ToList();
        }

        public AppliedMigration GetLastAppliedMigration()
        {
            return Collection
                .Find(FilterDefinition<AppliedMigration>.Empty)
                .SortByDescending(v => v.Version)
                .FirstOrDefault();
        }

        public AppliedMigration StartMigration(IMigration migration, string serverName)
        {
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            var appliedMigration = new AppliedMigration(migration)
            {
                ServerName = serverName
            };
            Collection.InsertOne(appliedMigration);
            return appliedMigration;
        }

        public void CompleteMigration(AppliedMigration appliedMigration)
        {
            if (appliedMigration == null) throw new ArgumentNullException(nameof(appliedMigration));
            appliedMigration.CompletedOn = DateTime.Now;
            Collection.UpdateOne(Builders<AppliedMigration>.Filter.Eq(x => x.Version, appliedMigration.Version),
                Builders<AppliedMigration>.Update.Set(x => x.CompletedOn, appliedMigration.CompletedOn));
        }
    }
}