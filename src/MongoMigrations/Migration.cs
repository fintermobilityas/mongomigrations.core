using MongoDB.Bson;

namespace MongoMigrations
{
	using MongoDB.Driver;

	public abstract class Migration
	{
		public MigrationVersion Version { get; protected set; }
		public string Description { get; protected set; }

		protected Migration(MigrationVersion version)
		{
			Version = version;
		}

		public IMongoDatabase Database { get; set; }

        public FilterDefinition<BsonDocument> Filter { get; set; } = FilterDefinition<BsonDocument>.Empty;

		public abstract void Update();

        /// <summary>
        /// Invoked before `Update`
        /// </summary>
	    public virtual void OnBeforeMigration()
	    {
	        
	    }

        /// <summary>
        /// Invoke after `Update`
        /// </summary>
	    public virtual void OnAfterSuccessfulMigration()
	    {
	        
	    }
	}
}