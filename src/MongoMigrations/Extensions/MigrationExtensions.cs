using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoMigrations.Documents;
using MongoMigrations.WriteModels;

namespace MongoMigrations.Extensions
{
    [UsedImplicitly]
    public static class MigrationExtensions
    {
        internal static IEnumerable<IWriteModel> AsEnumerable(this WriteModel<BsonDocument> writeModel)
        {
            return new List<IWriteModel> { new WriteModel(writeModel) };
        }

        [UsedImplicitly]
        public static BsonDocument ToUpdateDefinitionBsonDocument<TDocument>([NotNull] this UpdateDefinition<TDocument> updateDefinition)
        {
            if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedFilter = updateDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);
            return renderedFilter;
        }

        [UsedImplicitly]
        public static BsonDocument ToFilterDefinitionBsonDocument<TDocument>([NotNull] this FilterDefinition<TDocument> filterDefinition)
        {
            if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);
            return renderedFilter;
        }

        public static BsonDocument ToWriteModelBsonDocument<TDocument>([NotNull] this WriteModel<TDocument> writeModel)
        {
            if (writeModel == null) throw new ArgumentNullException(nameof(writeModel));

            var bsonDocument = new BsonDocument
            {
                { "ModelType", writeModel.ModelType }
            };

            switch (writeModel)
            {
                case UpdateOneModel<TDocument> updateOneModel:
                    return bsonDocument.Merge(new BsonDocument
                    {
                        { "Filter", updateOneModel.Filter.ToFilterDefinitionBsonDocument() },
                        { "Update", updateOneModel.Update.ToUpdateDefinitionBsonDocument() },
                        { "IsUpsert", updateOneModel.IsUpsert }
                    });
                case UpdateManyModel<TDocument> updateManyModel:
                    return bsonDocument.Merge(new BsonDocument
                    {
                        { "Filter", updateManyModel.Filter.ToFilterDefinitionBsonDocument() },
                        { "Update", updateManyModel.Update.ToUpdateDefinitionBsonDocument() },
                        { "IsUpsert", updateManyModel.IsUpsert }
                    });
                case DeleteOneModel<TDocument> deleteOneModel:
                    return bsonDocument.Merge(new BsonDocument
                    {
                        { "Filter", deleteOneModel.Filter.ToFilterDefinitionBsonDocument() }
                    });
                case DeleteManyModel<TDocument> deleteManyModel:
                    return bsonDocument.Merge(new BsonDocument
                    {
                        { "Filter", deleteManyModel.Filter.ToFilterDefinitionBsonDocument() }
                    });
                case ReplaceOneModel<TDocument> replaceOneModel:
                    return bsonDocument.Merge(new BsonDocument
                    {
                        { "Filter", replaceOneModel.Filter.ToFilterDefinitionBsonDocument() },
                        { "Replacement", replaceOneModel.Replacement.ToBsonDocument() },
                        { "IsUpsert", replaceOneModel.IsUpsert }
                    });
            }

            throw new Exception($"Unknown write model: {writeModel.GetType().FullName}.");
        }

        [UsedImplicitly]
        public static bool IsTypeOf(this MigrationDocument document, [NotNull] string typeName)
        {
           return document.BsonDocument.IsTypeOf(typeName);
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
                throw new KeyNotFoundException($"BsonDocument does not contain key: _t`{typeName}");
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