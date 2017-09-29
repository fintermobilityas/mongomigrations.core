using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoMigrations
{
	public sealed class MigrationRunner
	{
		static MigrationRunner()
		{
			Init();
		}

	    static void Init()
		{
			BsonSerializer.RegisterSerializer(typeof (MigrationVersion), new MigrationVersionSerializer());
		}

	    public MigrationRunner(string connectionString, string database) : this(new MongoClient(connectionString).GetDatabase(database))
	    {
	        
	    }

		public MigrationRunner(IMongoDatabase database)
		{
			Database = database;
			DatabaseStatus = new DatabaseMigrationStatus(this);
			MigrationLocator = new MigrationLocator();
		}

	    public IMongoDatabase Database { get; set; }
		public MigrationLocator MigrationLocator { get; set; }
		public DatabaseMigrationStatus DatabaseStatus { get; set; }

		public void UpdateToLatest()
		{
			Console.WriteLine(WhatWeAreUpdating() + " to latest...");
			UpdateTo(MigrationLocator.LatestVersion());
		}

	    string WhatWeAreUpdating()
		{
			return $"Updating server(s) \"{ServerAddresses()}\" for database \"{Database.DatabaseNamespace.DatabaseName}\"";
		}

	    string ServerAddresses()
	    {
	        return string.Join(",", Database.Client.Settings.Servers.Select(x => $"{x.Host}:{x.Port}"));
	    }

	    void ApplyMigrations(IEnumerable<Migration> migrations)
		{
			migrations.ToList()
			          .ForEach(ApplyMigration);
		}

	    void ApplyMigration(Migration migration)
		{
			Console.WriteLine(new {Message = "Applying migration", migration.Version, migration.Description, DatabaseName = Database.DatabaseNamespace.DatabaseName});

			var appliedMigration = DatabaseStatus.StartMigration(migration);
		    migration.Database = migration.Database;

            try
            {
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

	    void OnMigrationException(IMigration migration, Exception exception)
		{
			var message = new
				{
					Message = "Migration failed to be applied: " + exception.Message,
					migration.Version,
					Name = migration.GetType(),
					migration.Description,
					DatabaseName = Database.DatabaseNamespace.DatabaseName
				};
			Console.WriteLine(message);
			throw new MigrationException(message.ToString(), exception);
		}

		public void UpdateTo(MigrationVersion updateToVersion)
		{
			var currentVersion = DatabaseStatus.GetLastAppliedMigration();
			Console.WriteLine(new {Message = WhatWeAreUpdating(), currentVersion, updateToVersion, DatabaseName = Database.DatabaseNamespace.DatabaseName});

			var migrations = MigrationLocator.GetMigrationsAfter(currentVersion)
			                                 .Where(m => m.Version <= updateToVersion);

			ApplyMigrations(migrations);
		}

	    public bool ConfigureIf<TFeature>(IMigration migration, Action<TFeature> action) where TFeature: IMigrationProperty
	    {
	        if (!(migration is TFeature tFeature))
	        {
	            return false;
	        }

	        action?.Invoke(tFeature);
	        return true;
	    }

	    public bool InvokeIf<TFeature>(IMigration migration, Action<TFeature> action) where TFeature: IMigrationInvokable
	    {
	        if (!(migration is TFeature tFeature))
	        {
	            return false;
	        }

	        action?.Invoke(tFeature);
	        return true;
	    }

    }
}