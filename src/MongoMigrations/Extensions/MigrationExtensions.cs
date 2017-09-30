using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoMigrations.Extensions
{
    [UsedImplicitly]
    public static class MigrationExtensions
    {
        public static IEnumerable<IWriteModel> AsEnumerable(this WriteModel<BsonDocument> writeModel)
        {
            return new List<IWriteModel> { new WriteModel(writeModel) };
        }

        [UsedImplicitly]
        public static bool TryGetElement(this MigrationRootDocument rootDocument, string name, out BsonElement value)
        {
            return rootDocument.Document.TryGetElement(name, out value);
        }

        [UsedImplicitly]
        public static bool TryGetValue(this MigrationRootDocument rootDocument, string name, out BsonValue value)
        {
            return rootDocument.Document.TryGetValue(name, out value);
        }

        [UsedImplicitly]
        public static string ToString(this MigrationRootDocument rootDocument)
        {
            return rootDocument.Document.ToString();
        }

        [UsedImplicitly]
        public static bool IsTypeOf(this MigrationRootDocument rootDocument, [NotNull] string typeName)
        {
           return rootDocument.Document.IsTypeOf(typeName);
        }

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

        /// <summary>
        ///     Rename all instances of a name in a bson document to the new name.
        /// </summary>
        [UsedImplicitly]
        public static void ChangeName(this BsonDocument bsonDocument, string originalName, string newName)
        {
            var elements = bsonDocument.Elements
                .Where(e => e.Name == originalName)
                .ToList();
            foreach (var element in elements)
            {
                bsonDocument.RemoveElement(element);
                bsonDocument.Add(new BsonElement(newName, element.Value));
            }
        }

        public static object TryGetDocumentId(this BsonDocument bsonDocument)
        {
            bsonDocument.TryGetValue("_id", out var id);
            return id ?? "Cannot find id";
        }

        [UsedImplicitly]
        public static void Save(this IMongoCollection<BsonDocument> collection, BsonDocument bsonDocument, string id = "_id")
        {
            var documentId = bsonDocument.GetValue(id);
            collection.ReplaceOne(Builders<BsonDocument>.Filter.Eq(x => x[id], documentId), bsonDocument);
        }
    }
}