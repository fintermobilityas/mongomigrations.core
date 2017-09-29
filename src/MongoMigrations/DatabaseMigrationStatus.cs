namespace MongoMigrations
{
	using System;
	using System.Linq;
	using MongoDB.Driver;

	public class DatabaseMigrationStatus
	{
		private readonly MigrationRunner _runner;

		public string VersionCollectionName = "DatabaseVersion";

		public DatabaseMigrationStatus(MigrationRunner runner)
		{
			_runner = runner;
		}

		public virtual IMongoCollection<AppliedMigration> GetMigrationsApplied()
		{
            return _runner.Database.GetCollection<AppliedMigration>(VersionCollectionName);
		}

		public virtual bool IsNotLatestVersion()
		{
			return _runner.MigrationLocator.LatestVersion()
			       != GetVersion();
		}

		public virtual void ThrowIfNotLatestVersion()
		{
			if (!IsNotLatestVersion())
			{
				return;
			}
			var databaseVersion = GetVersion();
			var migrationVersion = _runner.MigrationLocator.LatestVersion();
			throw new ApplicationException("Database is not the expected version, database is at version: " + databaseVersion + ", migrations are at version: " + migrationVersion);
		}

		public virtual MigrationVersion GetVersion()
		{
			var lastAppliedMigration = GetLastAppliedMigration();
			return lastAppliedMigration?.Version ?? MigrationVersion.Default();
		}

		public virtual AppliedMigration GetLastAppliedMigration()
		{
            return GetMigrationsApplied()
				.Find(FilterDefinition<AppliedMigration>.Empty)
				.ToList() // in memory but this will never get big enough to matter
				.OrderByDescending(v => v.Version)
				.FirstOrDefault();
		}

		public virtual AppliedMigration StartMigration(IMigration migration)
		{
			var appliedMigration = new AppliedMigration(migration);
			GetMigrationsApplied().InsertOne(appliedMigration);
			return appliedMigration;
		}

		public virtual void CompleteMigration(AppliedMigration appliedMigration)
		{
			appliedMigration.CompletedOn = DateTime.Now;
		    GetMigrationsApplied().UpdateOne(Builders<AppliedMigration>.Filter.Eq(x => x.Version, appliedMigration.Version), Builders<AppliedMigration>.Update.Set(x => x.CompletedOn, appliedMigration.CompletedOn));
		}

		public virtual void MarkUpToVersion(MigrationVersion version)
		{
			_runner.MigrationLocator.GetAllMigrations()
				.Where(m => m.Version <= version)
				.ToList()
				.ForEach(m => MarkVersion(m.Version));
		}

		public virtual void MarkVersion(MigrationVersion version)
		{
			var appliedMigration = AppliedMigration.MarkerOnly(version);
			GetMigrationsApplied().InsertOne(appliedMigration);
		}
	}
}