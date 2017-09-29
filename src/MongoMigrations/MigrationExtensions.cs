using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MongoDB.Bson;
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

        [UsedImplicitly]
        public static bool IsTypeOf(this BsonDocument document, string typeName)
        {
            if (!document.TryGetValue("_t", out var value))
            {
                throw new KeyNotFoundException($"Document does not contain key: _t`{typeName}");
            }

            if (value.BsonType != BsonType.Array)
            {
                throw new Exception($"Expected _t`{typeName}` to be instance of {nameof(BsonType.Array)} but was {value.BsonType}.");
            }

            return value.AsBsonArray.Select(x => x.AsString).Contains(typeName, StringComparer.InvariantCulture);
        }
    }
}