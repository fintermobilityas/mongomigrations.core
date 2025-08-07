using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoMigrations.Core;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public interface IMigrationRunner
{
    IMongoDatabase Database { get; }
    IMigrationLocator MigrationLocator { get; }
    IDatabaseMigrationStatus DatabaseStatus { get; }
    void UpdateToLatest(string serverName = null);
    bool IsDatabaseUpToDate(ReadPreference readPreference, CancellationToken cancellationToken = default);
    Task<bool> IsDatabaseUpToDateAsync(ReadPreference readPreference, CancellationToken cancellationToken = default);
}

public sealed class MigrationRunner : IMigrationRunner
{
    readonly object _latestVersionLock;
    MigrationVersion? _latestVersion;

    public IMongoDatabase Database { get; }
    public IMigrationLocator MigrationLocator { get; }
    public IDatabaseMigrationStatus DatabaseStatus { get; }

    static MigrationRunner()
    {
        BsonSerializer.RegisterSerializer(typeof(MigrationVersion), new MigrationVersionSerializer());
    }

    MigrationRunner()
    {
        _latestVersionLock = new object();
    }

    public MigrationRunner([NotNull] string connectionString, [NotNull] string database) : this(new MongoClient(connectionString).GetDatabase(database))
    {
        if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
        if (database == null) throw new ArgumentNullException(nameof(database));
    }

    public MigrationRunner([NotNull] IMongoDatabase database, string collectionName = "DatabaseVersion", IMigrationLocator migrationLocator = null) : this()
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));
        DatabaseStatus = new DatabaseMigrationStatus(this, collectionName);
        MigrationLocator = migrationLocator ?? new MigrationLocator();
    }

    public bool IsDatabaseUpToDate(ReadPreference readPreference, CancellationToken cancellationToken = default)
    {
        var latestVersion = GetLatestVersion();
        return DatabaseStatus.IsDatabaseUpToDate(latestVersion, readPreference, cancellationToken);
    }

    public async Task<bool> IsDatabaseUpToDateAsync(ReadPreference readPreference, CancellationToken cancellationToken = default)
    {
        var latestVersion = GetLatestVersion();
        return await DatabaseStatus.IsDatabaseUpToDateAsync(latestVersion, readPreference, cancellationToken);
    }

    public void UpdateToLatest(string serverName = null)
    {
        UpdateTo(MigrationLocator.GetLatestVersion(), serverName);
    }

    MigrationVersion GetLatestVersion()
    {
        MigrationVersion latestVersion;
        lock (_latestVersionLock)
        {
            latestVersion = _latestVersion ??= MigrationLocator.GetLatestVersion();
        }
        return latestVersion;
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
            if (migration is not TFeature tFeature) return;
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