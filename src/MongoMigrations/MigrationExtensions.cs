using System;
using System.Reflection;
using MongoDB.Driver;

namespace MongoMigrations
{
    public static class MigrationExtensions
    {
        public static bool IsDbUpToDate(this IMongoDatabase db, Assembly migrationsAssembly, out MigrationVersion current)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (migrationsAssembly == null) throw new ArgumentNullException(nameof(migrationsAssembly));

            var runner = new MigrationRunner(db);
            runner.MigrationLocator.LookForMigrationsInAssembly(migrationsAssembly);
            current = runner.DatabaseStatus.GetVersion();
            return !runner.DatabaseStatus.IsNotLatestVersion();
        }

        public static bool IsDbUpToDate(this IMongoDatabase db, Assembly migrationsAssembly)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (migrationsAssembly == null) throw new ArgumentNullException(nameof(migrationsAssembly));

            MigrationVersion version;
            return IsDbUpToDate(db, migrationsAssembly, out version);
        }
    }
}
