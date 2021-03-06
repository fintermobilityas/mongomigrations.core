using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoMigrations.Core
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public interface IMigrationRunner
    {
        IMongoDatabase Database { get; }
        [JetBrains.Annotations.UsedImplicitly] IMigrationLocator MigrationLocator { get; }
        IDatabaseMigrationStatus DatabaseStatus { get; }
        void UpdateToLatest(string serverName = null);
        bool IsDatabaseUpToDate();
        Task<bool> IsDatabaseUpToDateAsync(CancellationToken cancellationToken = default);
    }

    public sealed class MigrationRunner : IMigrationRunner
    {
        public IMongoDatabase Database { get; }
        public IMigrationLocator MigrationLocator { get; }
        public IDatabaseMigrationStatus DatabaseStatus { get; }
        
        static MigrationRunner()
        {
            BsonSerializer.RegisterSerializer(typeof(MigrationVersion), new MigrationVersionSerializer());
        }

        [JetBrains.Annotations.UsedImplicitly]
        public MigrationRunner([NotNull] string connectionString, [NotNull] string database) : this(new MongoClient(connectionString).GetDatabase(database))
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            if (database == null) throw new ArgumentNullException(nameof(database));
        }

        public MigrationRunner([NotNull] IMongoDatabase database, string collectionName = "DatabaseVersion", IMigrationLocator migrationLocator = null)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            DatabaseStatus = new DatabaseMigrationStatus(this, collectionName);
            MigrationLocator = migrationLocator ?? new MigrationLocator();
        }

        public bool IsDatabaseUpToDate()
        {
            if (DatabaseStatus.IsMigrationInProgress())
            {
                return false;
            }

            var currentMigrationVersion = MigrationLocator.GetLatestVersion();
            var lastMigration = DatabaseStatus.GetLastAppliedMigration();
            return lastMigration?.CompletedOn != null && lastMigration.Version.Equals(currentMigrationVersion);
        }

        public async Task<bool> IsDatabaseUpToDateAsync(CancellationToken cancellationToken = default)
        {
            if (await DatabaseStatus.IsMigrationInProgressAsync(cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            var currentMigrationVersion = await MigrationLocator.GetLatestVersionAsync(cancellationToken).ConfigureAwait(false);
            var lastMigration = await DatabaseStatus.GetLastAppliedMigrationAsync(cancellationToken).ConfigureAwait(false);

            return lastMigration?.CompletedOn != null && lastMigration.Version.Equals(currentMigrationVersion);
        }

        public void UpdateToLatest(string serverName = null)
        {
            UpdateTo(MigrationLocator.GetLatestVersion(), serverName);
        }

        void UpdateTo(MigrationVersion updateToVersion, string serverName)
        {
            var lastAppliedMigration = DatabaseStatus.GetLastAppliedMigration();

            var migrations = MigrationLocator
                .GetMigrationsAfter(lastAppliedMigration)
                .Where(m => m.Version <= updateToVersion)
                .ToList();

            foreach (var migration in migrations)
            {
                ApplyMigration(migration);
            }

            static void InvokeIf<TFeature>(IMigration migration, Action<TFeature> action) where TFeature : IMigrationInvokable
            {
                if (!(migration is TFeature tFeature)) return;
                action?.Invoke(tFeature);
            }

            void ApplyMigration(IMigration migrationToApply)
            {
                if (migrationToApply == null) throw new ArgumentNullException(nameof(migrationToApply));

                AppliedMigration appliedMigration;
                try
                {
                    appliedMigration = DatabaseStatus.StartMigration(migrationToApply, serverName);
                }
                catch (MongoWriteException e) when (e.Message.Contains("E11000")) // Duplicate key
                {
                    throw new ConcurrentMigrationException(migrationToApply.Version, e);
                }

                if (migrationToApply is Migration migration)
                {
                    migration.Database = Database;
                }

                try
                {
                    if (migrationToApply is CollectionMigration collectionMigrationToApply)
                    {
                        collectionMigrationToApply.Collection = Database.GetCollection<BsonDocument>(collectionMigrationToApply.CollectionName);
                    }

                    InvokeIf<ISupportOnBeforeMigration>(migrationToApply, x => x.OnBeforeMigration());
                    migrationToApply.Update();
                    InvokeIf<ISupportOnAfterSuccessfullMigration>(migrationToApply, x => x.OnAfterSuccessfulMigration());

                }
                catch (Exception exception)
                {
                    var message = new
                    {
                        Message = $"Migration failed to be applied: {exception.Message}",
                        migrationToApply.Version,
                        Name = migrationToApply.GetType(),
                        migrationToApply.Description,
                        Database.DatabaseNamespace.DatabaseName
                    };

                    DatabaseStatus.FailMigration(appliedMigration, exception);

                    throw new MigrationException(message.ToString(), exception);
                }

                DatabaseStatus.CompleteMigration(appliedMigration);
            }
        }

    }
}