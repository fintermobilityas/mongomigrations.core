using System;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoMigrations.Core
{
    public interface IMigration
    {
        [UsedImplicitly] IMongoDatabase Database { get; }
        MigrationVersion Version { get; }
        string Description { get; [UsedImplicitly] set; }
        [UsedImplicitly] void Update();
    }

    public interface IMigrationProperty
    {
    }

    public interface IMigrationInvokable
    {
    }

    public interface ISupportFilter : IMigrationProperty
    {
        [UsedImplicitly] FilterDefinition<BsonDocument> Filter { get; set; }
    }

    public interface ISupportBatchSize : IMigrationProperty
    {
        [UsedImplicitly] int BatchSize { get; set; }
    }

    public interface ISupportProjection : IMigrationProperty
    {
        [UsedImplicitly]
        ProjectionDefinition<BsonDocument> Projection { get; set; }
    }

    public interface ISupportOnBeforeMigration : IMigrationInvokable
    {
        void OnBeforeMigration();
    }

    public interface ISupportOnAfterSuccessfullMigration : IMigrationInvokable
    {
        void OnAfterSuccessfulMigration();
    }

    public abstract class Migration : IMigration
    {
        // ReSharper disable once PublicConstructorInAbstractClass
        public Migration(int major)
        {
            if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
            Version = new MigrationVersion(major);
        }

        public IMongoDatabase Database { get; internal set; }
        public MigrationVersion Version { get; }
        public string Description { get; set; }

        public abstract void Update();
    }
}