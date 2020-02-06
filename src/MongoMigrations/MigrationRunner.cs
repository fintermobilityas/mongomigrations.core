using System;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoMigrations
{
    public sealed class MigrationRunner
    {
        public IMongoDatabase Database { get; [UsedImplicitly]set; }
        public MigrationLocator MigrationLocator { get; [UsedImplicitly]set; }
        public DatabaseMigrationStatus DatabaseStatus { get; [UsedImplicitly]set; }
        
        static MigrationRunner()
        {
            BsonSerializer.RegisterSerializer(typeof(MigrationVersion), new MigrationVersionSerializer());
        }

        [UsedImplicitly]
        public MigrationRunner([NotNull] string connectionString, [NotNull] string database) : this(new MongoClient(connectionString).GetDatabase(database))
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            if (database == null) throw new ArgumentNullException(nameof(database));
        }

        public MigrationRunner([NotNull] IMongoDatabase database)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            DatabaseStatus = new DatabaseMigrationStatus(this);
            MigrationLocator = new MigrationLocator();
        }

        [UsedImplicitly]
        public void UpdateToLatest()
        {
            UpdateTo(MigrationLocator.LatestVersion());
        }

        public void UpdateTo(MigrationVersion updateToVersion)
        {
            var currentVersion = DatabaseStatus.GetLastAppliedMigration();

            var migrations = MigrationLocator
                .GetMigrationsAfter(currentVersion)
                .Where(m => m.Version <= updateToVersion)
                .ToList();

            foreach (var migration in migrations)
            {
                ApplyMigration(migration);
            }

            void ApplyMigration(Migration migration)
            {
                var appliedMigration = DatabaseStatus.StartMigration(migration);
                migration.Database = Database;

                try
                {
                    if (migration is CollectionMigration collectionMigration)
                    {
                        collectionMigration.Collection = migration.Database.GetCollection<BsonDocument>(collectionMigration.CollectionName);
                    }

                    InvokeIf<ISupportOnBeforeMigration>(migration, x => x.OnBeforeMigration());
                    migration.Update();
                    InvokeIf<ISupportOnAfterSuccessfullMigration>(migration, x => x.OnAfterSuccessfulMigration());
                }
                catch (Exception exception)
                {
                    OnMigrationException(migration, exception);
                }

                DatabaseStatus.CompleteMigration(appliedMigration);
            }
        }

        [UsedImplicitly]
        public bool ConfigureIf<TFeature>(IMigration migration, Action<TFeature> action) where TFeature : IMigrationProperty
        {
            if (!(migration is TFeature tFeature))
                return false;

            action?.Invoke(tFeature);
            return true;
        }

        public bool InvokeIf<TFeature>(IMigration migration, Action<TFeature> action) where TFeature : IMigrationInvokable
        {
            if (!(migration is TFeature tFeature))
                return false;

            action?.Invoke(tFeature);
            return true;
        }

        void OnMigrationException([NotNull] IMigration migration, [NotNull] Exception exception)
        {
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            var message = new
            {
                Message = $"Migration failed to be applied: {exception.Message}",
                migration.Version,
                Name = migration.GetType(),
                migration.Description,
                Database.DatabaseNamespace.DatabaseName
            };
            throw new MigrationException(message.ToString(), exception);
        }

    }
}