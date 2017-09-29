using System;
using System.Reflection;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace MongoMigrations
{
    [UsedImplicitly]
    public static class MigrationExtensions
    {
        [UsedImplicitly]
        public static bool IsDbUpToDate(this IMongoDatabase db, Assembly migrationsAssembly, out MigrationVersion current)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (migrationsAssembly == null) throw new ArgumentNullException(nameof(migrationsAssembly));

            var runner = new MigrationRunner(db);
            runner.MigrationLocator.LookForMigrationsInAssembly(migrationsAssembly);
            current = runner.DatabaseStatus.GetVersion();
            return !runner.DatabaseStatus.IsNotLatestVersion();
        }

        [UsedImplicitly]
        public static bool IsDbUpToDate(this IMongoDatabase db, Assembly migrationsAssembly)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (migrationsAssembly == null) throw new ArgumentNullException(nameof(migrationsAssembly));

            return IsDbUpToDate(db, migrationsAssembly, out _);
        }
    }
}