﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoMigrations.Core
{
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public interface IDatabaseMigrationStatus
    {
        string CollectionName { get; }
        IMongoCollection<AppliedMigration> Collection { get; }
        void AddIndexes();
        List<AppliedMigration> GetMigrations();
        bool IsMigrationInProgress();
        Task<bool> IsMigrationInProgressAsync(CancellationToken cancellationToken = default);
        AppliedMigration GetLastAppliedMigration();
        Task<AppliedMigration> GetLastAppliedMigrationAsync(CancellationToken cancellationToken = default);
        AppliedMigration StartMigration([NotNull] IMigration migration, string serverName);
        void FailMigration(AppliedMigration appliedMigration, Exception exception);
        void CompleteMigration([NotNull] AppliedMigration appliedMigration);
    }

    public sealed class DatabaseMigrationStatus : IDatabaseMigrationStatus
    {
        readonly IMigrationRunner _runner;
        IMongoCollection<AppliedMigration> _collection;
        readonly IOrderedFindFluent<AppliedMigration, AppliedMigration> _getLastApplicationMigrationBuilder;

        public string CollectionName { get; }
        public IMongoCollection<AppliedMigration> Collection => _collection ??= _runner.Database.GetCollection<AppliedMigration>(CollectionName);

        public DatabaseMigrationStatus([NotNull] IMigrationRunner runner, [NotNull] string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(collectionName));
            CollectionName = collectionName;
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _getLastApplicationMigrationBuilder = Collection
                .Find(Builders<AppliedMigration>.Filter.Ne(x => x.CompletedOn, null))
                .SortByDescending(v => v.Version);
        }

        public void AddIndexes()
        {
            Collection.Indexes.CreateOne(new CreateIndexModel<AppliedMigration>(new IndexKeysDefinitionBuilder<AppliedMigration>().Ascending(x => x.CompletedOn)));
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
            return _getLastApplicationMigrationBuilder.FirstOrDefault();
        }

        public Task<AppliedMigration> GetLastAppliedMigrationAsync(CancellationToken cancellationToken = default)
        {
            return _getLastApplicationMigrationBuilder.FirstOrDefaultAsync(cancellationToken);
        }

        public bool IsMigrationInProgress()
        {
            return Collection
                .Find(Builders<AppliedMigration>.Filter.Eq(x => x.CompletedOn, null))
                .CountDocuments() > 0;
        }

        public async Task<bool> IsMigrationInProgressAsync(CancellationToken cancellationToken = default)
        {
            return await 
                Collection
                .Find(Builders<AppliedMigration>.Filter.Eq(x => x.CompletedOn, null))
                .CountDocumentsAsync(cancellationToken)
                .ConfigureAwait(false) > 0;
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

        public void FailMigration(AppliedMigration appliedMigration, [NotNull] Exception exception)
        {
            if (appliedMigration == null) throw new ArgumentNullException(nameof(appliedMigration));
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            appliedMigration.ExceptionMessage = exception.Message;
            appliedMigration.FailedOn = DateTime.Now;
            Collection.UpdateOne(Builders<AppliedMigration>.Filter.Eq(x => x.Version, appliedMigration.Version),
                Builders<AppliedMigration>.Update.Set(x => x.ExceptionMessage, appliedMigration.ExceptionMessage).Set(x => x.FailedOn, appliedMigration.FailedOn));
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
