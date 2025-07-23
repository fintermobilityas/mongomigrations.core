using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoMigrations.Core.Documents;

namespace MongoMigrations.Core.Extensions;


public static class MigrationExtensions
{

    public static string ToJson<TDocument>([NotNull] this UpdateDefinition<TDocument> updateDefinition)
    {
        if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
        var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        var renderedFilter = updateDefinition.Render(new RenderArgs<TDocument>
        {
            SerializerRegistry = BsonSerializer.SerializerRegistry,
            DocumentSerializer = documentSerializer
        });
        return renderedFilter.ToString();
    }

    public static BsonDocument ToUpdateDefinitionBsonDocument<TDocument>([NotNull] this UpdateDefinition<TDocument> updateDefinition)
    {
        if (updateDefinition == null) throw new ArgumentNullException(nameof(updateDefinition));
        var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        var renderedFilter = updateDefinition.Render(new RenderArgs<TDocument>
        {
            SerializerRegistry = BsonSerializer.SerializerRegistry,
            DocumentSerializer = documentSerializer
        });
        return renderedFilter.AsBsonDocument;
    }

    public static BsonDocument ToFilterDefinitionBsonDocument<TDocument>([NotNull] this FilterDefinition<TDocument> filterDefinition)
    {
        if (filterDefinition == null) throw new ArgumentNullException(nameof(filterDefinition));
        var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        var renderedFilter = filterDefinition.Render(new RenderArgs<TDocument>
        {
            SerializerRegistry = BsonSerializer.SerializerRegistry,
            DocumentSerializer = documentSerializer
        });
        return renderedFilter;
    }

    public static BsonDocument ToWriteModelBsonDocument<TDocument>([NotNull] this WriteModel<TDocument> writeModel)
    {
        if (writeModel == null) throw new ArgumentNullException(nameof(writeModel));

        var bsonDocument = new BsonDocument
        {
            { "ModelType", writeModel.ModelType }
        };

        return writeModel switch
        {
            UpdateOneModel<TDocument> updateOneModel => bsonDocument.Merge(new BsonDocument
            {
                { "Filter", updateOneModel.Filter.ToFilterDefinitionBsonDocument() },
                { "Update", updateOneModel.Update.ToUpdateDefinitionBsonDocument() },
                { "IsUpsert", updateOneModel.IsUpsert }
            }),
            UpdateManyModel<TDocument> updateManyModel => bsonDocument.Merge(new BsonDocument
            {
                { "Filter", updateManyModel.Filter.ToFilterDefinitionBsonDocument() },
                { "Update", updateManyModel.Update.ToUpdateDefinitionBsonDocument() },
                { "IsUpsert", updateManyModel.IsUpsert }
            }),
            DeleteOneModel<TDocument> deleteOneModel => bsonDocument.Merge(new BsonDocument
            {
                { "Filter", deleteOneModel.Filter.ToFilterDefinitionBsonDocument() }
            }),
            DeleteManyModel<TDocument> deleteManyModel => bsonDocument.Merge(new BsonDocument
            {
                { "Filter", deleteManyModel.Filter.ToFilterDefinitionBsonDocument() }
            }),
            ReplaceOneModel<TDocument> replaceOneModel => bsonDocument.Merge(new BsonDocument
            {
                { "Filter", replaceOneModel.Filter.ToFilterDefinitionBsonDocument() },
                { "Replacement", replaceOneModel.Replacement.ToBsonDocument() },
                { "IsUpsert", replaceOneModel.IsUpsert }
            }),
            _ => throw new Exception($"Unknown write model: {writeModel.GetType().FullName}."),
        };
    }


    public static bool IsTypeOf([NotNull] this MigrationDocument document, [NotNull] string typeName)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (typeName == null) throw new ArgumentNullException(nameof(typeName));
        return document.BsonDocument.IsTypeOf(typeName);
    }


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


    public static void DropAllIndexes([NotNull] this IMongoDatabase database, [NotNull] string collectionName)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(collectionName));
        database.GetCollection<BsonDocument>(collectionName).DropAllIndexes();
    }


    public static void DropAllIndexes([NotNull] this IMongoCollection<BsonDocument> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        collection.Indexes.DropAll();
    }


    public static void RenameCollectionAndDropOldOne([NotNull] IMongoDatabase database, [NotNull] string oldCollectionName, [NotNull] string newCollectionName)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (string.IsNullOrWhiteSpace(oldCollectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(oldCollectionName));
        if (string.IsNullOrWhiteSpace(newCollectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(newCollectionName));
        database.RenameCollectionAndDropOldOne(database.GetCollection<BsonDocument>(oldCollectionName), newCollectionName);
    }


    public static void RenameCollectionAndDropOldOne([NotNull] this IMongoDatabase database, [NotNull] IMongoCollection<BsonDocument> collection, [NotNull] string newCollectionName)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (string.IsNullOrWhiteSpace(newCollectionName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(newCollectionName));
        database.RenameCollection(collection.CollectionNamespace.CollectionName, newCollectionName);
        database.DropCollection(collection.CollectionNamespace.CollectionName);
    }

    /// <summary>
    ///     Rename all instances of a name in a bson document to the new name.
    /// </summary>

    public static void ChangeName([NotNull] this BsonDocument bsonDocument, [NotNull] string originalName,
        [NotNull] string newName)
    {
        if (bsonDocument == null) throw new ArgumentNullException(nameof(bsonDocument));
        if (originalName == null) throw new ArgumentNullException(nameof(originalName));
        if (newName == null) throw new ArgumentNullException(nameof(newName));
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
        if (bsonDocument != null && bsonDocument.TryGetValue("_id", out var id))
        {
            return id;
        }
        return BsonNull.Value;
    }
}