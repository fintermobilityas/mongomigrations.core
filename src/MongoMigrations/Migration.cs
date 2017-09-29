using System;
using MongoDB.Bson;

namespace MongoMigrations
{
	using MongoDB.Driver;

    public interface IMigration
    {
        IMongoDatabase Database { get; }
        MigrationVersion Version { get; }
        string Description { get; set; }
        void Update();
    }

    public interface IMigrationProperty
    {
        
    }

    public interface IMigrationInvokable
    {
        
    }

    public interface ISupportFilter : IMigrationProperty
    {
        FilterDefinition<BsonDocument> Filter { get; set; }
    }

    public interface ISupportBatchSize : IMigrationProperty
    {
        int BatchSize { get; set; }
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
        public IMongoDatabase Database { get; internal set; }
        public MigrationVersion Version { get; protected set; }
        public string Description { get; set; }

        protected Migration(MigrationVersion version)
		{
		    if (version == null)
		    {
		        throw new ArgumentNullException(nameof(version));
		    }
			Version = version;
		}

		public abstract void Update();
	}
}