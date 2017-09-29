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
        public static bool IsTypeOf([NotNull] this BsonDocument document, [NotNull] string typeName)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(typeName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(typeName));

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

        [UsedImplicitly]
        public static void DropAllIndexes([NotNull] this IMongoDatabase database, [NotNull] string collectionName)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(collectionName));
            database.GetCollection<BsonDocument>(collectionName).DropAllIndexes();
        }

        [UsedImplicitly]
        public static void DropAllIndexes([NotNull] this IMongoCollection<BsonDocument> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            collection.Indexes.DropAll();
        }

        [UsedImplicitly]
        public static void RenameCollectionAndDropOldOne([NotNull] IMongoDatabase database, [NotNull] string oldCollectionName, [NotNull] string newCollectionName)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (string.IsNullOrWhiteSpace(oldCollectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(oldCollectionName));
            if (string.IsNullOrWhiteSpace(newCollectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(newCollectionName));
            database.RenameCollectionAndDropOldOne(database.GetCollection<BsonDocument>(oldCollectionName), newCollectionName);
        }

        [UsedImplicitly]
        public static void RenameCollectionAndDropOldOne([NotNull] this IMongoDatabase database, [NotNull] IMongoCollection<BsonDocument> collection, [NotNull] string newCollectionName)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (string.IsNullOrWhiteSpace(newCollectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(newCollectionName));
            database.RenameCollection(collection.CollectionNamespace.CollectionName, newCollectionName);
            database.DropCollection(collection.CollectionNamespace.CollectionName);
        }

        [UsedImplicitly]
        public static IEnumerable<BsonDocument> ToEnumerable([NotNull] this IMongoCollection<BsonDocument> collection, 
            [NotNull] FilterDefinition<BsonDocument> filterDefinition,
            SortDefinition<BsonDocument> sortDefinition = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));

            var cursor = sortDefinition != null ? collection.Find(filterDefinition).Sort(sortDefinition).ToCursor() : collection.Find(filterDefinition).ToCursor();

            while (cursor.MoveNext())
            {
                foreach (var document in cursor.Current)
                {
                    yield return document;
                }
            }
        }
    }
}