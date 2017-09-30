using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoMigrations
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
        ProjectionDefinition<BsonDocument> Project { get; set; }
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
        protected Migration(MigrationVersion version)
        {
            Version = version;
        }

        public IMongoDatabase Database { get; internal set; }
        public MigrationVersion Version { get; protected set; }
        public string Description { get; set; }

        public abstract void Update();
    }
}